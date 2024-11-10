using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
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

        public static bool TryGetIntersectionPoints(this Entity ent, Entity other, out Point3dCollection intersectPts)
        {
            intersectPts = new Point3dCollection();
            var oPtr = new IntPtr();
            var planeXY = new Plane();

            ent.IntersectWith(other, Intersect.OnBothOperands, planeXY, intersectPts, oPtr, oPtr);

            return intersectPts.Count > 0;
        }

        public static ObjectId AddToModelSpace(this Entity ent)
        {
            var objId = ObjectId.Null;
            var db = HostApplicationServices.WorkingDatabase;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                BlockTable acBlkTbl = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write
                BlockTableRecord acBlkTblRec = acBlkTbl[BlockTableRecord.ModelSpace].GetObject(OpenMode.ForWrite) as BlockTableRecord;

                objId = acBlkTblRec.AppendEntity(ent);
                tr.AddNewlyCreatedDBObject(ent, true);

                tr.Commit();
            }

            return objId;
        }

        public static bool IsLayerLocked(this Entity ent)
        {
            var lyrTblRec = ent.LayerId.GetObject(OpenMode.ForRead) as LayerTableRecord;
            return lyrTblRec.IsLocked;
        }
    }
}
