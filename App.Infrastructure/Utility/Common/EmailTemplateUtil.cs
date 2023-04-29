using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Infrastructure.Utility.Common
{
    public class EmailTemplateUtil
    {
        public static string ReadTemplate(string path,string emailType) {
            string template = "";

            path += $"/html/{emailType}.html";
            if (File.Exists(path))
            {
                template=File.ReadAllText(path);
            }


            return template;
        }
    }
}
