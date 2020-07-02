using SerAPI.Models;
using SerAPI.Services;
using SerAPI.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using SerAPI.Utils;

namespace SerAPI.Managers
{
    public class AttachmentManager : GenericModelFactory<Attachment>
    {

        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;
        private readonly PostgresQLService _postgresQLService;

        public AttachmentManager(
            ApplicationDbContext db,
            ILogger<AttachmentManager> logger,
            IHttpContextAccessor httpContextAccessor,
            AuditManager cRepositoryLog,
            PostgresQLService postgresQLService,
            IConfiguration config)
            : base(db, logger, httpContextAccessor, cRepositoryLog, config)
        {
            _context = db;
            _logger = logger;
            _postgresQLService = postgresQLService;
        }

        /// <summary>
        /// Este metodo obtiene los datos de un request de type-content=form dara
        /// con la finalidad de guardar archivos media como imagenes en el servidor
        /// y demas parametros
        /// </summary>
        /// <param name="file"></param>
        /// <param name="entity"></param>
        /// <param name="company_id"></param>
        /// <returns></returns>
        internal async Task<Attachment> AddAttachmentAsync(IFormFile file, Attachment entity)
        {
            try
            {
                AWSS3Response fileInfo = await UploadS3File(file, $"/attachments/{entity.model}/");
                entity.key = fileInfo.Key;
            }
            catch (Exception e)
            {
                _logger.LogError("Error", "Message: {0}", e);
                return null;
            }
            
            entity.update_date = DateTime.Now;

            return await base.Add(entity);
        }

        internal async Task<Attachment> DeleteAttachmentAsync(int id)
        {
            var entity = await _context.attachments.FindAsync(id);
            if (entity == null) return null;
            try
            {
                await DeleteS3File(entity.key);

            }
            catch (Exception e)
            {
                _logger.LogError("Error", "Message: {0}", e);
                return null;
            }

            return await base.Remove(x => x.id == entity.id);
        }


    }
}
