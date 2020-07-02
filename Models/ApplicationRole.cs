using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SerAPI.Models
{
    public class ApplicationRole : IdentityRole
    {
        public ApplicationRole() { }
        public ApplicationRole(string roleName) : base(roleName)
        {
        }
        public bool IsLock { get; set; }
        [JsonIgnore]
        public virtual List<ApplicationUserRole> UserRoles { get; set; }
        [JsonIgnore]
        public virtual ICollection<IdentityRoleClaim<string>> Claims { get; set; }
    }
}
