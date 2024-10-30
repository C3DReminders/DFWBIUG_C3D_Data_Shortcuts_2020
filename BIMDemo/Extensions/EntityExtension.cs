using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIMDemo.Extensions
{
    public static class EntityExtension
    {
        public static bool IsOverlapGeometricExtents(this Entity ent, Entity other)
        {
            var extents1 = ent.GeometricExtents;
            var extents2 = other.GeometricExtents;
            
            if (extents1.MaxPoint.X < extents2.MinPoint.X || // Left of extents2
                extents1.MinPoint.X > extents2.MaxPoint.X || // Right of extents2
                extents1.MaxPoint.Y < extents2.MinPoint.Y || // Below extents2
                extents1.MinPoint.Y > extents2.MaxPoint.Y)
            {
                return false;
            }

            return true;
        }
    }
}
