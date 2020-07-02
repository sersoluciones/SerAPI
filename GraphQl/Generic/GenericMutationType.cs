using GraphQL;
using GraphQL.Types;
using Humanizer;
using Newtonsoft.Json;
using SerAPI.Utils;
using System;

namespace SerAPI.GraphQl.Generic
{
    public class GenericMutationType : ObjectGraphType
    {
        public GenericMutationType(TableMetadata metaTable)
        {
            //Creacion de instancia con reflector
            //Type objectGraphType = typeof(ObjectGraphType<>).MakeGenericType(new Type[] { type });
            //dynamic tableType = Activator.CreateInstance(objectGraphType);

            var permission = metaTable.Type.Name.ToLower().Pluralize();
            this.RequirePermissions($"{permission}.add", $"{permission}.update", $"{permission}.delete");

            Name = metaTable.Type.Name;
            foreach (var tableColumn in metaTable.TableColumns)
            {
                InitGraphTableColumn(metaTable.Type, tableColumn);
            }
        }

        private void InitGraphTableColumn(Type parentType, ColumnMetadata columnMetadata)
        {
            if (columnMetadata.Type == typeof(NetTopologySuite.Geometries.Point) ||
                 columnMetadata.Type == typeof(NetTopologySuite.Geometries.Coordinate) ||
                 columnMetadata.Type == typeof(NetTopologySuite.Geometries.LineString) ||
                 columnMetadata.Type == typeof(NetTopologySuite.Geometries.MultiLineString))
            {
                Field(
                    typeof(string).GetGraphTypeFromType(true),
                    columnMetadata.ColumnName,
                    resolve: context => {
                        dynamic point = context.Source.GetPropertyValue(columnMetadata.ColumnName);
                        return JsonExtensions.SerializeWithGeoJson(point, formatting: Formatting.None);
                    }
                );

            }
            else
            {
                var graphQLType = (ResolveColumnMetaType(columnMetadata.DataType)).GetGraphTypeFromType(true);

                Field(
                    graphQLType,
                    columnMetadata.ColumnName
                );
            }
        }

        private Type ResolveColumnMetaType(string dbType)
        {
            if (TableType.DatabaseTypeToSystemType.ContainsKey(dbType))
                return TableType.DatabaseTypeToSystemType[dbType];
            return typeof(string);
        }
    }
}
