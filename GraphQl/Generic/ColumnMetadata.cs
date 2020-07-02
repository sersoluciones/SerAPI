﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SerAPI.GraphQl.Generic
{
    public class ColumnMetadata
    {
        public string ColumnName { get; set; }
        public string DataType { get; set; }
        public bool IsNull { get; set; }
        public Type Type { get; set; }
        public bool IsList { get; set; }
    }
}
