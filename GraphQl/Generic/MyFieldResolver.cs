using GraphQL;
using GraphQL.Language.AST;
using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQL.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text;
using Humanizer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SerAPI.Data;
using SerAPI.Utils;
using System.Text.RegularExpressions;

namespace SerAPI.GraphQl.Generic
{
    public class MyFieldResolver : IFieldResolver
    {
        private TableMetadata _tableMetadata;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _dbContext;
        private readonly FillDataExtensions _fillDataExtensions;
        private readonly ILogger _logger;

        public MyFieldResolver(TableMetadata tableMetadata,
            ApplicationDbContext dbContext,
            FillDataExtensions fillDataExtensions,
            IHttpContextAccessor httpContextAccessor)
        {
            _tableMetadata = tableMetadata;
            _httpContextAccessor = httpContextAccessor;
            _dbContext = dbContext;
            _fillDataExtensions = fillDataExtensions;
            _logger = _httpContextAccessor.HttpContext.RequestServices.GetService<ILogger<MyFieldResolver>>();
        }

        public object Resolve(IResolveFieldContext context)
        {
            var type = _tableMetadata.Type;
            Type graphRepositoryType = typeof(IGraphRepository<>).MakeGenericType(new Type[] { type });
            dynamic service = _httpContextAccessor.HttpContext.RequestServices.GetService(graphRepositoryType);

            var whereArgs = new StringBuilder();
            var args = new List<object>();
            var includes = new List<string>();


            try
            {

                //foreach (var arg in context.FieldDefinition.Arguments)
                //    Console.WriteLine($"{arg.Name} {arg}");
                if (context.Arguments != null)
                {
                    var i = 0;
                    foreach (var argument in context.Arguments)
                    {
                        if (new string[] { "orderBy", "first", "page" }.Contains(argument.Key)) continue;
                        if (i > 0) whereArgs.Append(" AND ");
                        if (argument.Key == "all")
                        {
                            whereArgs.Append("( ");
                            var lastValide = false;
                            foreach (var (propertyInfo, j) in type.GetProperties().Select((v, j) => (v, j)))
                            {
                                if (!propertyInfo.GetCustomAttributes(true).Any(x => x.ToString() == "System.Text.Json.Serialization.JsonIgnoreAttribute")
                                    && !propertyInfo.GetCustomAttributes(true).Any(x => x.ToString() == "System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute")
                                    && !propertyInfo.PropertyType.Name.Contains("List"))
                                {
                                    var isValid = SqlCommandExt.ConcatFilter(args, whereArgs, $"@{i}", propertyInfo.Name, argument.Value.ToString(), "¬",
                                         typeProperty: propertyInfo.PropertyType, index: j, isValid: lastValide);
                                    if (isValid)
                                    {
                                        lastValide = true;
                                        i++;
                                    }
                                }
                            }
                            whereArgs.Append(" )");
                        }
                        else
                        {
                            Type fieldType = null;
                            var patternStr = @"_iext";
                            Match matchStr = Regex.Match(argument.Key, patternStr);
                            if (matchStr.Success)
                            {
                                var fieldName = Regex.Replace(argument.Key, patternStr, "");

                                foreach (var (propertyInfo, j) in type.GetProperties().Select((v, j) => (v, j)))
                                {
                                    if (propertyInfo.Name == fieldName)
                                    {
                                        fieldType = propertyInfo.PropertyType;
                                        if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Nullable<>))
                                        {
                                            fieldType = fieldType.GetGenericArguments()[0];
                                        }
                                        break;
                                    }
                                }
                            }

                            Utils.Utils.ConcatFilter(args, whereArgs, i, argument.Key, argument.Value, type: fieldType);
                            i++;
                        }
                    }
                }

                DetectChild(context.FieldAst.SelectionSet.Selections, includes, args, whereArgs);
                Console.WriteLine($"whereArgs: {whereArgs.ToString()}");
                if (context.FieldName.Contains("_list"))
                {
                    return service
                    .GetAllAsync(whereArgs: whereArgs.ToString(),
                        take: context.GetArgument<int?>("first"), offset: context.GetArgument<int?>("page"),
                        orderBy: context.GetArgument<string>("orderBy"),
                        includeExpressions: includes, args: args.ToArray())
                    .Result;
                }
                else
                {
                    var id = context.GetArgument<int>("id");

                    var dbEntity = service
                        .GetByIdAsync(id, whereArgs: whereArgs.ToString(),
                            includeExpressions: includes, args: args.ToArray())
                        .Result;

                    if (dbEntity == null)
                    {
                        GetError(context);
                        return null;
                    }
                    return dbEntity;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                var error = new ExecutionError(e.Message, e);
                context.Errors.Add(error);
                return null;
            }

        }

