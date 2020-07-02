using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SerAPI.Models
{
    public class ApplicationUser : IdentityUser
    {
        public bool? IsActive { get; set; }
        public DateTime? CreateDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? FinishDate { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(100)]
        public string LastName { get; set; }

        public bool DarkMode { get; set; }

        public bool MaximizedWindows { get; set; }

        public bool SidebarCollapse { get; set; }

        [StringLength(100)]
        public string Address { get; set; }

        [NotMapped]
        public string ProviderKey { get; set; }

        public int? AttachmentId { get; set; }
        [ForeignKey("AttachmentId")]
        public Attachment Attachment { get; set; }

        [JsonIgnore]
        public virtual ICollection<IdentityUserClaim<string>> Claims { get; set; }
        [JsonIgnore]
        public virtual ICollection<IdentityUserLogin<string>> Logins { get; set; }
        [JsonIgnore]
        public virtual ICollection<IdentityUserToken<string>> Tokens { get; set; }
        [JsonIgnore]
        public virtual List<ApplicationUserRole> UserRoles { get; set; }

    }

    public class ApplicationUserRole : IdentityUserRole<string>
    {
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
        [ForeignKey("RoleId")]
        public virtual ApplicationRole Role { get; set; }
    }
}
