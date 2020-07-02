using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SerAPI.Models.ViewModels
{
    public class AnyRegisterModel
    {
        [EmailAddress]
        public string email { get; set; }

        [StringLength(50)]
        public string username { get; set; }

        [StringLength(100)]
        public string name { get; set; }

        [StringLength(100)]
        public string last_name { get; set; }

        [StringLength(50)]
        public string phone_number { get; set; }

        [StringLength(50)]
        public string role { get; set; }

        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string password { get; set; }

        [DataType(DataType.Password)]
        [Compare("password", ErrorMessage = "The password and confirmation password do not match.")]
        public string confirm_password { get; set; }

        public string token { get; set; }
        public string third_type { get; set; }


        [StringLength(100)]
        public string address { get; set; }

    }

    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        public string email { get; set; }

        [Required]
        [StringLength(50)]
        public string username { get; set; }

        [Required]
        [StringLength(100)]
        public string name { get; set; }

        [StringLength(100)]
        public string last_name { get; set; }

        [StringLength(50)]
        public string phone_number { get; set; }

        public bool dark_mode { get; set; }

        public bool maximized_windows { get; set; }

        public bool sidebar_collapse { get; set; }

        [StringLength(50)]
        [Required]
        public string role { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string password { get; set; }

        [DataType(DataType.Password)]
        [Compare("password", ErrorMessage = "The password and confirmation password do not match.")]
        public string confirm_password { get; set; }


        [StringLength(100)]
        public string address { get; set; }

    }

    public class UpdateViewModel
    {
        [Required]
        public Guid? id { get; set; }

        [Required]
        [EmailAddress]
        public string email { get; set; }

        [Required]
        [StringLength(50)]
        public string username { get; set; }

        [Required]
        [StringLength(100)]
        public string name { get; set; }

        [StringLength(100)]
        public string last_name { get; set; }

        [StringLength(50)]
        public string phone_number { get; set; }

        public bool dark_mode { get; set; }

        [Required]
        [StringLength(50)]
        public string role { get; set; }

        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string password { get; set; }

        [DataType(DataType.Password)]
        [Compare("password", ErrorMessage = "The password and confirmation password do not match.")]
        public string confirm_password { get; set; }

        public bool is_active { get; set; }


        [StringLength(100)]
        public string address { get; set; }
    }

    public class UpdateUserToken
    {
        [StringLength(255)]
        public string firebase_token { get; set; }
        [StringLength(100)]
        public string username { get; set; }
    }
}
