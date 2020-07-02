using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using SerAPI.Data;
using SerAPI.Models;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Caching.Memory;
using SerAPI.Managers;
using SerAPI.Services;
using SerAPI.Utils;
using SerAPI.Models.ViewModels;

namespace SerAPI.Managers
{
    public class ClaimsManager : GenericModelFactory<IdentityRoleClaim<string>>
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly AuditManager _cRepositoryLog;
        private IMemoryCache _cache;
        private new readonly string model = "Claim";

        public ClaimsManager(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            ApplicationDbContext db,
            ILogger<ClaimsManager> logger,
            IHttpContextAccessor httpContextAccessor,
            AuditManager cRepositoryLog,
            IMemoryCache cache,
            IConfiguration config)
            : base(db, logger, httpContextAccessor, cRepositoryLog, config)
        {
            _context = db;
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
            _cRepositoryLog = cRepositoryLog;
            _httpContextAccessor = httpContextAccessor;
            _cache = cache;
        }

        public async Task<dynamic> All()
        {
            return await GetQueryable()
                .OrderBy(x => x.ClaimValue)
                .SortFilterAsync(_httpContextAccessor, true);
        }

        public async Task<object> AddClaimToRole(string RoleId, string PermissionName)
        {
            var role = await _roleManager.FindByIdAsync(RoleId);
            if (role != null)
            {
                var userIds = _context.UserRoles.Where(x => x.RoleId == RoleId).Select(x => x.UserId).ToList();
                // Remueve cache de permisos por rol
                foreach (var userId in userIds)
                {
                    var cacheKeySize = string.Format("_{0}_claims", userId);
                    _cache.Remove(cacheKeySize);
                }

                var result = await _roleManager.AddClaimAsync(role,
                        new Claim(CustomClaimTypes.Permission, PermissionName));

                if (!result.Succeeded)
                {
                    return result;
                }

                var jObjObser = new JObject();
                jObjObser.Add("Description", $"Add permission or claim {PermissionName} in the Role {RoleId}, name {role.Name}");
                await ((AuditManager)_cRepositoryLog).AddLog(new AuditBinding()
                {
                    action = AuditManager.CREATE,
                    objeto = this.model,
                    json_observations = jObjObser
                }, id: RoleId, commit: true);
            }
            return role;
        }


        public async Task<ApplicationRole> AddClaimsToRole(ClaimsRoleBingModel model)
        {
            var roleEntity = await _roleManager.FindByIdAsync(model.role_id);
            if (roleEntity != null)
            {
                var userIds = _context.UserRoles.Where(x => x.RoleId == roleEntity.Id).Select(x => x.UserId).ToList();
                // Remueve cache de permisos por rol
                foreach (var userId in userIds)
                {
                    var cacheKeySize = string.Format("_{0}_claims", userId);
                    _cache.Remove(cacheKeySize);
                }

                foreach (var permission in model.permission_names)
                {
                    if (await _context.RoleClaims
                        .AnyAsync(x => x.ClaimValue == permission && x.RoleId == model.role_id))
                    {
                        continue;
                    }
                    _logger.LogWarning(0, $"permission to add : {permission}");
                    await AddClaimToRole(roleEntity, permission);
                }

                foreach (var claim in await _roleManager.GetClaimsAsync(roleEntity))
                {
                    if (model.permission_names.Any(permission => permission == claim.Value))
                    {
                        continue;
                    }
                    _logger.LogInformation(0, $"Claim to delete: {claim}");
                    await RemoveClaimToRole(roleEntity, claim);
                }

                _context.Entry(roleEntity).Collection(x => x.Claims).Load();

                var jObjObser = new JObject();
                jObjObser.Add("Description", $"Update Claims or Permission to the Role {roleEntity.Name}");
                jObjObser.Add("Values", $"{JArray.FromObject(model.permission_names)}");
                await ((AuditManager)_cRepositoryLog).AddLog(new AuditBinding()
                {
                    action = AuditManager.UPDATE,
                    objeto = this.model,
                    json_observations = jObjObser
                }, commit: true, id: model.role_id);

                return roleEntity;
            }
            return null;
        }

        #region Helpers            
        private async Task AddClaimToRole(ApplicationRole role, string PermissionName)
        {
            var result = await _roleManager.AddClaimAsync(role,
                    new Claim(CustomClaimTypes.Permission, PermissionName));

            if (!result.Succeeded)
            {
                _logger.LogError(0, $"Claim {PermissionName} not added to Role {role.Name}");
            }
        }

        private async Task RemoveClaimToRole(ApplicationRole role, Claim claim)
        {
            var result = await _roleManager.RemoveClaimAsync(role, claim);
            if (!result.Succeeded)
            {
                _logger.LogError(0, $"Claim {claim.Value} not deleted to Role {role.Name}");
            }
        }
        #endregion
    }
}