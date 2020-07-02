using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SerAPI.Models;
using System.Linq;
using System.Threading.Tasks;
using SerAPI.Data;

namespace SerAPI.Utils.CustomFilters
{
    public class ValidateExistsAttribute
    {
    }

    //PERMISSION VALIDATION
    /// <summary>
    /// This class is used to validate request from controllers
    /// </summary>
    public class ValidatePermissionExistsAttribute : TypeFilterAttribute
    {
        /// <summary>
        /// Constructor 
        /// </summary>
        public ValidatePermissionExistsAttribute() : base(typeof
          (ValidatePermissionExistsFilterImpl))
        {
        }
        private class ValidatePermissionExistsFilterImpl : IAsyncActionFilter
        {
            private readonly ILogger _logger;
            private ApplicationDbContext _db;

            public ValidatePermissionExistsFilterImpl(
                ILogger<ValidatePermissionExistsFilterImpl> logger,
                ApplicationDbContext db)
            {
                _logger = logger;
                _db = db;
            }
            public async Task OnActionExecutionAsync(ActionExecutingContext context,
                ActionExecutionDelegate next)
            {
                if (!context.ModelState.IsValid)
                {
                    context.Result = new BadRequestObjectResult(context.ModelState);
                }
                var RequestMethod = context.HttpContext.Request.Method;
                PermissionBinding entityPut = null;
                Permission entity = null;
                int _id = 0;
                foreach (var elem in context.ActionArguments)
                {
                    if (elem.Value is PermissionBinding)
                    {
                        entityPut = (PermissionBinding)elem.Value;
                    }
                    else if (elem.Value is Permission)
                    {
                        entity = (Permission)elem.Value;
                    }
                    else if (elem.Value is int)
                    {
                        _id = (int)elem.Value;
                    }
                }
                if (RequestMethod.Equals("POST"))
                {
                    if (_db.permissions.Any(x => x.name == entity.name))
                    {
                        context.ModelState.AddModelError("Error", GetErrorMessage(entity.name));
                        context.Result = new BadRequestObjectResult(context.ModelState);
                        return;
                    }
                }
                else if (RequestMethod.Equals("PUT"))
                {
                    if (_id == 0)
                    {
                        context.ModelState.AddModelError("Error", "Not ID in the entity");
                        context.Result = new BadRequestObjectResult(context.ModelState);
                        return;
                    }
                    entity = await _db.permissions.AsNoTracking().FirstOrDefaultAsync(x => x.id == _id);
                    if (entity == null)
                    {
                        context.Result = new NotFoundObjectResult(_id);
                        return;
                    }
                    var list = _db.permissions.Except(_db.permissions
                        .Where(x => x.name == entity.name)).AsNoTracking().ToList();
                    if (list.Any(x => x.name == entityPut.name))
                    {
                        context.ModelState.AddModelError("Error", GetErrorMessage(entityPut.name));
                        context.Result = new BadRequestObjectResult(context.ModelState);
                        return;
                    }
                }
                await next();
            }

            private string GetErrorMessage(string name)
            {
                return $"Permission Name {name} is already in use";
            }
        }
    }
}
