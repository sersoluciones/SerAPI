using SerAPI.GraphQl.Generic;
using SerAPI.Models;
using SerAPI.Utils;
using GraphQL;
using GraphQL.Types;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Reflection;
using Newtonsoft.Json;

namespace SerAPI.GraphQl
{
    public class TableType : ObjectGraphType
    {
        private IDatabaseMetadata _dbMetadata;
        private ITableNameLookup _tableNameLookup;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public QueryArguments TableArgs { get; set; }
        private static IDictionary<string, Type> _databaseTypeToSystemType;
        public static IDictionary<string, Type> DatabaseTypeToSystemType
        {
            get
            {
                if (_databaseTypeToSystemType == null)
                {
                    _databaseTypeToSystemType = new Dictionary<string, Type> {
                    { "uniqueidentifier", typeof(int) },
                    { "String", typeof(string) },
                    { "Int32", typeof(int?) },
                    { "Double", typeof(double?) },
                    { "Decimal", typeof(decimal?) },
                    { "Long", typeof(long?) },
                    { "DateTime", typeof(DateTime?) },
                    { "TimeSpan", typeof(TimeSpan?) },
                    { "Boolean", typeof(bool) }
                };
                }
                return _databaseTypeToSystemType;
            }
        }

        public TableType(
            TableMetadata mainTable,
            IDatabaseMetadata dbMetadata,
            ITableNameLookup tableNameLookup,
            IHttpContextAccessor httpContextAccessor)
        {
            _tableNameLookup = tableNameLookup;
            _dbMetadata = dbMetadata;
            _httpContextAccessor = httpContextAccessor;
            var permission = mainTable.Type.Name.ToLower().Pluralize();
            var friendlyTableName = _tableNameLookup.GetFriendlyName(mainTable.TableName);
            this.ValidatePermissions(permission, friendlyTableName, mainTable.Type.Name);

            Name = mainTable.TableName;

            foreach (var mainTableColumn in mainTable.TableColumns)
            {
                InitMainGraphTableColumn(mainTable.Type, mainTableColumn);
            }
        }

        private void InitMainGraphTableColumn(Type parentType, ColumnMetadata mainTableColumn)
        {
            if (parentType.Name == "Customer")
                Console.WriteLine($"{mainTableColumn.ColumnName} DataType: {mainTableColumn.DataType} Type: {mainTableColumn.Type} IsList {mainTableColumn.IsList}");
            // instancias internas

            if (Assembly.GetCallingAssembly().GetTypes()
               .Where(x => !x.IsAbstract && typeof(BasicModel).IsAssignableFrom(x)).Any(x => x == mainTableColumn.Type)
                   || Constantes.SystemTablesSingular.Contains(mainTableColumn.DataType))
            {
                GetInternalInstances(mainTableColumn);
            }
            else if (mainTableColumn.IsList)    // incluye litas de cada objeto
            {
                var queryThirdArguments = new QueryArguments();
                queryThirdArguments.Add(new QueryArgument<IntGraphType> { Name = "first" });
                queryThirdArguments.Add(new QueryArgument<StringGraphType> { Name = "orderBy" });
                queryThirdArguments.Add(new QueryArgument<StringGraphType> { Name = "all" });
                queryThirdArguments.Add(new QueryArgument<BooleanGraphType> { Name = "join" });

                var listObjectGraph = GetInternalListInstances(parentType, mainTableColumn, queryThirdArguments: queryThirdArguments);
                AddField(new FieldType
                {
                    Name = $"{mainTableColumn.ColumnName}",
                    ResolvedType = listObjectGraph,
                    Arguments = queryThirdArguments,
                    Resolver = new CustomListResolver(mainTableColumn.Type, parentType, _httpContextAccessor)
                });
            }
            else if (mainTableColumn.Type == typeof(NetTopologySuite.Geometries.Point) ||
                 mainTableColumn.Type == typeof(NetTopologySuite.Geometries.Coordinate) ||
                 mainTableColumn.Type == typeof(NetTopologySuite.Geometries.LineString) ||
                 mainTableColumn.Type == typeof(NetTopologySuite.Geometries.MultiLineString))
            {
                Field(
                    typeof(string).GetGraphTypeFromType(true),
                    mainTableColumn.ColumnName,
                    resolve: context =>
                    {
                        dynamic point = context.Source.GetPropertyValue(mainTableColumn.ColumnName);
                        return JsonExtensions.SerializeWithGeoJson(point, formatting: Formatting.None);
                    }
               );
            }
            else
            {
                var graphQLType = (ResolveColumnMetaType(mainTableColumn.DataType)).GetGraphTypeFromType(true);
                Field(
                    graphQLType,
                    mainTableColumn.ColumnName
                );
                FillArgs(mainTableColumn.ColumnName, ResolveColumnMetaType(mainTableColumn.DataType));
            }

        }

