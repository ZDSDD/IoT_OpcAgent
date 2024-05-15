using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionAppsDemo.Functions.Model
{
    public class ProductionMessage
    {
        public string WindowStartTime { get; set; }
        public string DeviceId { get; set; }
        public long TotalGoodCount { get; set; }
        public long TotalBadCount { get; set; }
    }
}
