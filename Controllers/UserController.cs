using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SerAPI.Models;
using System;
using System.Threading.Tasks;
using IdentityServer4.AccessTokenValidation;
using System.Linq;
using SerAPI.Services;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using System.Text.Encodings.Web;
using Newtonsoft.Json.Linq;
using SerAPI.Utils;
using SerAPI.Managers;
using SerAPI.Models.ViewModels;
using SerAPI.Utils.CustomFilters;
using SerAPI.Utilities;

namespace SerAPI.Controllers
{
    /// <summary>
    /// This class is used as an api for the users requests.
    /// </summary>
    [Produces("application/json")]
    [Route("api/User")]
    [Authorize(AuthenticationSchemes = IdentityServerAuthenticationDefaults.AuthenticationScheme)]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRepository<ApplicationUser> _cRepository;
        private readonly IEmailSender _emailSender;
        private readonly Locales _locales;

        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="db"></param>
        /// <param name="userManager"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="roleManager"></param>
        /// <param name="cRepository"></param>
        public UserController(
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            Locales locales,
            IRepository<ApplicationUser> cRepository)
        {
            _userManager = userManager;
            _cRepository = cRepository;
            _emailSender = emailSender;
            _locales = locales;
        }

        // GET: api/User
        /// <summary>
        /// This method returns all user
        /// </summary>
        /// <returns>All users which were found</returns>        
        [HttpGet]
        [CustomAuthorize(CustomClaimTypes.Permission, "users.view", Roles: Constantes.SuperUser)]
        public async Task<IActionResult> GetUsers(bool select, string email)
        {
            return Content(await ((UsersManager)_cRepository).All(select: select, email: email), "application/json", System.Text.Encoding.UTF8);
        }

        // GET: api/User/5
        /// <summary>
        /// This method returns a user by id
        /// </summary>
        /// <param name="id">id of User</param>
        /// <returns>User which were found</returns>
        [HttpGet("{id}")]
        [CustomAuthorize(CustomClaimTypes.Permission, "users.view", Roles: Constantes.SuperUser)]
        [ValidateModel]
        public async Task<IActionResult> GetUser(string id)
        {
            var entity = await ((UsersManager)_cRepository).Find(id);
            if (entity == null) return NotFound();
            return Content(entity, "application/json", Encoding.UTF8);
        }

        // GET: api/User/VerifyEmail/{email}
        /// <summary>
        /// This method returns a user by id
        /// </summary>
        /// <param name="id">id of User</param>
        /// <returns>User which were found</returns>
        [HttpGet("[action]/{email}")]
        [AllowAnonymous]
        [ValidateModel]
        public async Task<IActionResult> VerifyEmail(string email)
        {
            if (!await ((UsersManager)_cRepository).VerifyEmail(email)) return NotFound();
            return Ok();
        }

        // POST: api/User/Register
        /// <summary>
        /// This method create a User obj
        /// </summary>
        /// <param name="model">Obj of user</param>
        /// <returns>User which was created</returns>
        [HttpPost("[action]")]
        [AllowAnonymous]
        [ValidateModel]
        public async Task<IActionResult> Register([FromBody] AnyRegisterModel model)
        {
            var result = await ((UsersManager)_cRepository).Register(model);
            if (result is IdentityResult)
            {
                AddErrors((IdentityResult)result);
                return BadRequest(ModelState);
            }
            if (result is ApplicationUser)
            {
                var entity = (ApplicationUser)result;
                return CreatedAtAction("GetUser", new { entity.Id }, entity);
            }
            switch ((int)result)
            {
                case 1:
                    var user = await ((GenericModelFactory<ApplicationUser>)_cRepository).Find(x => x.UserName == model.email);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Action("ConfirmEmail",
                        "Account",
                        values: new { userId = user.Id, code },
                        protocol: Request.Scheme);
                    try
                    {
                        await _emailSender.SendEmailAsync(user.Name, model.email, "Confirma tu cuenta", callbackUrl, "ConfirmAccount");
                    }
                    catch (Exception) { }
                    return Content(await ((UsersManager)_cRepository).Find(user.Id),
                        "application/json", Encoding.UTF8);
                case 2:
                    AddError($"Unable to save changes");
                    break;
                case 3:
                    AddError($"Invalid Token");
                    break;
                case 4:
                    // user in DB
                    AddError($"4");
                    break;
            }
            return BadRequest(ModelState);
        }

