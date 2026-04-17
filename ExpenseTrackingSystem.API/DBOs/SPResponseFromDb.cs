using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Domain.DBOs
{
    public class SPResponseFromDb
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Data { get; set; } 

    }
}
