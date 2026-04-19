using lab4_task3.DTO;
using Npgsql;
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
            List<LandPlotModel> plots = new List<LandPlotModel>();

            using (var conn = new NpgsqlConnection(DB.connectionString))
            {
                conn.Open();

                // SQL-запит з JOIN для отримання всіх пов'язаних даних
                string sql = @"
            SELECT p.id, p.usage, p.price, o.first_name, o.last_name, l.title as locality, d.water, d.soil
            FROM properties p
            JOIN owners o ON p.owner = o.id
            JOIN localities l ON p.locality = l.id
            JOIN descriptions d ON p.description = d.id
            WHERE 1=1";

                if (!string.IsNullOrWhiteSpace(location)) sql += " AND l.title ILIKE @loc";
                if (!string.IsNullOrWhiteSpace(purpose)) sql += " AND p.usage = @usage";
                if (!string.IsNullOrWhiteSpace(owner)) sql += " AND (o.first_name ILIKE @owner OR o.last_name ILIKE @owner)";
                if (minPrice > 0) sql += " AND p.price >= @minP";
                if (maxPrice > 0) sql += " AND p.price <= @maxP";

                using var cmd = new NpgsqlCommand(sql, conn);

                if (!string.IsNullOrWhiteSpace(location)) cmd.Parameters.AddWithValue("loc", "%" + location + "%");
                if (!string.IsNullOrWhiteSpace(purpose)) cmd.Parameters.AddWithValue("usage", purpose);
                if (!string.IsNullOrWhiteSpace(owner)) cmd.Parameters.AddWithValue("owner", "%" + owner + "%");
                if (minPrice > 0) cmd.Parameters.AddWithValue("minP", (decimal)minPrice);
                if (maxPrice > 0) cmd.Parameters.AddWithValue("maxP", (decimal)maxPrice);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    plots.Add(new LandPlotModel
                    {
                        Id = reader.GetInt32(0),
                        Pryznachennya = reader.GetString(1),
                        MarketValueFormatted = reader.GetDecimal(2).ToString("F2") + " грн",
                        OwnerName = reader.GetString(3) + " " + reader.GetString(4),
                        Location = reader.GetString(5)
                      
                    });
                }
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