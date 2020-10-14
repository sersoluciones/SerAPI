using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenIddict.Validation.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections;
using SerAPI.Managers;
using SerAPI.Utils.CustomFilters;
using SerAPI.Models;

namespace SerAPI.Controllers
{
    /// <summary>
    /// This class is used as an api for the Validations requests.
    /// </summary>
    [Produces("application/json")]
    [Route("api/Validation")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public class ValidationController : Controller
    {
        private readonly ValidationManager _service;

        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="cRepository"></param>
        public ValidationController(
            ValidationManager service)
        {
            _service = service;
        }

        // POST: api/Validation
        /// <summary>
        /// </summary>
        /// <param name="model">model</param>
        /// <returns>Validation which was created</returns>        
        [HttpPost]
        [ValidateModel]
        public async Task<IActionResult> PostValidation([FromBody] BaseValidationModel model)
        {
            var exist = await _service.ValidateAsync(model);
            return !exist ? NotFound() : (IActionResult)Ok(model);
        }

        // POST: api/Validation/UserModel
        /// <summary>
        /// </summary>
        /// <param name="model">model</param>
        /// <returns>Validation which was created</returns>        
        [HttpPost("[action]")]
        [ValidateModel]
        [AllowAnonymous]
        public async Task<IActionResult> UserModel([FromBody] UserValidationModel model)
        {
            (model as BaseValidationModel).Model = "User";
            var exist = await _service.ValidateAsync(model);
            return !exist ? NotFound() : (IActionResult)Ok(model);
        }

        #region Helpers
        private void AddError(string error)
        {
            ModelState.AddModelError("Error", error);
        }
        #endregion
    }
}