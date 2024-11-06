using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.DatabaseServices;
using BIMDemo.Extensions;
using BIMDemo.Extensions.Featurelines;
using BIMDemo.SQLiteDatabase;
using Gile.AutoCAD.R25.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;

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
                    DemoDbContext.EnsureDatabaseCreated();

                    var dbCont = new DemoDbContext();

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

                try
                {
                    var entity = objId.GetObject(OpenMode.ForRead) as Entity;

                    if (entity is null)
                    {
                        continue;
                    }

                    if (entity is Parcel)
                    {
                        BIMDemoApp.WriteMessage("Parcels not supported.");
                        continue;
                    }

                    if (entity is Site)
                    {
                        BIMDemoApp.WriteMessage("Sites not supported.");
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
                catch (System.Exception ex)
                {
                    BIMDemoApp.WriteMessage(ex.Message);
                }
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
            if (entity is FeatureLine featureLine)
            {
                var flSegments = featureLine.SplitFeatureLine(intersectPts);

                featureLine.UpgradeOpen();
                featureLine.Erase();

                foreach (var flSegment in flSegments)
                {
                    var distance = flSegment.GetDistanceAtParameter(flSegment.EndParam);
                    var testPt = flSegment.GetPointAtDist(distance / 2.0);
                    var ptContainment = polyArea.GetPointContainment(testPt);

                    addedObjIds.Add(flSegment.ObjectId);

                    switch (ptContainment)
                    {
                        case PolylineExtension.PointContainment.Inside:
                        case PolylineExtension.PointContainment.OnBoundary:
                            ConvertToDemoLayer(db, flSegment);
                            break;
                        case PolylineExtension.PointContainment.OutSide:
                            break;
                        default:
                            break;
                    }
                }

                return;
            }
            else if (entity is Curve curve)
            {
                var intersectionParams = curve.GetParamsAtPoints(intersectPts);

                var dblCollection = new DoubleCollection(intersectionParams.ToArray());

                if (dblCollection.Count == 0)
                {
                    ConvertToDemoLayer(db, entity);
                    return;
                }

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

            }

            ConvertToDemoLayer(db, entity);
        }
    }
}
