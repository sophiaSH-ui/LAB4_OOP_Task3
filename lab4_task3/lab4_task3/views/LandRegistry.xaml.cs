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
            CbLocation.AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent,
                                  new TextChangedEventHandler(CbLocation_TextChanged));
        }

        private void CbLocation_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdatePlotsCount();
        }

        private void UpdatePlotsCount()
        {
            if (CountText == null) return;

            DB db = new DB();
            string location = GetValidLocation();

            if (location != null)
            {
                var allProperties = db.GetProperties();
                int count = allProperties.Count(p => p.Locality.Title.Equals(location, StringComparison.OrdinalIgnoreCase));
                CountText.Text = count.ToString();
            }
            else
            {
                CountText.Text = db.GetPropertiesCount().ToString();
            }
        }

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            AppUtils.NavigateTo(this, new ViewWindow(GetValidLocation() ?? ""));
        }

        private bool IsLocationValid()
        {
            if (GetValidLocation() == null)
            {
                AppUtils.ShowWarning("Будь ласка, спочатку оберіть або введіть населений пункт!");
                return false;
            }
            return true;
        }

        private void BtnAddLandPlot_Click(object sender, RoutedEventArgs e)
        {
            if (!IsLocationValid()) return;
            AppUtils.NavigateTo(this, new AddEditWindow(CbLocation.Text.Trim()));
        }

        private string GetValidLocation()
        {
            string locationText = CbLocation?.Text?.Trim();
            bool hasLocation = !string.IsNullOrWhiteSpace(locationText)
                               && locationText != LocationPlaceholder;
            return hasLocation ? locationText : null;
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e) => AppUtils.GoBack(this);
    }
}