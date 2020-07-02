using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using SerAPI.Data;
using SerAPI.Models;
using SerAPI.Models.ViewModels;

namespace SerAPI.Utils.CustomFilters
{
    public class ValidateClaimExistAttribute : TypeFilterAttribute
    {
        /// <summary>
        /// Constructor 
        /// </summary>
        public ValidateClaimExistAttribute() : base(typeof
              (ValidateClaimExistFilterImpl))
        {
        }
        private class ValidateClaimExistFilterImpl : IAsyncActionFilter
        {
            private readonly ILogger _logger;
            private ApplicationDbContext _db;

            public ValidateClaimExistFilterImpl(
                ILogger<ValidateClaimExistFilterImpl> logger,
                ApplicationDbContext db)
            {
                _logger = logger;
                _db = db;
                //_db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            }
            public async Task OnActionExecutionAsync(ActionExecutingContext context,
                ActionExecutionDelegate next)
            {
                if (!context.ModelState.IsValid)
                {
                    context.Result = new BadRequestObjectResult(context.ModelState);
                }
                var RequestMethod = context.HttpContext.Request.Method;
                ClaimRoleBingModel entity = null;
                int claimId = 0;
                foreach (var elem in context.ActionArguments)
                {
                    if (elem.Value is ClaimRoleBingModel)
                    {
                        entity = (ClaimRoleBingModel)elem.Value;
                    }
                    else if (elem.Value is int)
                    {
                        claimId = (int)elem.Value;
                    }
                }

                var roleEntity = _db.Roles.FirstOrDefault(x => x.Id == entity.role_id);
                if (roleEntity == null)
                {
                    context.Result = new NotFoundObjectResult(new { Error = $"Role {entity.role_id} not found" });
                    return;
                }

                if (RequestMethod.Equals("POST"))
                {
                    if (_db.RoleClaims.AsNoTracking().Any(x => x.ClaimValue == entity.permission_name && x.RoleId == entity.role_id))
                    {
                        context.ModelState.AddModelError("Error", GetErrorMessage(roleEntity.Name, entity.permission_name));
                        context.Result = new BadRequestObjectResult(context.ModelState);
                        //context.Result = new NotFoundObjectResult(entity.Code);
                        return;
                    }
                }
                else
                {
                    var entitySave = await _db.RoleClaims.FirstOrDefaultAsync(x => x.Id == claimId);
                    if (entitySave == null)
                    {
                        context.Result = new NotFoundObjectResult(new { Error = $"Claim {claimId} not found" });
                        return;
                    }
                    var list = _db.RoleClaims.Except(_db.RoleClaims.Where(x => x.Id == entitySave.Id))
                        .ToList();
                    if (list.Any(x => x.ClaimValue == entity.permission_name && x.RoleId == entity.role_id))
                    {
                        context.ModelState.AddModelError("Code", GetErrorMessage(roleEntity.Name, entity.permission_name));
                        context.Result = new BadRequestObjectResult(context.ModelState);
                        //context.Result = new NotFoundObjectResult(entity.Code);
                        return;
                    }
                }

                await next();
            }

            private string GetErrorMessage(string roleName, string permission)
            {
                return $"Permission {permission} is already in use in this role {roleName}";
            }
        }
    }

    /// <summary>
    /// This class is used to validate request from controllers
    /// </summary>
    public class ValidateClaimsExistAttribute : TypeFilterAttribute
    {
        /// <summary>
        /// Constructor 
        /// </summary>
        public ValidateClaimsExistAttribute() : base(typeof
              (ValidateClaimsExistFilterImpl))
        {
        }
        private class ValidateClaimsExistFilterImpl : IAsyncActionFilter
        {
            private readonly ILogger _logger;
            private IConfigurationRoot _config;
            private ApplicationDbContext _db;
            private readonly RoleManager<ApplicationRole> _roleManager;

            public ValidateClaimsExistFilterImpl(
                ILogger<ValidateClaimsExistFilterImpl> logger,
                IConfigurationRoot Config,
                ApplicationDbContext db,
                RoleManager<ApplicationRole> roleManager)
            {
                _logger = logger;
                _config = Config;
                _db = db;
                //_db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                _roleManager = roleManager;
            }
            public async Task OnActionExecutionAsync(ActionExecutingContext context,
                ActionExecutionDelegate next)
            {
                if (!context.ModelState.IsValid)
                {
                    context.Result = new BadRequestObjectResult(context.ModelState);
                }
                var RequestMethod = context.HttpContext.Request.Method;
                ClaimsRoleBingModel entity = null;
                int claimId = 0;
                foreach (var elem in context.ActionArguments)
                {
                    if (elem.Value is ClaimsRoleBingModel)
                    {
                        entity = (ClaimsRoleBingModel)elem.Value;
                    }
                    else if (elem.Value is int)
                    {
                        claimId = (int)elem.Value;
                    }
                }

                var roleEntity = _roleManager.Roles.Include(x => x.Claims)
                    .FirstOrDefault(x => x.Id == entity.role_id);

                if (roleEntity == null)
                {
                    context.Result = new NotFoundObjectResult(new { Error = $"Role {entity.role_id} not found" });
                    return;
                }

                if (RequestMethod.Equals("POST"))
                {
                    foreach (var permission in entity.permission_names)
                    {
                        if (_db.RoleClaims.Any(x => x.ClaimValue == permission && x.RoleId == entity.role_id))
                        {
                            continue;
                        }
                        await AddClaimToRole(roleEntity, permission);
                    }

                    foreach (var claim in roleEntity.Claims)
                    {
                        if (entity.permission_names.Any(permission => permission == claim.ClaimValue))
                        {
                            continue;
                        }
                        _logger.LogInformation(0, $"Claim value: {claim.ClaimValue}");
                        await RemoveClaimToRole(roleEntity, claim);
                    }
                }
                await next();
            }

            private string GetErrorMessage(string roleName, string permission)
            {
                return $"Permission {permission} is already in use in this role {roleName}";
            }

            #region Helpers            
            public async Task AddClaimToRole(ApplicationRole role, string PermissionName)
            {
                var result = await _roleManager.AddClaimAsync(role,
                        new Claim(CustomClaimTypes.Permission, PermissionName));

                if (!result.Succeeded)
                {
                    _logger.LogError(0, $"Claim {PermissionName} not added to Role {role.Name}");
                }
            }

            public async Task RemoveClaimToRole(ApplicationRole role, IdentityRoleClaim<string> claim)
            {
                _db.RoleClaims.Remove(claim);
                await _db.SaveChangesAsync();
            }
            #endregion

        }
    }
}
