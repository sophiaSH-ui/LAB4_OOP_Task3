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

namespace lab4_task3
{
    public partial class ViewWindow : Window
    {
        public ViewWindow()
        {
            InitializeComponent();
            UpdateCount();
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Filter_Changed(object sender, EventArgs e)
        {
            UpdateCount();
        }

        private void BtnResetFilters_Click(object sender, RoutedEventArgs e)
        {
            FilterCbPryznachennya.SelectedIndex = 0;
            FilterTxtOwner.Clear();
            FilterTxtMinValue.Clear();
            FilterTxtMaxValue.Clear();
            UpdateCount();
        }

        private void LbDilyanky_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = LbDilyanky.SelectedItem != null;
            BtnDetails.IsEnabled = hasSelection;
            BtnEdit.IsEnabled = hasSelection;
            BtnMap.IsEnabled = hasSelection;
        }

        private void UpdateCount()
        {
            if (ResultCountText != null && LbDilyanky != null)
            {
                ResultCountText.Text = $"Знайдено: {LbDilyanky.Items.Count}";
            }
        }

        private void BtnDetails_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Відкриття деталей...");
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var editWin = new AddEditWindow();
            editWin.Owner = this;
            editWin.ShowDialog();
        }

        private void BtnMap_Click(object sender, RoutedEventArgs e)
        {
            var mapWin = new VisualizationWindow();
            mapWin.Owner = this;
            mapWin.ShowDialog();
        }
    }
}