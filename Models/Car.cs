using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SerAPI.Models
{
    public class Car : BasicModel
    {

        [StringLength(20)]
        public string modelo { get; set; }

        public int numero_puertas { get; set; }

    }
}
