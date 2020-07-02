using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SerAPI.Models.ViewModels
{
    public class EmailBinding
    {
        [Required]
        [StringLength(200)]
        public string name { get; set; }

        [Required]
        [StringLength(200)]
        [EmailAddress]
        public string email { get; set; }

        [Required]
        [StringLength(200)]
        public string subject { get; set; }

        [Required]
        [StringLength(4000)]
        public string message { get; set; }

        [Required]
        [StringLength(200)]
        public string template { get; set; }
    }


    public class SMSBinding
    {
        [Required]
        [StringLength(100)]
        public string api { get; set; }

        [Required]
        [StringLength(12)]
        public string cliente { get; set; }

        [Required]
        [StringLength(450)]
        public string sms { get; set; }

        [Required]
        [StringLength(12)]
        public string numero { get; set; }

        [StringLength(20)]
        public string referencia { get; set; } = null;

        //format: Y-m-d H:i:s
        public DateTime? fecha { get; set; } = null;
    }
}


