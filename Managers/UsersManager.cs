using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SerAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using SerAPI.Data;
using Google.Apis.Auth;
using Microsoft.Extensions.Caching.Memory;
using SerAPI.Managers;
using SerAPI.Services;
using static SerAPI.Utils.IdentityServerTransient.DelegationGrantValidator;
using SerAPI.Utils.IdentityServerTransient;
using SerAPI.Utils;
using SerAPI.Models.ViewModels;

namespace SerAPI.Managers
{
    public class UsersManager : GenericModelFactory<ApplicationUser>
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AuditManager _cRepositoryLog;
        private readonly PostgresQLService _postgresQLService;
        private readonly IRepository<Attachment> _cRepositoryAttachment;
        private IConfiguration _config;
        private IMemoryCache _cache;
        private new readonly string model = "User";

        public UsersManager(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            PostgresQLService postgresQLService,
            ApplicationDbContext db,
            ILogger<UsersManager> logger,
            IHttpContextAccessor httpContextAccessor,
            SignInManager<ApplicationUser> signInManager,
            IRepository<Attachment> cRepositoryAttachment,
            AuditManager cRepositoryLog,
            IMemoryCache memoryCache,
            IConfiguration config)
            : base(db, logger, httpContextAccessor, cRepositoryLog, config)
        {
            _context = db;
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _cRepositoryLog = cRepositoryLog;
            _postgresQLService = postgresQLService;
            _cRepositoryAttachment = cRepositoryAttachment;
            _config = config;
            _cache = memoryCache;
        }

        public async Task<string> All(bool select = false, string id = "", string email = "")
        {
            string Query = @"select u.id, u.is_active, u.name, u.last_name, u.user_name,
u.email, u.phone_number, u.create_date, r.name as role,
r.id as role_id, a.key as photo
from asp_net_users as u
left outer join asp_net_user_roles ru on ru.user_id = id
left outer join asp_net_roles r on r.id = ru.role_id
left outer join attachments a on u.attachment_id = a.id ";
            string OrderBy = @"u.user_name";

            if (select)
            {
                Query = @"SELECT u.id, u.name, u.last_name, u.user_name, a.key as photo from asp_net_users as u 
left outer join attachments a on u.attachment_id = a.id";
            }

            var Params = new Dictionary<string, object>();
            List<string> contentValues = new List<string>();
            var jObj = false;

            if (!string.IsNullOrEmpty(email))
            {
                contentValues.Add(@"u.user_name = @Email");
                Params.Add("@Email", email);
                jObj = true;
            }

            if (!string.IsNullOrEmpty(id))
            {
                contentValues.Add(@"u.id = @userId");
                Params.Add("@userId", id);
                jObj = true;
            }
            else if (!select)
            {
                contentValues.Add(@"u.id <> @user and r.name <> 'Super-user'");
                Params.Add("@User", GetCurrentUser());
            }

            if (contentValues.Count > 0)
                Query += makeParamsQuery(contentValues);

            return await _postgresQLService.GetDataFromDBAsync<ApplicationUser>(Query, Params: Params.Count == 0 ? null : Params,
                OrderBy: OrderBy, jObject: jObj);
        }

