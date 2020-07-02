using SerAPI.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using IdentityModel;
using Microsoft.EntityFrameworkCore;
using System;

namespace SerAPI.Utils.CustomFilters
{
    public class CustomAuthorizeAttribute : TypeFilterAttribute
    {
        public CustomAuthorizeAttribute(string claimType, string claimValues, string Roles = "")
            : base(typeof(ClaimRequirementFilter))
        {
            Arguments = new object[] { claimType, claimValues, Roles };
        }
    }

    public class ClaimRequirementFilter : ControllerBase, IAuthorizationFilter
    {
        private string _roles;
        private string _permissions;
        private string _claimType;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private ApplicationDbContext _db;
        private IMemoryCache _cache;

        public ClaimRequirementFilter(string claimType,
            string permissions,
            string roles,
            ApplicationDbContext db,
            IHttpContextAccessor httpContextAccessor,
            IMemoryCache memoryCache)
        {
            _claimType = claimType;
            _permissions = permissions;
            _roles = roles;
            _db = db;
            _httpContextAccessor = httpContextAccessor;
            _cache = memoryCache;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (context.HttpContext.User.Identity.IsAuthenticated)
            {
                List<string> rols = new List<string>(_roles.Split(','));
                rols.Reverse();

                if (!(!string.IsNullOrEmpty(_roles) &&
                    context.HttpContext.User.Claims.Any(x => x.Type == JwtClaimTypes.Role && rols.Any(s => x.Value.Contains(s.Trim())))))
                {
                    List<string> permissions = new List<string>(_permissions.Split(','));

                    bool hasClaim = false;
                    var claims = CacheGetOrCreateClaims();
                    foreach (var claim in claims)
                    {
                        if (permissions.Contains(claim.ClaimValue))
                        {
                            hasClaim = true;
                            break;
                        }
                    }

                    if (!hasClaim)
                    {
                        context.Result = StatusCode(403, new
                        {
                            RequiredPermission = _permissions
                        });
                    }
                }
            }
            else
            {
                context.Result = new UnauthorizedResult();
            }
        }

        public string GetCurrentUser()
        {
            return _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(x =>
                x.Type == JwtClaimTypes.Subject).Value;
        }

        public List<string> GetRolesUser()
        {
            return _httpContextAccessor.HttpContext.User.Claims.Where(x =>
                x.Type == JwtClaimTypes.Role).Select(x => x.Value).ToList();
        }

        public List<IdentityRoleClaim<string>> CacheGetOrCreateClaims()
        {
            string userId = GetCurrentUser();
            var cacheKeySize = string.Format("_{0}_claims", userId);
            var cacheEntry = _cache.GetOrCreate(cacheKeySize, entry =>
            {
                var roleIds = _db.Roles.Where(x => GetRolesUser().Contains(x.Name)).Select(x => x.Id);
                var claims = _db.RoleClaims
                    .Where(a => roleIds.Any(s => a.RoleId.Contains(s)) && a.ClaimType == _claimType)
                    .AsNoTracking()
                    .ToList();
                entry.Size = 1000;
                entry.SlidingExpiration = TimeSpan.FromDays(1);
                return claims;
            });

            return cacheEntry;
        }
    }
}