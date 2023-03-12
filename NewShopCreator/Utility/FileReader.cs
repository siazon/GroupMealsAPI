using System;
using System.Data;
using System.IO;
using ExcelDataReader;

namespace NewShopCreator.Utility
{
    public class FileReader
    {
        public FileReader()
        {
        }

        public DataSet Read(string path)
        {
            using (var stream = File.Open(path, FileMode.Open, FileAccess.Read))
            {


                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {


                    var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                        {
                            UseHeaderRow = true
                        }
                    });

                    return result;
                }
            }
        }
    }
}
