using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SerAPI.Models.ViewModels
{
    public class RoleBindingModel
    {
        [Required]
        public string name { get; set; }
    }

    public class EditRoleBindingModel
    {
        [Required]
        public string id { get; set; }

        [Required]
        public string name { get; set; }
        public bool is_lock { get; set; }
    }


    public class AddRoleBindingModel
    {
        [Required]
        public string[] roles { get; set; }
    }

    public class ClaimRoleBingModel
    {
        [Required]
        public string role_id { get; set; }

        [Required]
        public string permission_name { get; set; }
    }

    public class ClaimsRoleBingModel
    {
        [Required]
        public string role_id { get; set; }

        [Required]
        public string[] permission_names { get; set; }
    }
}