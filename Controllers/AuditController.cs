using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections;
using System.Threading.Tasks;
using IdentityServer4.AccessTokenValidation;
using SerAPI.Managers;
using SerAPI.Utils;
using SerAPI.Utils.CustomFilters;

namespace SerAPI.Controllers
{
    /// <summary>
    /// This class is used as an api for the Audit requests.
    /// </summary>
    [Produces("application/json")]
    [Route("api/Audit")]
    [Authorize(Roles = "Administrador, Super-User",
        AuthenticationSchemes = IdentityServerAuthenticationDefaults.AuthenticationScheme)]
    public class AuditController : Controller
    {
        private readonly AuditManager _cRepository;

        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="cRepository"></param>
        public AuditController(AuditManager cRepository)
        {
            _cRepository = cRepository;
        }

        // GET: api/Audit
        /// <summary>
        /// This method returns all Audits
        /// </summary>
        /// <param name="fromDate">fromDate</param>
        /// <param name="toDate">toDate</param>
        /// <returns>All Audits which were found</returns>
        [HttpGet]
        [CustomAuthorize(CustomClaimTypes.Permission, "audits.view", Roles: "Super-User")]
        public async Task<IEnumerable> GetAudit(string fromDate, string toDate, string option, string userName, string role)
        {
            return await ((AuditManager)_cRepository).All(fromDate, toDate, option: option, userName: userName, role: role);
        }
    }
}
