using BIMDemo.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BIMDemo.UI.Views
{
    /// <summary>
    /// Interaction logic for DemoLayersSettingsWindow.xaml
    /// </summary>
    public partial class DemoLayersSettingsWindow : Window
    {
        public DemoLayersSettingsWindow(DemoLayersSettingsVM model)
        {
            InitializeComponent();

            DataContext = model;
        }

        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
