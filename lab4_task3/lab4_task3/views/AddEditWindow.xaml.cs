using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using lab4_task3.DTO;
using lab4_task3.views;
using Npgsql;

namespace lab4_task3
{
    public partial class AddEditWindow : Window
    {
        private bool unsaved = false;
        private List<Owner> ownersList = new List<Owner>();
        private string _location;
        private int _editId = -1;

        public AddEditWindow(Plot plot) : this(plot.Location)
        {
            _editId = plot.Id;
            TxtLocation.IsReadOnly = true;
            TxtLocation.Opacity = 0.6;

            TxtMarketValue.Text = plot.MarketValueFormatted.Replace(" грн", "").Replace(".", ",");
            TxtGroundWater.Text = plot.GroundWater.ToString();

            CbOwner.SelectedValue = plot.OwnerId;

            foreach (ComboBoxItem item in CbPryznachennya.Items)
                if (item.Content?.ToString() == plot.Pryznachennya) { CbPryznachennya.SelectedItem = item; break; }

            foreach (ComboBoxItem item in CbSoilType.Items)
                if (item.Content?.ToString() == plot.SoilType) { CbSoilType.SelectedItem = item; break; }

            LbCoordinates.Items.Clear();
            foreach (var p in plot.CoordinatePoints)
                LbCoordinates.Items.Add($"X: {p.X}  |  Y: {p.Y}");

            unsaved = false;
        }
        public AddEditWindow(string location = "Не вказано")
        {
            InitializeComponent();

            InputValidator.AttachDecimalOnly(TxtMarketValue);
            InputValidator.AttachDecimalOnly(TxtGroundWater);
            InputValidator.AttachDecimalOnly(TxtCoordX);
            InputValidator.AttachDecimalOnly(TxtCoordY);

            _location = location;
            TxtLocation.Text = _location;

            LoadOwnersFromDatabase();
        }

        private void LoadOwnersFromDatabase()
        {
            ownersList.Clear();

            using (var conn = new NpgsqlConnection(DB.connectionString))
            {
                conn.Open();

                string sql = "SELECT id FROM owners ORDER BY last_name, first_name;";

                using var cmd = new NpgsqlCommand(sql, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    ownersList.Add(new Owner(reader.GetInt32(0)));
                }
            }

            RefreshOwnerComboBox();
        }
        private void RefreshOwnerComboBox()
        {
            int selectedId = CbOwner.SelectedValue is int id ? id : -1;

            CbOwner.ItemsSource = null;
            CbOwner.ItemsSource = ownersList.Select(o => new
            {
                Id = o.ID,
                FullName = $"{o.LastName} {o.FirstName}"
            }).ToList();

            CbOwner.DisplayMemberPath = "FullName";
            CbOwner.SelectedValuePath = "Id";

            if (selectedId != -1) CbOwner.SelectedValue = selectedId;
        }

        private Plot BuildPlot(int ownerId, string purpose, double marketValue,
                           double groundWater, string soilType,
                           List<string> coordinates, string description)
        {
            var points = new List<System.Windows.Point>();
            foreach (var coord in coordinates)
            {
                var matches = Regex.Matches(coord, @"[\d.]+");
                if (matches.Count >= 2)
                {
                    double x = double.Parse(matches[0].Value);
                    double y = double.Parse(matches[1].Value);
                    points.Add(new System.Windows.Point(x, y));
                }
            }

            return new Plot
            {
                OwnerId = ownerId,
                Location = _location,
                Purpose = purpose,
                MarketValue = marketValue,
                GroundWater = groundWater,
                SoilType = soilType,
                Description = description,
                Coordinates = coordinates,
                CoordinatePoints = points
            };
        }

        private void UpdatePlotInDatabase(int id, int ownerId, string purpose, double marketValue,
            double groundWater, string soilType, List<string> coordinates, string description)
        {
            Plot updatedPlot = BuildPlot(ownerId, purpose, marketValue, groundWater,
                                         soilType, coordinates, description);
            LocalStorage.UpdatePlot(id, updatedPlot);
            DatabaseSyncService.UpdateInDatabase(id, updatedPlot);
        }

