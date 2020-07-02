using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SerAPI.Models
{
    public class Permission : BasicModel
    {
        [Required]
        [StringLength(150)]
        public string name { get; set; }
    }

    public class PermissionBinding
    {
        [Required]
        [StringLength(150)]
        public string name { get; set; }
    }
}
