using GraphQL;
using GraphQL.Types;
using System;
using System.Linq;
using SerAPI.Utils;

namespace SerAPI.GraphQl.Generic
{
    public class GenericInputType : InputObjectGraphType
    {
        private IDatabaseMetadata _dbMetadata;

        public GenericInputType(TableMetadata metaTable, IDatabaseMetadata dbMetadata)
        {
            _dbMetadata = dbMetadata;
            Name = $"{metaTable.Type.Name.ToLower().ToSnakeCase()}_input";
            foreach (var tableColumn in metaTable.TableColumns)
            {
                InitGraphTableColumn(tableColumn);
            }
        }

        private void InitGraphTableColumn(ColumnMetadata columnMetadata)
        {
            //Console.WriteLine($"{columnMetadata.ColumnName} {columnMetadata.DataType}");
            if (columnMetadata.DataType == "uniqueidentifier") return;
            if (columnMetadata.IsList)    // incluye litas de cada objeto
            {
                var listObjectGraph = GetInternalListInstances(columnMetadata);
                AddField(new FieldType
                {
                    Name = columnMetadata.ColumnName,
                    ResolvedType = listObjectGraph
                    //Resolver = new CustomListResolver(mainTableColumn.Type, parentType, _httpContextAccessor)
                });
            }
            else if (columnMetadata.Type == typeof(NetTopologySuite.Geometries.Point))
            {
                Field(
                    (ResolveColumnMetaType(columnMetadata.DataType)).GetGraphTypeFromType(true),
                    columnMetadata.ColumnName
                );

            }
            else if (!Constantes.SystemTablesSingular.Contains(columnMetadata.DataType))
            {
                var graphQLType = (ResolveColumnMetaType(columnMetadata.DataType)).GetGraphTypeFromType(true);
                //Console.WriteLine($"graphQLType: {graphQLType.Name}");

                Field(
                    graphQLType,
                    columnMetadata.ColumnName
                );
            }
        }

        private ListGraphType<InputObjectGraphType> GetInternalListInstances(ColumnMetadata columnMetadata)
        {
            var metaTable = _dbMetadata.GetTableMetadatas().FirstOrDefault(x => x.Type.Name == columnMetadata.DataType);


            var objectGraphType = new InputObjectGraphType();
            objectGraphType.Name = $"{metaTable.Type.Name.ToLower().ToSnakeCase()}_list_input";

            foreach (var tableColumn in metaTable.TableColumns)
            {
                var graphQLType = (ResolveColumnMetaType(tableColumn.DataType)).GetGraphTypeFromType(true);
                objectGraphType.Field(
                    graphQLType,
                    tableColumn.ColumnName
                );
            }

            ListGraphType<InputObjectGraphType> listGraphType = new ListGraphType<InputObjectGraphType>();
            listGraphType.ResolvedType = objectGraphType;
            // Field<ListGraphType<CityType>>(nameof(State.cities));

            return listGraphType;

        }

        private Type ResolveColumnMetaType(string dbType)
        {
            if (TableType.DatabaseTypeToSystemType.ContainsKey(dbType))
                return TableType.DatabaseTypeToSystemType[dbType];
            return typeof(string);
        }
    }
}
