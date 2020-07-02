using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using SerAPI.Models;
using IdentityServer4.AccessTokenValidation;
using SerAPI.Managers;
using SerAPI.Utils;
using SerAPI.Utils.CustomFilters;
using SerAPI.Models.ViewModels;

namespace SerAPI.Controllers
{
    /// <summary>
    /// This class is used as an api for the roles requests.
    /// </summary>
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = IdentityServerAuthenticationDefaults.AuthenticationScheme)]
    [Route("api/Roles")]
    public class RolesController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRepository<ApplicationRole> _cRepository;

        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="userManager"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="roleManager"></param>
        /// <param name="cRepository"></param>
        public RolesController(
            UserManager<ApplicationUser> userManager,
            IRepository<ApplicationRole> cRepository)
        {
            _userManager = userManager;
            _cRepository = cRepository;
        }

        // GET: api/Roles
        /// <summary>
        /// This method returns all roles
        /// </summary>
        /// <returns>All roles which were found</returns>
        [HttpGet]
        [CustomAuthorize(CustomClaimTypes.Permission, "roles.view", Roles: "Super-User")]
        public async Task<IActionResult> GetRoles(string select)
        {
            return Content(await ((RolesManager)_cRepository).All(select), "application/json", System.Text.Encoding.UTF8);
        }

        // GET: api/Roles/5
        /// <summary>
        /// This method returns a role by id
        /// </summary>
        /// <param name="id">id of role</param>
        /// <returns>Role which were found</returns>
        [HttpGet("{id}")]
        [CustomAuthorize(CustomClaimTypes.Permission, "roles.view", Roles: "Super-User")]
        [ValidateModel]
        public async Task<IActionResult> GetRole(string id)
        {
            var entity = await ((RolesManager)_cRepository).Find(id);
            if (entity == null) return NotFound();
            return Content(entity, "application/json", System.Text.Encoding.UTF8);
        }

        // GET: api/Roles/ByName/Admin
        /// <summary>
        /// This method returns a role by Name
        /// </summary>
        /// <param name="name">Name of role</param>
        /// <returns>Role which were found</returns>
        [HttpGet("[action]/{name}")]
        [CustomAuthorize(CustomClaimTypes.Permission, "roles.view", Roles: "Super-User")]
        [ValidateModel]
        public async Task<IActionResult> ByName(string name)
        {
            var role = await ((RolesManager)_cRepository).Find(x => x.Name == name);
            if (role == null) return NotFound();
            return Ok(role);
        }

        // GET: api/Roles/GetRolesUserByID/5
        /// <summary>
        /// This method returns roles of user
        /// </summary>
        /// <param name="id">id of user</param>
        /// <returns>Roles which were found of user</returns>
        [HttpGet("[action]/{id}")]
        [CustomAuthorize(CustomClaimTypes.Permission, "roles.view", Roles: "Super-User")]
        [ValidateModel]
        public async Task<IActionResult> GetRolesUserByID(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(await _userManager.GetRolesAsync(user));
        }

        // POST: api/Roles
        /// <summary>
        /// This method create a Role obj
        /// </summary>
        /// <param name="model">Obj of role</param>
        /// <returns>Role which was created</returns>
        [HttpPost]
        [CustomAuthorize(CustomClaimTypes.Permission, "roles.add", Roles: "Super-User")]
        [ValidateModel]
        public async Task<IActionResult> PostRole([FromBody] RoleBindingModel model)
        {
            //BadRequest(ModelState);
            var result = await ((RolesManager)_cRepository).Add(model);
            if (result is IdentityResult)
            {
                AddErrors((IdentityResult)result);
                return BadRequest(ModelState);
            }
            else
            {
                var entity = (ApplicationRole)result;
                if (entity == null) return new StatusCodeResult(StatusCodes.Status409Conflict);
                return CreatedAtAction("GetRole", new { id = entity.Id }, entity);
            }
        }

        // PUT: api/Roles/5
        /// <summary>
        /// This method update a Role by id
        /// </summary>
        /// <param name="id">id of Role</param>
        /// <param name="model">Obj</param>
        /// <returns>Role which was updated</returns>
        [HttpPut("{id}")]
        [CustomAuthorize(CustomClaimTypes.Permission, "roles.update", Roles: "Super-User")]
        [ValidateModel]
        public async Task<IActionResult> PutRole(string id, [FromBody] EditRoleBindingModel model)
        {
            if (id != model.id) return BadRequest();
            var result = await ((RolesManager)_cRepository).Update(id, model);
            if (result is IdentityResult)
            {
                AddErrors((IdentityResult)result);
                return BadRequest(ModelState);
            }
            else
            {
                return (result != null ? Ok((ApplicationRole)result) : (IActionResult)NotFound());
            }
        }

        // DELETE: api/Roles/5
        /// <summary>
        /// This method delete a User obj
        /// </summary>
        /// <param name="id">id of User</param>
        /// <returns>User which were deleted</returns>
        [HttpDelete("{id}")]
        [CustomAuthorize(CustomClaimTypes.Permission, "roles.delete", Roles: "Super-User")]
        [ValidateModel]
        public async Task<IActionResult> DeleteRole(string id)
        {
            var result = await ((RolesManager)_cRepository).Remove(id);
            if (result is IdentityResult)
            {
                AddErrors((IdentityResult)result);
                return BadRequest(ModelState);
            }
            if ((ApplicationRole)result == null) return NotFound();
            return Ok((ApplicationRole)result);
        }

        // POST: api/Roles/AddRoleToUser/1
        /// <summary>
        /// This method add roles to user
        /// </summary>
        /// <param name="id">id of User</param>
        /// <param name="model">Model</param>
        /// <returns>User which was added role</returns>
        [HttpPost("[action]/{id}")]
        [Authorize(Roles = "Administrador, Super-User")]
        [ValidateModel]
        public async Task<IActionResult> AddRoleToUser(string id, [FromBody] AddRoleBindingModel model)
        {
            var user = await ((RolesManager)_cRepository).AddRoleUser(id, model);
            return (user != null ? (IActionResult)Ok(user) : NotFound());
        }

        // POST: api/Roles/RemoveRoleToUser/1
        /// <summary>
        /// This method remove roles of a user
        /// </summary>
        /// <param name="id">id of User</param>
        /// <param name="model">Model</param>
        /// <returns>User which was removed the roles</returns>
        [HttpPost("[action]/{id}")]
        [Authorize(Roles = "Administrador, Super-User")]
        [ValidateModel]
        public async Task<IActionResult> RemoveRoleToUser(string id, [FromBody] AddRoleBindingModel model)
        {
            var user = await ((RolesManager)_cRepository).RemoveRoleUser(id, model);
            return (user != null ? (IActionResult)Ok(user) : NotFound());
        }

        #region Helpers
        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("Error", error.Description);
            }
        }

        private void AddError(string error)
        {
            ModelState.AddModelError("Error", error);
        }
        #endregion
    }
}