        // POST: api/User
        /// <summary>
        /// This method create a User obj
        /// </summary>
        /// <param name="model">Obj of user</param>
        /// <returns>User which was created</returns>
        [HttpPost]
        [CustomAuthorize(CustomClaimTypes.Permission, "users.add", Roles: Constantes.SuperUser)]
        [ValidateModel]
        public async Task<IActionResult> PostUser([FromBody] RegisterViewModel model)
        {
            var result = await ((UsersManager)_cRepository).Add(model);
            if (result is IdentityResult)
            {
                AddErrors((IdentityResult)result);
                return BadRequest(ModelState);
            }
            switch ((int)result)
            {
                case 1:
                    return NotFound();
                case 2:
                    AddError($"Unable to save changes, review the log");
                    break;
                case 3:
                    var user = await ((GenericModelFactory<ApplicationUser>)_cRepository).Find(x => x.UserName == model.username);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Action("ConfirmEmail",
                            "Account",
                            values: new { userId = user.Id, code },
                            protocol: Request.Scheme);
                    try
                    {
                        await _emailSender.SendEmailAsync(user.Name, model.email, "Confirma tu cuenta", callbackUrl, "ConfirmAccount");
                    }
                    catch (Exception) { }
                    return Content(await ((UsersManager)_cRepository).Find(user.Id),
                        "application/json", System.Text.Encoding.UTF8);
            }
            return BadRequest(ModelState);
            //return new StatusCodeResult(StatusCodes.Status409Conflict);
        }

        [HttpPost("[action]/{email}")]
        [AllowAnonymous]
        [ValidateModel]
        public async Task<IActionResult> SendVerificationEmail([FromRoute]string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{await _userManager.GetUserIdAsync(user)}'.");
            }

