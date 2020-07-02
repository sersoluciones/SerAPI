using SerAPI.Data;
using SerAPI.Models;
using GraphQL.Types;
using Humanizer;
using Microsoft.AspNetCore.Http;
using System.Linq;
using SerAPI.Utils;

namespace SerAPI.GraphQl.Generic
{
    public class GraphQLQuery : ObjectGraphType<object>
    {
        private IDatabaseMetadata _dbMetadata;
        private ITableNameLookup _tableNameLookup;
        private ApplicationDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly FillDataExtensions _fillDataExtensions;

        public GraphQLQuery(
            ApplicationDbContext dbContext,
            IDatabaseMetadata dbMetadata,
            ITableNameLookup tableNameLookup,
            IHttpContextAccessor httpContextAccessor,
            FillDataExtensions fillDataExtensions
            )
        {
            _dbMetadata = dbMetadata;
            _tableNameLookup = tableNameLookup;
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
            _fillDataExtensions = fillDataExtensions;

            Name = "Query";
           
            foreach (var metaTable in _dbMetadata.GetTableMetadatas())
            {
                //var friendlyTableName = _tableNameLookup.GetFriendlyName(metaTable.TableName);
                var friendlyTableName = metaTable.Type.Name.ToSnakeCase().ToLower();

                ObjectGraphType objectGraphType = null;
                if (!_tableNameLookup.ExistGraphType(metaTable.Type.Name))
                {
                    objectGraphType = new TableType(metaTable, _dbMetadata, _tableNameLookup, _httpContextAccessor);
                }

                var tableType = _tableNameLookup.GetOrInsertGraphType(metaTable.Type.Name, objectGraphType);

                AddField(new FieldType
                {
                    Name = friendlyTableName,
                    Type = tableType.GetType(),
                    ResolvedType = tableType,
                    Resolver = new MyFieldResolver(metaTable, _dbContext, _fillDataExtensions, _httpContextAccessor),
                    Arguments = new QueryArguments((tableType as TableType).TableArgs)
                });

                var listType = new ListGraphType<ObjectGraphType<dynamic>>();
                listType.ResolvedType = tableType;

                AddField(new FieldType
                {
                    Name = $"{friendlyTableName.Pluralize()}_list",
                    Type = listType.GetType(),
                    ResolvedType = listType,
                    Resolver = new MyFieldResolver(metaTable, _dbContext, _fillDataExtensions, _httpContextAccessor),
                    Arguments = new QueryArguments((tableType as TableType).TableArgs)
                });
            }
        }
    }

}