        private void InitGraphTableColumn(Type parentType, ColumnMetadata columnMetadata, ObjectGraphType objectGraphType, QueryArguments queryArguments)
        {
            if (Assembly.GetCallingAssembly().GetTypes()
                    .Where(x => !x.IsAbstract && typeof(BasicModel).IsAssignableFrom(x)).Any(x => x == columnMetadata.Type)
                    || Constantes.SystemTablesSingular.Contains(columnMetadata.DataType))
            {
                var queryThirdArguments = new QueryArguments();
                queryThirdArguments.Add(new QueryArgument<StringGraphType> { Name = "all" });
                var metaTable = _dbMetadata.GetTableMetadatas().FirstOrDefault(x => x.Type.Name == columnMetadata.DataType);
                var tableType = GetThirdGraphType(metaTable, columnMetadata, queryThirdArguments);

                objectGraphType.AddField(new FieldType
                {
                    Name = $"{columnMetadata.ColumnName}",
                    ResolvedType = tableType,
                    Arguments = queryThirdArguments
                });
            }
            // incluye listas de cada objeto
            else if (columnMetadata.IsList)
            {
                try
                {
                    var queryThirdArguments = new QueryArguments();
                    queryThirdArguments.Add(new QueryArgument<StringGraphType> { Name = "all" });

                    var listObjectGraph = GetInternalListInstances(parentType, columnMetadata, queryThirdArguments: queryThirdArguments);

                    objectGraphType.AddField(new FieldType
                    {
                        Name = $"{columnMetadata.ColumnName}",
                        ResolvedType = listObjectGraph,
                        Arguments = queryThirdArguments
                    });
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    var graphQLType = (ResolveColumnMetaType(columnMetadata.DataType)).GetGraphTypeFromType(true);
                    objectGraphType.Field(
                        graphQLType,
                        columnMetadata.ColumnName
                    );
                    FillArguments(queryArguments, columnMetadata.ColumnName, ResolveColumnMetaType(columnMetadata.DataType));
                }
            }
            else
            {
                var graphQLType = (ResolveColumnMetaType(columnMetadata.DataType)).GetGraphTypeFromType(true);
                objectGraphType.Field(
                    graphQLType,
                    columnMetadata.ColumnName
                );
                FillArguments(queryArguments, columnMetadata.ColumnName, ResolveColumnMetaType(columnMetadata.DataType));
            }
        }


        private void GetInternalInstances(ColumnMetadata mainTableColumn)
        {
            string key = $"Internal{mainTableColumn.DataType}";
            var queryArguments = new QueryArguments();
            queryArguments.Add(new QueryArgument<StringGraphType> { Name = "all" });
            var metaTable = _dbMetadata.GetTableMetadatas().FirstOrDefault(x => x.Type.Name == mainTableColumn.DataType);
            var tableType = GetSecondGraphType(mainTableColumn, queryArguments, metaTable);
            // Field<StateType>(nameof(City.state));
            AddField(new FieldType
            {
                Name = $"{mainTableColumn.ColumnName}",
                ResolvedType = tableType,
                Arguments = queryArguments
            });
        }

