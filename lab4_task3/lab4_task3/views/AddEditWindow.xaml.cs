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

namespace lab4_task3
{
    public partial class AddEditWindow : Window
    {
        private bool unsaved = false;
        private Random random = new Random();
        private List<OwnerJson> ownersList = new List<OwnerJson>();
        private string _location;

        public AddEditWindow(string location = "Не вказано")
        {
            InitializeComponent();
            _location = location;
            TxtLocation.Text = _location;

            LoadOwnersFromDatabase();

            TxtMarketValue.TextChanged += NumericInput_TextChanged;
            TxtGroundWater.TextChanged += NumericInput_TextChanged;
            TxtCoordX.TextChanged += NumericInput_TextChanged;
            TxtCoordY.TextChanged += NumericInput_TextChanged;
        }

        private void LoadOwnersFromDatabase()
        {
            ownersList = LocalStorage.LoadOwners();
            RefreshOwnerComboBox();
        }

        private void RefreshOwnerComboBox()
        {
            int selectedId = CbOwner.SelectedValue is int id ? id : -1;

            CbOwner.ItemsSource = null;
            CbOwner.ItemsSource = ownersList.Select(o => new
            {
                Id = o.Id,
                FullName = $"{o.LastName} {o.FirstName}"
            }).ToList();

            CbOwner.DisplayMemberPath = "FullName";
            CbOwner.SelectedValuePath = "Id";

            if (selectedId != -1) CbOwner.SelectedValue = selectedId;
        }

        private void SavePlotToDatabase(int ownerId, string purpose, double marketValue, double groundWater,
    string soilType, bool river, bool flat, bool fertile, bool forest, bool road, List<string> coordinates, string description)
        {
            Plot newPlot = new Plot
            {
                OwnerId = ownerId,
                Location = _location,
                Purpose = purpose,
                MarketValue = marketValue,
                GroundWater = groundWater,
                SoilType = soilType,
                Description = description,
                Coordinates = coordinates,
                HasRiver = river,
                IsFlat = flat,
                IsFertile = fertile,
                NearForest = forest,
                NearRoad = road
            };

            LocalStorage.SavePlot(newPlot);
            DatabaseSyncService.PushToDatabase(newPlot);

            
        }

        private bool CheckForOverlapMock(List<string> coordinates)
        {
            return random.Next(100) < 30;
        }

        private void NumericInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                string text = textBox.Text;
                string cleanText = Regex.Replace(text, @"[^\d,.-]", "");
                if (text != cleanText)
                {
                    int caret = textBox.CaretIndex > 0 ? textBox.CaretIndex - 1 : 0;
                    textBox.Text = cleanText;
                    textBox.CaretIndex = caret;
                }
            }
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
            foreach (var item in LbCoordinates.Items)
            {
                coordinates.Add(item.ToString());
            }

            bool isOverlapping = CheckForOverlapMock(coordinates);

            if (isOverlapping)
            {
                btnSave.IsEnabled = true;
                btnSave.Content = originalContent;
                AppUtils.ShowWarning("Збій збереження! Виявлено накладання меж.");
                return;
            }

            int ownerId = (int)CbOwner.SelectedValue;
            string purpose = (CbPryznachennya.SelectedItem as ComboBoxItem)?.Content.ToString();
            string soilType = (CbSoilType.SelectedItem as ComboBoxItem)?.Content.ToString();
            string geoFeature = (CbGeoFeature.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Рівнинна";

            string description = $"Населений пункт: {_location}. Географічна ознака: {geoFeature}.";

            SavePlotToDatabase(ownerId, purpose, marketValue, groundWater, soilType,
                ChkRiver.IsChecked == true, ChkFlat.IsChecked == true, ChkFertile.IsChecked == true,
                ChkForest.IsChecked == true, ChkRoad.IsChecked == true, coordinates, description);

            unsaved = false;
            AppUtils.ShowInfo("Дані успішно збережено!");
            this.Close();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!unsaved) return;
            if (!AppUtils.AskConfirmation("Закрити без збереження?", "Увага")) e.Cancel = true;
        }

        //private void BtnVisualize_Click(object sender, RoutedEventArgs e)
        //{
        //    AppUtils.ShowDialog(this, new VisualizationWindow());
        //}

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
            CbGeoFeature.SelectedIndex = -1;
            TxtMarketValue.Clear(); TxtGroundWater.Clear();
            LbCoordinates.Items.Clear();
            unsaved = true;
        }
    }
}