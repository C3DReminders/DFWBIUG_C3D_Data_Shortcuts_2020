using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIMDemo.Extensions
{
    public static class Point3dExtensions
    {
        public static Point2d ToPoint2d(this Point3d pt3d)
        {
            return new Point2d(pt3d.X, pt3d.Y);
        }
    }
}
