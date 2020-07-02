using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SerAPI.GraphQl.Generic
{
    public class CustomListResolver : IFieldResolver
    {
        private Type _dataType;
        private string _mainTable;
        private Type _parentType;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CustomListResolver(Type dataType, Type parentType, IHttpContextAccessor httpContextAccessor)
        {
            _dataType = dataType;
            _mainTable = parentType.Name;
            _httpContextAccessor = httpContextAccessor;
            _parentType = parentType;
        }

        public object Resolve(IResolveFieldContext context)
        {
            string paramFK = "";
            var field = _dataType.GetGenericArguments().Count() > 0 ? _dataType.GetGenericArguments()[0] : null;
            foreach (var (propertyInfo, j) in field.GetProperties().Select((v, j) => (v, j)))
            {
                if (propertyInfo.PropertyType == _parentType)
                {
                    paramFK = propertyInfo.GetCustomAttributes(true)
                        .Where(x => x.GetType() == typeof(ForeignKeyAttribute))
                        .Select(attr => ((ForeignKeyAttribute)attr).Name)
                        .FirstOrDefault();
                    break;
                }
            }

            Type graphRepositoryType = typeof(IGraphRepository<>).MakeGenericType(new Type[] { field });
            dynamic service = _httpContextAccessor.HttpContext.RequestServices.GetService(graphRepositoryType);
            return service.GetLoader(context, $"{paramFK}");
        }
    }
}
