using IdentityModel;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SerAPI.Data;
using SerAPI.Managers;
using SerAPI.Models;
using SerAPI.Models.ViewModels;
using SerAPI.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SerAPI.Controllers
{
    [Route("api")]
    [Authorize(AuthenticationSchemes = IdentityServerAuthenticationDefaults.AuthenticationScheme)]
    public class UserinfoController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;
        private readonly AuditManager _cRepositoryLog;

        public UserinfoController(UserManager<ApplicationUser> userManager,
            AuditManager cRepositoryLog,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _db = context;
            _cRepositoryLog = cRepositoryLog;
        }

        [HttpGet("user/claims")]
        public IActionResult Get()
        {
            return new JsonResult(from c in User.Claims select new { c.Type, c.Value });
        }

        //
        // GET: /api/userinfo
        [Authorize]
        [HttpGet("userinfo"), Produces("application/json")]
        public async Task<IActionResult> Userinfo()
        {

            ApplicationUser user = await _db.Users
                .Include(x => x.Attachment)
                .SingleOrDefaultAsync(x => x.Id == User.FindFirst(JwtClaimTypes.Subject).Value);

            if (user == null)
            {
                return BadRequest(new
                {
                    Error = "InvalidGrant",
                    ErrorDescription = "The user profile is no longer available."
                });
            }

            var data = new UserInfoModel();

            // Note: the "sub" claim is a mandatory claim and must be included in the JSON response.
            data.sub = User.FindFirst(JwtClaimTypes.Subject).Value;
            data.name = user.Name;
            data.last_name = user.LastName;
            data.photo = user.Attachment?.key;
            data.dark_mode = user.DarkMode;
            data.username = user.UserName;
            data.is_super_user = _cRepositoryLog.GetRolesUser().Any(x => x == Constantes.SuperUser);

            // Uncomment for multi-tenant
            //if (!_cRepositoryLog.GetRolesUser().Any(x => x == Constantes.SuperUser))
            //{
            //    claims["customer_id"] = ((AuditManager)_cRepositoryLog).GetCustomerId();
            //}
            //else
            //{
            //    claims["customer_id"] = ((AuditManager)_cRepositoryLog).GetCustomerId();
            //}

            // Uncomment for multi-tenant
            //claims["customer_logo"] = _db.Customers.SingleOrDefault(x => x.id == int.Parse(
            //    ((AuditManager)_cRepositoryLog).GetCustomerId())).logo;

            if (User.HasClaim(JwtClaimTypes.Scope, JwtClaimTypes.Email))
            {
                data.email = await _userManager.GetEmailAsync(user);
                data.email_verified = await _userManager.IsEmailConfirmedAsync(user);
            }

            if (User.HasClaim(JwtClaimTypes.Scope, JwtClaimTypes.PhoneNumber))
            {
                data.phone_number = await _userManager.GetPhoneNumberAsync(user);
                data.phone_number_verified = await _userManager.IsPhoneNumberConfirmedAsync(user);
            }

            if (User.HasClaim(JwtClaimTypes.Scope, JwtClaimTypes.Profile))
            {
                //var rol = await _userManager.GetRolesAsync(user);
                var roles = User.Claims.Where(x => x.Type == JwtClaimTypes.Role).Select(x => x.Value).ToList();
                data.role = roles.FirstOrDefault();
                if (roles.Any(x => x == Constantes.SuperUser))
                {
                    data.claims = _db.permissions.AsNoTracking().Select(x => x.name).OrderBy(x => x).ToArray();
                }
                else
                {
                    var roleIds = (await _db.Users
                        .Include(x => x.UserRoles)
                        .FirstOrDefaultAsync(x => x.Id == user.Id))
                        .UserRoles
                        .Select(x => x.RoleId);

                    data.claims = _db.RoleClaims.AsEnumerable().Where(a => roleIds.Any(s => a.RoleId.Contains(s)))
                                    .Select(x => x.ClaimValue)
                                    .Distinct()
                                    .OrderBy(x => x)
                                    .ToArray();
                }
            }

            return Json(data);
        }
    }
}