using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SerAPI.Models;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.AccessTokenValidation;
using System.Text.Json;
using SerAPI.Utils;
using SerAPI.Managers;
using SerAPI.Models.ViewModels;
using SerAPI.Utils.CustomFilters;

namespace SerAPI.Controllers
{
    /// <summary>
    /// This class is used as an api for the IdentityRoleClaim requests.
    /// </summary>
    //[Authorize(Roles = "Administrador, Super-User")]
    [Produces("application/json")]
    [Route("api/Claims")]
    [Authorize(AuthenticationSchemes = IdentityServerAuthenticationDefaults.AuthenticationScheme)]
    public class ClaimsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger _logger;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IRepository<IdentityRoleClaim<string>> _cRepository;

        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="userManager"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="roleManager"></param>
        /// <param name="cRepository"></param>
        public ClaimsController(
            UserManager<ApplicationUser> userManager,
            ILoggerFactory loggerFactory,
            RoleManager<ApplicationRole> roleManager,
            IRepository<IdentityRoleClaim<string>> cRepository)
        {
            _userManager = userManager;
            _logger = loggerFactory.CreateLogger<ClaimsController>();
            _roleManager = roleManager;
            _cRepository = cRepository;
        }

        // GET: api/Claims
        /// <summary>
        /// This method returns all claims
        /// </summary>
        /// <returns>All claims which were found</returns>
        [HttpGet]
        [CustomAuthorize(CustomClaimTypes.Permission, "claims.view", Roles: "Super-User")]
        public async Task<IActionResult> GetClaims()
        {
            return Json((await ((ClaimsManager)_cRepository).All()), new JsonSerializerOptions
            {
                PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance
            });

        }

        // GET: api/Claims/5
        /// <summary>
        /// This method returns a claim by id
        /// </summary>
        /// <param name="id">id of claim</param>
        /// <returns>Claim which was found</returns>
        [HttpGet("{id}")]
        [CustomAuthorize(CustomClaimTypes.Permission, "claims.view", Roles: "Super-User")]
        [ValidateModel]
        public async Task<IActionResult> GetClaim(int id)
        {
            var entity = await _cRepository.Find(x => x.Id == id);
            if (entity == null) return NotFound();
            return Json(entity, new JsonSerializerOptions
            {
                PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance
            });
        }

        // GET: api/Claims/GetClaimsUser
        /// <summary>
        /// This method returns all permission of user
        /// </summary>
        /// <returns>Permission which were found of this user</returns>
        [HttpGet("[action]")]
        [CustomAuthorize(CustomClaimTypes.Permission, "claims.view", Roles: "Super-User")]
        public IActionResult GetClaimsUser()
        {
            var claims = User.Claims.Select(claim => new { claim.Type, claim.Value }).ToArray();
            return Json(claims, new JsonSerializerOptions
            {
                PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance
            });
        }

        // GET: api/Claims/GetClaimByRole/Administrador
        /// <summary>
        /// This method returns all claims by role
        /// </summary>
        /// <param name="name">Name of role</param>
        /// <returns>Claims which were found</returns>
        [HttpGet("[action]/{name}")]
        [CustomAuthorize(CustomClaimTypes.Permission, "claims.view", Roles: "Super-User")]
        [ValidateModel]
        public async Task<IActionResult> GetClaimByRole(string name)
        {
            var role = await _roleManager.FindByNameAsync(name);
            if (role == null) { return NotFound(); }
            var claims = await _roleManager.GetClaimsAsync(role);
            return Json(claims, new JsonSerializerOptions
            {
                PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance
            });
        }

        // POST: api/Claims/
        /// <summary>
        /// This method add claims to role
        /// </summary>
        /// <param name="model">Obj of ClaimRoleBingModel</param>
        /// <returns>ApplicationRole which was updated with claims</returns>
        [HttpPost]
        [CustomAuthorize(CustomClaimTypes.Permission, "claims.add", Roles: "Super-User")]
        [ValidateModel]
        [ValidateClaimExist]
        public async Task<IActionResult> PostClaim([FromBody] ClaimRoleBingModel model)
        {
            var result = await ((ClaimsManager)_cRepository).AddClaimToRole(model.role_id, model.permission_name);
            if (result is IdentityResult)
            {
                AddErrors((IdentityResult)result);
                return BadRequest(ModelState);
            }
            else
            {
                var role = (ApplicationRole)result;
                if (role == null) return NotFound();
                return CreatedAtAction("GetRole", "Roles", new { id = role.Id }, role);
                //return CreatedAtAction("GetClaimByRole", new { name = role.Name }, role);
            }
        }

        // POST: api/Claims/AddToRole/
        /// <summary>
        /// This method add mmultiples claims to role
        /// </summary>
        /// <param name="model">Obj of ClaimsRoleBingModel</param>
        /// <returns>ApplicationRole which was updated with claims</returns>
        [HttpPost("[action]")]
        [CustomAuthorize(CustomClaimTypes.Permission, "claims.add", Roles: "Super-User")]
        [ValidateModel]
        public async Task<IActionResult> AddToRole([FromBody] ClaimsRoleBingModel model)
        {
            var role = await ((ClaimsManager)_cRepository).AddClaimsToRole(model);
            if (role == null) return NotFound();
            return Ok(role);
        }

        // PUT: api/Claims/5
        /// <summary>
        /// This method update a Role by id
        /// </summary>
        /// <param name="id">id of Role</param>
        /// <param name="model">Obj</param>
        /// <returns>Role which was updated</returns>
        [HttpPut("{id}")]
        [CustomAuthorize(CustomClaimTypes.Permission, "claims.update", Roles: "Super-User")]
        [ValidateModel]
        [ValidateClaimExist]
        public async Task<IActionResult> PutClaim(int id, [FromBody] ClaimRoleBingModel model)
        {
            IdentityRoleClaim<string> entity = await _cRepository.Find(x => x.Id == id);
            entity.ClaimValue = model.permission_name;
            entity.RoleId = model.role_id;
            entity = await _cRepository.Update(entity, x => x.Id == id);
            return (entity != null ? (IActionResult)Ok(entity) : NotFound());
        }

        // DELETE: api/Claims/5
        /// <summary>
        /// This method delete a User obj
        /// </summary>
        /// <param name="id">id of User</param>
        /// <returns>User which were deleted</returns>        
        [HttpDelete("{id}")]
        [CustomAuthorize(CustomClaimTypes.Permission, "claims.delete", Roles: "Super-User")]
        [ValidateModel]
        public async Task<IActionResult> DeleteClaim(int id)
        {
            var entity = await _cRepository.Remove(x => x.Id == id);
            if (entity == null) return NotFound();
            return Ok(entity);
        }

        #region Helpers
        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("Error", error.Description);
            }
        }
        #endregion
    }
}
