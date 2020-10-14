﻿using SerAPI.GraphQl.Generic;
using SerAPI.Models;
using GraphQL.Types;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Reflection;
using SerAPI.Utilities;
using SerAPI.Utils;

namespace SerAPI.GraphQl
{
    public class CountTableType<T> : IntGraphType
    {
        private ITableNameLookup _tableNameLookup;
        private IDatabaseMetadata _dbMetadata;
        public QueryArguments TableArgs { get; set; }

        public CountTableType(
            IDatabaseMetadata dbMetadata,
            TableMetadata mainTable,
            ITableNameLookup tableNameLookup)
        {
            _tableNameLookup = tableNameLookup;
            _dbMetadata = dbMetadata;
            var permission = mainTable.Type.Name.ToLower().Pluralize();
            var friendlyTableName = _tableNameLookup.GetFriendlyName(mainTable.TableName);
            this.ValidatePermissions(permission, friendlyTableName, mainTable.Type.Name);
            // this.RequireAuthentication(); 

            Name = mainTable.TableName;

            foreach (var mainTableColumn in mainTable.Columns)
            {
                InitMainGraphTableColumn(mainTableColumn);
            }
        }

        private void InitMainGraphTableColumn(ColumnMetadata mainTableColumn)
        {
            if (mainTableColumn.IsList)
            {
                GetInternalInstances(mainTableColumn, isList: true);
            }
            else if (Assembly.GetCallingAssembly().GetTypes()
             .Where(x => !x.IsAbstract && typeof(BasicModel).IsAssignableFrom(x)).Any(x => x == mainTableColumn.Type)
                     || Constantes.SystemTablesSingular.Contains(mainTableColumn.Type.Name))
            {
                GetInternalInstances(mainTableColumn);
            }
            else
            {
                FillArgs(mainTableColumn.ColumnName, mainTableColumn.Type);
            }
        }

        private void GetInternalInstances(ColumnMetadata columnMetadata, bool isList = false)
        {
            var parentTypeName = columnMetadata.Type.Name;
            var metaTable = _dbMetadata.GetTableMetadatas().FirstOrDefault(x => x.Type.Name == parentTypeName);
            foreach (var tableColumn in metaTable.Columns)
            {
                if (tableColumn.IsList || Assembly.GetCallingAssembly().GetTypes()
                    .Where(x => !x.IsAbstract && typeof(BasicModel).IsAssignableFrom(x)).Any(x => x == tableColumn.Type)
                        || Constantes.SystemTablesSingular.Contains(tableColumn.Type.Name))
                {
                }
                else
                {
                    FillArgs(tableColumn.ColumnName, tableColumn.Type, parentModel: columnMetadata.ColumnName, isList: isList);
                }
            }
        }

        private void FillArgs(string columnName, Type type, string parentModel = "", bool isList = false)
        {
            if (!string.IsNullOrEmpty(parentModel))
                if (isList) columnName = $"{parentModel}__list__{columnName}";
                else columnName = $"{parentModel}__model__{columnName}";

            if (TableArgs == null)
            {
                TableArgs = new QueryArguments();
                TableArgs.Add(new QueryArgument<StringGraphType> { Name = "all" });
            }
            if (type.IsArray)
            {
                TableArgs.Add(new QueryArgument<StringGraphType> { Name = $"{columnName}_ext" });
            }
            if (columnName == "id")
            {
                TableArgs.Add(new QueryArgument<IdGraphType> { Name = "id" });
                TableArgs.Add(new QueryArgument<StringGraphType> { Name = "id_iext" });
                TableArgs.Add(new QueryArgument<StringGraphType> { Name = "id_iext_or" });
            }
            else
            {
                var queryArgument = new QueryArgument(GraphUtils.ResolveGraphType(type)) { Name = columnName };
                TableArgs.Add(queryArgument);

                if (type == typeof(DateTime?) || type == typeof(DateTime))
                {
                    TableArgs.Add(new QueryArgument<DateTimeGraphType> { Name = $"{columnName}_gt" });
                    TableArgs.Add(new QueryArgument<DateTimeGraphType> { Name = $"{columnName}_gte" });
                    TableArgs.Add(new QueryArgument<DateTimeGraphType> { Name = $"{columnName}_lt" });
                    TableArgs.Add(new QueryArgument<DateTimeGraphType> { Name = $"{columnName}_lte" });
                }
                else if (type == typeof(int?) || type == typeof(int))
                {
                    TableArgs.Add(new QueryArgument<IntGraphType> { Name = $"{columnName}_gt" });
                    TableArgs.Add(new QueryArgument<IntGraphType> { Name = $"{columnName}_gte" });
                    TableArgs.Add(new QueryArgument<IntGraphType> { Name = $"{columnName}_lt" });
                    TableArgs.Add(new QueryArgument<IntGraphType> { Name = $"{columnName}_lte" });
                    TableArgs.Add(new QueryArgument<StringGraphType> { Name = $"{columnName}_iext" });
                    TableArgs.Add(new QueryArgument<StringGraphType> { Name = $"{columnName}_iext_or" });
                    TableArgs.Add(new QueryArgument<BooleanGraphType> { Name = $"{columnName}_isnull" });
                }
                else if (type != typeof(bool))
                {
                    TableArgs.Add(new QueryArgument<StringGraphType> { Name = $"{columnName}_iext" });
                    TableArgs.Add(new QueryArgument<StringGraphType> { Name = $"{columnName}_iext_or" });
                    TableArgs.Add(new QueryArgument<BooleanGraphType> { Name = $"{columnName}_isnull" });
                }
            }
        }
    }
}
