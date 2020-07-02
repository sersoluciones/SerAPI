using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SerAPI.Models;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.AccessTokenValidation;
using SerAPI.Managers;
using SerAPI.Utils;
using SerAPI.Utils.CustomFilters;
using SerAPI.Utilities;

namespace SerAPI.Controllers
{
    /// <summary>
    /// This class is used as an api for the Market requests.
    /// </summary>=
    [Produces("application/json")]
    [Route("api/CommonOptions")]
    [Authorize(AuthenticationSchemes = IdentityServerAuthenticationDefaults.AuthenticationScheme)]
    public class CommonOptionsController : Controller
    {
        private readonly IRepository<CommonOption> _cRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IRepository<Attachment> _cRepositoryAttachment;

        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="cRepository"></param>
        public CommonOptionsController(IRepository<CommonOption> cRepository,
            IHttpContextAccessor httpContextAccessor, IRepository<Attachment> cRepositoryAttachment)
        {
            _httpContextAccessor = httpContextAccessor;
            _cRepository = cRepository;
            _cRepositoryAttachment = cRepositoryAttachment;
        }

        // GET: api/CommonOptions
        /// <summary>
        /// This method returns all CommonOptions
        /// </summary>
        /// <returns>All CommonOptions which were found</returns>
        [HttpGet]
        public async Task<IActionResult> GetCommonOption(string type)
        {
            var entities = _cRepository.GetQueryable();
            if (!string.IsNullOrEmpty(type))
            {
                entities = entities.Where(x => x.type == type);
            }
            return Ok(await entities.SortFilterAsync(_httpContextAccessor, true));
        }

        // GET: api/CommonOptions/5
        /// <summary>
        /// This method returns a CommonOption by id
        /// </summary>
        /// <param name="id">id of CommonOptions</param>
        /// <returns>CommonOption which was found</returns>
        [HttpGet("{id}")]
        [ValidateModel]
        public async Task<IActionResult> GetCommonOption([FromRoute] int id)
        {
            var entity = await _cRepository.Find(x => x.id == id);
            if (entity == null) return NotFound();
            return Ok(entity);
        }

        // GET: api/CommonOptions/ByType/xxx
        /// <summary>
        /// This method returns a CommonOption by Type
        /// </summary>
        /// <param name="type">type of CommonOptions</param>
        /// <returns>CommonOption which was found</returns>
        [HttpGet("[action]/{type}")]
        [ValidateModel]
        public async Task<IActionResult> ByType([FromRoute] string type)
        {
            var entity = await _cRepository.GetQueryable().AsNoTracking()
                .FirstOrDefaultAsync(x => x.type == type);
            if (entity == null) return NotFound();
            return Ok(entity);
        }

        // POST: api/CommonOptions
        /// <summary>
        /// This method create a CommonOption obj
        /// </summary>
        /// <param name="entity">Obj</param>
        /// <returns>CommonOption which was created</returns>
        [HttpPost]
        [ValidateModel]
        public async Task<IActionResult> PostCommonOption([FromBody] CommonOption entity)
        {
            entity = await _cRepository.Add(entity);
            if (entity == null) return new StatusCodeResult(StatusCodes.Status409Conflict);
            return CreatedAtAction("GetCommonOption", new { id = entity.id }, entity);
        }

        // PUT: api/CommonOptions/5
        /// <summary>
        /// This method update a CommonOption by id
        /// </summary>
        /// <param name="id">id of CommonOption</param>
        /// <param name="entity">Obj</param>
        /// <returns>CommonOption which was updated</returns>
        [HttpPut("{id}")]
        [ValidateModel]
        public async Task<IActionResult> PutCommonOption([FromRoute] int id,
            [FromBody] CommonOption entity)
        {
            if (id != entity.id) return BadRequest();
            entity = await _cRepository.Update(entity, x => x.id == id);
            return (entity != null ? (IActionResult)Ok(entity) : NotFound());
        }

        // DELETE: api/CommonOptions/5
        /// <summary>
        /// This method delete a CommonOption obj
        /// </summary>
        /// <param name="id">id of CommonOption</param>
        /// <returns>CommonOption which was deleted</returns>
        [HttpDelete("{id}")]
        [ValidateModel]
        public async Task<IActionResult> DeleteCommonOption([FromRoute] int id)
        {
            var entity = await _cRepository.Remove(x => x.id == id);
            if (entity == null) return NotFound();
            return Ok(entity);
        }

        // PUT: api/CommonOptions/UpdateFile/5
        /// <summary>
        /// This method update the logo of CommonOptions by id
        /// </summary>
        /// <param name="id">id of CommonOptions</param>
        /// <param name="file">file</param>
        /// <returns>CommonOptions which was updated</returns>
        [HttpPut("[action]/{id}")]
        [ValidateModel]
        public async Task<IActionResult> UpdateFile([FromRoute] int id,
            IFormFile file)
        {
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                AddError($"Expected a multipart request, but got {Request.ContentType}");
                return BadRequest(ModelState);
            }
            var entity = await _cRepository.Find(x => x.id == id);
            if (entity == null) return NotFound();
            var attachment = await ((AttachmentManager)_cRepositoryAttachment).AddAttachmentAsync(file, new Attachment(nameof(CommonOption)));
            entity.attachment_id = attachment.id;
            return Ok(await _cRepository.Update(entity, x => x.id == id));
        }

        private void AddError(string error)
        {
            ModelState.AddModelError("Error", error);
        }

      
    }
}
