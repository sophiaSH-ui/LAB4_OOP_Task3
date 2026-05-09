using lab4_task3.DTO;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace lab4_task3
{
    public partial class ViewWindow : Window
    {
        private string _locationFilter;
        private ObservableCollection<Property> _allProperties;
        private ObservableCollection<Property> _filteredProperties = new ObservableCollection<Property>();

        public ViewWindow(string locationFilter = "")
        {
            InitializeComponent();
            InputValidator.AttachTextOnly(FilterTxtOwner);
            InputValidator.AttachIntOnly(FilterTxtMinValue);
            InputValidator.AttachIntOnly(FilterTxtMaxValue);

            _locationFilter = locationFilter;
            DgDilyanky.ItemsSource = _filteredProperties;

            LoadData();
        }

        private void LoadData()
        {
            _allProperties = new DB().GetProperties();
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_allProperties == null) return;

            var query = _allProperties.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(_locationFilter))
                query = query.Where(p => p.Locality.Title.IndexOf(_locationFilter, StringComparison.OrdinalIgnoreCase) >= 0);

            if (FilterCbPryznachennya?.SelectedItem is ComboBoxItem cbItem && cbItem.Content.ToString() != "Всі")
            {
                string purpose = cbItem.Content.ToString();
                query = query.Where(p => p.Usage == purpose);
            }

            string ownerQuery = FilterTxtOwner?.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(ownerQuery))
                query = query.Where(p => (p.Owner.LastName + " " + p.Owner.FirstName).IndexOf(ownerQuery, StringComparison.OrdinalIgnoreCase) >= 0);

            int minPrice = ParsePrice(FilterTxtMinValue?.Text);
            if (minPrice > 0) query = query.Where(p => p.Price >= minPrice);

            int maxPrice = ParsePrice(FilterTxtMaxValue?.Text);
            if (maxPrice > 0) query = query.Where(p => p.Price <= maxPrice);

            _filteredProperties.Clear();
            foreach (var p in query)
            {
                _filteredProperties.Add(p);
            }
            UpdateCount();
        }

        private int ParsePrice(string priceStr)
        {
            if (string.IsNullOrWhiteSpace(priceStr)) return 0;
            string numbers = new string(priceStr.Where(char.IsDigit).ToArray());
            if (int.TryParse(numbers, out int price)) return price;
            return 0;
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e) => AppUtils.GoBack(this);
        private void Filter_Changed(object sender, RoutedEventArgs e) => ApplyFilters();

        private void BtnResetFilters_Click(object sender, RoutedEventArgs e)
        {
            FilterCbPryznachennya.SelectedIndex = 0;
            FilterTxtOwner.Clear();
            FilterTxtMinValue.Clear();
            FilterTxtMaxValue.Clear();
            ApplyFilters();
        }

        private void DgDilyanky_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = DgDilyanky.SelectedItem != null;
            BtnDetails.IsEnabled = hasSelection;
            BtnEdit.IsEnabled = hasSelection;
            BtnMap.IsEnabled = hasSelection;
            BtnDelete.IsEnabled = hasSelection;
        }

        private void UpdateCount()
        {
            if (ResultCountText != null) ResultCountText.Text = $"Знайдено: {_filteredProperties.Count}";
        }

        private void BtnDetails_Click(object sender, RoutedEventArgs e)
        {
            if (DgDilyanky.SelectedItem is Property selectedPlot)
            {
                string info = $"Власник: {selectedPlot.Owner.LastName} {selectedPlot.Owner.FirstName}\n" +
                              $"Розташування: {selectedPlot.Locality.Title}\n" +
                              $"Призначення: {selectedPlot.Usage}\n" +
                              $"Тип ґрунту: {selectedPlot.Description.Soil}\n" +
                              $"Ринкова вартість: {selectedPlot.Price:F2} грн";
                MessageBox.Show(info, "Деталі ділянки", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (DgDilyanky.SelectedItem is Property selectedPlot)
            {
                var editWin = new AddEditWindow(selectedPlot);
                editWin.Closed += (s, args) => LoadData();
                AppUtils.NavigateTo(this, editWin);
            }
        }

        private void BtnMap_Click(object sender, RoutedEventArgs e)
        {
            if (DgDilyanky.SelectedItem is Property selectedPlot)
            {
                var mapWin = new VisualizationWindow();
                mapWin.LoadPlotData(selectedPlot);
                AppUtils.NavigateTo(this, mapWin);
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (DgDilyanky.SelectedItem is Property selectedPlot)
            {
                if (AppUtils.AskConfirmation("Ви впевнені, що хочете видалити цю ділянку?", "Видалення"))
                {
                    selectedPlot.Delete();
                    selectedPlot.Description.Delete();
                    LoadData();
                }
            }
        }
    }
}