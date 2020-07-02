using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SerAPI.Models
{
    public class BaseModel : BasicModel
    {
        public DateTime? create_date { get; set; }

        public DateTime? update_date { get; set; }
    }

    public class BasicModel
    {
        public int id { get; set; }
    }
}
