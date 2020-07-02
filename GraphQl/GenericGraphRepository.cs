using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using IdentityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Linq.Dynamic.Core;
using GraphQL.Types;
using GraphQL.DataLoader;
using GraphQL;
using GraphQL.Language.AST;
using Humanizer;
using SerAPI.Data;
using SerAPI.Models;
using Microsoft.EntityFrameworkCore.DynamicLinq;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;
using SerAPI.Utils;

namespace SerAPI.GraphQl
{
    public class GenericGraphRepository<T> : IGraphRepository<T>
          where T : class
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly FillDataExtensions _fillDataExtensions;
        private IDataLoaderContextAccessor _dataLoader;
        private IConfiguration _config;
        private IMemoryCache _cache;
        private readonly ILogger _logger;
        public string model;
        public string nameModel;

        public GenericGraphRepository(ApplicationDbContext db,
            IHttpContextAccessor httpContextAccessor,
            FillDataExtensions fillDataExtensions,
            IDataLoaderContextAccessor dataLoader,
            IConfiguration config)
        {
            _context = db;
            _config = config;
            _httpContextAccessor = httpContextAccessor;
            model = typeof(T).Name;
            nameModel = typeof(T).Name.ToSnakeCase().ToLower();
            _cache = _httpContextAccessor.HttpContext.RequestServices.GetService<IMemoryCache>();
            _logger = _httpContextAccessor.HttpContext.RequestServices.GetService<ILogger<GenericGraphRepository<T>>>();
            _fillDataExtensions = fillDataExtensions;
            _dataLoader = dataLoader;

        }

        public string GetCompanyIdUser()
        {
            return _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(x =>
                x.Type == CustomClaimTypes.CompanyId)?.Value;
        }

        public string GetCurrentUser()
        {
            return _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(x =>
                x.Type == JwtClaimTypes.Subject)?.Value;
        }

