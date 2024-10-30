using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using BIMDemo.Extensions;
using Gile.AutoCAD.R25.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIMDemo.DemoLayers
{
    public static class Demolition
    {
        [CommandMethod("DemoLayers")]
        public static void DemolitionCommand()
        {
            // Your code here
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;

            try
            {
                using (var tr = doc.TransactionManager.StartTransaction())
                {

                    var typedValues = new List<TypedValue>()
                    {
                         new TypedValue((int)DxfCode.Operator, "<OR"),
                        new TypedValue((int)DxfCode.Start, RXObject.GetClass(typeof(Polyline)).DxfName),
                        new TypedValue((int)DxfCode.Operator, "OR>")
                    };

                    var polyObjId = ed.GetEntityByType(typedValues, "Select polyline: ", "Not a valid polyline. Try again: ");

                    if (polyObjId.IsNull)
                    {
                        ed.WriteMessage("\nNo polyline selected.");
                        return;
                    }

                    var poly = tr.GetObject(polyObjId, OpenMode.ForRead) as Polyline;

                    if (poly == null)
                    {
                        ed.WriteMessage("\nNo polyline selected.");
                        return;
                    }

                    ed.WriteMessage("\nPolyline Length: " + poly.Length);

                    ProcessDatabaseObjects(tr, poly);

                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\nError: " + ex.Message + "\n");
            }
        }

        public const string DemoSuffix = "-DEMO";

        private static void ProcessDatabaseObjects(Transaction tr, Polyline polyArea)
        {
            var db = Application.DocumentManager.MdiActiveDocument.Database;

            var blkTable = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
            var blkTblRecord = tr.GetObject(blkTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

            var processedObjIds = new List<ObjectId>() { polyArea.ObjectId };

            foreach (var objId in blkTblRecord)
            {
                if (processedObjIds.Contains(objId))
                {
                    continue;
                }

                processedObjIds.Add(objId);

                var entity = objId.GetObject(OpenMode.ForRead) as Entity;

                if (entity is null)
                {
                    continue;
                }

                if (!polyArea.IsOverlapGeometricExtents(entity))
                {
                    continue;
                }

                if (polyArea.TryGetIntersectionPoints(entity, out Point3dCollection intersectPts))
                {
                    ProcessIntersectionPoints(db, polyArea, entity, intersectPts, out List<ObjectId> addedObjIds);
                    processedObjIds.AddRange(addedObjIds);
                    continue;
                }

                if (entity is Curve curve)
                {
                    var testPt = curve.GetPointAtDist(curve.GetDistanceAtParameter(curve.EndParam) / 2.0);
                    var ptContainment = polyArea.GetPointContainment(testPt);
                    if (ptContainment == PolylineExtension.PointContainment.OutSide)
                    {
                        continue;
                    }
                }

                ConvertToDemoLayer(db, entity);
            }
        }

        private static void ConvertToDemoLayer(Database db, Entity entity)
        {
            var layerName = entity.Layer;

            if (layerName.EndsWith(DemoSuffix))
            {
                return;
            }

            entity.UpgradeOpen();
            entity.LayerId = db.GetLayerId(layerName + DemoSuffix, out bool _);
        }

        private static void ProcessIntersectionPoints(Database db, Polyline polyArea, Entity entity, Point3dCollection intersectPts, out List<ObjectId> addedObjIds)
        {
            addedObjIds = new List<ObjectId>();
            if (entity is Curve curve)
            {
                var dblCollection = new DoubleCollection();

                foreach (Point3d pt in intersectPts)
                {
                    var currentParam = curve.GetParameterAtPoint(curve.GetClosestPointToProjected(pt));
                    if (currentParam == curve.StartParam || currentParam == curve.EndParam)
                    {
                        continue;
                    }
                    dblCollection.Add(currentParam);
                }

                if (dblCollection.Count > 0)
                {
                    var splitCurves = curve.GetSplitCurves(dblCollection);

                    foreach (Curve splitCurve in splitCurves)
                    {
                        var distance = splitCurve.GetDistanceAtParameter(splitCurve.EndParam);
                        var testPt = splitCurve.GetPointAtDist(distance / 2.0);
                        var ptContainment = polyArea.GetPointContainment(testPt);

                        switch (ptContainment)
                        {
                            case PolylineExtension.PointContainment.Inside:
                            case PolylineExtension.PointContainment.OnBoundary:
                                var splitObjId = splitCurve.AddToModelSpace();
                                addedObjIds.Add(splitObjId);
                                ConvertToDemoLayer(db, splitCurve);
                                break;
                            case PolylineExtension.PointContainment.OutSide:
                                var outsideSplitObjId = splitCurve.AddToModelSpace();
                                addedObjIds.Add(outsideSplitObjId);
                                break;
                            default:
                                break;
                        }
                    }

                    curve.UpgradeOpen();
                    curve.Erase();

                    return;
                }
            }

            ConvertToDemoLayer(db, entity);
        }
    }
}
