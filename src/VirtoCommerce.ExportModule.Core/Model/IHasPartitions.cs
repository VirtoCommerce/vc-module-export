using System;
using System.Collections.Generic;
using System.Text;

namespace VirtoCommerce.ExportModule.Core.Model
{
    public interface IHasPartitions
    {
        public IEnumerable<ExportPartition> GetPartitions();
    }
}
