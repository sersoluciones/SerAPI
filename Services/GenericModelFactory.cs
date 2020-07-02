using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SerAPI.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using IdentityModel;
using System.Security.Claims;
using OfficeOpenXml;
using OfficeOpenXml.Table;
using SerAPI.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using IdentityServer4.Extensions;
using SerAPI.Managers;
using SerAPI.Utils;

namespace SerAPI.Services
{
    public class GenericModelFactory<T> : IRepository<T>
    where T : class
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger _logger;
        private readonly IHttpContextAccessor contextAccessor;
        private readonly AuditManager _cRepositoryLog;
        private IConfiguration _config;
        private IMemoryCache _cache;
        public string model;

        public GenericModelFactory(ApplicationDbContext db,
            ILogger<GenericModelFactory<T>> logger,
            IHttpContextAccessor httpContextAccessor,
            AuditManager cRepositoryLog,
            IConfiguration config)
        {
            _db = db;
            _logger = logger;
            _config = config;
            contextAccessor = httpContextAccessor;
            _cRepositoryLog = cRepositoryLog;
            model = typeof(T).Name;
            _cache = httpContextAccessor.HttpContext.RequestServices.GetService<IMemoryCache>();
        }

        public int? GetCompanyIdUser()
        {
            if (contextAccessor.HttpContext.User.IsAuthenticated())
                return int.Parse(contextAccessor.HttpContext.User.Claims.FirstOrDefault(x => x.Type == CustomClaimTypes.CompanyId)?.Value);
            return null;
        }

        public string GetCurrentUser()
        {
            if (contextAccessor.HttpContext.User.IsAuthenticated())
                return contextAccessor.HttpContext.User.Claims.FirstOrDefault(x =>
                x.Type == JwtClaimTypes.Subject).Value;
            return null;
        }

