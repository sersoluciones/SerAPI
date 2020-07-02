using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SerAPI.Models
{
    public class CommonOption : BasicModel
    {
        public bool? is_active { get; set; }

        [StringLength(50)]
        [Required]
        public string type { get; set; }

        [StringLength(255)]
        [Required]
        public string value { get; set; }

        [StringLength(2000)]
        public string description { get; set; }

        public int? attachment_id { get; set; }
        [JsonIgnore]
        [ForeignKey("attachment_id")]
        public Attachment attachment { get; set; }
    }

   
}