        private void SavePlotToDatabase(int ownerId, string purpose, double marketValue,
            double groundWater, string soilType, List<string> coordinates, string description)
        {
            Plot newPlot = BuildPlot(ownerId, purpose, marketValue, groundWater,
                                     soilType, coordinates, description);
            LocalStorage.SavePlot(newPlot);
            DatabaseSyncService.PushToDatabase(newPlot);
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            AppUtils.GoBack(this);
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (CbOwner.SelectedIndex == -1 ||
                CbPryznachennya.SelectedIndex == -1 ||
                CbSoilType.SelectedIndex == -1 ||
                string.IsNullOrWhiteSpace(TxtMarketValue.Text) ||
                string.IsNullOrWhiteSpace(TxtGroundWater.Text))
            {
                AppUtils.ShowWarning("Будь ласка, заповніть всі основні поля.");
                return;
            }

            if (!double.TryParse(TxtMarketValue.Text, out double marketValue) || marketValue < 0)
            {
                AppUtils.ShowWarning("Введіть коректну ринкову вартість.");
                return;
            }

            if (LbCoordinates.Items.Count < 3)
            {
                AppUtils.ShowWarning("Земельна ділянка повинна мати щонайменше 3 координати.");
                return;
            }

            if (!double.TryParse(TxtGroundWater.Text, out double groundWater))
            {
                AppUtils.ShowWarning("Введіть коректний рівень ґрунтових вод.");
                return;
            }

            if (groundWater < 0 || groundWater > 1000)
            {
                AppUtils.ShowWarning("Значення грунтових вод виходить за межі допустимого діапазону для збереження (0-1000).");
                return;
            }

            Button btnSave = sender as Button;
            string originalContent = btnSave.Content?.ToString();
            btnSave.IsEnabled = false;
            btnSave.Content = "⏳ Перевірка меж...";

            await Task.Delay(2000);

            List<string> coordinates = new List<string>();
            var coordsList = new List<List<int>>();

            foreach (var item in LbCoordinates.Items)
            {
                coordinates.Add(item.ToString());
                var matches = Regex.Matches(item.ToString(), @"[\d.]+");
                if (matches.Count >= 2)
                    coordsList.Add(new List<int> { (int)double.Parse(matches[0].Value), (int)double.Parse(matches[1].Value) });
            }

            var overlappingCoords = new DB().IsOverlapping(coordsList, _location, _editId);

            if (overlappingCoords != null)
            {
                btnSave.IsEnabled = true;
                btnSave.Content = originalContent;
                string coordsText = string.Join("\n", overlappingCoords.Select(c => $"X: {c[0]}  |  Y: {c[1]}"));
                AppUtils.ShowWarning($"Виявлено накладання меж з ділянкою:\n{coordsText}");
                return;
            }

            int ownerId = (int)CbOwner.SelectedValue;
            string purpose = (CbPryznachennya.SelectedItem as ComboBoxItem)?.Content.ToString();
            string soilType = (CbSoilType.SelectedItem as ComboBoxItem)?.Content.ToString();

            string description = $"Населений пункт: {_location}.";

            if (_editId == -1)
            {
                SavePlotToDatabase(ownerId, purpose, marketValue, groundWater, soilType,
                    coordinates, description);
            }
            else
            {
                UpdatePlotInDatabase(_editId, ownerId, purpose, marketValue, groundWater, soilType,
                    coordinates, description);
            }

            unsaved = false;
            AppUtils.ShowInfo("Дані успішно збережено!");
            AppUtils.GoBack(this);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!unsaved) return;
            if (!AppUtils.AskConfirmation("Закрити без збереження?", "Увага")) e.Cancel = true;
        }

        private void BtnAddNewOwner_Click(object sender, RoutedEventArgs e)
        {
            var window = new CreatingAccountOwner();
            window.ShowDialog();

            if (window.CreatedOwner != null)
            {
                LoadOwnersFromDatabase();
                CbOwner.SelectedValue = window.CreatedOwner.ID;
            }
        }

        private void BtnAddCoord_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtCoordX.Text) || string.IsNullOrWhiteSpace(TxtCoordY.Text)) return;
            LbCoordinates.Items.Add($"X: {TxtCoordX.Text}  |  Y: {TxtCoordY.Text}");
            TxtCoordX.Clear(); TxtCoordY.Clear();
            unsaved = true;
        }

        private void BtnRemoveCoord_Click(object sender, RoutedEventArgs e)
        {
            if (LbCoordinates.SelectedIndex != -1) { LbCoordinates.Items.RemoveAt(LbCoordinates.SelectedIndex); unsaved = true; }
        }

        private void LbCoordinates_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BtnRemoveCoord.IsEnabled = LbCoordinates.SelectedItem != null;
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            CbOwner.SelectedIndex = -1;
            CbPryznachennya.SelectedIndex = -1;
            CbSoilType.SelectedIndex = -1;
            TxtMarketValue.Clear(); TxtGroundWater.Clear();
            LbCoordinates.Items.Clear();
            unsaved = true;
        }
    }
}