using System;
using System.Collections.Generic;
using System.Text;

namespace VirtoCommerce.ExportModule.Core.Model
{
    public class ExportPartition
    {
        public string PartitionName { get; set; }
        public IEnumerable<IExportable> Items { get; set; }
    }
}
