using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIMDemo.SQLiteDatabase
{
    public class DemoLayerMap
    {
        public int Id { get; set; }
        public string LayerName { get; set; }
        public int LayerId { get; set; }
        public Layer Layer { get; set; }
    }
}
