using Autodesk.AutoCAD.DatabaseServices;
using BIMDemo.SQLiteDatabase;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIMDemo.UI.ViewModels
{
    public class LayerVM : ObservableObject
    {
        public Layer Layer { get; set; }

        public int Id { get; set; }
        
        private string _name;
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        public string Description { get; set; }

        private string _template;
        public string Template
        {
            get { return _template; }
            set { SetProperty(ref _template, value); }
        }

        public LayerVM(string name, string description, string template)
        {
            Name = name;
            Description = description;
            Template = template;

            Layer = new Layer()
            {
                Name = name,
                Description = description,
                TemplatePath = ""
            };
        }

        public LayerVM(Layer layer)
        {
            if (layer is null)
            {
                return;
            }
            Id = layer.Id;
            Name = layer.Name;
            Description = layer.Description;
            Template = layer.TemplatePath;
            Layer = layer;
        }

        public static List<LayerVM> GetLayers(string template)
        {
            var layerVms = new List<LayerVM>();

            try
            {
                using (var dwgDb = new Database(false, true))
                {
                    dwgDb.ReadDwgFile(template, FileShare.Read, true, "");

                    using (var tr = dwgDb.TransactionManager.StartTransaction())
                    {
                        var layerTbl = dwgDb.LayerTableId.GetObject(OpenMode.ForRead) as LayerTable;

                        foreach (var layerObjId in layerTbl)
                        {
                            var layerTblRec = layerObjId.GetObject(OpenMode.ForRead) as LayerTableRecord;
                            var layer= Layer.GetLayer(layerTblRec, template);

                            var layerVM = new LayerVM(layer);

                            layerVms.Add(layerVM);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                BIMDemoApp.WriteErrorMessage(ex.Message, ex);
            }

            return layerVms;
        }

        public static List<LayerVM> GetLayers(List<string> layerNames)
        {
            var layerVms = new List<LayerVM>();

            try
            {
                var layerTbl = HostApplicationServices.WorkingDatabase.LayerTableId.GetObject(OpenMode.ForRead) as LayerTable;

                foreach (var layerObjId in layerTbl)
                {
                    var layerTblRec = layerObjId.GetObject(OpenMode.ForRead) as LayerTableRecord;

                    if (layerNames.Contains(layerTblRec.Name))
                    {
                        continue;
                    }

                    var layer = Layer.GetLayer(layerTblRec, HostApplicationServices.WorkingDatabase.Filename);

                    var layerVM = new LayerVM(layer);

                    layerVms.Add(layerVM);
                }
            }
            catch (Exception ex)
            {
                BIMDemoApp.WriteErrorMessage(ex.Message, ex);
            }

            return layerVms;
        }
    }
}
