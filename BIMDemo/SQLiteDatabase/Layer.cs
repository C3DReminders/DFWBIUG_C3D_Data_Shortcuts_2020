using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIMDemo.SQLiteDatabase
{
    public class Layer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string PlotStyleName { get; set; }
        public int LineWeight { get; set; }
        public bool IsPlottable { get; set; }
        public string MaterialName { get; set; }
        public string LinetypeName { get; set; }
        public byte TransparencyAlpha { get; set; }
        public int ColorIndex { get; set; }
        public string ColorMap { get; set; }
        public bool IsLocked { get; set; }
        public bool IsOff { get; set; }
        public bool IsFrozen { get; set; }
        public string TemplatePath { get; set; }

        public ICollection<DemoLayerMap> DemoLayerMaps { get; set; }

        public static Layer GetLayer(LayerTableRecord layerTblRec, string templatePath)
        {
            var layer = new Layer()
            {
                Name = layerTblRec.Name,
                Description = layerTblRec.Description,
                ColorIndex = layerTblRec.Color.ColorIndex,
                IsFrozen = layerTblRec.IsFrozen,
                IsLocked = layerTblRec.IsLocked,
                IsOff = layerTblRec.IsOff,
                IsPlottable = layerTblRec.IsPlottable,
                LineWeight = (int)layerTblRec.LineWeight,
                MaterialName = layerTblRec.MaterialId.IsNull ? "" :
                                (layerTblRec.MaterialId.GetObject(OpenMode.ForRead) as Material).Name,
                TemplatePath = templatePath,
                LinetypeName = (layerTblRec.LinetypeObjectId.GetObject(OpenMode.ForRead) as LinetypeTableRecord).Name                
            };

            try
            {
                layer.PlotStyleName = layerTblRec.PlotStyleName;
            }
            catch (Exception)
            {

            }

            try
            {
                layer.TransparencyAlpha = layerTblRec.Transparency.IsByAlpha ? layerTblRec.Transparency.Alpha : (byte)255;
            }
            catch (Exception)
            {

            }

            GetColorInformation(layer, layerTblRec.Color);

            return layer;
        }

        public static void GetColorInformation(Layer layer, Color color)
        {
            if (color.ColorMethod == ColorMethod.ByColor)
            {
                layer.ColorIndex = color.ColorIndex;
                return;
            }

            // To do, make this return a mapping for value for non color index methods. 
            layer.ColorMap = "";
        }

    }

}
