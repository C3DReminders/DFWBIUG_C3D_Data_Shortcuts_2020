using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIMDemo.SQLiteDatabase
{
    public class QuantityCount
    {
        public int Id { get; set; }
        public string Handle { get; set; }
        public string DxfName { get; set; }
        public int LayerId { get; set; }
        public Layer Layer { get; set; }
        public int DrawingId { get; set; }
        public Drawing Drawing { get; set; }
    }
}
