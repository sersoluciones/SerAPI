﻿using IdentityModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SerAPI.Data;
using SerAPI.Hubs;
using SerAPI.Models;
using SerAPI.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SerAPI.Managers
{
    public class AuditManager
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;
        private readonly IHubContext<StateHub> _hub;
        private readonly IHttpContextAccessor _contextAccessor;
        private IMemoryCache _cache;
        public static byte READ = 0;
        public static byte CREATE = 1;
        public static byte UPDATE = 2;
        public static byte DELETE = 3;
        public static byte EXECUTE = 4;
        public static byte LOGIN = 5;
        public static byte LOGOUT = 6;

        public static Dictionary<string, byte> ACTIONS = new Dictionary<string, byte>()
        {
            {"READ", 0},
            {"CREATE", 1},
            {"UPDATE", 2},
            {"DELETE", 3},
            {"EXECUTE", 4},
            {"LOGIN", 5},
            {"LOGOUT", 6}
        };

        public AuditManager(ApplicationDbContext db,
            ILogger<AuditManager> logger,
            IHttpContextAccessor contextAccessor,
            IMemoryCache memoryCache,
            IHubContext<StateHub> hub,
            IConfiguration config)
        {
            _context = db;
            _logger = logger;
            _contextAccessor = contextAccessor;
            _cache = memoryCache;
            _hub = hub;
        }

        public async Task<IEnumerable> All(string fromDate, string toDate, string option = "", string userName = "", string role = "")
        {
            //var headers = _contextAccessor.HttpContext.Request.Headers;
            //foreach (var head in headers)
            //{
            //    _logger.LogInformation(1, $"headers: {head.Key} {head.Value}");
            //}
            //_logger.LogWarning(0, $"infoBrowser: {infoBrowser()}");
            //_logger.LogWarning(0, $"infoRequest: {infoRequest()}");
            IQueryable<Audit> objs = _context.audits;

            if (!string.IsNullOrEmpty(fromDate) && !string.IsNullOrEmpty(toDate))
            {
                var dtfromDate = DateTime.Parse(fromDate);
                var dtToDate = DateTime.Parse(toDate);
                //dtToDate = dtToDate.AddHours(24);
                objs = objs.Where(x => x.current_date >= dtfromDate && x.current_date <= dtToDate);
            }
            if (!string.IsNullOrEmpty(option))
            {
                _logger.LogWarning(0, $"action: {option}");
                objs = objs.Where(x => x.action == ACTIONS[option]);
            }
            if (!string.IsNullOrEmpty(userName))
            {
                objs = objs.Where(x => x.username == userName);
            }
            if (!string.IsNullOrEmpty(role))
            {
                objs = objs.Where(x => x.role == role);
            }
            return await objs.AsNoTracking().ToListAsync();
        }


        public async Task AddLog(AuditBinding entity, string id = "", bool commit = false)
        {
            _context.ChangeTracker.DetectChanges();

            var entities = _context.ChangeTracker.Entries()
                .Where(x => x.State == EntityState.Modified
                || x.State == EntityState.Added
                || x.State == EntityState.Deleted && x.Entity != null).ToList();


            if (!string.IsNullOrEmpty(id))
            {
                entity.json_observations.Add("ObjectId", id);
            }
            if ((new int[] { CREATE, UPDATE, DELETE }).ToList().Contains(entity.action))
            {
                foreach (var add in entities.Where(p => p.State == EntityState.Added))
                {
                    string entityName = add.Entity.GetType().Name;
                    _logger.LogWarning($"EntityState.Added, entityName {entityName}\n");
                }

                foreach (var change in entities.Where(p => p.State == EntityState.Modified))
                {
                    string entityName = change.Entity.GetType().Name;
                    _logger.LogWarning($"EntityState.Modified, entityName {entityName}\n");
                    //if (!(new string[] { "Claim" }).ToList().Contains(entity.Object))
                    //    entity.Object = entityName;
                    //entity.Action = UPDATE;
                    entity.json_observations.Add("Values", AuditEntityModified(change));
                }

                foreach (var delete in entities.Where(p => p.State == EntityState.Deleted))
                {
                    string entityName = delete.Entity.GetType().Name;
                    _logger.LogWarning($"EntityState.Deleted, entityName {entityName}\n");
                }
            }
            var user = await _context.Users.AsNoTracking()
                .Select(x => new
                {
                    x.Id,
                    x.UserName
                })
                .SingleOrDefaultAsync(x => x.Id == GetCurrentUser());
            Audit log = new Audit()
            {
                current_date = DateTime.Now,
                action = entity.action,
                objeto = entity.objeto,
                username = user.UserName,
                role = string.Join(",", GetRolesUser().ToArray()),
                json_browser = infoBrowser(),
                json_request = infoRequest(),
                json_observation = entity.json_observations.ToString(),
                user_id = user.Id.ToString()
            };
            //if (commit) await Add(log, x => x.id == log.id);
            //else await _context.Audits.AddAsync(log);
            var json = JsonSerializer.Serialize<object>(
                new
                {
                    CurrentDate = DateTime.Now,
                    entity.action,
                    entity.objeto,
                },
                new JsonSerializerOptions { WriteIndented = true, });


            await SendMsgSignalR(json);
            if (commit) await _context.SaveChangesAsync();
        }

        private async Task SendMsgSignalR(string msg)
        {
            await _hub.Clients.User(GetCurrenUserName())
                .SendAsync("ReceiveMessage", msg);
        }


        public JArray AuditEntityModified(EntityEntry objectStateEntry)
        {
            JArray jArrayProp = new JArray();
            foreach (var prop in objectStateEntry.OriginalValues.Properties)
            {
                string originalValue = null;
                if (objectStateEntry.OriginalValues[prop] != null)
                    originalValue = objectStateEntry.OriginalValues[prop].ToString();
                string currentValue = null;
                if (objectStateEntry.CurrentValues[prop] != null)
                    currentValue = objectStateEntry.CurrentValues[prop].ToString();
                JObject jObjectProp = new JObject();
                if (originalValue != currentValue) //Only create a log if the value changes
                {
                    jObjectProp.Add("PropertyName", prop.Name);
                    jObjectProp.Add("OldValue", originalValue);
                    jObjectProp.Add("NewValue", currentValue);
                    jArrayProp.Add(jObjectProp);
                }
            }
            _logger.LogWarning($"json values: {jArrayProp.ToString()}");
            return jArrayProp;
        }


        public string infoBrowser()
        {
            UserAgent ua = new UserAgent();
            try
            {
                string userAgent = _contextAccessor.HttpContext.Request.Headers["User-Agent"];
                _logger.LogInformation(3, $"userAgent: {userAgent}");
                ua = new UserAgent(_contextAccessor.HttpContext.Request.Headers["User-Agent"]);
            }
            catch (Exception) { }
            //string.Join(",", dogs.ToArray());
            return JsonSerializer.Serialize(ua);
        }

        public string infoRequest()
        {
            string refer = _contextAccessor.HttpContext.Request.Headers["Referer"];
            var infoRequest = new InfoRequest
            {
                verb = string.Format("{0}", _contextAccessor.HttpContext.Request.Method),
                content_type = string.Format("{0}", _contextAccessor.HttpContext.Request.ContentType),
                encoded_url = string.Format("{0}", _contextAccessor.HttpContext.Request.GetEncodedUrl()),
                path = string.Format("{0}", _contextAccessor.HttpContext.Request.Path),
                remote_ip_address = string.Format("{0}", _contextAccessor.HttpContext.Connection.RemoteIpAddress),
                host = string.Format("{0}", _contextAccessor.HttpContext.Request.Host),
                refferer_url = string.Format("{0}", (string.IsNullOrEmpty(refer)) ? "" : refer)
            };
            return JsonSerializer.Serialize<InfoRequest>(infoRequest,
                new JsonSerializerOptions { WriteIndented = true, });
        }

        class InfoRequest
        {
            public string verb { get; set; }
            public string content_type { get; set; }
            public string encoded_url { get; set; }
            public string path { get; set; }
            public string remote_ip_address { get; set; }
            public string host { get; set; }
            public string refferer_url { get; set; }
        }

        public string GetCurrentUser()
        {
            return _contextAccessor.HttpContext.User.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Subject).Value;
        }

        public string GetCurrenUserName()
        {
            return _contextAccessor.HttpContext.User.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Name).Value;
        }

        public List<string> GetRolesUser()
        {
            return _contextAccessor.HttpContext.User.Claims.Where(x =>
                x.Type == JwtClaimTypes.Role).Select(x => x.Value).ToList();
        }
    }
}