        public string GetCurrenUserName()
        {
            return _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Name)?.Value;
        }

        public List<string> GetRolesUser()
        {
            return _httpContextAccessor.HttpContext.User.Claims.Where(x =>
                x.Type == JwtClaimTypes.Role).Select(x => x.Value).ToList();
        }

        public async Task<IEnumerable<T>> GetAllAsync(List<string> includeExpressions = null,
            string orderBy = "", string whereArgs = "", int? take = null, int? offset = null, params object[] args)
        {
            return await GetQuery(includeExpressions: includeExpressions, orderBy: orderBy,
                first: take, offset: offset, whereArgs: whereArgs, args: args)
                .AsNoTracking().ToListAsync();
        }

        public IQueryable<T> GetQuery(List<string> includeExpressions = null,
            string orderBy = "", string whereArgs = "", int? first = null, int? offset = null, params object[] args)
        {
            IQueryable<T> query = GetModel;

            if (includeExpressions != null && includeExpressions.Count > 0)
            {
                foreach (var include in includeExpressions)
                    query = query.Include(include);
            }
            if (!string.IsNullOrEmpty(whereArgs) && args.Length > 0)
                query = query.Where(whereArgs, args);

            query = FilterQueryByCompany(query);

            if (!string.IsNullOrEmpty(orderBy))
                query = query.OrderBy(orderBy);

            if (offset != null && first != null)
            {
                var result = new PagedResultBase();

                result.current_page = offset.Value;
                result.page_size = first.Value;
                var allowCache = args.Length == 0;

                int? rowCount = null;
                if (allowCache)
                    rowCount = CacheGetOrCreate(query);

                result.row_count = rowCount ?? query.CountAsync().Result;

                var pageCount = (double)result.row_count / first.Value;
                result.page_count = (int)Math.Ceiling(pageCount);

                _fillDataExtensions.Add($"{typeof(T).Name.ToSnakeCase().ToLower().Pluralize()}_list", result);
                query = query.Skip((offset.Value - 1) * first.Value);
            }
            if (first != null)
            {
                query = query.Take(first.Value);
            }

            return query;
        }

        private IQueryable<T> FilterQueryByCompany(IQueryable<T> query)
        {
            foreach (var (propertyInfo, j) in typeof(T).GetProperties().Select((v, j) => (v, j)))
            {
                if (propertyInfo.Name == "company_id")
                {
                    query = query.Where($"company_id  = @0", GetCompanyIdUser());
                    break;
                }
            }
            return query;
        }

        public async Task<ILookup<Tkey, T>> GetItemsByIds<Tkey>(IEnumerable<Tkey> ids, IResolveFieldContext context, string param,
            bool isString = false)
        // where Tkey : struct
        {
            var whereArgs = new StringBuilder();
            var args = new List<object>();
            if (isString) whereArgs.Append($"@0.Contains({param})");
            else
                whereArgs.Append($"@0.Contains(int({param}))");
            args.Add(ids);
            var orderBy = context.GetArgument<string>("orderBy");

            if (context.Arguments != null)
            {
                var i = 1;
                foreach (var argument in context.Arguments)
                {
                    if (new string[] { "orderBy", "first", "join" }.Contains(argument.Key)) continue;
                    whereArgs.Append(" AND ");
                    if (argument.Key == "all")
                    {
                        Utils.Utils.FilterAllFields(typeof(T), args, whereArgs, i, argument.Value.ToString());
                    }
                    else
                    {

                        Type fieldType = null;
                        var patternStr = @"_iext";
                        Match matchStr = Regex.Match(argument.Key, patternStr);
                        if (matchStr.Success)
                        {
                            var fieldName = Regex.Replace(argument.Key, patternStr, "");
                            foreach (var (propertyInfo, j) in typeof(T).GetProperties().Select((v, j) => (v, j)))
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
                            //_logger.LogWarning($"type {typeof(T).Name} argument: {argument.Key} fieldName {fieldName} fieldType {fieldType}");

                        }

                        Utils.Utils.ConcatFilter(args, whereArgs, i, argument.Key, argument.Value, type: fieldType);
                        i++;
                    }
                }
            }

            IQueryable<T> query = GetModel;

            foreach (var selection in context.FieldAst.SelectionSet.Selections)
            {
                if (((Field)selection).SelectionSet.Selections.Count > 0)
                {
                    var model = ((Field)selection).Name;
                    if (model == "role") model = "Role";
                    if (model == "user") model = "User";

                    if ((((Field)selection).Name.Pluralize(inputIsKnownToBeSingular: false)) == model)
                    {
                        _logger.LogInformation($"model {model} is plural");
                        if (((Field)selection).Arguments != null && ((Field)selection).Arguments.Count() > 0)
                        {
                            var i = 0;
                            foreach (var argument in ((Field)selection).Arguments)
                            {
                                if (new string[] { "orderBy", "first", "join" }.Contains(argument.Name)) continue;
                                if (whereArgs.Length > 0) whereArgs.Append(" AND ");
                                if (argument.Name == "all")
                                {
                                    Type modelType = null;
                                    foreach (var entityType in _context.Model.GetEntityTypes())
                                    {
                                        modelType = Type.GetType(entityType.Name);
                                        if (modelType == null) continue;
                                        if (modelType.Name.ToSnakeCase().ToLower().Pluralize() == model)
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
                                        foreach (var entityType in _context.Model.GetEntityTypes())
                                        {
                                            modelType = Type.GetType(entityType.Name);
                                            if (modelType == null) continue;
                                            if (modelType.Name.ToSnakeCase().ToLower().Pluralize() == model)
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
                                        //_logger.LogWarning($"model {model} modelType {modelType} argument: {argument.Name} fieldName {fieldName} fieldType {fieldType}");

                                    }

                                    Utils.Utils.ConcatFilter(args, whereArgs, args.Count + i, $"{model}.Any({argument.Name}", argument.Value.Value, isList: true, type: fieldType);
                                    i++;
                                }
                            }

                        }
                    }
                    else
                    {
                        if (((Field)selection).Arguments != null)
                        {
                            var i = 0;
                            foreach (var argument in ((Field)selection).Arguments)
                            {
                                whereArgs.Append(" AND ");
                                if (argument.Name == "all")
                                {
                                    Type modelType = null;
                                    foreach (var entityType in _context.Model.GetEntityTypes())
                                    {
                                        modelType = Type.GetType(entityType.Name);
                                        if (modelType == null) continue;
                                        if (modelType.Name.ToSnakeCase().ToLower() == model)
                                            break;
                                    }
                                    Utils.Utils.FilterAllFields(modelType, args, whereArgs, args.Count() + i, argument.Value.Value.ToString(), parentModel: model);
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
                                        foreach (var entityType in _context.Model.GetEntityTypes())
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

                                    Utils.Utils.ConcatFilter(args, whereArgs, args.Count() + i, $"{model}.{argument.Name}", argument.Value.Value, type: fieldType);
                                    i++;
                                }
                            }
                        }
                    }
                    //if (!_context.ChangeTracker.Entries().Select(x => x.Entity).Any(x => Type.GetType(x.ToString()).Name.ToLower() == model))
                    query = query.Include(model);
                }
            }
            _logger.LogWarning($"whereArgs: {whereArgs}");
            query = query.Where(whereArgs.ToString(), args.ToArray());

            query = FilterQueryByCompany(query);

            if (!string.IsNullOrEmpty(orderBy))
                query = query.OrderBy(orderBy);

            var items = await query.AsNoTracking().ToListAsync();
            var pi = typeof(T).GetProperty(param);
            //if (typeof(Tkey) == typeof(int))
            return items.ToLookup(x => (Tkey)pi.GetValue(x, null));
        }

        public Task<IEnumerable<T>> GetLoader(IResolveFieldContext context, string param)
        {
            var first = context.GetArgument<int?>("first");
            Task<IEnumerable<T>> res = null;
            try
            {
                //if (Assembly.GetCallingAssembly().GetTypes()
                //    .Where(x => !x.IsAbstract && typeof(BasicModel).IsAssignableFrom(x)).Any(x => x == context.Source.GetType()))
                //IEquatable
                if (context.Source is BasicModel)
                {
                    var accesor = _httpContextAccessor.HttpContext.RequestServices.GetService<IDataLoaderContextAccessor>();
                    var loader = accesor.Context.GetOrAddCollectionBatchLoader<int, T>($"GetItemsByIds",
                       (ids) => GetItemsByIds(ids, context, param));

                    res = loader.LoadAsync(((BasicModel)context.Source).id);

                }
                else // if (context.Source is ApplicationUser || context.Source is ApplicationRole)
                {
                    var loader = _dataLoader.Context.GetOrAddCollectionBatchLoader<string, T>($"GetItemsByIds",
                        (ids) => GetItemsByIds(ids, context, param, isString: true));
                    res = loader.LoadAsync(((ApplicationUser)context.Source).Id);
                }

                if (first.HasValue && first.Value > 0)
                {
                    return res?.ContinueWith(x => x.Result.Take(first.Value));
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }
            return res;
        }

        public async Task<T> GetByIdAsync(int id, List<string> includeExpressions = null,
          string whereArgs = "", params object[] args)
        {
            if (id == 0) return null;
            var entity = await GetQuery(includeExpressions: includeExpressions,
                first: 1, whereArgs: whereArgs, args: args)
                .AsNoTracking().FirstOrDefaultAsync();

            //var entity = await GetModel.FindAsync(id);
            if (entity == null) return null;
            return entity;
        }

        private DbSet<T> GetModel
        {
            get { return _context.Set<T>(); }
        }

        public T Create(T entity, string alias = "")
        {
            // var objstr = JsonSerializer.Serialize(entity);
            //_logger.LogInformation($"----------------------------objstr {objstr}");

            var cacheKeySize = string.Format("_{0}_size", model);
            _cache.Remove(cacheKeySize);
            nameModel = $"create_{nameModel}";
            try
            {
                var obj = _context.Add(entity);
                _context.SaveChanges();
                return obj.Entity;
            }
            catch (ValidationException exc)
            {
                _logger.LogError(exc, $"{nameof(Create)} validation exception: {exc?.Message}");
                _fillDataExtensions.Add($"{(string.IsNullOrEmpty(alias) ? nameModel : alias)}", $"{exc?.Message }");
                _context.Entry(entity).State = EntityState.Detached;

            }
            catch (DbUpdateException e)
            {
                _logger.LogError(e, $"{nameof(Create)} db update error: {e?.InnerException?.Message}");
                _fillDataExtensions.Add($"{(string.IsNullOrEmpty(alias) ? nameModel : alias)}", $"{ e.InnerException?.Message }");
                _context.Entry(entity).State = EntityState.Detached;
            }
            return entity;
        }

        public T Update(int id, T entity, string alias = "")
        {
            var obj = GetModel.Find(id);
            if (obj != null)
            {
                nameModel = $"update_{nameModel}";
                try
                {
                    foreach (var propertyInfo in typeof(T).GetProperties())
                    {
                        try
                        {
                            if (propertyInfo.Name == "id") continue;

                            var oldValue = propertyInfo.GetValue(obj);
                            var newValue = propertyInfo.GetValue(entity);
                            if (propertyInfo.Name == "update_date") newValue = DateTime.Now;

                            if (newValue == null && oldValue != null) continue;
                            //if (newValue == oldValue) continue;

                            //Console.WriteLine($"___________TRACEEEEEEEEEEEEEEEEE____________: key: {propertyInfo.Name} {oldValue} {newValue}");
                            var currentpropertyInfo = obj.GetType().GetProperty(propertyInfo.Name);
                            if (currentpropertyInfo != null)
                            {
                                currentpropertyInfo.SetValue(obj, newValue, null);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }

                    //_context.Entry(obj).State = EntityState.Modified;
                    _context.SaveChanges();
                }
                catch (ValidationException exc)
                {
                    _logger.LogError(exc, $"{nameof(Update)} validation exception: {exc?.Message}");
                    _fillDataExtensions.Add($"{(string.IsNullOrEmpty(alias) ? nameModel : alias)}", @$"{id} => { exc?.Message}");
                    _context.Entry(entity).State = EntityState.Detached;

                }
                catch (DbUpdateException e)
                {
                    _context.Entry(obj).State = EntityState.Detached;
                    _fillDataExtensions.Add($"{(string.IsNullOrEmpty(alias) ? nameModel : alias)}", $"{id} => {e.InnerException?.Message}");
                }
            }
            return obj;
        }

        public T Delete(int id, string alias = "")
        {
            var obj = GetModel.Find(id);
            if (obj != null)
            {
                nameModel = $"delete_{nameModel}";
                try
                {
                    //GetModel.Remove(obj);
                    _context.Entry(obj).State = EntityState.Deleted;
                    _context.SaveChanges();
                }
                catch (ValidationException exc)
                {
                    _logger.LogError(exc, $"{nameof(Update)} validation exception: {exc?.Message}");
                    _context.Entry(obj).State = EntityState.Detached;
                    _fillDataExtensions.Add($"{(string.IsNullOrEmpty(alias) ? nameModel : alias)}", @$"{id} => {exc?.Message}");
                }
                catch (DbUpdateException e)
                {
                    _context.Entry(obj).State = EntityState.Detached;
                    _fillDataExtensions.Add($"{(string.IsNullOrEmpty(alias) ? nameModel : alias)}", $"{id} => {e.InnerException?.Message}");
                }

                var cacheKeySize = string.Format("_{0}_size", model);
                _cache.Remove(cacheKeySize);
            }
            return obj;
        }

        #region utilities
        public int CacheGetOrCreate<E>(IQueryable<E> query)
             where E : class
        {
            var cacheKeySize = string.Format("_{0}_size", typeof(T).Name);
            var cacheEntry = _cache.GetOrCreate(cacheKeySize, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromDays(1);
                entry.Size = 1000;
                return query.Count();
            });

            return cacheEntry;
        }
        #endregion
    }

}
