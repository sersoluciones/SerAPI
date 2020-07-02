using SerAPI.Data;
using SerAPI.Utils;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace SerAPI.GraphQl.Generic
{
    public interface IDatabaseMetadata
    {
        void ReloadMetadata();
        IEnumerable<TableMetadata> GetTableMetadatas();
    }

    public sealed class DatabaseMetadata : IDatabaseMetadata
    {
        private readonly ITableNameLookup _tableNameLookup;
        private readonly IConfiguration _config;
        private IEnumerable<TableMetadata> _tables;

        public DatabaseMetadata(
            ITableNameLookup tableNameLookup,
            IConfiguration config)
        {
            _config = config;
            _tableNameLookup = tableNameLookup;
            //_databaseName = _dbContext.Database.GetDbConnection().Database; 
            if (_tables == null)
                ReloadMetadata();
        }
        public IEnumerable<TableMetadata> GetTableMetadatas()
        {
            if (_tables == null)
                return new List<TableMetadata>();
            return _tables;
        }
        public void ReloadMetadata()
        {
            _tables = FetchTableMetaData();
        }
        private IReadOnlyList<TableMetadata> FetchTableMetaData()
        {
            var metaTables = new List<TableMetadata>();

            string SqlConnectionStr = _config.GetConnectionString("PsqlConnection");
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql(SqlConnectionStr, o => o.UseNetTopologySuite());
            optionsBuilder.EnableSensitiveDataLogging();

            using (var _dbContext = new ApplicationDbContext(optionsBuilder.Options))
            {
                foreach (var entityType in _dbContext.Model.GetEntityTypes())
                {
                    var tableName = entityType.GetTableName();

                    if (Constantes.SystemTablesSnakeCase.Contains(tableName))
                    {
                        continue;
                    }

                    Type elementType = Type.GetType(entityType.Name);

                    metaTables.Add(new TableMetadata
                    {
                        TableName = tableName,
                        AssemblyFullName = entityType.ClrType.FullName,
                        Columns = GetColumnsMetadata(entityType, elementType),
                        TableColumns = GetColumnsMetadata(elementType),
                        Type = elementType
                    });
                    //Console.WriteLine($"tabla registrada exitosamente {entityType.Name}");
                    _tableNameLookup.InsertKeyName(tableName);

                }
            }

            return metaTables;
        }
        private IReadOnlyList<ColumnMetadata> GetColumnsMetadata(IEntityType entityType, Type type)
        {
            var tableColumns = new List<ColumnMetadata>();

            foreach (var propertyType in type.GetProperties())
            {
                var field = propertyType.PropertyType;
                if (field.IsGenericType && field.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    field = field.GetGenericArguments()[0];
                }

                var isList = propertyType.PropertyType.Name.Contains("List");
                if (isList)
                    field = propertyType.PropertyType.GetGenericArguments().Count() > 0 ? propertyType.PropertyType.GetGenericArguments()[0] : propertyType.PropertyType;

                //Console.WriteLine($"Columna de la tabla: {entityType.GetTableName()} Name: {propertyType.Name} " +
                //    $"Type: {propertyType.GetType()} type3: {prop.Name} {field?.Name}");
                if (propertyType.GetCustomAttributes(true)
                       .Any(x => x.GetType() == typeof(NotMappedAttribute))) continue;
                tableColumns.Add(new ColumnMetadata
                {
                    ColumnName = propertyType.Name,
                    DataType = propertyType.Name == "id" ? "uniqueidentifier" : field.Name,
                    IsNull = field != null,
                    Type = propertyType.GetType(),
                    IsList = isList
                });
            }
            return tableColumns;
        }

        private IReadOnlyList<ColumnMetadata> GetColumnsMetadata(Type type)
        {
            var tableColumns = new List<ColumnMetadata>();

            foreach (var propertyInfo in type.GetProperties())
            {
                var field = propertyInfo.PropertyType;

                if (field.IsGenericType && field.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    field = field.GetGenericArguments()[0];
                }
                var isList = propertyInfo.PropertyType.Name.Contains("List");
                if (isList)
                    field = propertyInfo.PropertyType.GetGenericArguments().Count() > 0 ? propertyInfo.PropertyType.GetGenericArguments()[0] : propertyInfo.PropertyType;

                //Console.WriteLine($"Columna de la tabla: {type.Name} Name: {propertyInfo.Name} " +
                //    $" field.Name: { field.Name} isList: {isList}");
                if (propertyInfo.GetCustomAttributes(true)
                       .Any(x => x.GetType() == typeof(NotMappedAttribute))) continue;

                tableColumns.Add(new ColumnMetadata
                {
                    ColumnName = propertyInfo.Name,
                    DataType = propertyInfo.Name == "id" ? "uniqueidentifier" : field.Name,
                    IsNull = field != null,
                    Type = propertyInfo.PropertyType,
                    IsList = isList
                });
            }
            return tableColumns;
        }
    }
}
