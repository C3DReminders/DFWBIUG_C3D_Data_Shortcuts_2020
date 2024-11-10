using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.DatabaseServices;
using BIMDemo.Extensions;
using BIMDemo.Extensions.Featurelines;
using BIMDemo.SQLiteDatabase;
using BIMDemo.UI.ViewModels;
using BIMDemo.UI.Views;
using Gile.AutoCAD.R25.Geometry;
using Microsoft.EntityFrameworkCore;
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
            var db = doc.Database;
            var ed = doc.Editor;

            try
            {
                using (var tr = doc.TransactionManager.StartTransaction())
                {
                    DemoDbContext.EnsureDatabaseCreated();

                    var dbContext = new DemoDbContext();

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

                    ProcessDatabaseObjects(tr, poly, out Dictionary<string, List<ObjectId>> layersUnprocessed);

                    var settingsMdl = new DemoLayersSettingsVM();

                    var layersMappings = dbContext.DemoLayerMaps.Where(x => layersUnprocessed.Keys.Contains(x.LayerName))
                                                                .Include(x => x.Layer)
                                                                .Select(x => new DemoLayerMapVM(settingsMdl, x, dbContext.Layers.Where(l => x.LayerId == l.Id).FirstOrDefault()))
                                                                .ToList();

                    ProcessLayerMappings(db, layersUnprocessed, layersMappings);

                    if (!layersUnprocessed.Any())
                    {
                        tr.Commit();
                        return;
                    }

                    // Get the layers from the database.
                    var layers = dbContext.Layers
                                          .OrderBy(x => x.Name)
                                          .ToList()
                                          .Select(x => new LayerVM(x))
                                          .ToList();

                    // Get the layers in the current drawing.
                    layers.AddRange(LayerVM.GetLayers(layers.Select(x => x.Name).ToList()));

                    settingsMdl.AddLayers(layers);

                    foreach (var layerUnprocessed in layersUnprocessed)
                    {
                        var layerMappingVM = new DemoLayerMapVM(settingsMdl, layerUnprocessed.Key, settingsMdl.Layers.FirstOrDefault());

                        settingsMdl.LayerMappings.Add(layerMappingVM);
                    }

                    var settingsWndw = new DemoLayersSettingsWindow(settingsMdl);

                    if (!Application.ShowModalWindow(Application.MainWindow.Handle, settingsWndw, true) == true)
                    {
                        tr.Commit();
                        return;
                    }

                    settingsMdl.UpdateDatabase(false, dbContext);

                    ProcessLayerMappings(db, layersUnprocessed, settingsMdl.LayerMappings.ToList());

                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\nError: " + ex.Message + "\n");
            }
        }

        [CommandMethod("DemoLayersMapping")]
        public static void DemolitionMappingCommand()
        {
            // Your code here
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;

            try
            {
                using (var tr = doc.TransactionManager.StartTransaction())
                {
                    DemoDbContext.EnsureDatabaseCreated();

                    var dbContext = new DemoDbContext();

                    var settingsMdl = new DemoLayersSettingsVM();

                    var layers = dbContext.Layers
                                          .OrderBy(x => x.Name)
                                          .ToList()
                                          .Select(x => new LayerVM(x))
                                          .ToList();

                    var layersMappings = dbContext.DemoLayerMaps.ToList()
                                                                .Select(x => new DemoLayerMapVM(settingsMdl, x, layers.Where(l => l.Id == x.LayerId).FirstOrDefault()))
                                                                .ToList();

                    // Get the layers in the current drawing.
                    layers.AddRange(LayerVM.GetLayers(layers.Select(x => x.Name).ToList()));

                    settingsMdl.AddLayers(layers);
                    foreach (var layerMapping in layersMappings)
                    {
                        settingsMdl.LayerMappings.Add(layerMapping);
                    }
                    
                    var settingsWndw = new DemoLayersSettingsWindow(settingsMdl);

                    if (Application.ShowModalWindow(Application.MainWindow.Handle, settingsWndw, true) != true)
                    {
                        tr.Commit();
                        return;
                    }

                    settingsMdl.UpdateDatabase(true, dbContext);

                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\nError: " + ex.Message + "\n");
            }
        }

        private static void ProcessLayerMappings(Database db, Dictionary<string, List<ObjectId>> layersUnprocessed, List<DemoLayerMapVM> layersMappings)
        {
            foreach (var layerMapping in layersMappings)
            {
                if (layerMapping?.Layer?.Name is null)
                {
                    continue;
                }

                if (!layersUnprocessed.ContainsKey(layerMapping.LayerName))
                {
                    // The key no longer exists, there is probably two mappings in the
                    // database.
                    continue;
                }

                var layerId = db.GetLayerId(layerMapping.Layer.Name, out bool _);

                foreach (var objectIdsToProcess in layersUnprocessed[layerMapping.LayerName])
                {
                    var entity = objectIdsToProcess.GetObject(OpenMode.ForWrite) as Entity;
                    entity.LayerId = layerId;
                }

                layersUnprocessed.Remove(layerMapping.LayerName);
            }
        }

        private static void ProcessDatabaseObjects(Transaction tr, Polyline polyArea, out Dictionary<string, List<ObjectId>> layersUnprocessed)
        {
            layersUnprocessed = new Dictionary<string, List<ObjectId>>();

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

                if (objId.ObjectClass.DxfName.Equals("ACAD_PROXY_ENTITY", StringComparison.OrdinalIgnoreCase))
                {
                    BIMDemoApp.WriteMessage("Found proxy entity, skipping.");
                    continue;
                }

                processedObjIds.Add(objId);

                var layersLocked = new HashSet<ObjectId>();

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

                    if (layersLocked.Contains(entity.LayerId) || entity.IsLayerLocked())
                    {
                        BIMDemoApp.WriteMessage(string.Join("\t", "\tLayer is locked, skipping object.",
                                                "Layer:",
                                                entity.Layer,
                                                objId.ObjectClass.DxfName));
                        continue;
                    }

                    if (!polyArea.IsOverlapGeometricExtents(entity))
                    {
                        continue;
                    }

                    if (polyArea.TryGetIntersectionPoints(entity, out Point3dCollection intersectPts))
                    {
                        ProcessIntersectionPoints(db, polyArea, entity, intersectPts, layersUnprocessed, out List<ObjectId> addedObjIds);
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

                    CollectLayerInformation(db, entity, layersUnprocessed);
                }
                catch (System.Exception ex)
                {
                    BIMDemoApp.WriteMessage(ex.Message);
                }
            }
        }

        private static void CollectLayerInformation(Database db, Entity entity, Dictionary<string, List<ObjectId>> layersUnprocessed)
        {
            var layerName = entity.Layer;

            if (!layersUnprocessed.ContainsKey(layerName))
            {
                layersUnprocessed[layerName] = new List<ObjectId>();
            }

            layersUnprocessed[layerName].Add(entity.ObjectId);
        }

        private static void ProcessIntersectionPoints(Database db, Polyline polyArea, Entity entity, Point3dCollection intersectPts, 
                                                      Dictionary<string, List<ObjectId>> layersUnprocessed, out List<ObjectId> addedObjIds)
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
                            CollectLayerInformation(db, flSegment, layersUnprocessed);
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
                    CollectLayerInformation(db, entity, layersUnprocessed);
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
                            CollectLayerInformation(db, splitCurve, layersUnprocessed);
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

            CollectLayerInformation(db, entity, layersUnprocessed);
        }
    }
}
