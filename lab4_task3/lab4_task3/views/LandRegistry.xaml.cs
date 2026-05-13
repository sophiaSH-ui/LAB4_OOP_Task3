using lab4_task3.DTO;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace lab4_task3.views
{
    public partial class LandRegistry : Window
    {
        private const string LocationPlaceholder = "Почніть вводити назву...";

        public LandRegistry()
        {
            InitializeComponent();
            InputValidator.AttachTextOnly(CbLocation);
            UpdatePlotsCount();
            this.Activated += (s, e) => UpdatePlotsCount();
        }

        private void CbLocation_LostFocus(object sender, RoutedEventArgs e)
        {
            string locationText = GetValidLocation();
            if (string.IsNullOrWhiteSpace(locationText)) return;

            bool exists = false;
            foreach (var item in CbLocation.Items)
            {
                string itemText = item is ComboBoxItem cbItem ? cbItem.Content?.ToString() : item?.ToString();
                if (string.Equals(itemText, locationText, StringComparison.OrdinalIgnoreCase))
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                CbLocation.Items.Add(new ComboBoxItem { Content = locationText });
            }
        }
       
        private void CbLocation_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdatePlotsCount();
        }

        private void CbLocation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePlotsCount();
        }

        private void UpdatePlotsCount()
        {
            if (CountText == null) return;

            DB db = new DB();
            string location = GetValidLocation();
            var allProperties = db.GetProperties();

            if (!string.IsNullOrEmpty(location))
            {
                CountText.Text = allProperties.Count(p => p.Locality.Title.Equals(location, StringComparison.OrdinalIgnoreCase)).ToString();
            }
            else
            {
                CountText.Text = allProperties.Count().ToString();
            }
        }

        private bool IsLocationValid()
        {
            if (string.IsNullOrEmpty(GetValidLocation()))
            {
                AppUtils.ShowWarning("Будь ласка, спочатку оберіть або введіть населений пункт!");
                return false;
            }
            return true;
        }
        private string GetValidLocation()
        {
            string locationText = CbLocation.Text?.Trim();

            if (!string.IsNullOrWhiteSpace(locationText) && locationText != "Почніть вводити назву...")
            {
                return locationText;
            }

            return string.Empty;
        }

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            AppUtils.NavigateTo(this, new ViewWindow(GetValidLocation()));
        }

        private void BtnAddLandPlot_Click(object sender, RoutedEventArgs e)
        {
            if (!IsLocationValid()) return;
            AppUtils.NavigateTo(this, new AddEditWindow(CbLocation.Text.Trim()));
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e) => AppUtils.GoBack(this);
    }
}