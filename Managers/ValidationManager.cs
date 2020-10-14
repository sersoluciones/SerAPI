using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using SerAPI.Data;
using SerAPI.Services;
using SerAPI.Models;
using SerAPI.GraphQl;

namespace SerAPI.Managers
{
    public class ValidationManager
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _config;
        private readonly PostgresQLService _postgresQLService;
        private string Namespace;

        public ValidationManager(ApplicationDbContext db,
            ILogger<ValidationManager> logger,
            IConfiguration config,
            IHttpContextAccessor contextAccessor,
            PostgresQLService postgresQLService)
        {
            _context = db;
            _config = config;
            _logger = logger;
            _contextAccessor = contextAccessor;
            _postgresQLService = postgresQLService;
            Namespace = _config["Validation:namespace"];
        }

        public async Task<bool> ValidateAsync(BaseValidationModel model)
        {
            var asm = Assembly.GetCallingAssembly();
            asm = Assembly.Load(Namespace);
            Type type;
            if (model.Model == "User") type = typeof(ApplicationUser);
            else if (model.Model == "Role") type = typeof(ApplicationRole);
            else
                type = asm.GetTypes().Where(x => !x.IsAbstract && typeof(BasicModel).IsAssignableFrom(x)).SingleOrDefault(x => x.Name == model.Model);
            //_logger.LogInformation($"----------------------type {type} model.Model {model.Model }--------------");
            if (type != null)
            {
                Type fieldLocalType = null;
                foreach (var (propertyInfo, j) in type.GetProperties().Select((v, j) => (v, j)))
                {
                    if (propertyInfo.Name == model.Field)
                    {
                        fieldLocalType = propertyInfo.PropertyType;
                        if (fieldLocalType.IsGenericType && fieldLocalType.GetGenericTypeDefinition() == typeof(Nullable<>))
                            fieldLocalType = fieldLocalType.GetGenericArguments()[0];
                        break;
                    }
                }
                if (fieldLocalType != null)
                {
                    Type graphRepositoryType = typeof(IGraphRepository<>).MakeGenericType(new Type[] { type });
                    dynamic service = _contextAccessor.HttpContext.RequestServices.GetService(graphRepositoryType);
                    return await service.ValidateObj(model);
                }
            }
            return true;
        }

        public IQueryable GetQueryable(Type type) => GetType()
               .GetMethod("GetListHelper")
               .MakeGenericMethod(type)
               .Invoke(this, null) as IQueryable;

        public DbSet<T> GetListHelper<T>() where T : class
        {
            return _context.Set<T>();
        }

        #region Helpers            
        private static Expression FilterAny(Type type, string propertyName, object value)
        {
            var lambdaParam = Expression.Parameter(type);
            var property = Expression.Property(lambdaParam, propertyName);
            var expr = Expression.Call(
                           typeof(DbFunctions),
                           nameof(DbFunctions.Equals),
                           Type.EmptyTypes,
                           Expression.Property(null, typeof(EF), nameof(EF.Functions)),
                           property,
                           Expression.Constant($"{value}"));

            return Expression.Lambda(expr, lambdaParam);
            //return Expression.Lambda<Func<Country, bool>>(expr, lambdaParam);
        }
        #endregion
    }
}

