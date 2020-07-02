using SerAPI.Models;
using SerAPI.Managers;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using SerAPI.Utils;
using SerAPI.Utils.CustomFilters;
using SerAPI.Utilities;

namespace SerAPI.Controllers
{
    /// <summary>
    /// This class is used as an api for the Attachments requests.
    /// </summary>
    [Produces("application/json")]
    [Route("api/Attachment")]
    [Authorize(AuthenticationSchemes = IdentityServerAuthenticationDefaults.AuthenticationScheme)]
    public class AttachmentController : Controller
    {
        private readonly IRepository<Attachment> _cRepository;

        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="cRepository"></param>
        public AttachmentController(IRepository<Attachment> cRepository)
        {
            _cRepository = cRepository;
        }

        // POST: api/Attachment
        /// <summary>
        /// This method allow create attachments
        /// </summary>
        /// <param name="file">file</param>
        /// <returns>Attachment which was created</returns>
        [HttpPost()]
        [CustomAuthorize(CustomClaimTypes.Permission, "attachments.add", Roles: Constantes.SuperUser)]
        public async Task<IActionResult> PostFile(IFormFile file, string model)
        {
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                ModelState.AddModelError("Error", $"Expected a multipart request, but got {Request.ContentType}");
                return BadRequest(ModelState);
            }

            if (file.Length == 0)
            {
                ModelState.AddModelError("Error", $"Not file");
                return BadRequest(ModelState);
            }

            var attachment = new Attachment(model);
            var instance = await ((AttachmentManager)_cRepository).AddAttachmentAsync(file, attachment);
            return (instance != null ? (IActionResult)Ok(instance) : BadRequest());
        }
    }
}