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
    [Route("api/Car")]
    [Authorize(Roles = Constantes.SuperUser, AuthenticationSchemes = IdentityServerAuthenticationDefaults.AuthenticationScheme)]
    public class CarController : Controller
    {
        private readonly IRepository<Car> _cRepository;

        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="cRepository"></param>
        public CarController(IRepository<Car> cRepository)
        {
            _cRepository = cRepository;
        }

        // GET: api/Car
        /// <summary>
        /// This method returns all Car
        /// </summary>
        /// <returns>All Car which were found</returns>
        [HttpGet]
        [CustomAuthorize(CustomClaimTypes.Permission, "cars.view", Roles: Constantes.SuperUser)]
        public async Task<IActionResult> GetCars()
        {
            return Ok(await _cRepository.GetAll());
        }

        // GET: api/Car/5
        /// <summary>
        /// This method returns a Car by id
        /// </summary>
        /// <param name="id">id of Car</param>
        /// <returns>Car which was found</returns>
        [HttpGet("{id}")]
        [CustomAuthorize(CustomClaimTypes.Permission, "cars.view", Roles: Constantes.SuperUser)]
        [ValidateModel]
        public async Task<IActionResult> GetCar([FromRoute] int id)
        {
            var entity = await _cRepository.Find(x => x.id == id);
            if (entity == null) return NotFound();
            return Ok(entity);
        }

        // POST: api/Car
        /// <summary>
        /// This method create a Car obj
        /// </summary>
        /// <param name="entity">Obj</param>
        /// <returns>Car which was created</returns>
        [HttpPost]
        [CustomAuthorize(CustomClaimTypes.Permission, "cars.add", Roles: Constantes.SuperUser)]
        [ValidateModel]
        public async Task<IActionResult> PostCar([FromBody] Car entity)
        {
            entity = await _cRepository.Add(entity);
            if (entity == null) return new StatusCodeResult(StatusCodes.Status409Conflict);
            return CreatedAtAction("GetCar", new { entity.id }, entity);
        }

        // PUT: api/Car/5
        /// <summary>
        /// This method update a Car by id
        /// </summary>
        /// <param name="id">id of Car</param>
        /// <param name="model">Obj</param>
        /// <returns>Car which was updated</returns>
        [HttpPut("{id}")]
        [CustomAuthorize(CustomClaimTypes.Permission, "cars.update", Roles: Constantes.SuperUser)]
        [ValidateModel]
        public async Task<IActionResult> PutCar([FromRoute] int id, [FromBody] Car model)
        {
            var entity = await _cRepository.SelfUpdate(x => x.id == id, model);
            return (entity != null ? (IActionResult)Ok(entity) : NotFound());
        }

        // DELETE: api/Car/5
        /// <summary>
        /// This method delete a Car obj
        /// </summary>
        /// <param name="id">id of Car</param>
        /// <returns>Car which was deleted</returns>
        [HttpDelete("{id}")]
        [CustomAuthorize(CustomClaimTypes.Permission, "cars.delete", Roles: Constantes.SuperUser)]
        [ValidateModel]
        public async Task<IActionResult> DeleteCar([FromRoute] int id)
        {
            var entity = await _cRepository.Remove(x => x.id == id);
            if (entity == null) return NotFound();
            return Ok(entity);
        }
    }
}