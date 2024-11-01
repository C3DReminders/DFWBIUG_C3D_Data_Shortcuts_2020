using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIMDemo.Extensions.Featurelines
{
    public class FeatureLinePointData
    {
        public double Parameter { get; set; }
        public Point3d Location { get; set; }
        public FeatureLinePointType PointType { get; set; }
    }
}
