using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Domain.Common
{
    public class ResponseModel
    {
        public int code { get; set; }
        public string msg { get; set; }
        public string  token { get; set; }
        public object data { get; set; }
    }
}
