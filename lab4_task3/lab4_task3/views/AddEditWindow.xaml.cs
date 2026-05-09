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
                using var cmd = new NpgsqlCommand("SELECT id FROM owners ORDER BY last_name, first_name;", conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read()) ownersList.Add(new Owner(reader.GetInt32(0)));
            }
            RefreshOwnerComboBox();
        }

        private void RefreshOwnerComboBox()
        {
            int selectedId = CbOwner.SelectedValue is int id ? id : -1;
            CbOwner.ItemsSource = ownersList.Select(o => new { Id = o.ID, FullName = $"{o.LastName} {o.FirstName}" }).ToList();
            CbOwner.DisplayMemberPath = "FullName";
            CbOwner.SelectedValuePath = "Id";
            if (selectedId != -1) CbOwner.SelectedValue = selectedId;
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (CbOwner.SelectedIndex == -1 || CbPryznachennya.SelectedIndex == -1 || CbSoilType.SelectedIndex == -1 ||
                string.IsNullOrWhiteSpace(TxtMarketValue.Text) || string.IsNullOrWhiteSpace(TxtGroundWater.Text))
            {
                AppUtils.ShowWarning("Будь ласка, заповніть всі основні поля.");
                return;
            }

            if (LbCoordinates.Items.Count < 3)
            {
                AppUtils.ShowWarning("Земельна ділянка повинна мати щонайменше 3 координати.");
                return;
            }

            double marketValue = double.Parse(TxtMarketValue.Text);
            int groundWater = int.Parse(TxtGroundWater.Text);
            int ownerId = (int)CbOwner.SelectedValue;
            string purpose = (CbPryznachennya.SelectedItem as ComboBoxItem)?.Content.ToString();
            string soilType = (CbSoilType.SelectedItem as ComboBoxItem)?.Content.ToString();

            List<List<int>> coords = new List<List<int>>();
            foreach (var item in LbCoordinates.Items)
            {
                var matches = Regex.Matches(item.ToString(), @"[\d.]+");
                coords.Add(new List<int> { (int)double.Parse(matches[0].Value), (int)double.Parse(matches[1].Value) });
            }

            if (_editId == -1)
            {
                Locality locality = new Locality(_location);
                Description desc = new Description(groundWater, soilType, coords);
                Owner owner = new Owner(ownerId);
                new Property(owner, desc, locality, purpose, marketValue);
            }
            else
            {
                UpdateExistingPlot(_editId, ownerId, purpose, marketValue, groundWater, soilType, coords);
            }

            unsaved = false;
            AppUtils.ShowInfo("Дані успішно збережено!");
            AppUtils.GoBack(this);
        }

        private void UpdateExistingPlot(int propertyId, int ownerId, string purpose, double marketValue, int water, string soil, List<List<int>> coords)
        {
            using var conn = new NpgsqlConnection(DB.connectionString);
            conn.Open();
            
            using var cmd = new NpgsqlCommand("SELECT description, locality FROM properties WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", propertyId);
            using var reader = cmd.ExecuteReader();
            reader.Read();
            int descId = reader.GetInt32(0);
            int locId = reader.GetInt32(1);
            reader.Close();

            Description desc = new Description(descId);
            // Тут викликається метод Update(), який буде в DTO
            // desc.Update(water, soil, coords); 

            Property prop = new Property(new Owner(ownerId), desc, new Locality(locId), propertyId);
            // prop.Update(ownerId, locId, descId, purpose, marketValue);
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e) => AppUtils.GoBack(this);

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (unsaved && !AppUtils.AskConfirmation("Закрити без збереження?", "Увага")) e.Cancel = true;
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

        private void LbCoordinates_SelectionChanged(object sender, SelectionChangedEventArgs e) => BtnRemoveCoord.IsEnabled = LbCoordinates.SelectedItem != null;

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            CbOwner.SelectedIndex = -1; CbPryznachennya.SelectedIndex = -1; CbSoilType.SelectedIndex = -1;
            TxtMarketValue.Clear(); TxtGroundWater.Clear(); LbCoordinates.Items.Clear();
            unsaved = true;
        }
    }
}