using lab4_task3.DTO;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
            InputValidator.AttachTextOnly(FilterTxtOwner);
            InputValidator.AttachIntOnly(FilterTxtMinValue);
            InputValidator.AttachIntOnly(FilterTxtMaxValue);

            _locationFilter = locationFilter;

            ApplyFilters();
        }

        private void ApplyFilters()
        {
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


        private List<Plot> GetPlotsFromDatabase(string location, string purpose, string owner, int minPrice, int maxPrice)
        {
            List<Plot> plots = new List<Plot>();

            using (var conn = new NpgsqlConnection(DB.connectionString))
            {
                conn.Open();

                string sql = @"
                SELECT p.id, p.usage, p.price, o.first_name, o.last_name, o.id as owner_id,
                       l.title as locality, d.water, d.soil, d.coordinates
                FROM properties p
                JOIN owners o ON p.owner = o.id
                JOIN localities l ON p.locality = l.id
                JOIN descriptions d ON p.description = d.id";


                if (!string.IsNullOrWhiteSpace(location)) sql += " AND l.title ILIKE @loc";
                if (!string.IsNullOrWhiteSpace(purpose)) sql += " AND p.usage = @usage";
                if (!string.IsNullOrWhiteSpace(owner)) sql += " AND (o.first_name ILIKE @owner OR o.last_name ILIKE @owner)";
                if (minPrice > 0) sql += " AND p.price::numeric >= @minP";
                if (maxPrice > 0) sql += " AND p.price::numeric <= @maxP";

                using var cmd = new NpgsqlCommand(sql, conn);

                if (!string.IsNullOrWhiteSpace(location)) cmd.Parameters.AddWithValue("loc", "%" + location + "%");
                if (!string.IsNullOrWhiteSpace(purpose)) cmd.Parameters.AddWithValue("usage", purpose);
                if (!string.IsNullOrWhiteSpace(owner)) cmd.Parameters.AddWithValue("owner", "%" + owner + "%");
                if (minPrice > 0) cmd.Parameters.AddWithValue("minP", (decimal)minPrice);
                if (maxPrice > 0) cmd.Parameters.AddWithValue("maxP", (decimal)maxPrice);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var jsonCoords = reader.GetString(reader.GetOrdinal("coordinates"));
                    var points = new List<Point>();

                    var matches = Regex.Matches(jsonCoords, @"[0-9]+(?:\.[0-9]+)?");
                    for (int i = 0; i < matches.Count; i += 2)
                    {
                        if (i + 1 < matches.Count)
                        {
                            double x = double.Parse(matches[i].Value, System.Globalization.CultureInfo.InvariantCulture);
                            double y = double.Parse(matches[i + 1].Value, System.Globalization.CultureInfo.InvariantCulture);
                            points.Add(new Point(x, y));
                        }
                    }

                    plots.Add(new Plot
                    {
                        Id = reader.GetInt32(0),
                        Pryznachennya = reader.GetString(1),
                        MarketValueFormatted = reader.GetDecimal(2).ToString("F2") + " грн",
                        OwnerName = reader.GetString(3) + " " + reader.GetString(4),
                        OwnerId = reader.GetInt32(reader.GetOrdinal("owner_id")),
                        Location = reader.GetString(reader.GetOrdinal("locality")),
                        GroundWater = reader.GetDouble(reader.GetOrdinal("water")),
                        SoilType = reader.GetString(reader.GetOrdinal("soil")),
                        CoordinatePoints = points,
                        Coordinates = new List<string>()
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
            if (LbDilyanky.SelectedItem is Plot selectedPlot)
            {
                string info = $"Власник: {selectedPlot.OwnerName}\n" +
                              $"Розташування: {selectedPlot.Location}\n" +
                              $"Призначення: {selectedPlot.Pryznachennya}\n" +
                              $"Тип ґрунту: {selectedPlot.SoilType}\n" +
                              $"Ринкова вартість: {selectedPlot.MarketValueFormatted}";
                MessageBox.Show(info, "Деталі ділянки", MessageBoxButton.OK, MessageBoxImage.None);
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (LbDilyanky.SelectedItem is Plot selectedPlot)
            {
                AppUtils.NavigateTo(this, new AddEditWindow(selectedPlot));
            }
        }

        private void BtnMap_Click(object sender, RoutedEventArgs e)
        {
            if (LbDilyanky.SelectedItem is Plot selectedPlot)
            {
                var mapWin = new VisualizationWindow();
                mapWin.LoadPlotData(selectedPlot);
                AppUtils.NavigateTo(this, mapWin);
            }
        }
    }
}