using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using BIMDemo.Extensions;
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

        private static void ProcessDatabaseObjects(Transaction tr, Polyline polyArea)
        {
            var db = Application.DocumentManager.MdiActiveDocument.Database;

            var blkTable = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
            var blkTblRecord = tr.GetObject(blkTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

            var demoSuffix = "-DEMO";

            foreach (var objId in blkTblRecord)
            {
                var entity = objId.GetObject(OpenMode.ForRead) as Entity;

                if (!polyArea.IsOverlapGeometricExtents(entity))
                {
                    continue;
                }

                var layerName = entity.Layer;

                if (layerName.EndsWith(demoSuffix))
                {
                    continue;
                }

                entity.UpgradeOpen();
                entity.LayerId = db.GetLayerId(layerName + demoSuffix, out bool _);
            }
        }
    }
}
