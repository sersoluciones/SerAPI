using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SerAPI.Data;
using SerAPI.Utils;

namespace SerAPI.Services
{
    public class PostgresQLService
    {
        private readonly ILogger _logger;
        private IConfiguration _config;
        private readonly IHttpContextAccessor _contextAccessor;
        private IMemoryCache _cache;
        public static readonly ILoggerFactory MyLoggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter((category, level) => category == DbLoggerCategory.Database.Command.Name
                        && level == LogLevel.Information)
                .AddConsole();
        });

        public PostgresQLService(
            ILogger<PostgresQLService> logger,
            IHttpContextAccessor httpContextAccessor,
            IMemoryCache memoryCache,
            IConfiguration config)
        {
            _logger = logger;
            _config = config;
            _contextAccessor = httpContextAccessor;
            _cache = memoryCache;
        }

        private string Filter<E>(out Dictionary<string, object> Params) where E : class
        {
            var expresion = new StringBuilder();
            Params = new Dictionary<string, object>();

            var currentClass = typeof(E).Name;
            if (currentClass.Equals("ApplicationUser"))
                currentClass = "user";
            else if (currentClass.Equals("ApplicationRole"))
                currentClass = "role";
            string initialClass = currentClass.ToLower().First().ToString();

            // Filter By
            if (_contextAccessor.HttpContext.Request.Query.Any(x => x.Key.Equals("filter_by")))
            {
                var columnStr = _contextAccessor.HttpContext.Request.Query.FirstOrDefault(x => x.Key.Equals("filter_by")).Value.ToString();
                string pattern = @"\/|\|";
                string[] columns = Regex.Split(columnStr, pattern);
                var properties = new Dictionary<string, Type>();
                var divider = new List<string>();
                Match match = Regex.Match(columnStr, pattern);

                foreach (var propertyInfo in typeof(E).GetProperties())
                {
                    if (!propertyInfo.GetCustomAttributes(true).Any(x => x.ToString() == "System.Text.Json.Serialization.JsonIgnoreAttribute"))
                        properties.Add(propertyInfo.Name, propertyInfo.PropertyType);
                }

                // query filtro por AND o OR
                foreach (var (value, index) in columns.Select((v, i) => (v, i)))
                {
                    if (index > 0)
                    {
                        match = match.NextMatch();
                        if (match.Success)
                        {
                            if (match.Value == "/") divider.Add(" AND ");
                            else divider.Add(" OR ");
                        }

                    }
                    else
                    {
                        if (match.Success)
                        {
                            if (match.Value == "/") divider.Add(" AND ");
                            else divider.Add(" OR ");
                        }
                    }
                }

                //Procesamiento query
                string dividerOld = "";
                foreach (var (column, index) in columns.Select((v, i) => (v, i)))
                {

                    if (index > 0)
                    {
                        if (index > 1 && dividerOld != divider[index - 1])
                        {
                            expresion.Append(")");
                            expresion.Append(divider[index - 1]);
                            expresion.Append("(");
                        }
                        else expresion.Append(divider[index - 1]);
                        dividerOld = divider[index - 1];
                    }
                    else expresion.Append("(");

                    var patternStr = @"\=|¬";
                    string[] value = Regex.Split(column, patternStr);
                    if (string.IsNullOrEmpty(value[1])) break;

                    initialClass = currentClass.ToLower().First().ToString() + ".";
                    if (!properties.Keys.Contains(value[0]) && value[0] != "$")
                        initialClass = "";

                    if (value[0] == "$")
                    {
                        foreach (var (field, i) in properties.Select((v, i) => (v, i)))
                        {
                            ConcatFilter(Params, expresion, string.Format("@P_{0}_", i + index), field.Key, value[1], column, initialClass,
                                typeProperty: field.Value, index: i);
                        }
                        break;
                    }
                    var paramName = string.Format("@P_{0}_", index);
                    ConcatFilter(Params, expresion, paramName, value[0], value[1], column, initialClass);

                }
                expresion.Append(")");
                _logger.LogInformation("{0}", expresion.ToString());

            }
            return expresion.ToString();
        }

        private void ConcatFilter(Dictionary<string, object> Params, StringBuilder expresion, string paramName,
            string key, string value, string column, string initialClass, Type typeProperty = null, int? index = null)
        {
            var select = "";
            var enable = true;
            var patternStr = @"\=|¬";
            if (typeProperty != null && typeProperty == typeof(string))
            {
                Params.Add(paramName, $"%{value.ToString()}%");
                select = string.Format("{0}{1} ilike {2}", initialClass, key, paramName);
            }
            else
            {
                if (int.TryParse(value, out int number))
                {
                    select = string.Format("{0}{1} = {2}", initialClass, key, paramName);
                    Params.Add(paramName, number);
                }
                else if (float.TryParse(value, out float fnumber))
                {
                    select = string.Format("{0}{1} = {2}", initialClass, key, paramName);
                    Params.Add(paramName, fnumber);
                }
                else if (double.TryParse(value, out double dnumber))
                {
                    select = string.Format("{0}{1} = {2}", initialClass, key, paramName);
                    Params.Add(paramName, dnumber);
                }
                else if (decimal.TryParse(value, out decimal denumber))
                {
                    select = string.Format("{0}{1} = {2}", initialClass, key, paramName);
                    Params.Add(paramName, denumber);
                }
                else if (bool.TryParse(value, out bool boolean))
                {
                    select = string.Format("{0}{1} = {2}", initialClass, key, paramName);
                    Params.Add(paramName, boolean);
                }
                else if (DateTime.TryParse(value, out DateTime dateTime) == true)
                {
                    Params.Add(paramName, dateTime);
                    select = string.Format("{0}{1}::date = {2}::date", initialClass, key, paramName);
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
                            Params.Add(paramName, value.ToString());
                            select = string.Format("{0}{1} = {2}", initialClass, key, paramName);
                        }
                        else
                        {
                            Params.Add(paramName, $"%{value.ToString()}%");
                            select = string.Format("{0}{1} ilike {2}", initialClass, key, paramName);
                        }
                    }
                }
            }

            if (enable)
            {
                if (index != null && index > 0 && expresion.Length > 1)
                    expresion.Append(" OR ");
                expresion.Append(select);
            }

        }

        private string Pagination<E>(string query, out PagedResultBase result,
           Dictionary<string, object> Params) where E : class
        {
            result = new PagedResultBase();
            StringBuilder st = new StringBuilder();
            var ParamsPagination = new Dictionary<string, object>();
            int count = Params == null ? 0 : Params.Count;

            // Pagination
            if (_contextAccessor.HttpContext.Request.Query.Any(x => x.Key.Equals("enable_pagination"))
                && bool.TryParse(_contextAccessor.HttpContext.Request.Query.FirstOrDefault(x => x.Key.Equals("enable_pagination")).Value.ToString(),
                   out bool enablePagination))
            {
                bool allowCache = true;
                if (_contextAccessor.HttpContext.Request.Query.Any(x => x.Key.Equals("filter_by")))
                {
                    var columnStr = _contextAccessor.HttpContext.Request.Query.FirstOrDefault(x => x.Key.Equals("filter_by")).Value.ToString();
                    string[] columns = columnStr.Split(';');
                    if (columns.Count() > 0) allowCache = false;
                }

                var pageSizeRequest = _contextAccessor.HttpContext.Request.Query.FirstOrDefault(x => x.Key.Equals("page_size")).Value;
                var currentPageRequest = _contextAccessor.HttpContext.Request.Query.FirstOrDefault(x => x.Key.Equals("current_page")).Value;
                int pageSize = string.IsNullOrEmpty(pageSizeRequest) ? 20 : int.Parse(pageSizeRequest);
                int pageNumber = string.IsNullOrEmpty(currentPageRequest) ? 1 : int.Parse(currentPageRequest);

                for (int i = 0; i < 2; i++)
                {
                    var param = $"@P_{count + i}_";
                    if (i == 0)
                    {
                        st.Append("LIMIT ");
                        st.Append(param);
                        ParamsPagination.Add(param, pageSize);
                    }
                    else
                    {
                        st.Append(" OFFSET ");
                        st.Append(param);
                        var value = (pageNumber * pageSize) - pageSize;
                        ParamsPagination.Add(param, value);
                    }
                }

                result.current_page = pageNumber;
                result.page_size = pageSize;

                int? rowCount = null;
                if (allowCache && count == 0)
                    rowCount = CacheGetOrCreate<E>();

                result.row_count = rowCount ?? GetCountDBAsync(query, Params).Result;

                var pageCount = (double)result.row_count / pageSize;
                result.page_count = (int)Math.Ceiling(pageCount);

                foreach (var param in ParamsPagination)
                    Params.Add(param.Key, param.Value);

                return st.ToString();
            }
            else
            {
                result = null;
            }
            return string.Empty;
        }

        private int CacheGetOrCreate<E>()
            where E : class
        {
            var cacheKeySize = string.Format("_{0}_size", typeof(E).Name);
            var cacheEntry = _cache.GetOrCreate(cacheKeySize, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromDays(1);
                entry.Size = 1000;
                return CountFromEF<E>();
            });

            return cacheEntry;
        }

        public int CountFromEF<E>() where E : class
        {
            string SqlConnectionStr = _config.GetConnectionString("PsqlConnection");
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql(SqlConnectionStr, o => o.UseNetTopologySuite());
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.UseLoggerFactory(MyLoggerFactory);

            using (var _db = new ApplicationDbContext(optionsBuilder.Options))
            {
                return _db.Set<E>().Count();
            }
        }

        public async Task<int> GetCountDBAsync(string query, Dictionary<string, object> Params = null)
        {
            string SqlConnectionStr = _config.GetConnectionString("PsqlConnection");
            string Query = @"select count(*) from ( " + query + " ) as p";
            Stopwatch sw = new Stopwatch();

            using (NpgsqlConnection _conn = new NpgsqlConnection(SqlConnectionStr))
            {
                try
                {
                    _conn.Open();
                    sw.Start();
                    using (var cmd = _conn.CreateCommand())
                    {
                        cmd.CommandText = Query;
                        cmd.CommandTimeout = 120;
                        cmd.Parameters.AddRange(cmd.SetSqlParamsPsqlSQL(Params, _logger));
                        _logger.LogInformation($"Executed DbCommand [Parameters=[{ParamsToString(cmd.Parameters.ToArray())}], " +
                            $"CommandType={cmd.CommandType}, CommandTimeout='{cmd.CommandTimeout}']\n" +
                            $"      Query\n      {cmd.CommandText}");
                        return int.Parse((await cmd.ExecuteScalarAsync()).ToString());
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogCritical("Error {0} {1} {2}\n{3}", ex.Message, ex.StackTrace, ex.Data, ex.InnerException);
                }
                finally
                {
                    //_conn.Close();
                    sw.Stop();
                    //_logger.LogDebug($"Closing connection to database {_context.GetType().name}");
                    _logger.LogDebug($"Executed DbCommand Time total: {sw.Elapsed} ({sw.Elapsed.Milliseconds}ms)");
                }
            }
            return 0;
        }

        public async Task<string> GetDataFromDBAsync<E>(string query, Dictionary<string, object> Params = null,
           string OrderBy = "", string GroupBy = "", bool commit = false, bool jObject = false, bool json = true)
            where E : class
        {
            string SqlConnectionStr = _config.GetConnectionString("PsqlConnection");
            StringBuilder sb = new StringBuilder();
            string Query = query;
            PagedResultBase pageResult = null;

            if (!commit)
            {
                if (_contextAccessor.HttpContext.Request.Query.Any(x => x.Key.Equals("select_args")))
                {
                    var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                    optionsBuilder.UseNpgsql(SqlConnectionStr, o => o.UseNetTopologySuite());
                    optionsBuilder.EnableSensitiveDataLogging();
                    optionsBuilder.UseLoggerFactory(MyLoggerFactory);

                    using (var _context = new ApplicationDbContext(optionsBuilder.Options))
                    {
                        return JsonSerializer.Serialize(await _context.Set<E>().SortFilterAsync(_contextAccessor, pagination: true));
                    }
                }

                Dictionary<string, object> ParamsRequest;
                var clauseWhere = Filter<E>(out ParamsRequest);

                if (Params == null)
                {
                    Params = new Dictionary<string, object>();
                    if (ParamsRequest.Count > 0)
                        Query = string.Format("{0}\nWHERE {1}", Query, clauseWhere);
                }
                else
                {
                    if (ParamsRequest.Count > 0)
                        Query = string.Format("{0}\nAND ({1})", Query, clauseWhere);
                }

                foreach (var param in ParamsRequest)
                    Params.Add(param.Key, param.Value);

                if (!string.IsNullOrEmpty(GroupBy))
                {
                    Query = string.Format("{0}\nGROUP BY {1}", Query, GroupBy);
                }

                // Order By
                if (_contextAccessor.HttpContext.Request.Query.Any(x => x.Key.Equals("order_by")))
                {
                    var currentClass = typeof(E).Name;
                    if (currentClass.Equals("ApplicationUser"))
                        currentClass = "user";
                    else if (currentClass.Equals("ApplicationRole"))
                        currentClass = "role";
                    var initialClass = currentClass.ToLower().First();

                    OrderBy = _contextAccessor.HttpContext.Request.Query.FirstOrDefault(x => x.Key.Equals("order_by")).Value.ToString();
                    string[] paramsOrderBy = OrderBy.Split(',');
                    OrderBy = string.Join(", ", paramsOrderBy.Select(x => $"{initialClass}." + x.Trim()));
                }

                if (!string.IsNullOrEmpty(OrderBy))
                {
                    Query = string.Format("{0}\nORDER BY {1}", Query, OrderBy);
                }

                // Pagination
                var paginate = Pagination<E>(Query, out pageResult, Params);

                if (!string.IsNullOrEmpty(paginate))
                    Query = string.Format("{0}\n{1}", Query, paginate);

                if (pageResult != null)
                {
                    sb.Append(JsonSerializer.Serialize(pageResult));
                    sb.Replace("}", ",", sb.Length - 2, 2);
                    sb.Append("\n\"results\": ");

                }

                if (json)
                {
                    if (jObject)
                        Query = string.Format(@"SELECT row_to_json(t) FROM ({0}) t", Query);
                    else
                        Query = string.Format(@"SELECT COALESCE(array_to_json(array_agg(row_to_json(t))), '[]') FROM ({0}) t", Query);
                }
            }

            Stopwatch sw = new Stopwatch();
            //var conn = _context.Database.GetDbConnection();

            using (NpgsqlConnection _conn = new NpgsqlConnection(SqlConnectionStr))
            {
                try
                {
                    _conn.Open();
                    //if (conn.State != ConnectionState.Open)
                    //{
                    //    await conn.OpenAsync();
                    //}

                    sw.Start();
                    using (var cmd = _conn.CreateCommand())
                    {
                        cmd.CommandText = Query;
                        cmd.CommandTimeout = 120;
                        cmd.Parameters.AddRange(cmd.SetSqlParamsPsqlSQL(Params, _logger));
                        _logger.LogInformation($"Executed DbCommand [Parameters=[{ParamsToString(cmd.Parameters.ToArray())}], " +
                            $"CommandType={cmd.CommandType}, CommandTimeout='{cmd.CommandTimeout}']\n" +
                            $"      Query\n      {cmd.CommandText}");
                        if (commit)
                        {
                            // Insert some data                            
                            await cmd.ExecuteNonQueryAsync();
                        }
                        else
                        {
                            var dataReader = cmd.ExecuteReader();
                            if (dataReader.HasRows)
                            {
                                if (!json)
                                {
                                    List<string> cols = new List<string>();
                                    int ncols = dataReader.FieldCount;
                                    for (int i = 0; i < ncols; ++i)
                                    {
                                        cols.Add(dataReader.GetName(i));
                                    }

                                    if (!jObject)
                                        sb.Append("[");

                                    //process each row
                                    var index = 0;
                                    while (await dataReader.ReadAsync())
                                    {
                                        index = 0;
                                        sb.Append("{");
                                        foreach (string col in cols)
                                        {
                                            if (dataReader.IsDBNull(index))
                                            {
                                                sb.AppendFormat("\"{0}\":null, ", col);
                                            }
                                            else if (dataReader.GetFieldType(index) == typeof(string))
                                            {
                                                if (!string.IsNullOrEmpty(dataReader[col].ToString())
                                                    && (dataReader[col].ToString().First().Equals('{')
                                                    || dataReader[col].ToString().First().Equals('[')))
                                                    sb.AppendFormat("\"{0}\":{1}, ", col, dataReader[col]);
                                                else
                                                    sb.AppendFormat("\"{0}\":\"{1}\", ", col, dataReader[col]);
                                            }
                                            else if (dataReader.GetFieldType(index) == typeof(Array))
                                            {
                                                sb.AppendFormat("\"{0}\":[{1}], ", col, string.Join(",", dataReader[col] as int[]));
                                            }
                                            else if (dataReader.GetFieldType(index) == typeof(bool))
                                            {
                                                sb.AppendFormat("\"{0}\":{1}, ", col, dataReader[col].ToString().ToLower());
                                            }
                                            else if (dataReader.GetFieldType(index) == typeof(DateTime))
                                            {
                                                sb.AppendFormat("\"{0}\":\"{1}\", ", col, ((DateTime)dataReader[col]).ToString("o"));
                                            }
                                            else if (dataReader.GetFieldType(index) == typeof(TimeSpan))
                                            {
                                                sb.AppendFormat("\"{0}\":\"{1}\", ", col, dataReader[col]);
                                            }
                                            else
                                                sb.AppendFormat("\"{0}\":{1}, ", col, dataReader[col]);
                                            index++;
                                        }
                                        sb.Replace(", ", "},", sb.Length - 2, 2);
                                    }

                                    if (!jObject)
                                        sb.Replace("},", "}]", sb.Length - 2, 2);
                                    else
                                        sb.Replace("},", "}", sb.Length - 2, 2);
                                }
                                else
                                {
                                    while (await dataReader.ReadAsync())
                                    {
                                        if (dataReader.IsDBNull(0))
                                        {
                                            sb.Append("");
                                            continue;
                                        }
                                        if (dataReader.GetFieldType(0) == typeof(string))
                                        {
                                            sb.Append(dataReader.GetString(0));
                                        }
                                        else if (dataReader.GetFieldType(0) == typeof(int))
                                        {
                                            sb.Append(dataReader.GetInt32(0));
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (!jObject)
                                    sb.Append("[]");
                            }
                            dataReader.Dispose();
                            dataReader.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogCritical("Error {0} {1} {2}\n{3}", ex.Message, ex.StackTrace, ex.Data, ex.InnerException);
                    throw ex;
                }
                finally
                {
                    //_conn.Close();
                    sw.Stop();
                    //_logger.LogDebug($"Closing connection to database {_context.GetType().name}");
                    _logger.LogDebug($"Executed DbCommand Time total: {sw.Elapsed} ({sw.Elapsed.Milliseconds}ms)");
                }
            }
            if (pageResult != null)
            {
                sb.Append("}");
            }
            string response = sb.ToString();
            //if (jObject == false)
            //{
            //    if (response.Equals("[]"))
            //        response = string.Empty;
            //}
            //else
            if (jObject == true)
            {
                if (response.Equals("{}"))
                    response = string.Empty;
            }
            return response;
        }

        private static string ParamsToString(NpgsqlParameter[] dictionary)
        {
            return "{" + string.Join(",", dictionary.Select(kv => kv.ParameterName + "=" + kv.Value).ToArray()) + "}";
        }
    }

}