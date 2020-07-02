using SerAPI.Models;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using SerAPI.Utils;
using SerAPI.Utils.CustomFilters;

namespace SerAPI.Controllers
{
    /// <summary>
    /// This class is used as an api for the Market requests.
    /// </summary>
    [Produces("application/json")]
    [Route("api/Permissions")]
    [Authorize(Roles = Constantes.SuperUser, AuthenticationSchemes = IdentityServerAuthenticationDefaults.AuthenticationScheme)]
    public class PermissionsController : Controller
    {
        private readonly IRepository<Permission> _cRepository;

        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="cRepository"></param>
        public PermissionsController(IRepository<Permission> cRepository)
        {
            _cRepository = cRepository;
        }

        // GET: api/Permissions
        /// <summary>
        /// This method returns all Permissions
        /// </summary>
        /// <returns>All Permissions which were found</returns>
        [HttpGet]
        public async Task<IActionResult> GetPermission()
        {
            return Ok(await _cRepository.GetAll());
        }

        // GET: api/Permissions/5
        /// <summary>
        /// This method returns a Permission by id
        /// </summary>
        /// <param name="id">id of Permissions</param>
        /// <returns>Permission which was found</returns>
        [HttpGet("{id}")]
        [ValidateModel]
        public async Task<IActionResult> GetPermission([FromRoute] int id)
        {
            var entity = await _cRepository.Find(x => x.id == id);
            if (entity == null) return NotFound();
            return Ok(entity);
        }

        // POST: api/Permissions
        /// <summary>
        /// This method create a Permission obj
        /// </summary>
        /// <param name="entity">Obj</param>
        /// <returns>Permission which was created</returns>
        [HttpPost]
        [ValidateModel]
        [ValidatePermissionExists]
        public async Task<IActionResult> PostPermission([FromBody] Permission entity)
        {
            entity = await _cRepository.Add(entity);
            if (entity == null) return new StatusCodeResult(StatusCodes.Status409Conflict);
            return CreatedAtAction("GetPermission", new { entity.id }, entity);
        }

        // PUT: api/Permissions/5
        /// <summary>
        /// This method update a Permission by id
        /// </summary>
        /// <param name="id">id of Permission</param>
        /// <param name="entity">Obj</param>
        /// <returns>Permission which was updated</returns>
        [HttpPut("{id}")]
        [ValidateModel]
        [ValidatePermissionExists]
        public async Task<IActionResult> PutPermission([FromRoute] int id,
            [FromBody] PermissionBinding model)
        {
            Permission entity = _cRepository.GetQueryable().SingleOrDefault(x => x.id == id);
            entity.name = model.name;
            await _cRepository.Update(entity, x => x.id == id);
            return (entity != null ? (IActionResult)Ok(entity) : NotFound());
        }        

        // DELETE: api/Permissions/5
        /// <summary>
        /// This method delete a Permission obj
        /// </summary>
        /// <param name="id">id of Permission</param>
        /// <returns>Permission which was deleted</returns>
        [HttpDelete("{id}")]
        [ValidateModel]
        public async Task<IActionResult> DeletePermission([FromRoute] int id)
        {
            var entity = await _cRepository.Remove(x => x.id == id);
            if (entity == null) return NotFound();
            return Ok(entity);
        }
    }
}