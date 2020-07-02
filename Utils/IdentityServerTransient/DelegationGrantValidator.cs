using Google.Apis.Auth;
using IdentityModel;
using IdentityServer4.Events;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SerAPI.Models;

namespace SerAPI.Utils.IdentityServerTransient
{
    public class DelegationGrantValidator : IExtensionGrantValidator
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private IEventService _events;
        private readonly ILogger _logger;
        private IConfiguration _config;

        public DelegationGrantValidator(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IEventService events,
            IConfiguration config,
            ILogger<DelegationGrantValidator> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _events = events;
            _config = config;
        }

        public string GrantType => "delegation";

        public async Task ValidateAsync(ExtensionGrantValidationContext context)
        {
            var token = context.Request.Raw.Get("token");
            var thirdType = context.Request.Raw.Get("third_type");
            var clientId = context.Request?.Client?.ClientId;

            if (string.IsNullOrEmpty(token))
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "missing token");
                return;
            }

            if (string.IsNullOrEmpty(thirdType))
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "missing third_type");
                return;
            }

            ApplicationUser user = null;
            var errors = new Dictionary<string, object>();

            try
            {
                if (Enum.TryParse(thirdType, out ThirdTypes third))
                {
                    switch (third)
                    {
                        case ThirdTypes.google: { user = await ValidateGoogleToken(_userManager, token, _config["Authentication:Google:ClientId"]); break; }
                        case ThirdTypes.facebook: { user = await ValidateFacebookToken(_userManager, token); break; }
                        case ThirdTypes.microsoft: { user = await ValidateMSToken(_userManager, token, _config["Authentication:Microsoft:ClientId"]); break; }
                        default:
                            {
                                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "wrong third_type");
                                return;
                            }
                    }
                }
                else
                {
                    context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "wrong third_type");
                    return;
                }
            }
            catch (InvalidTokenException)
            {
                errors.Add("error_code", 2);
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "invalid third token", errors);
                return;
            }

            if (user != null)
            {
                if (user.Id == null)
                {
                    return;
                }
                
                // Reject the token request if two-factor authentication has been enabled by the user.
                if (_userManager.SupportsUserTwoFactor && await _userManager.GetTwoFactorEnabledAsync(user))
                {
                    _logger.LogInformation("Authentication failed for username: {username}, reason: not allowed", user.UserName);
                    await _events.RaiseAsync(new UserLoginFailureEvent(user.UserName, "not allowed", false, clientId));
                }

                // Ensure the user is not already locked out.
                if (_userManager.SupportsUserLockout && await _userManager.IsLockedOutAsync(user))
                {
                    _logger.LogInformation("Authentication failed for username: {username}, reason: locked out", user.UserName);
                    await _events.RaiseAsync(new UserLoginFailureEvent(user.UserName, "locked out", false, clientId));
                }

                context.Result = new GrantValidationResult(user.Id, GrantType);
                return;
            }
            else
            {
                _logger.LogError("No user found matching username");
                await _events.RaiseAsync(new UserLoginFailureEvent("username", "Not found", false, clientId));

            }

            errors.Add("error_code", 1);
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "User Not found", errors);
            return;
        }

        public static async Task<ApplicationUser> ValidateMSToken(UserManager<ApplicationUser> userManager, string token, string audience, bool getUser = false)
        {
            try
            {
                var providerKey = await ValidateJWTwithOpenId(token, audience);
                if (providerKey != null)
                {
                    JwtSecurityToken jwt = new JwtSecurityToken(token);
                    var validPayload = jwt.Payload;
                    var username = validPayload.FirstOrDefault(x => x.Key == JwtClaimTypes.PreferredUserName).Value.ToString();
                    var name = validPayload.FirstOrDefault(c => c.Key == JwtClaimTypes.Name).Value.ToString();
                    return await GetUser(userManager, providerKey, "Microsoft", username, name, getUser: getUser);
                }
                else
                {
                    throw new InvalidTokenException(nameof(InvalidJwtException));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw new InvalidTokenException(nameof(InvalidJwtException));
            }

        }

        public static async Task<ApplicationUser> ValidateGoogleToken(UserManager<ApplicationUser> userManager, string token, string audience,
            bool getUser = false)
        {
            ApplicationUser user = null;
            var validation = new GoogleJsonWebSignature.ValidationSettings()
            {
                Audience = new string[] { audience }
            };

            try
            {
                var validPayload = await GoogleJsonWebSignature.ValidateAsync(token, validation);
                if (validPayload != null)
                {
                    var username = validPayload.Email;
                    string providerKey = validPayload.Subject;
                    user = await GetUser(userManager, providerKey, "Google", username, validPayload.GivenName, lastName: validPayload.FamilyName,
                        photo: validPayload.Picture, getUser: getUser);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw new InvalidTokenException(nameof(InvalidJwtException));
            }
            return user;
        }

        public static async Task<ApplicationUser> ValidateFacebookToken(UserManager<ApplicationUser> userManager, string token, bool getUser = false)
        {
            var jObjFB = await GetProfileFromFB(token);
            if (jObjFB == null || !jObjFB.ContainsKey("email"))
            {
                throw new InvalidTokenException(nameof(InvalidJwtException));
            }
            var username = jObjFB.GetValue("email").ToString();
            string providerKey = jObjFB.GetValue("id").ToString();
            var user = await GetUser(userManager, providerKey, "Facebook", username, jObjFB.GetValue("first_name").ToString(),
                lastName: jObjFB.GetValue("last_name").ToString(), photo: ((JObject)jObjFB["picture"]["data"]).GetValue("url").ToString(),
                getUser: getUser);
            return user;
        }

        #region request token facebook
        public static async Task<JObject> GetProfileFromFB(string access_token)
        {
            var url = $"https://graph.facebook.com/v5.0/me?fields=id,name,last_name,first_name,email,picture&access_token={access_token}";
            try
            {
                using (var httpClient = new HttpClient())
                {
                    HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, url);
                    HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(req);
                    httpResponseMessage.EnsureSuccessStatusCode();
                    HttpContent httpContent = httpResponseMessage.Content;
                    var responseString = JObject.Parse(await httpContent.ReadAsStringAsync());
                    return responseString;
                };
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return null;
            }
        }

        #endregion

        #region get user

        public static async Task<ApplicationUser> GetUser(UserManager<ApplicationUser> userManager,
            string providerKey, string loginProvider, string username, string name, string lastName = null,
            string photo = null, bool getUser = false)
        {
            var user = await userManager.FindByLoginAsync(loginProvider, providerKey);
            if (user == null)
            {
                user = await userManager.FindByNameAsync(username);

                if (user == null)
                {
                    //user = await CreateUser(username, name, lastName: lastName, photo: photo);
                    if (getUser)
                        return new ApplicationUser
                        {
                            UserName = username,
                            Email = username,
                            Name = name,
                            LastName = lastName ?? null,
                            //Photo = photo ?? null,
                            EmailConfirmed = true,
                            IsActive = true,
                            ProviderKey = providerKey
                        };
                    return null;
                }
                if (user != null && user.Id != null)
                {
                    await userManager.AddLoginAsync(user, new UserLoginInfo(
                                loginProvider, providerKey, loginProvider));
                }

            }
            return user;
        }

        private async Task<ApplicationUser> CreateUser(
            string username, string name, string lastName = null, string photo = null)
        {
            var user = new ApplicationUser
            {
                UserName = username,
                Email = username,
                Name = name,
                LastName = lastName ?? null,
                //Photo = photo ?? null,
                EmailConfirmed = true,
                IsActive = true
            };
            var result = await _userManager.CreateAsync(user);
            if (result.Succeeded)
            {
                if (await _roleManager.RoleExistsAsync(Constantes.Usuario))
                {
                    if (!await _userManager.IsInRoleAsync(user, Constantes.Usuario))
                    {
                        await _userManager.AddToRoleAsync(user, Constantes.Usuario);
                    }
                }
            }
            else
            {
                _logger.LogError($"{result.Errors.First().Description}");
                await _events.RaiseAsync(new UserLoginFailureEvent(user.Id, result.Errors.First().Description, false, "third_baccu_client"));
                user.Id = null;
            }
            return user;
        }
        #endregion

        #region validate JWT open id library
        public static async Task<string> ValidateJWTwithOpenId(string token, string audience)
        {
            IdentityModelEventSource.ShowPII = true;
            JwtSecurityToken jwt = new JwtSecurityToken(token);
            var issuer = jwt.Payload.Iss;

            var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>($"{issuer}/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever());
            OpenIdConnectConfiguration openIdConfig = await configurationManager.GetConfigurationAsync(CancellationToken.None);

            TokenValidationParameters validationParameters =
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    IssuerSigningKeys = openIdConfig.SigningKeys
                };

            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
            try
            {
                var claimsPrincipal = handler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                //JwtSecurityToken jwt = new JwtSecurityToken(token);
                var validPayload = jwt.Payload;
                var providerKey = Guid.Parse(validPayload.FirstOrDefault(c => c.Key == "oid").Value.ToString())
                    .ToString("N").TrimStart(new char[] { '0' });
                return providerKey;
            }
            catch (SecurityTokenValidationException ex)
            {
                // Validation failed
                Console.WriteLine(ex.ToString());
            }
            return null;
        }
        #endregion

        public enum ThirdTypes { google, facebook, microsoft };

    }

    //
    // Summary:
    //     An exception that is thrown when a Third Token is invalid.
    public class InvalidTokenException : Exception
    {
        public InvalidTokenException(string message) : base(message)
        {
        }
    }
}