        public async Task<string> Find(string id)
        {
            var result = await All(id: id);
            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }
            else return null;
        }

        public async Task<bool> VerifyEmail(string email)
        {
            return await _context.Users.AnyAsync(x => x.UserName == email);
        }

        public async Task<object> Register(AnyRegisterModel model)
        {
            var user = new ApplicationUser
            {
                UserName = model.email,
                Email = model.email,
                Name = model.name,
                LastName = model.last_name,
                PhoneNumber = model.phone_number,
                EmailConfirmed = false,
                Address = model.address
            };

            string loginProvider = "";
            if (!string.IsNullOrEmpty(model.token) && !string.IsNullOrEmpty(model.third_type))
            {
                try
                {
                    if (Enum.TryParse(model.third_type, out ThirdTypes third))
                    {
                        switch (third)
                        {
                            case ThirdTypes.google:
                                {
                                    user = await DelegationGrantValidator.ValidateGoogleToken(_userManager,
                                        model.token, _config["Authentication:Google:ClientId"], getUser: true);
                                    loginProvider = "Google";
                                    break;
                                }
                            case ThirdTypes.facebook:
                                {
                                    user = await DelegationGrantValidator.ValidateFacebookToken(_userManager, model.token, getUser: true);
                                    loginProvider = "Facebook";
                                    break;
                                }
                            case ThirdTypes.microsoft:
                                {
                                    user = await DelegationGrantValidator.ValidateMSToken(_userManager, model.token,
                                        _config["Authentication:Microsoft:ClientId"], getUser: true);
                                    loginProvider = "Microsoft";
                                    break;
                                }
                        }
                    }
                }
                catch (InvalidTokenException)
                {
                    return 3;
                }
                user.Address = model.address;
            }

            if ((await _userManager.FindByIdAsync(user.Id)) != null) return 4;

            IdentityResult result;
            if (!string.IsNullOrEmpty(model.token) && !string.IsNullOrEmpty(model.third_type))
                result = await _userManager.CreateAsync(user);
            else if (!string.IsNullOrEmpty(model.password))
                result = await _userManager.CreateAsync(user, model.password);
            else
                return 2;

            if (result.Succeeded)
            {

                if (!await _userManager.IsInRoleAsync(user, Constantes.Usuario))
                {
                    await _userManager.AddToRoleAsync(user, Constantes.Usuario);
                }

                _logger.LogInformation(3, "User created a new account with password.");
                if (!string.IsNullOrEmpty(model.password))
                    return 1;
                else
                {
                    await _userManager.AddLoginAsync(user, new UserLoginInfo(loginProvider, user.ProviderKey, loginProvider));
                    return user;
                }
            }
            else
            {
                return result;
            }
        }

        public async Task<object> Add(RegisterViewModel entity)
        {
            var user = new ApplicationUser
            {
                UserName = entity.username,
                Email = entity.email,
                Name = entity.name,
                LastName = entity.last_name,
                PhoneNumber = entity.phone_number,
                DarkMode = entity.dark_mode,               
                Address = entity.address,
            };

            int attempt = 1;
            bool saveFailed;
            do
            {
                saveFailed = false;
                try
                {
                    IdentityResult result = await _userManager.CreateAsync(user, entity.password);
                    if (result.Succeeded)
                    {
                        if (await _roleManager.RoleExistsAsync(entity.role))
                        {
                            if (!await _userManager.IsInRoleAsync(user, entity.role))
                            {
                                await _userManager.AddToRoleAsync(user, entity.role);
                            }
                        }
                        else return 1;


                        var jObjObser = new JObject();
                        jObjObser.Add("Description", $"User {user.UserName} created successfull with ID {user.Id}");
                        jObjObser.Add("Role", $"Role {entity.role}");
                        await ((AuditManager)_cRepositoryLog).AddLog(new AuditBinding()
                        {
                            action = AuditManager.CREATE,
                            objeto = model,
                            json_observations = jObjObser
                        }, id: user.Id.ToString(), commit: true);
                        _logger.LogInformation(3, "User created a new account with password.");
                        return 3;
                    }
                    else
                    {
                        return result;
                    }
                }
                catch (DbUpdateConcurrencyException error)
                {
                    saveFailed = true;
                    _logger.LogCritical(0, $"Unable to save changes. Optimistic concurrency failure, " +
                     $"object has been modified\n {error.Message} {error.InnerException} {error.Data}");
                    // Update the values of the entity that failed to save from the store 
                    error.Entries.Single().Reload();
                    if (attempt > 1) return 2;
                    attempt++;
                }
            } while (saveFailed);
            return 2;
        }


        public async Task<object> Update(UpdateViewModel entity)
        {
            int attempt = 1;
            bool saveFailed;

            var user = await _userManager.FindByIdAsync(entity.id.ToString());
            if (user == null) return 1;
            user.IsActive = entity.is_active;
            user.UserName = entity.username;
            user.Email = entity.email;
            user.Name = entity.name;
            user.LastName = entity.last_name;
            user.PhoneNumber = entity.phone_number;
            user.DarkMode = entity.dark_mode;
            user.Address = entity.address;

            do
            {
                saveFailed = false;
                try
                {

                    if (!string.IsNullOrEmpty(entity.confirm_password))
                    {
                        await _userManager.RemovePasswordAsync(user);
                        var resultPass = await _userManager.AddPasswordAsync(user, entity.confirm_password);
                        if (resultPass.Succeeded)
                        {
                            _logger.LogInformation(3, "User changed their password successfully.");
                        }
                    }

                    //_context.Entry(user).Collection(x => x.Roles).Load();
                    if (await _roleManager.RoleExistsAsync(entity.role))
                    {
                        var rolesUser = (await _userManager.GetRolesAsync(user)).ToList();

                        //_logger.LogWarning(3, $"rolesUser {rolesUser} {string.Join(", ", rolesUser.ToArray())} " +
                        //    $"{rolesUser.FirstOrDefault()} count {rolesUser.Count} entity.Role {entity.Role}");
                        var cacheKeySize = string.Format("_{0}_claims", user.Id);
                        _cache.Remove(cacheKeySize);

                        if (rolesUser.Count() == 0)
                        {
                            await _userManager.AddToRoleAsync(user, entity.role);
                            _logger.LogWarning(3, "User changed your role.");
                        }
                        else if (entity.role != rolesUser.FirstOrDefault())
                        {
                            var identResult = await _userManager.RemoveFromRolesAsync(user, rolesUser);
                            if (identResult.Succeeded)
                            {
                                await _userManager.RemoveFromRoleAsync(user, entity.role);
                                if (!await _userManager.IsInRoleAsync(user, entity.role))
                                {
                                    await _userManager.AddToRoleAsync(user, entity.role);
                                    _logger.LogWarning(3, "User changed your role.");
                                }
                            }
                        }
                    }
                    else
                        _logger.LogCritical(0, $"Role not is found {entity.role} in the db");

                    var jObjObser = new JObject();
                    jObjObser.Add("Description", $"User {user.UserName} update successfull with ID {user.Id}");
                    jObjObser.Add("Role", $"Role {entity.role}");
                    await ((AuditManager)_cRepositoryLog).AddLog(new AuditBinding()
                    {
                        action = AuditManager.UPDATE,
                        objeto = this.model,
                        json_observations = jObjObser
                    }, id: user.Id.ToString());
                    IdentityResult result = await _userManager.UpdateAsync(user);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation(3, $"User {user.UserName} update successfull.");
                        return 3;
                    }
                    else return result;
                }
                catch (DbUpdateConcurrencyException error)
                {
                    saveFailed = true;
                    _logger.LogCritical(0, $"Unable to save changes. Optimistic concurrency failure, " +
                     $"object has been modified\n {error.Message} {error.InnerException} {error.Data}");
                    // Update the values of the entity that failed to save from the store 
                    error.Entries.Single().Reload();
                    user = await _context.Users.SingleOrDefaultAsync(x => x.Id == entity.id.ToString());
                    if (attempt > 5) return 2;
                    attempt++;
                }
            } while (saveFailed);
            return 2;

        }


        override
        public async Task<ApplicationUser> Remove(Expression<Func<ApplicationUser, bool>> condition)
        {
            if (!await Exist<ApplicationUser>(condition))
            {
                return null;
            }
            //_db.Entry(entity).State = EntityState.Deleted;
            var entity = _context.Users
                .Include(x => x.UserRoles)
                .First(condition);

            var cacheKeySize = string.Format("_{0}_claims", entity.Id);
            _cache.Remove(cacheKeySize);

            var roles = await _userManager.GetRolesAsync(entity);
            var identResult = await _userManager.RemoveFromRolesAsync(entity, roles);
            if (!identResult.Succeeded) return null;

            try
            {
                await _context.SaveChangesAsync();
                foreach (var login in await _userManager.GetLoginsAsync(entity))
                {
                    await _userManager.RemoveLoginAsync(entity, login.LoginProvider, login.ProviderKey);
                }

                var result = await _userManager.DeleteAsync(entity);
                if (result.Succeeded)
                {
                    var jObjObser = new JObject();
                    jObjObser.Add("Description", $"User {entity.UserName} deleted successfull with ID {entity.Id}");
                    await ((AuditManager)_cRepositoryLog).AddLog(new AuditBinding()
                    {
                        action = AuditManager.DELETE,
                        objeto = this.model,
                        json_observations = jObjObser
                    }, id: entity.Id, commit: true);
                    return entity;
                }
            }
            catch (DbUpdateConcurrencyException error)
            {
                _logger.LogCritical(0, $"Unable to save changes. Optimistic concurrency failure, " +
                    $"object has been modified\n {error.Message} {error.InnerException}");
                error.Entries.Single().Reload();
                try
                {
                    await _userManager.DeleteAsync(entity);
                }
                catch (DbUpdateException e)
                {
                    _logger.LogCritical(0, "Unable to save changes. " +
                    "Try again, and if the problem persists " +
                    "see your system administrator.Error: {0} {1}", e.Message, e.InnerException);
                    return null;
                }
            }

            return null;
        }

        public async Task<IdentityResult> ChangePassword(ChangePasswordViewModel model)
        {
            var user = await _userManager.FindByIdAsync(GetCurrentUser().ToString());

            if (user != null)
            {
                var result = await _userManager.ChangePasswordAsync(user, model.old_password, model.new_password);
                if (result.Succeeded)
                {
                    await ((AuditManager)_cRepositoryLog).AddLog(new AuditBinding()
                    {
                        action = AuditManager.EXECUTE,
                        objeto = this.model,
                        json_observations = new JObject(
                            new JProperty("Description", "Change Password")
                        )
                    }, commit: true);
                    //await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogInformation(3, "User changed their password successfully.");
                }
                return result;
            }

            return null;
        }

        public async Task Logout()
        {
            await ((AuditManager)_cRepositoryLog).AddLog(new AuditBinding()
            {
                action = AuditManager.LOGOUT,
                objeto = this.model
            }, commit: true);
        }

        public async Task Login()
        {
            await ((AuditManager)_cRepositoryLog).AddLog(new AuditBinding()
            {
                action = AuditManager.LOGIN,
                objeto = this.model
            }, commit: true);
        }

        public async Task Print(string jsonObservations)
        {
            await ((AuditManager)_cRepositoryLog).AddLog(new AuditBinding()
            {
                action = AuditManager.EXECUTE,
                objeto = "Datasheet",
                json_observations = new JObject(
                    new JProperty("Description", jsonObservations)
                )
            }, commit: true);
        }

        public async Task<ApplicationUser> UpdatePhotoProfile(IFormFile file)
        {
            ApplicationUser entity = await _context.Users
                .Include(x => x.Attachment)
                .SingleOrDefaultAsync(x => x.Id == GetCurrentUser());
            if (entity == null) return null;

            if (entity.AttachmentId != null) await DeleteS3File(entity.Attachment.key);

            var attachment = new Attachment(nameof(ApplicationUser));
            attachment.isPrivate = true;
            attachment = await ((AttachmentManager)_cRepositoryAttachment).AddAttachmentAsync(file, attachment);
            entity.AttachmentId = attachment.id;

            await _context.SaveChangesAsync();

            return entity;
        }

        /* 
         * Resumen: 
         *      Metodo para guardar un obj de tipo Pre-sales
         *      Este metodo obtiene los datos de un request de type-content=form dara
         *      con la finalidad de guardar archivos media como imagenes en el servidor
         *      y demas parametros 
         */
        public async Task<ApplicationUser> UpdatePhoto(IFormFile file, Expression<Func<ApplicationUser, bool>> condition)
        {
            ApplicationUser entity = await _context.Users
                .Include(x => x.Attachment)
                .SingleOrDefaultAsync(condition);
            if (entity == null) return null;

            if (entity.AttachmentId != null) await DeleteS3File(entity.Attachment.key);
            var attachment = new Attachment(nameof(ApplicationUser));
            attachment.isPrivate = true;
            attachment = await ((AttachmentManager)_cRepositoryAttachment).AddAttachmentAsync(file, attachment);
            entity.AttachmentId = attachment.id;

            return await base.Update(entity, condition);
        }

        public async Task<IdentityResult> UpdateProfile(UpdateProfileModel entity)
        {
            var user = await _userManager.FindByIdAsync(GetCurrentUser().ToString());
            user.Email = entity.email;
            user.Name = entity.name;
            user.LastName = entity.last_name;
            user.PhoneNumber = entity.phone_number;
            user.Address = entity.address;

            try
            {
                IdentityResult result = await _userManager.UpdateAsync(user);
                return result;
            }
            catch (Exception error)
            {
                _logger.LogCritical(0, $"Unable to save changes. Optimistic concurrency failure, " +
                 $"object has been modified\n {error.Message} {error.InnerException} {error.Data}");
                // Update the values of the entity that failed to save from the store                
            }
            return null;
        }

        public async Task<IdentityResult> SetDarkMode(UpdateBooleanModel instance)
        {
            var user = await _userManager.FindByIdAsync(GetCurrentUser().ToString());
            user.DarkMode = instance.new_value;

            try
            {
                IdentityResult result = await _userManager.UpdateAsync(user);
                return result;
            }
            catch (Exception error)
            {
                _logger.LogCritical(0, $"Unable to save changes. Optimistic concurrency failure, " +
                 $"object has been modified\n {error.Message} {error.InnerException} {error.Data}");
                // Update the values of the entity that failed to save from the store                
            }
            return null;
        }

        public async Task<IdentityResult> SetMaximizedWindows(UpdateBooleanModel instance)
        {
            var user = await _userManager.FindByIdAsync(GetCurrentUser().ToString());
            user.MaximizedWindows = instance.new_value;

            try
            {
                IdentityResult result = await _userManager.UpdateAsync(user);
                return result;
            }
            catch (Exception error)
            {
                _logger.LogCritical(0, $"Unable to save changes. Optimistic concurrency failure, " +
                 $"object has been modified\n {error.Message} {error.InnerException} {error.Data}");
                // Update the values of the entity that failed to save from the store                
            }
            return null;
        }

        public async Task<ApplicationUser> AddExternalLogin(string id, UserExternalLogin model)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                try
                {
                    if (Enum.TryParse(model.Provider, out ThirdTypes third))
                    {
                        switch (third)
                        {
                            case ThirdTypes.google:
                                {
                                    if (ValidateGoogleToken(model.Token, out string providerKey))
                                        await _userManager.AddLoginAsync(user, new UserLoginInfo("Google", providerKey, "Google"));
                                    break;
                                }
                            case ThirdTypes.facebook:
                                {
                                    if (ValidateFacebookToken(model.Token, out string providerKey))
                                        await _userManager.AddLoginAsync(user, new UserLoginInfo("Facebook", providerKey, "Facebook"));
                                    break;
                                }
                            case ThirdTypes.microsoft:
                                {
                                    if (ValidateMSToken(model.Token, out string providerKey))
                                        await _userManager.AddLoginAsync(user, new UserLoginInfo("Microsoft", providerKey, "Microsoft"));
                                    break;
                                }
                        }
                    }
                }
                catch (InvalidTokenException ex)
                {
                    _logger.LogError(ex.ToString());
                }
            }
            return user;
        }

        #region valide token
        private bool ValidateMSToken(string token, out string providerKey)
        {
            try
            {
                var audience = _config["Authentication:Microsoft:ClientId"];
                providerKey = ValidateJWTwithOpenId(token, audience).Result;
                return providerKey != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                throw new InvalidTokenException(nameof(InvalidJwtException));
            }

        }

        private bool ValidateGoogleToken(string token, out string providerKey)
        {
            try
            {
                var validation = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new string[] { _config["Authentication:Google:ClientId"] }
                };
                var validPayload = GoogleJsonWebSignature.ValidateAsync(token, validation).Result;
                providerKey = validPayload?.Subject;
                return validPayload != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                throw new InvalidTokenException(nameof(InvalidJwtException));
            }
        }

        private bool ValidateFacebookToken(string token, out string providerKey)
        {
            var jObjFB = GetProfileFromFB(token).Result;
            if (jObjFB == null || !jObjFB.ContainsKey("email"))
            {
                throw new InvalidTokenException(nameof(InvalidJwtException));
            }

            providerKey = jObjFB.GetValue("id").ToString();
            return true;
        }
        #endregion

    }
}
