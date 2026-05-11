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
        private List<Owner> ownersList = new List<Owner>();
        private List<Usage> usagesList = new List<Usage>();
        private string _location;
        private Property _editProperty;

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
            LoadUsagesFromDatabase(); 
        }

        public AddEditWindow(Property property) : this(property.Locality.Title)
        {
            _editProperty = property;
            TxtMarketValue.Text = property.Price.ToString().Replace(".", ",");
            TxtGroundWater.Text = property.Description.Water.ToString();

            CbOwner.SelectedValue = property.Owner.ID;
            CbPryznachennya.SelectedValue = property.Usage.ID;

            foreach (ComboBoxItem item in CbSoilType.Items)
                if (item.Content?.ToString() == property.Description.Soil) { CbSoilType.SelectedItem = item; break; }

            LbCoordinates.Items.Clear();
            foreach (var p in property.Description.Coordinates)
                LbCoordinates.Items.Add($"X: {p[0]}  |  Y: {p[1]}");

            unsaved = false;
        }

        private void LoadOwnersFromDatabase()
        {
            ownersList = new DB().GetOwners().ToList();
            RefreshOwnerComboBox();
        }

        private void LoadUsagesFromDatabase()
        {
            usagesList = new DB().GetUsages().ToList();
            CbPryznachennya.ItemsSource = usagesList;
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

            Button btnSave = sender as Button;
            string originalContent = btnSave.Content?.ToString();
            btnSave.IsEnabled = false;
            btnSave.Content = "⏳ Збереження...";

            await Task.Delay(500);

            double marketValue = double.Parse(TxtMarketValue.Text);
            int groundWater = int.Parse(TxtGroundWater.Text);

            int ownerId = (int)CbOwner.SelectedValue;
            int usageId = (int)CbPryznachennya.SelectedValue;
            string soilType = (CbSoilType.SelectedItem as ComboBoxItem)?.Content.ToString();

            List<List<int>> coords = new List<List<int>>();
            foreach (var item in LbCoordinates.Items)
            {
                var matches = Regex.Matches(item.ToString(), @"-?[\d.]+");
                coords.Add(new List<int> { (int)double.Parse(matches[0].Value), (int)double.Parse(matches[1].Value) });
            }

            DB db = new DB();

            if (_editProperty == null)
            {
                Locality locality = new Locality(_location);
                Description desc = new Description(groundWater, soilType, coords);
                Owner owner = new Owner(ownerId);
                Usage usage = new Usage(usageId);

                Property newProp = new Property(owner, desc, locality, usage, marketValue);

                var allProps = db.GetProperties();
                if (db.CheckOverlapping(newProp, allProps) != null)
                {
                    newProp.Delete();
                    desc.Delete();
                    locality.Delete();

                    btnSave.IsEnabled = true;
                    btnSave.Content = originalContent;
                    AppUtils.ShowWarning("Земельна ділянка накладається на існуючу!");
                    return;
                }
            }
            else
            {
                var oldWater = _editProperty.Description.Water;
                var oldSoil = _editProperty.Description.Soil;
                var oldCoords = _editProperty.Description.Coordinates;

                _editProperty.Description.Update(_editProperty.Description.ID, groundWater, soilType, coords);

                var allProps = db.GetProperties();
                var updatedProp = allProps.FirstOrDefault(p => p.ID == _editProperty.ID);

                if (updatedProp != null && db.CheckOverlapping(updatedProp, allProps) != null)
                {
                    _editProperty.Description.Update(_editProperty.Description.ID, oldWater, oldSoil, oldCoords);

                    btnSave.IsEnabled = true;
                    btnSave.Content = originalContent;
                    AppUtils.ShowWarning("Земельна ділянка накладається на існуючу!");
                    return;
                }

                Owner owner = new Owner(ownerId);
                Usage usage = new Usage(usageId);
                _editProperty.Update(owner, updatedProp.Description, _editProperty.Locality, usage, marketValue);
            }

            btnSave.IsEnabled = true;
            btnSave.Content = originalContent;
            unsaved = false;
            AppUtils.ShowInfo("Дані успішно збережено!");
            AppUtils.GoBack(this);
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
            LoadOwnersFromDatabase();
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