        private ListGraphType<ObjectGraphType> GetInternalListInstances(Type parentType, ColumnMetadata columnMetadata,
            QueryArguments queryThirdArguments = null)
        {
            var metaTable = _dbMetadata.GetTableMetadatas().FirstOrDefault(x => x.Type.Name == columnMetadata.DataType);

            ListGraphType<ObjectGraphType> listGraphType = null;
            if (!_tableNameLookup.ExistListGraphType(columnMetadata.ColumnName))
            {
                var tableType = GetSecondGraphType(columnMetadata, queryThirdArguments, metaTable);
                listGraphType = new ListGraphType<ObjectGraphType>();
                listGraphType.ResolvedType = tableType;
                // Field<ListGraphType<CityType>>(nameof(State.cities));
            }
            else
            {
                foreach (var tableColumn in metaTable.TableColumns)
                {
                    FillArguments(queryThirdArguments, tableColumn.ColumnName, ResolveColumnMetaType(tableColumn.DataType));
                }
            }

            return _tableNameLookup.GetOrInsertListGraphType(columnMetadata.ColumnName, listGraphType);

        }

        private ObjectGraphType GetSecondGraphType(ColumnMetadata columnMetadata, QueryArguments queryArguments, TableMetadata metaTable = null)
        {
            string key = $"Internal{columnMetadata.DataType}";
            ObjectGraphType objectGraphType = null;
            if (metaTable == null)
                metaTable = _dbMetadata.GetTableMetadatas().FirstOrDefault(x => x.Type.Name == columnMetadata.DataType);
            if (!_tableNameLookup.ExistGraphType(key))
            {
                //Creacion de instancia
                objectGraphType = new ObjectGraphType();
                objectGraphType.Name = key;
                var permission = columnMetadata.DataType.ToLower().Pluralize();
                var friendlyTableName = TableNameLookup.CanonicalName(columnMetadata.DataType.ToSnakeCase());
                objectGraphType.ValidatePermissions(permission, friendlyTableName, columnMetadata.DataType);
                //if (!typesWithoutPermission.Contains(permission) &&
                //    !typesWithoutPermission.Contains(friendlyTableName))
                //{
                //    if (Constantes.SystemTablesSingular.Contains(columnMetadata.DataType))
                //        objectGraphType.RequirePermissions($"{friendlyTableName}.view");
                //    else
                //        objectGraphType.RequirePermissions($"{permission}.view");
                //}
                foreach (var tableColumn in metaTable.TableColumns)
                {
                    InitGraphTableColumn(metaTable.Type, tableColumn, objectGraphType, queryArguments);
                }
            }
            else
            {
                foreach (var tableColumn in metaTable.TableColumns)
                {
                    FillArguments(queryArguments, tableColumn.ColumnName, ResolveColumnMetaType(tableColumn.DataType));
                }
            }
            return _tableNameLookup.GetOrInsertGraphType(key, objectGraphType);
        }

