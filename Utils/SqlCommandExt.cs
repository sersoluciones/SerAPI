using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using SerAPI.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SerAPI.Utils
{
    public static class SqlCommandExt
    {
        public static async Task<dynamic> PaginationAsync<T>(this IQueryable<T> query, IHttpContextAccessor _contextAccessor) where T : class
        {
            bool allowCache = true;
            if (_contextAccessor.HttpContext.Request.Query.Any(x => x.Key.Equals("filter_by")))
            {
                var columnStr = _contextAccessor.HttpContext.Request.Query.FirstOrDefault(x => x.Key.Equals("filter_by")).Value.ToString();
                string[] columns = columnStr.Split(';');
                if (columns.Count() > 0) allowCache = false;
            }

            var pageSizeRequest = _contextAccessor.HttpContext.Request.Query.FirstOrDefault(x => x.Key.Equals("take")).Value;
            var currentPageRequest = _contextAccessor.HttpContext.Request.Query.FirstOrDefault(x => x.Key.Equals("page")).Value;
            int pageSize = string.IsNullOrEmpty(pageSizeRequest) ? 20 : int.Parse(pageSizeRequest);
            int pageNumber = string.IsNullOrEmpty(currentPageRequest) ? 1 : int.Parse(currentPageRequest);
            var result = new PagedResult<T>();

            result.current_page = pageNumber;
            result.page_size = pageSize;
            int? rowCount = null;
            if (allowCache)
                rowCount = CacheGetOrCreate(query, _contextAccessor);

            result.row_count = rowCount ?? await query.CountAsync();

            var pageCount = (double)result.row_count / pageSize;
            result.page_count = (int)Math.Ceiling(pageCount);

            string selectArgs = null;
            // select args
            if (_contextAccessor.HttpContext.Request.Query.Any(x => x.Key.Equals("select_args")))
            {
                selectArgs = _contextAccessor.HttpContext.Request.Query.FirstOrDefault(x => x.Key.Equals("select_args")).Value.ToString();
                selectArgs = $"new({selectArgs})";

                IDictionary<string, object> expando = new ExpandoObject();

                foreach (var propertyInfo in typeof(PagedResultBase).GetProperties())
                {
                    var currentValue = propertyInfo.GetValue(result);
                    expando.Add(propertyInfo.Name, currentValue);
                }
                expando.Add("results", await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).AsNoTracking()
                    .Select(selectArgs).ToDynamicListAsync());
                return expando as ExpandoObject;
            }
            else
            {
                result.results = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).AsNoTracking().ToListAsync();
            }
            //var skip = (page - 1) * pageSize;
            return result;
        }

        private static int CacheGetOrCreate<E>(IQueryable<E> query, IHttpContextAccessor _contextAccessor)
           where E : class
        {
            //var provider = new ServiceCollection()
            //           .AddMemoryCache()
            //           .BuildServiceProvider();
            var cache = _contextAccessor.HttpContext.RequestServices.GetService<IMemoryCache>();

            var cacheKeySize = string.Format("_{0}_size", typeof(E).Name);
            var cacheEntry = cache.GetOrCreate(cacheKeySize, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromDays(1);
                entry.Size = 1000;
                return query.Count();
            });

            return cacheEntry;
        }

        public static async Task<dynamic> SortFilterAsync<E>(this IQueryable<E> source, IHttpContextAccessor _contextAccessor, bool pagination = true)
              where E : class
        {
            // Filter By
            if (_contextAccessor.HttpContext.Request.Query.Any(x => x.Key.Equals("filter_by")))
            {
                var properties = new Dictionary<string, Type>();

                foreach (var propertyInfo in typeof(E).GetProperties())
                {
                    //Console.WriteLine($"_________________TRACEEEEEEEEEEEEEEEEE____________: key: {propertyInfo.Name} value: {propertyInfo.PropertyType.Name}");
                    if (!propertyInfo.GetCustomAttributes(true).Any(x => x.GetType() == typeof(JsonIgnoreAttribute))
                         && !propertyInfo.GetCustomAttributes(true).Any(x => x.GetType() == typeof(NotMappedAttribute))
                         && !propertyInfo.PropertyType.Name.Contains("List"))
                        properties.Add(propertyInfo.Name, propertyInfo.PropertyType);
                }

                var columnStr = _contextAccessor.HttpContext.Request.Query.FirstOrDefault(x => x.Key.Equals("filter_by")).Value.ToString();
                string pattern = @"\/|\|";
                string[] columns = Regex.Split(columnStr, pattern);
                Match match = Regex.Match(columnStr, pattern);

                var listExpOR = new List<Expression<Func<E, bool>>>();
                var listExpAND = new List<Expression<Func<E, bool>>>();

                int index = 0;
                //Procesamiento query
                foreach (var column in columns)
                {
                    var patternStr = @"\=|¬";
                    string[] value = Regex.Split(column, patternStr);
                    //Console.WriteLine($"=======================index {index} count {value.Count()} {string.Join(",", value)}");
                    if (value.Count() == 0 || string.IsNullOrEmpty(value[0]) || string.IsNullOrEmpty(value[1])) continue;

                    if (value[0] == "$")
                    {
                        try
                        {
                            foreach (var (field, i) in properties.Select((v, i) => (v, i)))
                            {
                                ConcatFilter(listExpOR, listExpAND, index + i, field.Key, value[1], "¬", field.Value);
                            }
                            break;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
                        }
                    }

                    Type fieldType = null;
                    var matchStr = Regex.Match(column, patternStr);
                    if (matchStr.Success)
                    {
                        var fieldName = Regex.Replace(value[0], patternStr, "");
                        foreach (var (propertyInfo, j) in typeof(E).GetProperties().Select((v, j) => (v, j)))
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
                        if (fieldType == null && value[0].Contains(".")) fieldType = typeof(BasicModel);
                        ConcatFilter(listExpOR, listExpAND, index, value[0], value[1], matchStr.Value, fieldType, match);

                    }

                    index++;
                }
                
                if (listExpOR.Count() > 0)
                    source = source.Where(Join(Expression.Or, listExpOR));
                if (listExpAND.Count() > 0)
                    source = source.Where(Join(Expression.And, listExpAND));
            }

            // Order By
            if (_contextAccessor.HttpContext.Request.Query.Any(x => x.Key.Equals("order_by")))
            {
                source = source.OrderBy(_contextAccessor.HttpContext.Request.Query.FirstOrDefault(x => x.Key.Equals("order_by")).Value.ToString());
            }

            string selectArgs = null;
            // select args
            if (_contextAccessor.HttpContext.Request.Query.Any(x => x.Key.Equals("select_args")))
            {
                selectArgs = _contextAccessor.HttpContext.Request.Query.FirstOrDefault(x => x.Key.Equals("select_args")).Value.ToString();
                selectArgs = $"new({selectArgs})";
                //IQueryable query = source.Select(selectArgs);
            }

            // Pagination
            if (pagination && _contextAccessor.HttpContext.Request.Query.Any(x => x.Key.Equals("take"))
                && _contextAccessor.HttpContext.Request.Query.Any(x => x.Key.Equals("page")))
                return await source.PaginationAsync(_contextAccessor);

            if (!string.IsNullOrEmpty(selectArgs))
            {
                return await source.AsNoTracking().Select(selectArgs).ToDynamicListAsync();
            }

            return await source.AsNoTracking().ToListAsync();
        }

        private static Expression<Func<T, bool>> FilterILike<T>(string propertyName, string value) where T : class
        {
            var lambdaParam = Expression.Parameter(typeof(T));
            var property = Expression.Property(lambdaParam, propertyName);
            var expr = Expression.Call(
                           typeof(NpgsqlDbFunctionsExtensions),
                           nameof(NpgsqlDbFunctionsExtensions.ILike),
                           Type.EmptyTypes,
                           Expression.Property(null, typeof(EF), nameof(EF.Functions)),
                           property,
                           Expression.Constant($"%{value}%"));

            return Expression.Lambda<Func<T, bool>>(expr, lambdaParam);
        }

        //EF.Functions.Like(x.OrderNumber, v1) || EF.Functions.Like(x.OrderNumber, v2).
        private static Expression<Func<T, bool>> PerformMultipleLike<T>(string propertyName, List<int> numbers) where T : class
        {
            // We want to build dynamically something like:
            // x => EF.Functions.Like(x.OrderNumber, v1) || EF.Functions.Like(x.OrderNumber, v2)...

            // typeof(DbFunctionsExtensions).GetMethod(nameof(DbFunctionsExtensions.Like), new[] { typeof(DbFunctions), typeof(string), typeof(string) });
            var likeMethod = typeof(DbFunctionsExtensions).GetMethod(nameof(DbFunctionsExtensions.Like), new[] { typeof(DbFunctions), typeof(string), typeof(string) });
            var entityProperty = typeof(T).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);

            // EF.Functions.Like(x.OrderNumber, v1) || EF.Functions.Like(x.OrderNumber, v2)...
            Expression likePredicate = null;

            var efFunctionsInstance = Expression.Constant(EF.Functions);

            // Will be the predicate paramter (the 'x' in x => EF.Functions.Like(x.OrderNumber, v1)...)
            var lambdaParam = Expression.Parameter(typeof(T));
            foreach (var number in numbers)
            {
                // EF.Functions.Like(x.OrderNumber, v1)
                //                                 |__|
                var numberValue = Expression.Constant(number);

                // EF.Functions.Like(x.OrderNumber, v1)
                //                  |_____________|
                var propertyAccess = Expression.Property(lambdaParam, entityProperty);

                // EF.Functions.Like(x.OrderNumber, v1)
                //|____________________________________|
                var likeMethodCall = Expression.Call(likeMethod, efFunctionsInstance, propertyAccess, numberValue);

                // Aggregating the current predicate with "OR" (||)
                likePredicate = likePredicate == null
                                    ? (Expression)likeMethodCall
                                    : Expression.OrElse(likePredicate, likeMethodCall);
            }

            // x => EF.Functions.Like(x.OrderNumber, v1) || EF.Functions.Like(x.OrderNumber, v2)...
            var lambdaPredicate = Expression.Lambda<Func<T, bool>>(likePredicate, lambdaParam);

            return lambdaPredicate;
        }

        public static T ReplaceParameter<T>(T expr, ParameterExpression toReplace, ParameterExpression replacement)
            where T : Expression
        {
            var replacer = new ExpressionReplacer(e => e == toReplace ? replacement : e);
            return (T)replacer.Visit(expr);
        }

        public static Expression<Func<T, TReturn>> Join<T, TReturn>(Func<Expression, Expression, BinaryExpression> joiner,
            IReadOnlyCollection<Expression<Func<T, TReturn>>> expressions)
        {
            if (!expressions.Any())
            {
                throw new ArgumentException("No expressions were provided");
            }
            var firstExpression = expressions.First();
            var otherExpressions = expressions.Skip(1);
            var firstParameter = firstExpression.Parameters.Single();
            var otherExpressionsWithParameterReplaced = otherExpressions.Select(e => ReplaceParameter(e.Body, e.Parameters.Single(), firstParameter));
            var bodies = new[] { firstExpression.Body }.Concat(otherExpressionsWithParameterReplaced);
            var joinedBodies = bodies.Aggregate(joiner);
            return Expression.Lambda<Func<T, TReturn>>(joinedBodies, firstParameter);
        }

        public static bool ConcatFilter(List<object> values, StringBuilder expresion, string paramName,
              string key, string value, string column, Type typeProperty = null, int? index = null, bool isList = false,
              bool isValid = false)
        {
            var select = "";
            var enable = true;
            var expValided = false;
            var patternStr = @"\=|¬";
            if (typeProperty != null)
            {
                if (typeProperty == typeof(string))
                {
                    expValided = true;
                    values.Add(value.ToLower());
                    select = string.Format(".ToLower().Contains({0})", paramName);
                }
            }
            else
            {
                expValided = true;
                if (int.TryParse(value, out int number))
                {
                    values.Add(number);
                    select = string.Format(" = {0}", paramName);
                }
                else if (bool.TryParse(value, out bool boolean))
                {
                    values.Add(boolean);
                    select = string.Format(" = {0}", paramName);
                }
                else if (float.TryParse(value, out float fnumber))
                {
                    values.Add(fnumber);
                    select = string.Format(" = {0}", paramName);
                }
                else if (double.TryParse(value, out double dnumber))
                {
                    values.Add(dnumber);
                    select = string.Format(" = {0}", paramName);
                }
                else if (decimal.TryParse(value, out decimal denumber))
                {
                    values.Add(denumber);
                    select = string.Format(" = {0}", paramName);
                }
                else if (DateTime.TryParse(value, out DateTime dateTime) == true)
                {
                    values.Add(dateTime.Date);
                    select = string.Format(" = {0}", paramName);
                }
                else
                {
                    if (typeProperty != null && typeProperty != typeof(string))
                    {
                        enable = false;
                    }
                    Match matchStr = Regex.Match(column, patternStr);
                    if (matchStr.Success)
                    {
                        if (matchStr.Value == "=")
                        {
                            values.Add(value);
                            select = string.Format(" = {0}", paramName);
                        }
                        else
                        {
                            values.Add(value.ToLower());
                            select = string.Format(".ToLower().Contains({0})", paramName);
                        }
                    }

                }
            }

            if (enable)
            {
                if (index != null && index > 0 && expresion.Length > 3 && isValid && expValided)
                {
                    if (isList)
                        expresion.Append(")");
                    expresion.Append(" OR ");
                }

                if (expValided)
                {
                    expresion.Append(key);
                    expresion.Append(select);
                }

            }
            return expValided;
        }
        public static void ConcatFilter<T>(List<Expression<Func<T, bool>>> listExpOR, List<Expression<Func<T, bool>>> listExpAND,
          int index, string key, object value, string patternToEvaluate, Type fieldType, Match match = null) where T : class
        {
            string select = "";
            Expression<Func<T, bool>> expToEvaluate = null;

            if (patternToEvaluate == "¬")
            {
                if (fieldType == typeof(string))
                {
                    // expToEvaluate = FilterILike<T>(key, $"%{value}%");
                    expToEvaluate = (b => EF.Functions.ILike(EF.Property<string>(b, key), $"%{value}%"));
                }
                else if (fieldType == typeof(BasicModel))
                {
                    select = string.Format("{0}.ToLower().Contains(@{1})", key, 0);
                    expToEvaluate = DynamicExpressionParser.ParseLambda<T, bool>(new ParsingConfig(), true, select, ((string)value).ToLower());

                }
                else if (TypeExtensions.IsNumber(fieldType))
                {
                    select = string.Format("string(object({0})).Contains(@{1})", key, 0);
                    expToEvaluate = DynamicExpressionParser.ParseLambda<T, bool>(new ParsingConfig(), true, select, value);
                }

            }
            else
            {
                if (value is DateTime)
                {
                }
                else
                {
                    select = string.Format("{0} = @{1}", key, 0);
                    expToEvaluate = DynamicExpressionParser.ParseLambda<T, bool>(new ParsingConfig(), true, select, value);
                }
            }

            if (match == null || index == 0) { if (expToEvaluate != null) listExpOR.Add(expToEvaluate); }
            else
            {
                // query filtro por AND o OR  
                if (index > 1)
                    match = match.NextMatch();

                if (match.Success)
                {
                    if (match.Value == "/")
                    {
                        if (expToEvaluate != null) listExpAND.Add(expToEvaluate);
                    }
                    else
                    {
                        if (expToEvaluate != null) listExpOR.Add(expToEvaluate);
                    }
                }
            }

        }

        public static async Task<dynamic> SortFilterSelectAsync<E>(this IQueryable source, IHttpContextAccessor _contextAccessor)
              where E : class
        {
            // select args
            if (_contextAccessor.HttpContext.Request.Query.Any(x => x.Key.Equals("select_args")))
            {
                var selectArgs = _contextAccessor.HttpContext.Request.Query.FirstOrDefault(x => x.Key.Equals("select_args")).Value.ToString();
                selectArgs = $"new({selectArgs})";
                source = source.Select(selectArgs);
            }

            return await source.ToDynamicListAsync();
        }

        public static List<T> GetPaged<T, U>(this IQueryable<T> query,
                                            int page, int pageSize) where U : class
        {
            var result = new PagedResult<U>();
            result.current_page = page;
            result.page_size = pageSize;
            result.row_count = query.Count();

            var pageCount = (double)result.row_count / pageSize;
            result.page_count = (int)Math.Ceiling(pageCount);

            var skip = (page - 1) * pageSize;
            //var res = query.Skip(skip)
            //                      .Take(pageSize)
            //                      .ProjectTo<U>()
            //                      .ToList();
            return query.ToList();
        }

        public static NpgsqlParameter[] GetArrayParameters<T>(this NpgsqlCommand cmd, IEnumerable<T> values,
            string paramNameRoot, int start = 1)
        {
            /* An array cannot be simply added as a parameter to a SqlCommand so we need to loop through things and add it manually. 
             * Each item in the array will end up being it's own SqlParameter so the return value for this must be used as part of the
             * IN statement in the CommandText.
             */
            var parameters = new List<NpgsqlParameter>();
            var parameterNames = new List<string>();
            var paramNbr = start;
            foreach (var value in values)
            {
                var paramName = string.Format("@{0}{1}", paramNameRoot, paramNbr++);
                parameterNames.Add(paramName);
                parameters.Add(new NpgsqlParameter(paramName, value));
                //_logger.LogInformation("@{0}={1}", paramName, value);
            }
            cmd.CommandText = cmd.CommandText.Replace("{" + paramNameRoot + "}", string.Join(", ", parameterNames));

            return parameters.ToArray();
        }

        public static NpgsqlParameter[] SetSqlParamsPsqlSQL(this NpgsqlCommand command, Dictionary<string, object> Params = null,
            ILogger _logger = null)
        {
            List<NpgsqlParameter> SqlParameters = new List<NpgsqlParameter>();
            if (Params != null)
            {
                foreach (var pair in Params)
                {
                    //_logger.LogInformation(0, $"{pair.Key} = {pair.Value}");
                    if (pair.Value is null)
                    {
                        SqlParameters.Add(new NpgsqlParameter(pair.Key, DBNull.Value));
                    }
                    else if (pair.Value.GetType() == typeof(string))
                    {
                        SqlParameters.Add(new NpgsqlParameter(pair.Key, (string)pair.Value));
                    }
                    else if (pair.Value.GetType() == typeof(int))
                    {
                        SqlParameters.Add(new NpgsqlParameter(pair.Key, (int)pair.Value));
                    }
                    else if (pair.Value.GetType() == typeof(bool))
                    {
                        SqlParameters.Add(new NpgsqlParameter(pair.Key, (bool)pair.Value));
                    }
                    else if (pair.Value.GetType() == typeof(decimal))
                    {
                        SqlParameters.Add(new NpgsqlParameter(pair.Key, (decimal)pair.Value));
                    }
                    else if (pair.Value.GetType() == typeof(float))
                    {
                        SqlParameters.Add(new NpgsqlParameter(pair.Key, (float)pair.Value));
                    }
                    else if (pair.Value.GetType() == typeof(double))
                    {
                        SqlParameters.Add(new NpgsqlParameter(pair.Key, (double)pair.Value));
                    }
                    else if (pair.Value.GetType() == typeof(DateTime))
                    {
                        SqlParameters.Add(new NpgsqlParameter(pair.Key, (DateTime)pair.Value));
                    }
                    else if (pair.Value.GetType().IsArray)
                    {
                        if (pair.Value.GetType() == typeof(string[]))
                        {
                            foreach (var param in command.GetArrayParameters((string[])pair.Value, pair.Key))
                            {
                                SqlParameters.Add(param);
                            }
                        }
                        else if (pair.Value.GetType() == typeof(int[]))
                        {
                            foreach (var param in command.GetArrayParameters((int[])pair.Value, pair.Key))
                            {
                                SqlParameters.Add(param);
                            }
                        }
                    }
                }
            }
            return SqlParameters.ToArray();
        }
    }

    public class ExpressionReplacer : ExpressionVisitor
    {
        private readonly Func<Expression, Expression> replacer;

        public ExpressionReplacer(Func<Expression, Expression> replacer)
        {
            this.replacer = replacer;
        }

        public override Expression Visit(Expression node)
        {
            return base.Visit(replacer(node));
        }
    }
}