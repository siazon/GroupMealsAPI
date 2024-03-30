using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Domain.Common
{
    public class ExchangeModel
    {
        public string base_code { get; set; }
        public string result { get; set; }
        public Dictionary<string,string> conversion_rates { get; set; }
        public string documentation { get; set; }
        public string terms_of_use { get; set; }
        public long time_last_update_unix { get; set; }
        public string time_last_update_utc { get; set; }
        public long time_next_update_unix { get; set; }
        public string time_next_update_utc { get; set; }
    }
}
 