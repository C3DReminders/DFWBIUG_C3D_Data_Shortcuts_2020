using Autodesk.AutoCAD.DatabaseServices;
using BIMDemo.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIMDemo.SQLiteDatabase.Blocks
{
    public class Block
    {
        public int Id { get; set; }
        public string BlockName { get; set; }
        public int DrawingId { get; set; }
        public Drawing Drawing { get; set; }
        public double InsertionPointX { get; set; }
        public double InsertionPointY { get; set; }
        public double InsertionPointZ { get; set; }
        public int LayerId { get; set; }
        public Layer Layer { get; set; }
        public double ScaleX { get; set; }
        public double ScaleY { get; set; }
        public double ScaleZ { get; set; }
        public double Rotation { get; set; }
        public List<BlockAttribute> Attributes { get; set; }

        public Block()
        {

        }

        public Block(Drawing dwg, BlockReference blkRef)
        {
            BlockName = blkRef.GetBlockName();
            DrawingId = dwg.Id;
            Drawing = dwg;
            InsertionPointX = blkRef.Position.X;
            InsertionPointY = blkRef.Position.Y;
            InsertionPointZ = blkRef.Position.Z;
            ScaleX = blkRef.ScaleFactors.X;
            ScaleY = blkRef.ScaleFactors.Y;
            ScaleZ = blkRef.ScaleFactors.Z;
            Rotation = blkRef.Rotation;
        }
    }
}
