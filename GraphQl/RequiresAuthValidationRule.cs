using GraphQL;
using GraphQL.Language.AST;
using GraphQL.Server.Authorization.AspNetCore;
using GraphQL.Types;
using GraphQL.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SerAPI.Data;
using System.Security.Claims;
using SerAPI.Utilities;
using static OpenIddict.Abstractions.OpenIddictConstants;
using SerAPI.Utils;

namespace SerAPI.GraphQl
{
    public class RequiresAuthValidationRule : IValidationRule
    {
        private readonly ILogger _logger;
        private IConfiguration _config;
        private readonly IAuthorizationService _authorizationService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public static readonly ILoggerFactory MyLoggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter((category, level) => category == DbLoggerCategory.Database.Command.Name
                        && level == LogLevel.Information)
                .AddConsole();
        });

        public RequiresAuthValidationRule(
            IAuthorizationService authorizationService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<RequiresAuthValidationRule> logger,
            IConfiguration config)
        {
            _config = config;
            _authorizationService = authorizationService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public string GetCurrentUser()
        {
            return _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(x =>
                x.Type == Claims.Subject).Value;
        }

        public Task<INodeVisitor> ValidateAsync(ValidationContext context)
        {
            var userContext = context.UserContext as GraphQLUserContext;
            var authenticated = userContext.User?.Identity.IsAuthenticated ?? false;
            var userRoles = userContext.User.Claims.Where(x =>
                x.Type == Claims.Role).Select(x => x.Value).ToList();
            var operationType = OperationType.Query;
            var nameField = "";
            return Task.FromResult(
                new EnterLeaveListener(_ =>
           {
               _.Match<Operation>(op =>
               {
                   if (!authenticated && op.OperationType == OperationType.Mutation)
                   {
                       operationType = op.OperationType;

                       var type = context.TypeInfo.GetLastType();
                       type.GetPolicies();
                       AuthorizeAsync(op, type, context, operationType).GetAwaiter().GetResult();
                   }
                   else if (op.OperationType == OperationType.Mutation)
                   {
                       var field = (op.SelectionSet.Selections[0] as Field);

                       nameField = field.Name;
                       var type = context.TypeInfo.GetLastType();
                       var permissions = type.GetPermissions().ToList();
                       _logger.LogWarning($"type: {type} name: {nameField} permissions: {string.Join(",", permissions)}");
                       //    context.ReportError(new ValidationError(
                       //        context.OriginalQuery,
                       //        "auth-required",
                       //        $"Authorization is required to access {op.Name}.",
                       //       new INode[] { op }));
                   }
               });

               _.Match<ObjectField>(objectFieldAst =>
               {
                   if (context.TypeInfo.GetArgument().ResolvedType.GetNamedType() is IComplexGraphType argumentType)
                   {
                       var fieldType = argumentType.GetField(objectFieldAst.Name);
                       AuthorizeAsync(objectFieldAst, fieldType, context, operationType).GetAwaiter().GetResult();
                   }
               });


               // this could leak info about hidden fields in error messages
               // it would be better to implement a filter on the schema so it
               // acts as if they just don't exist vs. an auth denied error
               // - filtering the schema is not currently supported
               _.Match<Field>(fieldAst =>
              {
                  //_logger.LogWarning($"---------------------authenticated------------------- {authenticated}");
                  var fieldDef = context.TypeInfo.GetFieldDef();
                  if (fieldDef == null)
                  {
                      return;
                  }
                  if (!authenticated)
                  {
                      // check target field
                      AuthorizeAsync(fieldAst, fieldDef, context, operationType).GetAwaiter().GetResult();
                      // check returned graph type
                      AuthorizeAsync(fieldAst, fieldDef.ResolvedType.GetNamedType(), context, operationType).GetAwaiter().GetResult();
                  }
                  else
                  {

                      if (fieldDef.RequiresPermissions() &&
                           (!fieldDef.CanAccess(userContext.User.Claims) && !userRoles.Contains(Constantes.SuperUser)))
                      {
                          context.ReportError(new ValidationError(
                              context.OriginalQuery,
                              "auth-required",
                              $"You are not authorized to run this query.",
                              new PermissionRequiredException(string.Join(",", fieldDef.ResolvedType.GetNamedType().GetPermissions().ToList())),
                              new INode[] { fieldAst }));
                      }
                      else if (fieldDef.ResolvedType.GetNamedType().RequiresPermissions() &&
                            (!fieldDef.ResolvedType.GetNamedType().CanAccess(userContext.User.Claims)
                            && !userRoles.Contains(Constantes.SuperUser)))
                      {
                          var permissions = fieldDef.ResolvedType.GetNamedType().GetPermissions().ToList();
                          if (!string.IsNullOrEmpty(nameField))
                          {
                              Match matchUpdate = Regex.Match(nameField, @"update_", RegexOptions.IgnoreCase);
                              if (matchUpdate.Success)
                              {
                                  permissions = permissions.Where(x => x.Contains("update")).ToList();
                              }

                              Match matchDelete = Regex.Match(nameField, @"delete_", RegexOptions.IgnoreCase);
                              if (matchDelete.Success)
                              {
                                  permissions = permissions.Where(x => x.Contains("delete")).ToList();
                              }

                              Match matchCreate = Regex.Match(nameField, @"create_", RegexOptions.IgnoreCase);
                              if (matchCreate.Success)
                              {
                                  permissions = permissions.Where(x => x.Contains("add")).ToList();
                              }
                          }
                          else
                          {
                              permissions = permissions.Where(x => x.Contains("view")).ToList();
                          }
                          //_logger.LogWarning($"permissions: {string.Join(",", permissions)} operationType {operationType} name: {nameField}");

                          if (permissions.Count > 0 && !VerifyClaimDB(permissions, userRoles))
                              context.ReportError(new ValidationError(
                                  context.OriginalQuery,
                                  "auth-required",
                                  $"You are not authorized to run this query",
                                  new PermissionRequiredException(string.Join(",", permissions)),
                                  new INode[] { fieldAst }));
                      }

                  }

              });
           }) as INodeVisitor);
        }

        public List<IdentityRoleClaim<string>> CacheGetOrCreateClaims(List<string> userRoles)
        {
            var cache = _httpContextAccessor.HttpContext.RequestServices.GetService<IMemoryCache>();
            string userId = GetCurrentUser();
            var cacheKeySize = string.Format("_{0}_claims", userId);
            var cacheEntry = cache.GetOrCreate(cacheKeySize, entry =>
            {
                string SqlConnectionStr = _config.GetConnectionString("PsqlConnection");
                var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                optionsBuilder.UseNpgsql(SqlConnectionStr, o => o.UseNetTopologySuite());
                optionsBuilder.EnableSensitiveDataLogging();
                optionsBuilder.UseLoggerFactory(MyLoggerFactory);

                using (var _db = new ApplicationDbContext(optionsBuilder.Options))
                {
                    var roleIds = _db.Roles.Where(x => userRoles.Contains(x.Name)).Select(x => x.Id);
                    //return await _db.RoleClaims.AnyAsync(a => roleIds.Any(s => a.RoleId.Contains(s))
                    //    && a.ClaimType == CustomClaimTypes.Permission && permissions.Contains(a.ClaimValue));

                    var claims = _db.RoleClaims.Where(a => roleIds.Any(s => a.RoleId.Contains(s))
                        && a.ClaimType == CustomClaimTypes.Permission)
                        .AsNoTracking()
                        .ToList();
                    entry.SlidingExpiration = TimeSpan.FromDays(1);
                    entry.Size = 1000;
                    return claims;
                }

            });

            return cacheEntry;
        }

        private bool VerifyClaimDB(List<string> permissions, List<string> userRoles)
        {
            bool hasClaim = false;
            foreach (var claim in CacheGetOrCreateClaims(userRoles))
            {
                if (permissions.Contains(claim.ClaimValue))
                {
                    hasClaim = true;
                    break;
                }
            }

            return hasClaim;
        }

        private async Task AuthorizeAsync(
           INode node,
           IProvideMetadata type,
           ValidationContext context,
           OperationType operationType)
        {
            if (type == null || !type.RequiresAuthorization())
            {
                return;
            }

            var policyNames = type.GetPolicies();
            if (policyNames.Count == 0)
            {
                return;
            }

            var tasks = new List<Task<AuthorizationResult>>(policyNames.Count);
            foreach (string policyName in policyNames)
            {
                var task = _authorizationService.AuthorizeAsync(_httpContextAccessor.HttpContext.User, policyName);
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);

            foreach (var task in tasks)
            {
                var result = task.Result;
                if (!result.Succeeded)
                {
                    var stringBuilder = new StringBuilder("You are not authorized to run this ");
                    stringBuilder.Append(operationType.ToString().ToLower());
                    stringBuilder.AppendLine(".");

                    foreach (var failure in result.Failure.FailedRequirements)
                    {
                        AppendFailureLine(stringBuilder, failure);
                    }

                    context.ReportError(
                        new ValidationError(context.OriginalQuery, "authorization", stringBuilder.ToString(), node));
                }
            }
        }

        private static void AppendFailureLine(
            StringBuilder stringBuilder,
            IAuthorizationRequirement authorizationRequirement)
        {
            switch (authorizationRequirement)
            {
                case ClaimsAuthorizationRequirement claimsAuthorizationRequirement:
                    stringBuilder.Append("Required claim '");
                    stringBuilder.Append(claimsAuthorizationRequirement.ClaimType);
                    if (claimsAuthorizationRequirement.AllowedValues == null || !claimsAuthorizationRequirement.AllowedValues.Any())
                    {
                        stringBuilder.AppendLine("' is not present.");
                    }
                    else
                    {
                        stringBuilder.Append("' with any value of '");
                        stringBuilder.Append(string.Join(", ", claimsAuthorizationRequirement.AllowedValues));
                        stringBuilder.AppendLine("' is not present.");
                    }
                    break;

                case DenyAnonymousAuthorizationRequirement _:
                    stringBuilder.AppendLine("The current user must be authenticated.");
                    break;

                case NameAuthorizationRequirement nameAuthorizationRequirement:
                    stringBuilder.Append("The current user name must match the name '");
                    stringBuilder.Append(nameAuthorizationRequirement.RequiredName);
                    stringBuilder.AppendLine("'.");
                    break;

                case OperationAuthorizationRequirement operationAuthorizationRequirement:
                    stringBuilder.Append("Required operation '");
                    stringBuilder.Append(operationAuthorizationRequirement.Name);
                    stringBuilder.AppendLine("' was not present.");
                    break;

                case RolesAuthorizationRequirement rolesAuthorizationRequirement:
                    if (rolesAuthorizationRequirement.AllowedRoles == null || !rolesAuthorizationRequirement.AllowedRoles.Any())
                    {
                        // This should never happen.
                        stringBuilder.AppendLine("Required roles are not present.");
                    }
                    else
                    {
                        stringBuilder.Append("Required roles '");
                        stringBuilder.Append(string.Join(", ", rolesAuthorizationRequirement.AllowedRoles));
                        stringBuilder.AppendLine("' are not present.");
                    }
                    break;

                default:
                    stringBuilder.Append("Requirement '");
                    stringBuilder.Append(authorizationRequirement.GetType().Name);
                    stringBuilder.AppendLine("' was not satisfied.");
                    break;
            }
        }
    }

    public class PermissionRequiredException : Exception
    {
        public PermissionRequiredException(string message) : base(message)
        {
        }
    }
}