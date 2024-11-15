using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIMDemo.Extensions
{
    public static class BlockReferenceExtensions
    {
        public static string GetBlockName(this BlockReference blkRef)
        {
            if (blkRef.IsDynamicBlock)
            {
                return (blkRef.DynamicBlockTableRecord.GetObject(OpenMode.ForRead) as BlockTableRecord).Name;
            }

            return blkRef.Name;
        }
    }
}
