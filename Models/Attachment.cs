using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SerAPI.Models
{
    public class Attachment : BaseModel
    {
        public Attachment(string model)
        {
            this.model = model;
        }

        public bool isPrivate { get; set; }

        public string[] tags { get; set; }

        [Required]
        [StringLength(80)]
        public string model { get; set; }

        [Required]
        [StringLength(300)]
        public string key { get; set; }


    }
}
