using System.Threading.Tasks;
using SerAPI.Managers;
using SerAPI.Models;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json.Linq;
using SerAPI.Resources;

namespace SerAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/xlsx")]
    [Authorize(AuthenticationSchemes = IdentityServerAuthenticationDefaults.AuthenticationScheme)]
    public class XlsxController : Controller
    {
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly IRepository<Xlsx> _cRepository;

        public XlsxController(IStringLocalizer<SharedResource> localizer,
            IRepository<Xlsx> cRepository)
        {
            _localizer = localizer;
            _cRepository = cRepository;
        }

        //GET: api/xlsx
        /// <summary>
        /// Descargar datos serializados en JSON ó XLSX
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Download(string modelName, string columnStr, string orderBy, string parameters, string format)
        {
            JArray obj = (JArray)await ((XlsxManager)_cRepository).GetDataFromDB(modelName,
                               columnStr: columnStr,
                               orderBy: orderBy,
                               parameters: parameters);

            switch (format)
            {
                case "json":
                    return Ok(obj);

                case "xlsx":
                    var file = await ((XlsxManager)_cRepository).GenerateXlsx(obj, modelName);
                    return Ok(file);

                default:
                    return Ok(obj);
            }
        }

        //GET: api/xlsx/Download/{modelName}/
        /// <summary>
        /// Descargar datos serializados o en xlsx
        /// </summary>
        [HttpGet]
        [Route("[action]/{modelName}")]
        public async Task<IActionResult> Download([FromRoute]string modelName, string columnStr, string orderBy,
            bool download, string parameters)
        {
            var obj = await ((XlsxManager)_cRepository).GetDataFromDB(modelName, columnStr: columnStr,
                orderBy: orderBy, download: true, parameters: parameters);

            // if (download)
            // {
                string fileName = $"{modelName}.xlsx";
                string path = string.Format("wwwroot/files/{0}", modelName);
                string filePath = await ((XlsxManager)_cRepository).SaveFileInServer(fileName, path, (byte[])obj);
                /* JObject jsonObject = new JObject(
                  new JProperty("Path", string.Format("files/{0}/{1}", modelName, fileName))
                  ); */
                return Ok(string.Format("files/{0}/{1}", modelName, fileName));
                //return File((byte[])obj, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", modelName + ".xlsx");
            // }

            // return Ok((JArray)obj);
        }
    }
}