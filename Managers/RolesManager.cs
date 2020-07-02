using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SerAPI.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SerAPI.Data;
using Microsoft.Extensions.Caching.Memory;
using SerAPI.Services;
using SerAPI.Utils;
using SerAPI.Models.ViewModels;

namespace SerAPI.Managers
{
    public class RolesManager : GenericModelFactory<ApplicationRole>
    {
        public readonly ApplicationDbContext _context;
        private readonly ILogger _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly AuditManager _cRepositoryLog;
        private readonly PostgresQLService _postgresQLService;
        private readonly IMemoryCache _cache;
        private new readonly string model = "Role";

        public RolesManager(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            ApplicationDbContext db,
            ILogger<RolesManager> logger,
            IHttpContextAccessor httpContextAccessor,
            AuditManager cRepositoryLog,
            PostgresQLService postgresQLService,
            IMemoryCache cache,
            IConfiguration config)
            : base(db, logger, httpContextAccessor, cRepositoryLog, config)
        {
            _context = db;
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
            _cRepositoryLog = cRepositoryLog;
            _postgresQLService = postgresQLService;
            _cache = cache;
        }


        public async Task<string> All(string select = "", string id = "")
        {
            string Query = @"
            select r.id, r.name, r.normalized_name, r.is_lock,
            coalesce((
                select array_to_json(array_agg(row_to_json(d)))
                from(
                    select rc.id, rc.claim_value, rc.claim_type
                    from asp_net_role_claims as rc
                    where rc.role_id = r.id
                ) d
            ), '[]') as permissions
            from asp_net_roles as r";

            var jObj = false;
            var Params = new Dictionary<string, object>();
            List<string> contentValues = new List<string>();

            if (!string.IsNullOrEmpty(select))
            {
                Query = @"select id, name from asp_net_roles as r";
            }

            if (!GetRolesUser().Any(x => x == Constantes.SuperUser))
            {
                contentValues.Add(@"name <> @RoleName");
                Params.Add("@RoleName", Constantes.SuperUser);
            }

            if (!string.IsNullOrEmpty(id))
            {
                jObj = true;
                contentValues.Add(@"r.id = @id");
                Params.Add("@id", id);
            }

            if (contentValues.Count > 0)
                Query += makeParamsQuery(contentValues);

            return await _postgresQLService.GetDataFromDBAsync<ApplicationRole>(Query, Params: Params.Count == 0 ? null : Params, jObject: jObj);
        }

        public async Task<string> Find(string id)
        {
            var result = await All(id: id);
            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }
            else return null;
        }

        public async Task<object> Add(RoleBindingModel model)
        {
            //if (await _roleManager.RoleExistsAsync(model.Name))
            //{
            //    return string.Format("Role name '{}' is already taken.", model.Name);
            //}
            var role = new ApplicationRole(model.name);
            try
            {
                IdentityResult result = await _roleManager.CreateAsync(role);
                if (!result.Succeeded)
                {
                    return result;
                }
                else _logger.LogInformation(LoggingEvents.INSERT_ITEM, "Rol creado exitosamente");
            }
            catch (DbUpdateException)
            {
                if (await Exist<ApplicationRole>(x => x.Id == role.Id))
                {
                    return null;
                }
                else
                {
                    return null;
                }
            }
            return role;
        }

        public async Task<object> Update(string id, EditRoleBindingModel model)
        {
            var role = await _roleManager.FindByIdAsync(model.id);
            if (role == null) return null;
            role.Name = model.name;
            role.IsLock = model.is_lock;

            await ((AuditManager)_cRepositoryLog).AddLog(new AuditBinding()
            {
                action = AuditManager.UPDATE,
                objeto = this.model
            }, id: id);
            //_context.Roles.Update(role);
            try
            {
                var result = await _roleManager.UpdateAsync(role);
                if (!result.Succeeded)
                {
                    return result;
                }
                else _logger.LogInformation(LoggingEvents.INSERT_ITEM, $"Rol actualizado exitosamente {role.Name}");
                // Attempt to save changes to the database
                //await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                ex.Entries.Single().Reload();
                foreach (var entry in ex.Entries)
                {

                    if (entry.Entity is ApplicationRole)
                    {
                        _context.Roles.Update(role);
                    }
                }
                await _context.SaveChangesAsync();
            }
            return role;
        }

        public async Task<ApplicationUser> AddRoleUser(string id, AddRoleBindingModel model)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return null;
            }

            var cacheKeySize = string.Format("_{0}_claims", user.Id);
            _cache.Remove(cacheKeySize);

            foreach (string role in model.roles)
            {
                if (await _roleManager.RoleExistsAsync(role))
                {
                    if (!await _userManager.IsInRoleAsync(user, role))
                    {
                        await _userManager.AddToRoleAsync(user, role);
                    }
                }
                else return null;
            }
            return user;
        }

        public async Task<ApplicationUser> RemoveRoleUser(string id, AddRoleBindingModel model)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                var cacheKeySize = string.Format("_{0}_claims", user.Id);
                _cache.Remove(cacheKeySize);

                foreach (string role in model.roles)
                {
                    if (await _roleManager.RoleExistsAsync(role))
                    {
                        if (await _userManager.IsInRoleAsync(user, role))
                        {
                            await _userManager.RemoveFromRoleAsync(user, role);
                        }
                    }
                    else return null;
                }
            }
            return user;
        }

        public async Task<object> Remove(string id)
        {
            if (!await Exist<ApplicationRole>(x => x.Id == id))
            {
                return null;
            }
            var role = await _context.Roles
                .SingleOrDefaultAsync(x => x.Id == id);

            _context.RoleClaims.RemoveRange(_context.RoleClaims
                .Where(x => x.RoleId == id).ToList());
            await _context.SaveChangesAsync();

            var userRoles = _context.UserRoles.Where(x => x.RoleId == id).ToList();
            foreach (var userId in userRoles.Select(x => x.UserId))
            {
                var cacheKeySize = string.Format("_{0}_claims", userId);
                _cache.Remove(cacheKeySize);
            }

            _context.UserRoles.RemoveRange(userRoles);
            await _context.SaveChangesAsync();
            _context.Roles.Remove(role);

            //var result = await _roleManager.DeleteAsync(role);
            //if (!result.Succeeded) return result;
            try
            {
                // Attempt to save changes to the database
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                ex.Entries.Single().Reload();
                foreach (var entry in ex.Entries)
                {

                    if (entry.Entity is ApplicationRole)
                    {
                        _context.Roles.Remove(role);
                    }
                }
                _context.SaveChanges();
            }

            return role;
        }
    }
}