using BIMDemo.SQLiteDatabase;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;

namespace BIMDemo.UI.ViewModels
{
    public partial class DemoLayersSettingsVM : ObservableObject
    {
        public ObservableCollection<LayerVM> Layers { get; set; }

        public ObservableCollection<DemoLayerMapVM> LayerMappings { get; set; }

        private ICollectionView _layerMappingsView;
        public ICollectionView LayerMappingsView
        {
            get => _layerMappingsView;
            set => SetProperty(ref _layerMappingsView, value);
        }

        [ObservableProperty]
        private string filter;

        partial void OnFilterChanged(string value)
        {
            ApplyFilter();
        }

        public DemoLayersSettingsVM()
        {
            Layers = new ObservableCollection<LayerVM>();
            LayerMappings = new ObservableCollection<DemoLayerMapVM>();

            LayerMappingsView = CollectionViewSource.GetDefaultView(LayerMappings);
            ApplyFilter();
        }

        public void AddLayerMappings(List<DemoLayerMapVM> mappings)
        {
            foreach (var map in mappings)
            {
                LayerMappings.Add(map);
            }
        }

        public void AddLayers(List<LayerVM> layers)
        {
            foreach (var layer in layers)
            {
                Layers.Add(layer);
            }
        }

        private void ApplyFilter()
        {
            if (LayerMappingsView == null) return;

            LayerMappingsView.Filter = item =>
            {
                if (item is DemoLayerMapVM layerMap)
                {
                    return string.IsNullOrEmpty(Filter) || 
                           layerMap.LayerName.Contains(Filter, StringComparison.OrdinalIgnoreCase) ||
                           (layerMap?.Layer?.Name?.Contains(Filter, StringComparison.OrdinalIgnoreCase) ?? false);
                }
                return false;
            };

            LayerMappingsView.Refresh();
        }

        public void UpdateDatabase(bool checkForModifiedMappings, DemoDbContext dbContext)
        {
            var mappingsToAdd = LayerMappings.Where(x => x.DemoLayerMap is null && 
                                                         !(x.Layer is null))
                                             .Select(x => new DemoLayerMap()
                                             { 
                                                 LayerName = x.LayerName,
                                                 Layer = x.Layer.Layer
                                             }).ToList();

            if (checkForModifiedMappings)
            {
                foreach (var mapping in LayerMappings.Where(x => !(x.DemoLayerMap is null) && 
                                                                 x.DemoLayerMap.Layer?.Name != x.Layer.Name))
                {
                    
                }
            }

            if (mappingsToAdd.Any())
            {
                dbContext.DemoLayerMaps.AddRange(mappingsToAdd);
            }

            dbContext.SaveChanges();
        }

        [RelayCommand]
        private void ImportLayers()
        {
            // Prompt the user to select a template or dwg file
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "DWG Files (*.dwg)|*.dwg|Template Files (*.dwt)|*.dwt";
                openFileDialog.Title = "Select a Template or DWG File";

                if (openFileDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                var templateFilePath = openFileDialog.FileName;

                var layerVms = LayerVM.GetLayers(templateFilePath);

                foreach (var layervm in layerVms.OrderBy(x=> x.Name))
                {
                    Layers.Add(layervm);
                }
            };
        }

        [RelayCommand]
        private void AddLayer()
        {
            var mapping = new DemoLayerMapVM(this, "New Layer", Layers.FirstOrDefault());
            LayerMappings.Add(mapping);
            mapping.IsSelected = true;
        }

        [RelayCommand]
        private void DeleteLayers()
        {

        }
    }
}