            var userId = await _userManager.GetUserIdAsync(user);
            //var email = await _userManager.GetEmailAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = userId, code = code },
                protocol: Request.Scheme);
            try
            {
                await _emailSender.SendEmailAsync(
                    user.Name,
                    email,
                    "Confirm your email",
                    HtmlEncoder.Default.Encode(callbackUrl),
                    "ConfirmEmail");
            }
            catch (Exception) { }
            return Ok(JObject.FromObject(new
            {
                CallbackUrl = callbackUrl
            }));
        }

        // PUT: api/User/5
        /// <summary>
        /// This method update a User by id
        /// </summary>
        /// <param name="id">id of User</param>
        /// <param name="model">Obj</param>
        /// <returns>User which was updated</returns>
        [HttpPut("{id}")]
        [CustomAuthorize(CustomClaimTypes.Permission, "users.update", Roles: Constantes.SuperUser)]
        [ValidateModel]
        public async Task<IActionResult> PutUser(string id, [FromBody] UpdateViewModel model)
        {
            if (id != model.id.ToString())
            {
                AddError($"{id} from route not is equal to model.id");
                return BadRequest(ModelState);
            }
            var result = await ((UsersManager)_cRepository).Update(model);
            if (result is IdentityResult)
            {
                AddErrors((IdentityResult)result);
                return BadRequest(ModelState);
            }
            switch ((int)result)
            {
                case 1:
                    return NotFound();
                case 2:
                    AddError($"Unable to save changes, review the log");
                    break;
                case 3:
                    return Content(await ((UsersManager)_cRepository).Find(id),
                        "application/json", System.Text.Encoding.UTF8);
            }
            return BadRequest(ModelState);
        }

        // DELETE: api/User/5
        /// <summary>
        /// This method delete a User obj
        /// </summary>
        /// <param name="id">id of User</param>
        /// <returns>User which was deleted</returns>
        [HttpDelete("{id}")]
        [CustomAuthorize(CustomClaimTypes.Permission, "users.delete", Roles: Constantes.SuperUser)]
        [ValidateModel]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var entity = await _cRepository.Remove(x => x.Id == id);
            if (entity == null) return NotFound();
            return Ok(entity);
        }


        // POST: api/User/ChangePassword
        /// <summary>
        /// This method create a User obj
        /// </summary>
        /// <param name="model">Obj of changeUser</param>
        /// <returns>User which was changed pass</returns>
        [HttpPost("[action]")]
        [ValidateModel]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordViewModel model)
        {
            var result = await ((UsersManager)_cRepository).ChangePassword(model);
            if (result == null) return NotFound();
            if (!result.Succeeded) return BadRequest(result);
            return Ok(result);
        }

        // POST: api/User/Logout
        /// <summary>
        /// This method save register logout of user
        /// </summary>
        [HttpPost("[action]")]
        [ValidateModel]
        public async Task<IActionResult> Logout()
        {
            await ((UsersManager)_cRepository).Logout();
            return Ok();
        }

        // POST: api/User/Login
        /// <summary>
        /// This method save register login
        /// </summary>
        [HttpPost("[action]")]
        [ValidateModel]
        public async Task<IActionResult> Login()
        {
            await ((UsersManager)_cRepository).Login();
            return Ok();
        }

        // POST: api/User/Print
        /// <summary>
        /// This method save who print
        /// </summary>
        [HttpPost("[action]")]
        [ValidateModel]
        public async Task<IActionResult> Print(string jsonObservations)
        {
            await ((UsersManager)_cRepository).Print(jsonObservations);
            return Ok();
        }

        // POST: api/User/AddExternalLogin
        /// <summary>
        /// This method add external login to a user
        /// </summary>
        [HttpPost("[action]")]
        [ValidateModel]
        public async Task<IActionResult> AddExternalLogin(string id, [FromBody] UserExternalLogin model)
        {
            return Ok(await ((UsersManager)_cRepository).AddExternalLogin(id, model));
        }

        // PUT: api/User/UpdatePhoto/5
        /// <summary>
        /// This method update the photo of User by id
        /// </summary>
        /// <param name="id">id of User</param>
        /// <param name="Photo">Photo</param>
        /// <returns>User which was updated</returns>
        [HttpPut("[action]/{id}")]
        //[CustomAuthorize(CustomClaimTypes.Permission, "users.update", Roles: Constantes.SuperUser)]
        [ValidateModel]
        public async Task<IActionResult> UpdatePhoto([FromRoute] string id, IFormFile file)
        {
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                AddError($"Expected a multipart request, but got {Request.ContentType}");
                return BadRequest(ModelState);
            }

            if (file.Length == 0)
            {
                AddError($"Not file");
                return BadRequest(ModelState);
            }

            var entity = await ((UsersManager)_cRepository).UpdatePhoto(file, x => x.Id == id);
            return (entity != null ? (IActionResult)Ok(entity) : NotFound());
        }

        // PUT: api/User/UpdateProfile
        /// <summary>
        /// This method update profile of User obj
        /// </summary>
        /// <param name="model">Obj of changeUser</param>
        /// <returns>User which was changed pass</returns>
        [HttpPut("[action]")]
        [ValidateModel]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileModel model)
        {
            var result = await ((UsersManager)_cRepository).UpdateProfile(model);
            if (result == null)
            {
                return BadRequest(ModelState);
            }

            if (!result.Succeeded) return BadRequest(result);
            return Ok(result);
        }

        // PUT: api/User/SetDarkMode
        /// <summary>
        /// This method update dark mode of User obj
        /// </summary>
        /// <param name="model">Obj of changeUser</param>
        /// <returns>User which was changed pass</returns>
        [HttpPut("[action]")]
        [ValidateModel]
        public async Task<IActionResult> SetDarkMode([FromBody] UpdateBooleanModel model)
        {
            var result = await ((UsersManager)_cRepository).SetDarkMode(model);
            if (result == null)
            {
                return BadRequest(ModelState);
            }

            if (!result.Succeeded) return BadRequest(result);
            return Ok(result);
        }

        // PUT: api/User/SetDarkMode
        /// <summary>
        /// This method update maximized windows mode of User obj
        /// </summary>
        /// <param name="model">Obj of changeUser</param>
        /// <returns>User which was changed pass</returns>
        [HttpPut("[action]")]
        [ValidateModel]
        public async Task<IActionResult> SetMaximizedWindows([FromBody] UpdateBooleanModel model)
        {
            var result = await ((UsersManager)_cRepository).SetMaximizedWindows(model);
            if (result == null)
            {
                return BadRequest(ModelState);
            }

            if (!result.Succeeded) return BadRequest(result);
            return Ok(result);
        }

        // PUT: api/User/UpdatePhotoProfile
        /// <summary>
        /// This method update the photo of profile user
        /// </summary>
        /// <param name="Photo">Photo</param>
        /// <returns>User which was updated</returns>
        [HttpPut("[action]")]
        [ValidateModel]
        public async Task<IActionResult> UpdatePhotoProfile(IFormFile file)
        {
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                AddError($"Expected a multipart request, but got {Request.ContentType}");
                return BadRequest(ModelState);
            }

            if (file.Length == 0)
            {
                AddError($"Not file");
                return BadRequest(ModelState);
            }

            var entity = await ((UsersManager)_cRepository).UpdatePhotoProfile(file);
            return (entity != null ? (IActionResult)Ok(entity) : NotFound());
        }

       

        // POST: api/User/ForgotPassword/{email}
        /// <summary>
        /// This method recover password
        /// </summary>
        /// <param name="email_username">email or username</param>
        [HttpPost("[action]/{email_username}")]
        [AllowAnonymous]
        [ValidateModel]
        public async Task<IActionResult> ForgotPassword(string email_username)
        {
            var user = await _userManager.FindByEmailAsync(email_username);
            if (user == null) //|| !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                user = await _userManager.FindByNameAsync(email_username);

                if (user == null)
                {
                    return NotFound();
                }
            }

            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { area = "Identity", code },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(
                user.Name,
                user.Email,
                _locales.__("forget_password_2"),
                HtmlEncoder.Default.Encode(callbackUrl),
                "MailRecoverPassword");

            return Ok();
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
