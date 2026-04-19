using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Infrastructure.Data
{
    public static class SqlTableTypeHelper
    {
        public static DataTable CreateStringListTable(IEnumerable<string> values)
        {
            var table = new DataTable();
            table.Columns.Add("Value", typeof(string));

            var items = values?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            foreach (var item in items)
            {
                table.Rows.Add(item);
            }

            return table;
        }
    }
}
