﻿using GraphQL;
using GraphQL.Language.AST;
using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQL.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SerAPI.Data;

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
            var alias = string.IsNullOrEmpty(context.FieldAst.Alias) ? context.FieldAst.Name : context.FieldAst.Alias;
            var whereArgs = new StringBuilder();
            var args = new List<object>();
            var includes = new List<string>();

            try
            {
                //var listFieldType = ((dynamic)context.FieldDefinition.ResolvedType).ResolvedType.Fields;

                if (context.FieldName.Contains("_list"))
                {
                    GraphUtils.DetectChild(context.FieldAst.SelectionSet.Selections, includes,
                        ((dynamic)context.FieldDefinition.ResolvedType).ResolvedType, args, whereArgs,
                        arguments: context.Arguments, mainType: _tableMetadata.Type);
                    Console.WriteLine($"whereArgs: {whereArgs.ToString()}");

                    return service
                    .GetAllAsync(alias, whereArgs: whereArgs.ToString(),
                        take: context.GetArgument<int?>("first"), offset: context.GetArgument<int?>("page"),
                        orderBy: context.GetArgument<string>("orderBy"),
                        includeExpressions: includes, args: args.ToArray())
                    .Result;
                }
                else if (context.FieldName.Contains("_count"))
                {
                    GraphUtils.DetectChild(context.FieldAst.SelectionSet.Selections, includes,
                        context.FieldDefinition.ResolvedType, args, whereArgs,
                        arguments: context.Arguments, mainType: _tableMetadata.Type);
                    Console.WriteLine($"whereArgs: {whereArgs.ToString()}");

                    return service.GetCountQuery(whereArgs: whereArgs.ToString(),                      
                        includeExpressions: includes, args: args.ToArray());
                }
                else
                {
                    var id = context.GetArgument<dynamic>("id");
                    GraphUtils.DetectChild(context.FieldAst.SelectionSet.Selections, includes,
                        context.FieldDefinition.ResolvedType, args, whereArgs,
                        arguments: context.Arguments, mainType: _tableMetadata.Type);
                    Console.WriteLine($"whereArgs: {whereArgs.ToString()}");

                    var dbEntity = service
                        .GetByIdAsync(alias, id, whereArgs: whereArgs.ToString(),
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