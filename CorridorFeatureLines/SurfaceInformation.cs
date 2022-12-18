using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFWBIUG_C3D_Data_Shortcuts_2020.CorridorFeatureLines
{
    public class SurfaceInformation
    {
        public string Name { get; set; }
        public List<string> PointCodes { get; set; }
        public List<string> LinkCodes { get; set; }
        public ObjectId ObjId { get; set; }

        public SurfaceInformation(string name, ObjectId objId)
        {
            Name = name;
            ObjId = objId;
            PointCodes = new List<string>();
            LinkCodes = new List<string>();
        }

        public string GetString()
        {
            return "Surface Name: " + Name + "\n\tLinks: " + string.Join(",", LinkCodes) + "\n\tCodes: " + string.Join(",", PointCodes);
        }
    }
}
