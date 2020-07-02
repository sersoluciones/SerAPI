using SerAPI.Data;
using SerAPI.Models;
using IdentityModel;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SerAPI
{
    /// <summary>
    /// IProfileService to integrate with ASP.NET Identity.
    /// </summary>
    /// <seealso cref="IdentityServer4.Services.IProfileService" />
    public class ProfileService : IProfileService
    {
        /// <summary>
        /// The claims factory.
        /// </summary>
        protected readonly IUserClaimsPrincipalFactory<ApplicationUser> _claimsFactory;

        /// <summary>
        /// The logger
        /// </summary>
        protected readonly ILogger<ProfileService> _logger;

        /// <summary>
        /// The user manager.
        /// </summary>
        protected readonly UserManager<ApplicationUser> _userManager;

        private readonly ApplicationDbContext _db;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileService"/> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="claimsFactory">The claims factory.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="db">db context</param>
        public ProfileService(UserManager<ApplicationUser> userManager,
            IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
            ILogger<ProfileService> logger,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _claimsFactory = claimsFactory;
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// This method is called whenever claims about the user are requested (e.g. during token creation or via the userinfo endpoint)
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public virtual async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var sub = context.Subject?.GetSubjectId();
            if (sub == null) throw new Exception("No sub claim present");

            var user = await _db.Users
                .SingleOrDefaultAsync(x => x.Id == sub);
            if (user == null)
            {
                _logger?.LogWarning("No user found matching subject id: {0}", sub);
            }
            else
            {
                var principal = await _claimsFactory.CreateAsync(user);
                if (principal == null) throw new Exception("ClaimsFactory failed to create a principal");

                //var claimsUser = await GetClaimsFromUser(user, context);
                //principal.Claims.ToList().AddRange(claimsUser);
                //if (context.RequestedClaimTypes.Count() > 0)
                //    context.AddRequestedClaims(principal.Claims);
                //else
                context.IssuedClaims.AddRange(await GetClaimsFromUser(user, context));
            }
        }

        /// <summary>
        /// This method gets called whenever identity server needs to determine if the user is valid or active (e.g. if the user's account has been deactivated since they logged in).
        /// (e.g. during token issuance or validation).
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public virtual async Task IsActiveAsync(IsActiveContext context)
        {
            var sub = context.Subject?.GetSubjectId();
            if (sub == null) throw new Exception("No subject id claim present");

            var user = await _userManager.FindByIdAsync(sub);
            if (user == null)
            {
                _logger?.LogWarning("No user found matching subject id: {0}", sub);
            }

            context.IsActive = user != null;
        }

        private async Task<List<Claim>> GetClaimsFromUser(ApplicationUser user, ProfileDataRequestContext context)
        {
            var User = context.Subject;
            var claims = new List<Claim>
            {
                new Claim(JwtClaimTypes.Subject, user.Id),
                new Claim(JwtClaimTypes.PreferredUserName, user.UserName),

            };

            var scope = context.RequestedResources.Resources.IdentityResources;
            if (scope.Any(x => x.Name == JwtClaimTypes.Profile))
            {
                claims.AddRange(new[]
                {
                    new Claim(JwtClaimTypes.Name, (string.IsNullOrEmpty(user.Name) ? "" : $"{user.Name}") + (string.IsNullOrEmpty(user.LastName) ? "" : $" {user.LastName}")),
                    new Claim(JwtClaimTypes.GivenName, user.Name ?? ""),
                    new Claim(JwtClaimTypes.FamilyName, user.LastName ?? ""),
                    new Claim("dark_mode", user.DarkMode.ToString())
                });
            }

            if (scope.Any(x => x.Name == JwtClaimTypes.Email) && _userManager.SupportsUserEmail)
            {
                claims.AddRange(new[]
                {
                    new Claim(JwtClaimTypes.Email, user.Email),
                    new Claim(JwtClaimTypes.EmailVerified, user.EmailConfirmed? "true" : "false", ClaimValueTypes.Boolean)
                });
            }

            if (scope.Any(x => x.Name == JwtClaimTypes.PhoneNumber) &&
                _userManager.SupportsUserPhoneNumber && !string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                claims.AddRange(new[]
                {
                new Claim(JwtClaimTypes.PhoneNumber, user.PhoneNumber),
                new Claim(JwtClaimTypes.PhoneNumberVerified, user.PhoneNumberConfirmed? "true" : "false", ClaimValueTypes.Boolean)
            });
            }

            if (_userManager.SupportsUserClaim)
            {
                claims.AddRange(await _userManager.GetClaimsAsync(user));
            }

            if (scope.Any(x => x.Name == "roles") && _userManager.SupportsUserRole)
            {
                var roles = await _userManager.GetRolesAsync(user);
                claims.AddRange(roles.Select(role => new Claim(JwtClaimTypes.Role, role)));
            }

            return claims;
        }
    }
}