        private void DetectChild(IList<ISelection> selections, List<string> includes, List<object> args, StringBuilder whereArgs,
            string mainModel = "", bool isList = false)
        {
            foreach (var selection in selections)
            {
                if (((Field)selection).SelectionSet.Selections.Count > 0)
                {
                    var model = ((Field)selection).Name;
                    // IsPlural: 
                    if (((Field)selection).Name.Pluralize(inputIsKnownToBeSingular: false) == ((Field)selection).Name)
                    {
                        isList = true;
                        if (((Field)selection).Arguments != null && ((Field)selection).Arguments.Count() > 0)
                        {
                            if (((Field)selection).Arguments.Any(x => x.Name == "join" && (bool)x.Value.Value == true))
                            {
                                var i = 0;
                                foreach (var argument in ((Field)selection).Arguments)
                                {
                                    if (new string[] { "orderBy", "first", "join" }.Contains(argument.Name)) continue;
                                    if (whereArgs.Length > 0) whereArgs.Append(" AND ");
                                    if (argument.Name == "all")
                                    {
                                        Type modelType = null;
                                        foreach (var entityType in _dbContext.Model.GetEntityTypes())
                                        {
                                            modelType = Type.GetType(entityType.Name);
                                            if (modelType == null) continue;
                                            if (modelType.Name.ToSnakeCase().Pluralize().ToLower() == model)
                                                break;
                                        }
                                        Utils.Utils.FilterAllFields(modelType, args, whereArgs, args.Count() + i, argument.Value.Value.ToString(),
                                            isList: true, parentModel: model);
                                    }
                                    else
                                    {
                                        Type fieldType = null;
                                        var patternStr = @"_iext";
                                        Match matchStr = Regex.Match(argument.Name, patternStr);
                                        if (matchStr.Success)
                                        {
                                            var fieldName = Regex.Replace(argument.Name, patternStr, "");

                                            Type modelType = null;
                                            foreach (var entityType in _dbContext.Model.GetEntityTypes())
                                            {
                                                modelType = Type.GetType(entityType.Name);
                                                if (modelType == null) continue;
                                                if (modelType.Name.ToSnakeCase().ToLower() == model)
                                                    break;
                                            }

                                            foreach (var (propertyInfo, j) in modelType.GetProperties().Select((v, j) => (v, j)))
                                            {
                                                if (propertyInfo.Name == fieldName)
                                                {
                                                    fieldType = propertyInfo.PropertyType;
                                                    if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Nullable<>))
                                                    {
                                                        fieldType = fieldType.GetGenericArguments()[0];
                                                    }
                                                    break;
                                                }
                                            }
                                        }

                                        Utils.Utils.ConcatFilter(args, whereArgs, args.Count + i, $"{model}.Any({argument.Name}", argument.Value.Value, isList: true, type: fieldType);
                                        i++;
                                    }
                                }
                            }
                        }

                    }
                    else
                    {
                        if (((Field)selection).Arguments != null && !isList)
                        {
                            var i = 0;
                            foreach (var argument in ((Field)selection).Arguments)
                            {
                                if (new string[] { "orderby", "first", "page" }.Contains(argument.Name)) continue;
                                if (whereArgs.Length > 0) whereArgs.Append(" AND ");
                                var headerModel = model;
                                if (!string.IsNullOrEmpty(mainModel))
                                    headerModel = $"{mainModel}.{model}";
                                if (argument.Name == "all")
                                {
                                    Type modelType = null;
                                    foreach (var entityType in _dbContext.Model.GetEntityTypes())
                                    {
                                        modelType = Type.GetType(entityType.Name);
                                        if (modelType == null) continue;
                                        if (modelType.Name.ToSnakeCase().ToLower() == model)
                                            break;
                                    }
                                    Utils.Utils.FilterAllFields(modelType, args, whereArgs, args.Count() + i, argument.Value.Value.ToString(), parentModel: headerModel);
                                }
                                else
                                {
                                    Type fieldType = null;
                                    var patternStr = @"_iext";
                                    Match matchStr = Regex.Match(argument.Name, patternStr);
                                    if (matchStr.Success)
                                    {
                                        var fieldName = Regex.Replace(argument.Name, patternStr, "");

                                        Type modelType = null;
                                        foreach (var entityType in _dbContext.Model.GetEntityTypes())
                                        {
                                            //_logger.LogError($"model {model} modelType {entityType.Name}");
                                            modelType = Type.GetType(entityType.Name);
                                            if (modelType == null) continue;
                                            if (modelType.Name.ToSnakeCase().ToLower() == model)
                                                break;
                                        }

                                        foreach (var (propertyInfo, j) in modelType.GetProperties().Select((v, j) => (v, j)))
                                        {
                                            if (propertyInfo.Name == fieldName)
                                            {
                                                fieldType = propertyInfo.PropertyType;
                                                if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Nullable<>))
                                                {
                                                    fieldType = fieldType.GetGenericArguments()[0];
                                                }
                                                break;
                                            }
                                        }
                                    }

                                    Utils.Utils.ConcatFilter(args, whereArgs, args.Count + i, $"{headerModel}.{argument.Name}", argument.Value.Value,
                                        type: fieldType);
                                    i++;
                                }
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(mainModel))
                        model = $"{mainModel}.{model}";
                    if (!isList)
                        includes.Add(model);

                    DetectChild(((Field)selection).SelectionSet.Selections, includes, args, whereArgs, mainModel: model, isList: isList);
                }
            }
        }

        private void GetError(IResolveFieldContext context)
        {
            var error = new ValidationError(context.Document.OriginalQuery,
                "not-found",
                "Couldn't find entity in db.",
                new INode[] { context.FieldAst });
            context.Errors.Add(error);
        }

        public IQueryable GetQueryable(Type type) => GetType()
                .GetMethod("GetListHelper")
                .MakeGenericMethod(type)
                .Invoke(this, null) as IQueryable;

        public DbSet<T> GetListHelper<T>() where T : class
        {
            return _dbContext.Set<T>();
        }

    }
}