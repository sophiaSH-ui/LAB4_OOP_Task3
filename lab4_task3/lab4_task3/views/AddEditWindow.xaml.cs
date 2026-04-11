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
using System.ComponentModel;

namespace lab4_task3
{
    public partial class AddEditWindow : Window
    {
        private bool unsaved = true;

        public AddEditWindow()
        {
            InitializeComponent();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // заглушка треба дописувати 
            unsaved = false;
            MessageBox.Show("Дані збережено!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {

            TxtLastName.Clear();
            TxtFirstName.Clear();
            DpBirthDate.SelectedDate = null;

            CbPryznachennya.SelectedIndex = -1;
            TxtMarketValue.Clear();
            TxtLength.Clear();
            TxtWidth.Clear();

            TxtGroundWater.Clear();
            CbSoilType.SelectedIndex = -1;
            ChkRiver.IsChecked = ChkFlat.IsChecked = ChkFertile.IsChecked =
            ChkForest.IsChecked = ChkRoad.IsChecked = false;

            TxtCoordX.Clear();
            TxtCoordY.Clear(); 
            LbCoordinates.Items.Clear(); 

            unsaved = true;
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!unsaved) return;

            var res = MessageBox.Show("Закрити без збереження?", "Увага", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res == MessageBoxResult.No) e.Cancel = true;
        }

        private void BtnAddCoord_Click(object sender, RoutedEventArgs e)
        {
            unsaved = true;
        }

        private void BtnRemoveCoord_Click(object sender, RoutedEventArgs e)
        {
            unsaved = true;
        }

        private void LbCoordinates_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BtnRemoveCoord.IsEnabled = LbCoordinates.SelectedItem != null;
        }

        private void BtnVisualize_Click(object sender, RoutedEventArgs e)
        {
            var win = new VisualizationWindow();
            win.Owner = this;
            win.ShowDialog();
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}