using BIMDemo.SQLiteDatabase;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIMDemo.UI.ViewModels
{
    public partial class DemoLayerMapVM : ObservableObject
    {
        public DemoLayersSettingsVM Parent { get; set; }

        public DemoLayerMap DemoLayerMap { get; set; }

        private string _layerName;
		public string LayerName
        {
			get { return _layerName; }
			set 
            {
                SetProperty(ref _layerName, value);

                if (DemoLayerMap is null)
                {
                    return;
                }

                DemoLayerMap.LayerName = value;
            }
		}

		private LayerVM _layer;
		public LayerVM Layer
		{
			get { return _layer; }
			set 
            { 
                SetProperty(ref _layer, value);

                if (Parent is null)
                {
                    return;
                }

                foreach (var layerMapping in Parent.LayerMappings.Where(x => x.IsSelected))
                {
                    layerMapping.UpdateLayer(value);
                }

                if (DemoLayerMap is null)
                {
                    return;
                }

                DemoLayerMap.Layer = value?.Layer;
            }
		}

        [ObservableProperty]
        private bool isSelected;

        public DemoLayerMapVM(DemoLayersSettingsVM parent, string layerName, LayerVM layerVm)
        {
			Parent = parent;
            LayerName = layerName;
            UpdateLayer(layerVm);
        }

		public DemoLayerMapVM(DemoLayersSettingsVM parent, DemoLayerMap demoLayerMap, Layer layerDb)
        {
			Parent = parent;

            LayerName = demoLayerMap.LayerName;
            Layer = new LayerVM(layerDb);

            DemoLayerMap = demoLayerMap;
        }

        public DemoLayerMapVM(DemoLayersSettingsVM parent, DemoLayerMap demoLayerMap, LayerVM layerVm)
        {
            Parent = parent;

            LayerName = demoLayerMap.LayerName;
            Layer = layerVm;

            DemoLayerMap = demoLayerMap;
        }

        public void UpdateLayer(LayerVM layer)
        {
            SetProperty(ref _layer, layer, nameof(Layer));
        }

    }
}
