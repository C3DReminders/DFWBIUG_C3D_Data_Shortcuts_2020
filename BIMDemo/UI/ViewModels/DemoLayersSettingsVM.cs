using BIMDemo.SQLiteDatabase;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quux.AcadUtilities.CommandParameters;
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

        private ICollectionView _layersView;
        public ICollectionView LayersView
        {
            get => _layersView;
            set => SetProperty(ref _layersView, value);
        }

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

        [ObservableProperty]
        private string suffix;

        public List<DemoLayerMap> DemoLayerMapsToDelete { get; set; }

        public DemoLayersSettingsVM()
        {
            DemoLayerMapsToDelete = new List<DemoLayerMap>();
            Layers = new ObservableCollection<LayerVM>();
            LayerMappings = new ObservableCollection<DemoLayerMapVM>();

            LayerMappingsView = CollectionViewSource.GetDefaultView(LayerMappings);
            ApplyFilter();

            LayersView = CollectionViewSource.GetDefaultView(Layers);

            Suffix = CommandDefault.ReadCommandDefault(nameof(Suffix) + "DemoLayersKey", "", PersistenceLocation.RegistryOnly);
        }

        partial void OnSuffixChanged(string value)
        {
            
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

            ApplyLayersSort();
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

        private void ApplyLayersSort()
        {
            if (LayersView == null) return;

            LayersView.SortDescriptions.Clear();
            LayersView.SortDescriptions.Add(new SortDescription(nameof(LayerVM.Name), ListSortDirection.Ascending));

            LayersView.Refresh();
        }

        public void UpdateDatabase(DemoDbContext dbContext)
        {
            var mappingsToAdd = LayerMappings.Where(x => x.DemoLayerMap is null && 
                                                         !(x.Layer is null))
                                             .Select(x => new DemoLayerMap()
                                             { 
                                                 LayerName = x.LayerName,
                                                 Layer = x.Layer.Layer
                                             }).ToList();

            if (mappingsToAdd.Any())
            {
                dbContext.DemoLayerMaps.AddRange(mappingsToAdd);
            }

            dbContext.DemoLayerMaps.RemoveRange(DemoLayerMapsToDelete);

            dbContext.SaveChanges();

            CommandDefault.WriteCommandDefault(nameof(Suffix) + "DemoLayersKey", Suffix, PersistenceLocation.RegistryOnly);
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
            var mappintsToDelete = LayerMappings.Where(x => x.IsSelected).ToList();
            foreach (var layerMapping in mappintsToDelete)
            {
                LayerMappings.Remove(layerMapping);
                if (layerMapping.DemoLayerMap is null || layerMapping.DemoLayerMap.Id == 0)
                {
                    continue;
                }

                DemoLayerMapsToDelete.Add(layerMapping.DemoLayerMap);
            }
        }

        [RelayCommand]
        private void ApplySuffix()
        {
            if (string.IsNullOrEmpty(Suffix))
            {
                return;
            }

            foreach (var mapping in LayerMappings.Where(x => x.IsSelected))
            {
                var demoLayer = mapping.LayerName + Suffix;
                var layer = Layers.Where(x => x.Equals(demoLayer)).FirstOrDefault();

                if (layer != null)
                {
                    mapping.Layer = layer;
                    continue;
                }

                var layerVm = new LayerVM(demoLayer, "DEMO", "DEMO Suffix");
                Layers.Add(layerVm);
                mapping.UpdateLayer(layerVm);
            }

            ApplyLayersSort();
        }
    }
}
