using SerAPI.GraphQl.Generic;
using SerAPI.Data;
using SerAPI.Models;
using GraphQL.Server.Authorization.AspNetCore;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using SerAPI.Utils;

namespace SerAPI.GraphQl
{
    public class AppMutation : ObjectGraphType<object>
    {
        private IDatabaseMetadata _dbMetadata;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AppMutation(
            IGraphRepository<Permission> permissionRepository,
            IDatabaseMetadata dbMetadata,
            IHttpContextAccessor httpContextAccessor
            )
        {
            _dbMetadata = dbMetadata;
            _httpContextAccessor = httpContextAccessor;

            this.AuthorizeWith("Authorized");

            foreach (var metaTable in _dbMetadata.GetTableMetadatas())
            {
                var type = metaTable.Type;
                var friendlyTableName = type.Name.ToLower().ToSnakeCase();

                var genericInputType = new GenericInputType(metaTable, _dbMetadata);               
                var tableType = new GenericMutationType(metaTable);               

                AddField(new FieldType
                {
                    Name = $"create_{friendlyTableName}",
                    Type = tableType.GetType(),
                    ResolvedType = tableType,
                    Resolver = new CUDResolver(type, _httpContextAccessor),
                    Arguments = new QueryArguments(
                        new QueryArgument(typeof(InputObjectGraphType)) { Name = friendlyTableName, ResolvedType = genericInputType }
                    ),
                });

                AddField(new FieldType
                {
                    Name = $"update_{friendlyTableName}",
                    Type = tableType.GetType(),
                    ResolvedType = tableType,
                    Resolver = new CUDResolver(type, _httpContextAccessor),
                    Arguments = new QueryArguments(
                        new QueryArgument(typeof(InputObjectGraphType)) { Name = friendlyTableName, ResolvedType = genericInputType },
                        new QueryArgument<NonNullGraphType<IdGraphType>> { Name = "id" }
                    )
                });

                AddField(new FieldType
                {
                    Name = $"delete_{friendlyTableName}",
                    Type = tableType.GetType(),
                    ResolvedType = tableType,
                    Arguments = new QueryArguments(new QueryArgument<NonNullGraphType<IdGraphType>> { Name = $"{friendlyTableName}Id" }),
                    Resolver = new CUDResolver(type, _httpContextAccessor)
                });
            }
        }
    }
}