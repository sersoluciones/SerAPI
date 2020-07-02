﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SerAPI.Models
{
    public class Audit : BasicModel
    {
        public DateTime current_date { get; set; }

        public byte action { get; set; }

        [StringLength(20)]
        public string objeto { get; set; }

        [StringLength(20)]
        public string username { get; set; }

        [StringLength(20)]
        public string role { get; set; }

        [StringLength(2000)]
        public string json_browser { get; set; }

        [StringLength(2000)]
        public string json_request { get; set; }

        public string json_observation { get; set; }

        [StringLength(60)]
        public string user_id { get; set; }
    }

    public class AuditBinding
    {
        public byte action { get; set; }
        public string objeto { get; set; }
        public JObject json_observations { get; set; } = new JObject();
    }
}
