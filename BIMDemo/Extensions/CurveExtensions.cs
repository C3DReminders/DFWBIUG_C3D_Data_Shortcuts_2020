using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.DatabaseServices;
using BIMDemo.Extensions.Featurelines;
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

        public static Polyline ToPolyline(this Curve curve)
        {
            Polyline poly;

            poly = curve as Polyline;
            if (poly != null)
            {
                return poly;
            }

            if (curve is FeatureLine fl)
            {
                var flCurve = fl.GetFlatCurve();

                return flCurve.ToPolyline();
            }

            if (curve is Polyline2d)
            {
                var poly2d = curve as Polyline2d;
                var copyPoly2d = poly2d.Clone() as Polyline2d;
                poly = new Polyline();
                poly.ConvertFrom(copyPoly2d, false);
                return poly;
            }

            if (curve is Polyline3d)
            {
                var poly3d = curve as Polyline3d;
                poly = new Polyline();
                var currentVertex = 0;
                foreach (ObjectId vId in poly3d)
                {
                    PolylineVertex3d v3d = vId.GetObject(OpenMode.ForRead) as PolylineVertex3d;

                    poly.AddVertexAt(currentVertex, v3d.Position.ToPoint2d(), 0, 0, 0);
                }
                return poly;
            }

            return poly;
        }

        public static DoubleCollection GetParamsAtPoints(this Curve curve, Point3dCollection intersectPts)
        {
            return new DoubleCollection(intersectPts.Cast<Point3d>()
                               .Select(p => curve.GetParameterAtPoint(curve.GetClosestPointToProjected(p)))
                               .Where(d => d > curve.StartParam && d < curve.EndParam)
                               .OrderBy(d => d)
                               .ToArray());
        }
    }
}
