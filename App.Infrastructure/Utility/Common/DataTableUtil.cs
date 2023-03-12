using System.Collections.Generic;
using System.ComponentModel;
using System.Data;

namespace App.Infrastructure.Utility.Common
{
    public class DataTableUtil
    {
        public static DataTable ConvertToDataTable<T>(IEnumerable<T> data)
        {
            var props =
                TypeDescriptor.GetProperties(typeof(T));
            var table = new DataTable();
            for (var i = 0; i < props.Count; i++)
            {
                var prop = props[i];
                table.Columns.Add(prop.Name, prop.PropertyType);
            }
            object[] values = new object[props.Count];
            foreach (T item in data)
            {
                for (var i = 0; i < values.Length; i++)
                {
                    values[i] = props[i].GetValue(item);
                }
                table.Rows.Add(values);
            }
            return table;
        }
    }
}