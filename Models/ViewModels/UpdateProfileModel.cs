using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SerAPI.Models.ViewModels
{
    public class UpdateProfileModel
    {
        [Required]
        [EmailAddress]
        public string email { get; set; }

        [Required]
        [StringLength(50)]
        public string name { get; set; }

        [StringLength(50)]
        public string last_name { get; set; }

        public string phone_number { get; set; }

        public bool dark_mode { get; set; }

        public bool maximized_windows { get; set; }

        public bool sidebar_collapse { get; set; }
      

        [StringLength(100)]
        public string address { get; set; }
    }

    public class UpdateBooleanModel
    {
        [Required]
        public bool new_value { get; set; }
    }
}
