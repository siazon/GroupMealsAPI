using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace NewFoodCreator.Utility
{
    public class FileCreator
    {
        public bool CreateFiles<T>(List<T> collection, string folderPath, string fileName)
        {

            var count = 1;

            foreach (var json in collection.Select(item => JsonConvert.SerializeObject(item)))
            {
                using (var writer =
                    File.AppendText(Path.Combine(folderPath, string.Format("{1}{0}.json", count, fileName))))
                {
                    writer.Write(json);
                }

                count++;
            }


            return true;
        }


        public bool CreateFile<T>(T item, string folderPath, string fileName)
        {
            var json = JsonConvert.SerializeObject(item);

            using (var writer = File.AppendText(Path.Combine(folderPath, string.Format("{0}.json", fileName))))
            {
                writer.Write(json);
            }


            return true;
        }
    }
}