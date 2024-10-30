using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIMDemo.Extensions
{
    public static class CurveExtensions
    {
        // https://forums.autodesk.com/t5/net/given-point2d-find-point3d-on-polyline3d/td-p/6867686
        /// <summary>
        /// Returns the closest point to a 3D curve based on the projected distance. 
        /// This is due to the pick point using a zero elevation instead of the elevation
        /// of the object selected. This should work as long as the projected curve is a
        /// polyline with curves.
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="pt3d"></param>
        /// <returns></returns>
        public static Point3d GetClosestPointToProjected(this Curve curve, Point3d pt3d)
        {
            var planeXY = new Plane(new Point3d(0, 0, 0), Vector3d.ZAxis);

            var vectorNormal = planeXY.Normal;
            var closestPt = curve.GetClosestPointTo(pt3d, vectorNormal, false);

            return closestPt;

        }
    }
}
