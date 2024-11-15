using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIMDemo.SQLiteDatabase.Blocks
{
    public class BlockAttributeValue
    {
        public int Id { get; set; }
        public int BlockId { get; set; }
        public int AttributeId { get; set; }
        public string Value { get; set; }
    }
}