        private ObjectGraphType GetThirdGraphType(TableMetadata metaTable, ColumnMetadata columnMetadata, QueryArguments queryArguments)
        {
            string key = $"Third{columnMetadata.DataType}";
            ObjectGraphType objectGraphInternal = null;
            if (!_tableNameLookup.ExistGraphType(key))
            {
                objectGraphInternal = Activator.CreateInstance(typeof(ObjectGraphType)) as ObjectGraphType;
                objectGraphInternal.Name = key;
                var permission = columnMetadata.DataType.ToLower().Pluralize();
                var friendlyTableName = TableNameLookup.CanonicalName(columnMetadata.DataType.ToSnakeCase());
                objectGraphInternal.ValidatePermissions(permission, friendlyTableName, columnMetadata.DataType);


                foreach (var tableColumn in metaTable.Columns)
                {
                    var graphQLType = (ResolveColumnMetaType(tableColumn.DataType)).GetGraphTypeFromType(true);
                    objectGraphInternal.Field(
                        graphQLType,
                        tableColumn.ColumnName
                    );
                    FillArguments(queryArguments, tableColumn.ColumnName, ResolveColumnMetaType(tableColumn.DataType));
                }
            }
            else
            {
                foreach (var tableColumn in metaTable.Columns)
                {
                    FillArguments(queryArguments, tableColumn.ColumnName, ResolveColumnMetaType(tableColumn.DataType));
                }
            }
            return _tableNameLookup.GetOrInsertGraphType(key, objectGraphInternal);
        }

        private void FillArgs(string columnName, Type type)
        {
            if (TableArgs == null)
            {
                TableArgs = new QueryArguments();
                TableArgs.Add(new QueryArgument<IntGraphType> { Name = "first" });
                TableArgs.Add(new QueryArgument<IntGraphType> { Name = "page" });
                TableArgs.Add(new QueryArgument<StringGraphType> { Name = "orderBy" });
                TableArgs.Add(new QueryArgument<StringGraphType> { Name = "all" });
            }
            if (columnName == "id")
            {
                TableArgs.Add(new QueryArgument<IdGraphType> { Name = "id" });
                TableArgs.Add(new QueryArgument<StringGraphType> { Name = "id_iext" });
            }
            else
            {
                var queryArgument = new QueryArgument(type.GetGraphTypeFromType(true)) { Name = columnName };
                TableArgs.Add(queryArgument);

                if (type == typeof(DateTime?))
                {
                    TableArgs.Add(new QueryArgument<DateTimeGraphType> { Name = $"{columnName}_gt" });
                    TableArgs.Add(new QueryArgument<DateTimeGraphType> { Name = $"{columnName}_gte" });
                    TableArgs.Add(new QueryArgument<DateTimeGraphType> { Name = $"{columnName}_lt" });
                    TableArgs.Add(new QueryArgument<DateTimeGraphType> { Name = $"{columnName}_lte" });
                }
                else if (type != typeof(bool))
                {
                    queryArgument = new QueryArgument<StringGraphType> { Name = $"{columnName}_iext" };
                    TableArgs.Add(queryArgument);
                }
            }

        }

        private void FillArguments(QueryArguments queryArguments, string columnName, Type type)
        {
            if (queryArguments == null) return;
            if (columnName == "id")
            {
                queryArguments.Add(new QueryArgument<IdGraphType> { Name = "id" });
                queryArguments.Add(new QueryArgument<StringGraphType> { Name = "id_iext" });
            }
            else
            {
                var queryArgument = new QueryArgument(type.GetGraphTypeFromType(true)) { Name = columnName };
                queryArguments.Add(queryArgument);
                if (type == typeof(DateTime?))
                {
                    queryArguments.Add(new QueryArgument<DateTimeGraphType> { Name = $"{columnName}_gt" });
                    queryArguments.Add(new QueryArgument<DateTimeGraphType> { Name = $"{columnName}_gte" });
                    queryArguments.Add(new QueryArgument<DateTimeGraphType> { Name = $"{columnName}_lt" });
                    queryArguments.Add(new QueryArgument<DateTimeGraphType> { Name = $"{columnName}_lte" });
                }
                else if (type != typeof(bool))
                {
                    queryArgument = new QueryArgument<StringGraphType> { Name = $"{columnName}_iext" };
                    queryArguments.Add(queryArgument);
                }
            }

        }


        private Type ResolveColumnMetaType(string dbType)
        {
            if (DatabaseTypeToSystemType.ContainsKey(dbType))
                return DatabaseTypeToSystemType[dbType];
            return typeof(string);
        }
    }
}
