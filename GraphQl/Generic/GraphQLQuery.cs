﻿using SerAPI.Data;
using SerAPI.Models;
using GraphQL.Types;
using Humanizer;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System;
using System.Reflection;
using SerAPI.Utilities;
using GraphQL.DataLoader;

namespace SerAPI.GraphQl.Generic
{
    public class GraphQLQuery : ObjectGraphType<object>
    {
        private IDatabaseMetadata _dbMetadata;
        private ITableNameLookup _tableNameLookup;
        private ApplicationDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly FillDataExtensions _fillDataExtensions;
        private readonly IDataLoaderContextAccessor _accessor;

        public GraphQLQuery(
            ApplicationDbContext dbContext,
            IDatabaseMetadata dbMetadata,
            ITableNameLookup tableNameLookup,
            IHttpContextAccessor httpContextAccessor,
            FillDataExtensions fillDataExtensions,
            IDataLoaderContextAccessor accessor
            )
        {
            _dbMetadata = dbMetadata;
            _tableNameLookup = tableNameLookup;
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
            _fillDataExtensions = fillDataExtensions;
            _accessor = accessor;

            Name = "Query";

            foreach (var metaTable in _dbMetadata.GetTableMetadatas())
            {
                //var friendlyTableName = _tableNameLookup.GetFriendlyName(metaTable.TableName);
                var friendlyTableName = metaTable.Type.Name.ToSnakeCase().ToLower();

                dynamic objectGraphType = null;
                if (!_tableNameLookup.ExistGraphType(metaTable.Type.Name))
                {
                    var inherateType = typeof(TableType<>).MakeGenericType(new Type[] { metaTable.Type });
                    objectGraphType = Activator.CreateInstance(inherateType, new object[] { metaTable,
                        _dbMetadata, _tableNameLookup, _httpContextAccessor, _accessor, false  });
                }

                var tableType = _tableNameLookup.GetOrInsertGraphType(metaTable.Type.Name, objectGraphType);

                dynamic objectCountGraphType = null;
                if (!_tableNameLookup.ExistGraphType($"{metaTable.Type.Name}_count"))
                {
                    var inherateType = typeof(CountTableType<>).MakeGenericType(new Type[] { metaTable.Type });
                    objectCountGraphType = Activator.CreateInstance(inherateType, new object[] { _dbMetadata, metaTable, _tableNameLookup });
                }

                var countTableType = _tableNameLookup.GetOrInsertGraphType($"{metaTable.Type.Name}_count", objectCountGraphType);

                AddField(new FieldType
                {
                    Name = friendlyTableName,
                    Type = tableType.GetType(),
                    ResolvedType = tableType,
                    Resolver = new MyFieldResolver(metaTable, _dbContext, _fillDataExtensions, _httpContextAccessor),
                    Arguments = new QueryArguments(tableType.TableArgs)
                });

                var listType = new ListGraphType<ObjectGraphType<dynamic>>();
                listType.ResolvedType = tableType;

                AddField(new FieldType
                {
                    Name = $"{friendlyTableName.Pluralize()}_list",
                    Type = listType.GetType(),
                    ResolvedType = listType,
                    Resolver = new MyFieldResolver(metaTable, _dbContext, _fillDataExtensions, _httpContextAccessor),
                    Arguments = new QueryArguments(tableType.TableArgs)
                });

                AddField(new FieldType
                {
                    Name = $"{friendlyTableName.Pluralize()}_count",
                    Type = countTableType.GetType(),
                    ResolvedType = countTableType,
                    Resolver = new MyFieldResolver(metaTable, _dbContext, _fillDataExtensions, _httpContextAccessor),
                    Arguments = new QueryArguments(countTableType.TableArgs)
                });
            }
        }
    }

}