        public string GetCurrenUserName()
        {
            if (contextAccessor.HttpContext.User.IsAuthenticated())
                return contextAccessor.HttpContext.User.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Name).Value;
            return null;
        }

        public List<string> GetRolesUser()
        {
            return contextAccessor.HttpContext.User.Claims.Where(x =>
                x.Type == JwtClaimTypes.Role).Select(x => x.Value).ToList();
        }

        public virtual async Task<dynamic> GetAll()
        {
            return await GetModel
                .SortFilterAsync(contextAccessor, true);
        }

        public virtual async Task<T> Find(Expression<Func<T, bool>> condition)
        {
            /*if (!await Exist(condition))
            {
                return null;
            }*/
            //return await GetModel.FindAsync(condition);
            return await GetObj<T>(condition);
        }

        public virtual async Task<T> Add(T entity)
        {
            var obj = await GetModel.AddAsync(entity);

            try
            {
                await _db.SaveChangesAsync();
                entity = obj.Entity;

                var cacheKeySize = string.Format("_{0}_size", model);
                _cache.Remove(cacheKeySize);

                if (entity is BaseModel)
                {
                    await ((AuditManager)_cRepositoryLog).AddLog(new AuditBinding()
                    {
                        action = AuditManager.CREATE,
                        objeto = this.model
                    }, id: (entity as BaseModel).id.ToString(), commit: true);
                }
            }
            catch (DbUpdateException error)
            {
                _logger.LogCritical(0, "Unable to save changes. " +
                    "Try again, and if the problem persists " +
                    "see your system administrator.Error: {0} {1}", error.Message, error.InnerException);
                return null;
            }
            finally
            {
                _db.Entry(entity).State = EntityState.Detached;
            }

            return entity;
        }

        public virtual async Task<T> Update(T entity, Expression<Func<T, bool>> condition)
        {
            if (!await Exist<T>(condition))
            {
                return null;
            }
            //GetModel.Update(entity);
            if (entity is BaseModel)
            {
                await ((AuditManager)_cRepositoryLog).AddLog(new AuditBinding()
                {
                    action = AuditManager.UPDATE,
                    objeto = this.model
                }, id: (entity as BaseModel).id.ToString());
            }
            _db.Entry(entity).State = EntityState.Modified;
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException error)
            {
                _logger.LogCritical(0, $"Unable to save changes. Optimistic concurrency failure, " +
                     $"object has been modified\n {error.Message} {error.InnerException}");
                error.Entries.Single().Reload();
                foreach (var entry in error.Entries)
                {
                    if (entry.Entity is T)
                    {
                        GetModel.Update(entity);
                    }
                }
                try
                {
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateException e)
                {
                    _logger.LogCritical(0, "Unable to save changes. " +
                    "Try again, and if the problem persists " +
                    "see your system administrator.Error: {0} {1}", e.Message, e.InnerException);
                    return null;
                }
            }
            finally
            {
                _db.Entry(entity).State = EntityState.Detached;
            }

            return entity;
        }

        public virtual async Task<T> SelfUpdate(Expression<Func<T, bool>> condition, T entity)
        {
            var obj = await GetModel.FirstOrDefaultAsync(condition);
            if (obj != null)
            {
                foreach (var propertyInfo in typeof(T).GetProperties())
                {
                    if (propertyInfo.Name == "id") continue;
                    try
                    {
                        var oldValue = propertyInfo.GetValue(obj);
                        var newValue = propertyInfo.GetValue(entity);
                        var currentpropertyInfo = obj.GetType().GetProperty(propertyInfo.Name);
                        if (currentpropertyInfo != null)
                        {
                            currentpropertyInfo.SetValue(obj, newValue, null);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e.ToString());
                    }
                }

                _db.Entry(obj).State = EntityState.Modified;
                await _db.SaveChangesAsync();
            }
            return obj;
        }


        public virtual async Task<T> Remove(Expression<Func<T, bool>> condition)
        {
            if (!await Exist<T>(condition))
            {
                return null;
            }
            var entity = await GetObj<T>(condition);
            //_db.Entry(entity).State = EntityState.Deleted;
            GetModel.Remove(entity);
            try
            {
                await _db.SaveChangesAsync();

                var cacheKeySize = string.Format("_{0}_size", model);
                _cache.Remove(cacheKeySize);

                if (entity is BaseModel)
                {
                    await ((AuditManager)_cRepositoryLog).AddLog(new AuditBinding()
                    {
                        action = AuditManager.DELETE,
                        objeto = this.model
                    }, id: (entity as BaseModel).id.ToString(), commit: true);
                }
            }
            catch (DbUpdateConcurrencyException error)
            {
                _logger.LogCritical(0, $"Unable to save changes. Optimistic concurrency failure, " +
                    $"object has been modified\n {error.Message} {error.InnerException}");
                error.Entries.Single().Reload();
                foreach (var entry in error.Entries)
                {

                    if (entry.Entity is T)
                    {
                        GetModel.Remove(entity);
                    }
                }
                try
                {
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateException e)
                {
                    _logger.LogCritical(0, "Unable to save changes. " +
                    "Try again, and if the problem persists " +
                    "see your system administrator.Error: {0} {1}", e.Message, e.InnerException);
                    return null;
                }
            }
            return entity;
        }

        public virtual async Task<T> UploadFileAsync(IFormFile file, Expression<Func<T, bool>> condition, string propertyName, string S3Path)
        {
            var entity = await _db.Set<T>().SingleOrDefaultAsync(condition);
            if (entity == null) return null;

            var propertyInfo = typeof(T).GetProperty(propertyName);
            if (propertyInfo == null) return null;
            var oldValue = (string)propertyInfo.GetValue(entity);

            if (!string.IsNullOrEmpty(oldValue)) await DeleteS3File(oldValue);
            AWSS3Response fileInfo = await UploadS3File(file, S3Path);
            propertyInfo.SetValue(entity, fileInfo.Key, null);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return entity;
        }

        public async Task Clear()
        {
            GetModel.RemoveRange(GetModel);
            await _db.SaveChangesAsync();
        }

        private DbSet<T> GetModel
        {
            get { return _db.Set<T>(); }
        }

        public virtual DbSet<T> GetDbSet() => GetModel;
        public virtual IQueryable<T> GetQueryable() => GetModel;

        public ApplicationDbContext GetDbContext() => _db;

        public async Task<bool> Exist<E>(Expression<Func<E, bool>> condition)
            where E : class
        {
            return await _db.Set<E>().AsNoTracking().AnyAsync(condition);
        }

        public bool Exists<E>(E entity)
            where E : class
        {
            return _db.Set<E>().AsNoTracking().Count(x => x == entity) > 0;
        }

        public async Task<E> GetObj<E>(Expression<Func<E, bool>> condition)
            where E : class
        {
            return await _db.Set<E>().SingleOrDefaultAsync(condition);
        }

        public dynamic GetPaged<E>(IQueryable<E> source) where E : class
        {
            bool allowCache = true;
            if (contextAccessor.HttpContext.Request.Query.Any(x => x.Key.Equals("filter_by")))
            {
                var columnStr = contextAccessor.HttpContext.Request.Query.FirstOrDefault(x => x.Key.Equals("filter_by")).Value.ToString();
                string[] columns = columnStr.Split(';');
                if (columns.Count() > 0) allowCache = false;
            }

            if (contextAccessor.HttpContext.Request.Query.Any(x => x.Key.Equals("enable_pagination"))
            && bool.TryParse(contextAccessor.HttpContext.Request.Query.FirstOrDefault(x => x.Key.Equals("enable_pagination")).Value.ToString(),
               out bool enablePagination))
            {
                var pageSizeRequest = contextAccessor.HttpContext.Request.Query.FirstOrDefault(x => x.Key.Equals("page_size")).Value;
                var currentPageRequest = contextAccessor.HttpContext.Request.Query.FirstOrDefault(x => x.Key.Equals("current_page")).Value;
                int pageSize = string.IsNullOrEmpty(pageSizeRequest) ? 20 : int.Parse(pageSizeRequest);
                int pageNumber = string.IsNullOrEmpty(currentPageRequest) ? 1 : int.Parse(currentPageRequest);

                var result = new PagedResult<E>();

                result.current_page = pageNumber;
                result.page_size = pageSize;

                int? rowCount = null;
                if (allowCache)
                    rowCount = CacheGetOrCreate(GetModel);

                result.row_count = rowCount ?? source.CountAsync().Result;

                var pageCount = (double)result.row_count / pageSize;
                result.page_count = (int)Math.Ceiling(pageCount);

                result.results = source.Skip((pageNumber - 1) * pageSize).Take(pageSize).AsNoTracking().ToList();
                return result;
            }
            return source.AsNoTracking().ToList();
        }

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
            _logger.LogInformation($"cacheKeySize: {cacheKeySize} cacheEntry {cacheEntry}");
            return cacheEntry;
        }

        /// <summary>
        /// Upload service for Amazon S3
        /// </summary>
        /// <param name="file">IForm Instance</param>
        /// <param name="path">Path desired in the Bucket</param>
        /// <param name="name">Alternative name, if is empty, the service generate a guid name</param>
        /// <returns>string[]</returns>
        public async Task<AWSS3Response> UploadS3File(IFormFile file, string path, string name = "")
        {

            string FileName;
            string FileExtension = Path.GetExtension(file.FileName).ToLower();

            if (string.IsNullOrEmpty(name))
            {
                FileName = string.Format("file_{0}{1}", Guid.NewGuid(), FileExtension);
            }
            else
            {
                FileName = name + FileExtension;
            }

            string key = _config.GetSection("AWS").GetSection("S3").GetSection("Folder").Value + path + FileName;

            _logger.LogDebug(key);

            try
            {
                using (var client = new AmazonS3Client(
                    _config.GetSection("AWS").GetSection("AccessKeyId").Value,
                    _config.GetSection("AWS").GetSection("SecretAccessKey").Value,
                    RegionEndpoint.USEast1))
                {
                    using (var newMemoryStream = new MemoryStream())
                    {
                        file.CopyTo(newMemoryStream);

                        var uploadRequest = new TransferUtilityUploadRequest
                        {
                            InputStream = newMemoryStream,
                            Key = key,
                            BucketName = _config.GetSection("AWS").GetSection("S3").GetSection("Bucket").Value,
                            CannedACL = S3CannedACL.NoACL
                        };

                        var fileTransferUtility = new TransferUtility(client);
                        await fileTransferUtility.UploadAsync(uploadRequest);

                        return new AWSS3Response()
                        {
                            Key = key,
                            Path = path,
                            FileName = FileName
                        };
                    }
                }
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                _logger.LogError(amazonS3Exception.ErrorCode.ToString());
                if (amazonS3Exception.ErrorCode != null &&
                (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") ||
                amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    _logger.LogError("Please check the provided AWS Credentials.");
                }
                else
                {
                    _logger.LogError("An error occurred with the message '{0}' when upload an object", amazonS3Exception.Message);
                }

                throw;
            }
        }

        /// <summary>
        /// Upload service for Amazon S3
        /// </summary>
        /// <param name="stream">IForm Instance</param>
        /// <param name="path">Path desired in the Bucket</param>
        /// <param name="name">Alternative name, if is empty, the service generate a guid name</param>
        /// <returns>string[]</returns>
        public async Task<AWSS3Response> UploadS3File(MemoryStream stream, string path, string name = "", string ext = "")
        {

            string FileName;

            if (string.IsNullOrEmpty(name))
            {
                FileName = string.Format("file_{0}{1}", Guid.NewGuid(), ext);
            }
            else
            {
                FileName = name + ext;
            }

            string key = _config["AWS:S3:Folder"] + path + FileName;

            _logger.LogDebug("Uploading: " + key);

            try
            {
                using (var client = new AmazonS3Client(
                    _config["AWS:AccessKeyId"],
                    _config["AWS:SecretAccessKey"],
                    RegionEndpoint.USEast1))
                {
                    var uploadRequest = new TransferUtilityUploadRequest
                    {
                        InputStream = stream,
                        Key = key,
                        BucketName = _config["AWS:S3:Bucket"],
                        CannedACL = S3CannedACL.NoACL
                    };

                    var fileTransferUtility = new TransferUtility(client);
                    await fileTransferUtility.UploadAsync(uploadRequest);

                    return new AWSS3Response()
                    {
                        Key = key,
                        Path = path,
                        FileName = FileName
                    };
                }
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                _logger.LogError(amazonS3Exception.ErrorCode.ToString());
                if (amazonS3Exception.ErrorCode != null &&
                (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") ||
                amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    _logger.LogError("Please check the provided AWS Credentials.");
                }
                else
                {
                    _logger.LogError("An error occurred with the message '{0}' when upload an object", amazonS3Exception.Message);
                }

                throw;
            }
        }

        /// <summary>
        /// Delete service for Amazon S3
        /// </summary>
        /// <param name="key">Path of the file in the Bucket</param>
        /// <returns>string[]</returns>
        public async Task<bool> DeleteS3File(string key)
        {

            _logger.LogDebug(key);

            try
            {
                using (var client = new AmazonS3Client(
                    _config.GetSection("AWS").GetSection("AccessKeyId").Value,
                    _config.GetSection("AWS").GetSection("SecretAccessKey").Value,
                    RegionEndpoint.USEast1))
                {
                    DeleteObjectRequest request = new DeleteObjectRequest()
                    {
                        BucketName = _config.GetSection("AWS").GetSection("S3").GetSection("Bucket").Value,
                        Key = key
                    };

                    await client.DeleteObjectAsync(request);

                    return true;
                }
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                _logger.LogError(amazonS3Exception.ErrorCode.ToString());
                if (amazonS3Exception.ErrorCode != null &&
                (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") ||
                amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    _logger.LogError("Please check the provided AWS Credentials.");
                }
                else
                {
                    _logger.LogError("An error occurred with the message '{0}' when upload an object", amazonS3Exception.Message);
                }

                return false;
            }
        }


        #region Just use if AWS S3 is not available for this project
        public async Task<string> SaveFileInServer(string fileName, string path, byte[] bytes)
        {
            var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), path);
            if (!Directory.Exists(webRootPath))
            {
                _logger.LogInformation("That path not exists already.");
                // Try to create the directory.
                DirectoryInfo di = Directory.CreateDirectory(webRootPath);
                _logger.LogInformation("The directory was created successfully at {0}.", Directory.GetCreationTime(webRootPath));

            }
            var filePath = webRootPath + $@"/{fileName}";
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await stream.WriteAsync(bytes, 0, bytes.Length);
            }
            return filePath;
        }

        public async Task<string[]> SaveFile(IFormFile file, string path, string name = "")
        {
            if (file.Length > 0)
            {
                var fileName = file.FileName.Trim();
                var extFile = Path.GetExtension(fileName);
                string fileNameGuid;
                if (string.IsNullOrEmpty(name))
                    fileNameGuid = string.Format("file_{0}{1}", Guid.NewGuid(), extFile.Trim('"'));
                else
                    fileNameGuid = string.Format("file_{0}{1}", name, extFile.Trim('"'));

                var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), path);
                if (!Directory.Exists(webRootPath))
                {
                    _logger.LogInformation("That path not exists already.");
                    // Try to create the directory.
                    DirectoryInfo di = Directory.CreateDirectory(webRootPath);
                    _logger.LogInformation("The directory was created successfully at {0}.", Directory.GetCreationTime(webRootPath));

                }
                var filePath = webRootPath + $@"/{fileNameGuid}";
                _logger.LogCritical(LoggingEvents.GET_ITEM, "Copied the uploaded file {filePath}", filePath);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                return new string[] { fileNameGuid, filePath };
            }
            return new string[] { "", "" };
        }

        //protected string[] SaveImage(HttpRequest Request, string path)
        //{
        //    long size = 0;
        //    var files = Request.Form.Files;
        //    var filePath = "";
        //    string fileNameGuid = "";
        //    foreach (var file in files)
        //    {
        //        string fileName = ContentDispositionHeaderValue
        //                        .Parse(file.ContentDisposition)
        //                        .FileName
        //                        .Trim().Value;
        //        var extFile = Path.GetExtension(fileName);
        //        fileNameGuid = string.Format("image_{0}{1}", Guid.NewGuid(), extFile);

        //        var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), path);
        //        filePath = webRootPath + $@"\{fileNameGuid}";
        //        size += file.Length;
        //        using (FileStream fs = File.Create(filePath))
        //        {
        //            file.CopyTo(fs);
        //            fs.Flush();
        //        }
        //        _logger.LogCritical(LoggingEvents.GET_ITEM, "Copied the uploaded image file {filePath}", filePath);
        //        break;
        //    }
        //    return new string[] { fileNameGuid, filePath };
        //}
        #endregion

        public string makeParamsQuery(List<string> contentValues, bool start = false)
        {
            StringBuilder result = new StringBuilder();
            bool first = true;
            foreach (string data in contentValues)
            {
                if (!start)
                {
                    if (first)
                    {
                        result.Append(" WHERE ");
                        first = false;
                    }
                    else
                        result.Append(" AND ");
                }
                else
                {
                    result.Append(" AND ");
                }
                result.Append(data);
            }
            return result.ToString();
        }

        public async Task<object> GetDataFromPsqlDB(PostgresQLService postgresService, string modelName,
            string columnStr = "", string orderBy = "", string parameters = "", bool download = false)
        {
            if (!string.IsNullOrEmpty(columnStr))
            {
                string[] columns = columnStr.Split(',');
                columnStr = string.Join("\",\"", columns);
                columnStr = string.Format("\"{0}\"", columnStr).Replace(" ", string.Empty);
            }
            else
            {
                columnStr = "*";
            }

            JArray jArray = new JArray();
            string Query = $"SELECT {columnStr} FROM \"{modelName}\"";
            var Params = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(parameters))
            {
                JObject jObjParams = JObject.Parse(parameters);
                int index = 0;
                foreach (JProperty pair in jObjParams.Properties())
                {
                    string propertyName = pair.Name;

                    switch (pair.Value.Type)
                    {
                        case JTokenType.String:
                            Params.Add(string.Format("@{0}", propertyName), (string)pair.Value);
                            break;
                        case JTokenType.Integer:
                            Params.Add(string.Format("@{0}", propertyName), (int)pair.Value);
                            break;
                        case JTokenType.Boolean:
                            Params.Add(string.Format("@{0}", propertyName), (bool)pair.Value);
                            break;
                        case JTokenType.Float:
                            Params.Add(string.Format("@{0}", propertyName), (float)pair.Value);
                            break;
                    }

                    if (index == 0)
                        Query = string.Format(@"{0} WHERE ""{1}"" = @{1}", Query, propertyName);
                    else
                        Query = string.Format(@"{0} AND ""{1}"" = @{1}", Query, propertyName);

                    index++;
                }
            }

            if (!string.IsNullOrEmpty(orderBy))
            {
                Query = string.Format("{0} ORDER BY \"{1}\"", Query, orderBy);
            }

            var result = await postgresService.GetDataFromDBAsync<T>(Query, Params: string.IsNullOrEmpty(parameters) ? null : Params);

            if (!string.IsNullOrEmpty(result))
            {
                jArray = JArray.Parse(result);
            }

            if (download)
            {
                return GenerateExcel(jArray, modelName);
            }

            return jArray;
        }

        public byte[] GenerateExcel(JArray jArray, String modelName)
        {
            if (jArray.Count == 0) return null;
            byte[] bytes;
            MemoryStream stream = new MemoryStream();
            using (ExcelPackage package = new ExcelPackage(stream))
            {
                int column = 1;
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add(modelName);
                //First add the headers
                int row = 1;
                foreach (JObject parsedObject in jArray.Children<JObject>())
                {
                    column = 1;
                    foreach (JProperty pair in parsedObject.Properties())
                    {
                        string propertyName = pair.Name;
                        worksheet.Cells[row, column].Value = propertyName.ToUpper();
                        column++;
                    }
                }
                row++;

                var numberformat = "#,##0";
                //var dataCellStyleName = "TableNumber";
                //var numStyle = package.Workbook.Styles.CreateNamedStyle(dataCellStyleName);
                //numStyle.Style.Numberformat.Format = numberformat;

                foreach (JObject parsedObject in jArray.Children<JObject>())
                {
                    column = 1;
                    foreach (JProperty pair in parsedObject.Properties())
                    {
                        string propertyName = pair.Name;
                        //_logger.LogInformation($"Name: {propertyName}, Type: {parsedObject[propertyName].Type}, Value: {pair.Value}");

                        if (pair.Value.Type == JTokenType.None || pair.Value.Type == JTokenType.Null)
                        {
                            worksheet.Cells[row, column].Value = string.Empty;
                        }
                        else if (pair.Value.Type == JTokenType.String)
                        {
                            worksheet.Cells[row, column].Value = (string)pair.Value;
                        }
                        else if (pair.Value.Type == JTokenType.Integer)
                        {
                            numberformat = "#";
                            worksheet.Cells[row, column].Style.Numberformat.Format = numberformat;
                            worksheet.Cells[row, column].Value = (int)pair.Value;
                        }
                        else if (pair.Value.Type == JTokenType.Boolean)
                        {
                            worksheet.Cells[row, column].Value = (bool)pair.Value;
                        }
                        else if (pair.Value.Type == JTokenType.Float)
                        {
                            numberformat = "#,###0";
                            worksheet.Cells[row, column].Style.Numberformat.Format = numberformat;
                            worksheet.Cells[row, column].Value = (float)pair.Value;
                        }
                        else if (pair.Value.Type == JTokenType.Date)
                        {
                            worksheet.Cells[row, column].Style.Numberformat.Format = "yyyy-mm-dd HH:MM:ss";
                            worksheet.Cells[row, column].Value = (DateTime)pair.Value;
                        }
                        else if (pair.Value.Type == JTokenType.Array)
                        {
                            worksheet.Cells[row, column].Value = ((JArray)pair.Value).ToString(Newtonsoft.Json.Formatting.None);
                        }
                        else
                        {
                            worksheet.Cells[row, column].Value = pair.Value;
                        }
                        column++;
                    }
                    row++;
                }

                // Add to table / Add summary row
                var tbl = worksheet.Tables.Add(new ExcelAddressBase(fromRow: 1, fromCol: 1, toRow: row - 1, toColumn: column - 1), "Data");
                tbl.ShowHeader = true;
                tbl.ShowTotal = true;
                tbl.TableStyle = TableStyles.None;

                //tbl.Columns[3].DataCellStyleName = dataCellStyleName;
                //tbl.Columns[3].TotalsRowFunction = RowFunctions.Sum;
                //worksheet.Cells[5, 4].Style.Numberformat.Format = "#,##0";

                // AutoFitColumns
                worksheet.Cells[1, 1, row - 1, column - 1].AutoFitColumns();
                bytes = package.GetAsByteArray();
            }
            return bytes;
        }
    }
}
