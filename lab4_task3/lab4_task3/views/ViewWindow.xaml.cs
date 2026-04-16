using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace lab4_task3
{
    public partial class ViewWindow : Window
    {
        private string _locationFilter;

        public ViewWindow(string locationFilter = "")
        {
            InitializeComponent();
            _locationFilter = locationFilter;

            ApplyFilters();
        }

        private void ApplyFilters()
        {
            ValidateInputs();

            string purpose = "";
            if (FilterCbPryznachennya != null && FilterCbPryznachennya.SelectedItem is ComboBoxItem selectedComboItem)
            {
                purpose = selectedComboItem.Content?.ToString();
                if (purpose == "Всі") purpose = "";
            }

            string ownerQuery = FilterTxtOwner?.Text?.Trim() ?? "";
            int minPrice = ParsePrice(FilterTxtMinValue?.Text);
            int maxPrice = ParsePrice(FilterTxtMaxValue?.Text);

            var plots = GetPlotsFromDatabase(_locationFilter, purpose, ownerQuery, minPrice, maxPrice);

            if (LbDilyanky != null)
            {
                LbDilyanky.ItemsSource = plots;
                UpdateCount();
            }
        }

        private void ValidateInputs()
        {
            if (FilterTxtOwner != null && !string.IsNullOrWhiteSpace(FilterTxtOwner.Text))
            {
                string text = FilterTxtOwner.Text;
                string cleanText = Regex.Replace(text, @"[^а-яА-ЯіІїЇєЄґҐa-zA-Z\s\-']", "");
                if (text != cleanText)
                {
                    int caret = FilterTxtOwner.CaretIndex > 0 ? FilterTxtOwner.CaretIndex - 1 : 0;
                    FilterTxtOwner.Text = cleanText;
                    FilterTxtOwner.CaretIndex = caret;
                }
            }

            if (FilterTxtMinValue != null && !string.IsNullOrWhiteSpace(FilterTxtMinValue.Text))
            {
                string text = FilterTxtMinValue.Text;
                string cleanText = Regex.Replace(text, @"[^\d]", "");
                if (text != cleanText)
                {
                    int caret = FilterTxtMinValue.CaretIndex > 0 ? FilterTxtMinValue.CaretIndex - 1 : 0;
                    FilterTxtMinValue.Text = cleanText;
                    FilterTxtMinValue.CaretIndex = caret;
                }
            }

            if (FilterTxtMaxValue != null && !string.IsNullOrWhiteSpace(FilterTxtMaxValue.Text))
            {
                string text = FilterTxtMaxValue.Text;
                string cleanText = Regex.Replace(text, @"[^\d]", "");
                if (text != cleanText)
                {
                    int caret = FilterTxtMaxValue.CaretIndex > 0 ? FilterTxtMaxValue.CaretIndex - 1 : 0;
                    FilterTxtMaxValue.Text = cleanText;
                    FilterTxtMaxValue.CaretIndex = caret;
                }
            }
        }

        private List<LandPlotModel> GetPlotsFromDatabase(string location, string purpose, string owner, int minPrice, int maxPrice)
        {

            //зараз працює з тестовими даним, сюдти додати sql запит в БД
            var plots = TestDataStore.GetTestPlots();

            if (!string.IsNullOrWhiteSpace(location))
            {
                plots = plots.Where(p => !string.IsNullOrWhiteSpace(p.Location) &&
                                         p.Location.IndexOf(location, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }

            if (!string.IsNullOrWhiteSpace(purpose))
            {
                plots = plots.Where(p => p.Pryznachennya == purpose).ToList();
            }

            if (!string.IsNullOrWhiteSpace(owner))
            {
                plots = plots.Where(p => p.OwnerName.IndexOf(owner, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }

            if (minPrice > 0)
            {
                plots = plots.Where(p => ParsePrice(p.MarketValueFormatted) >= minPrice).ToList();
            }

            if (maxPrice > 0)
            {
                plots = plots.Where(p => ParsePrice(p.MarketValueFormatted) <= maxPrice).ToList();
            }

            return plots;
        }

        private int ParsePrice(string priceStr)
        {
            if (string.IsNullOrWhiteSpace(priceStr)) return 0;
            string numbers = new string(priceStr.Where(char.IsDigit).ToArray());
            if (int.TryParse(numbers, out int price)) return price;
            return 0;
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            AppUtils.GoBack(this);
        }

        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void BtnResetFilters_Click(object sender, RoutedEventArgs e)
        {
            FilterCbPryznachennya.SelectedIndex = 0;
            FilterTxtOwner.Clear();
            FilterTxtMinValue.Clear();
            FilterTxtMaxValue.Clear();

            ApplyFilters();
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
            AppUtils.ShowInfo("Відкриття деталей...");
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            AppUtils.NavigateTo(this, new AddEditWindow());
        }

        private void BtnMap_Click(object sender, RoutedEventArgs e)
        {
            if (LbDilyanky.SelectedItem is LandPlotModel selectedPlot)
            {
                var mapWin = new VisualizationWindow();
                mapWin.LoadPlotData(selectedPlot);
                AppUtils.NavigateTo(this, mapWin);
            }
        }
    }
}