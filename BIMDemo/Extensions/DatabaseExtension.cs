using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using BIMDemo.SQLiteDatabase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIMDemo.Extensions
{
    public static class DatabaseExtension
    {
        public static ObjectId GetLayerId(this Database db, string layerName, out bool exists)
        {
            var lyrTable = db.LayerTableId.GetObject(OpenMode.ForRead) as LayerTable;
            if (lyrTable.Has(layerName))
            {
                exists = true;
                return lyrTable[layerName];
            }

            exists = false;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                ObjectId layObjId = ObjectId.Null;
                using (var lyrTableRec = new LayerTableRecord())
                {
                    lyrTableRec.Name = layerName;

                    // Upgrade the Layer table for write
                    lyrTable.UpgradeOpen();

                    // Append the new layer to the Layer table and the transaction
                    layObjId = lyrTable.Add(lyrTableRec);
                    tr.AddNewlyCreatedDBObject(lyrTableRec, true);
                    tr.Commit();

                    return layObjId;
                }
            }
        }

        public static ObjectId GetLayerId(this Database db, Layer layer, out bool exists)
        {
            var lyrTable = db.LayerTableId.GetObject(OpenMode.ForRead) as LayerTable;
            if (lyrTable.Has(layer.Name))
            {
                exists = true;
                return lyrTable[layer.Name];
            }

            exists = false;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                ObjectId layObjId = ObjectId.Null;
                using (var lyrTableRec = new LayerTableRecord())
                {
                    lyrTableRec.Name = layer.Name;

                    lyrTableRec.Description = layer.Description;
                    lyrTableRec.IsFrozen = layer.IsFrozen;
                    if (layer.ColorIndex > 0)
                    {
                        lyrTableRec.Color = Color.FromColorIndex(ColorMethod.ByAci, (short)layer.ColorIndex);
                    }

                    if (layer.TransparencyAlpha != (byte)255)
                    {
                        lyrTableRec.Transparency = new Transparency(layer.TransparencyAlpha);
                    }
                    
                    lyrTableRec.LineWeight = (LineWeight)layer.LineWeight;
                    lyrTableRec.LinetypeObjectId = db.GetLinetypeId(layer.LinetypeName);
                    try
                    {
                        lyrTableRec.PlotStyleName = layer.PlotStyleName;
                    }
                    catch (Exception)
                    {

                    }

                    lyrTableRec.IsPlottable = layer.IsPlottable;

                    // Upgrade the Layer table for write
                    lyrTable.UpgradeOpen();

                    // Append the new layer to the Layer table and the transaction
                    layObjId = lyrTable.Add(lyrTableRec);
                    tr.AddNewlyCreatedDBObject(lyrTableRec, true);
                    tr.Commit();

                    return layObjId;
                }
            }
        }

        public static ObjectId GetLinetypeId(this Database db, string linetypeName)
        {
            // Get the linetype table from the database
            var linetypeTable = db.LinetypeTableId.GetObject(OpenMode.ForRead) as LinetypeTable;

            if (linetypeTable.Has(linetypeName))
            {
                return linetypeTable[linetypeName];
            }

            return db.ContinuousLinetype;
        }
    }
}
