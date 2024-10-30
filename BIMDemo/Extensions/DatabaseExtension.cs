using Autodesk.AutoCAD.DatabaseServices;
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
    }
}
