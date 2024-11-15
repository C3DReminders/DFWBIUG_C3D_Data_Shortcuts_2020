using Autodesk.Aec.PropertyData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIMDemo.SQLiteDatabase.PropertySets
{
    public class PropertyDefinition
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DataType Type { get; set; }

        public PropertyDefinition()
        {
            
        }
    }

}
