using lab4_task3.DTO;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace lab4_task3.views
{
    public partial class LandRegistry : Window
    {
        public LandRegistry()
        {
            InitializeComponent();
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

            string locationText = CbLocation?.Text?.Trim();
            bool hasLocation = !string.IsNullOrWhiteSpace(locationText)
                               && locationText != "Почніть вводити назву...";

            DB db = new DB();
            if (hasLocation)
            {
                CountText.Text = db.GetPropertiesCountByLocation(locationText).ToString();
            }
            else
            {
                CountText.Text = db.GetPropertiesCount().ToString(); 
            }
        }


        private bool IsLocationValid()
        {
            string locationText = CbLocation.Text?.Trim();

            if (string.IsNullOrWhiteSpace(locationText) || locationText == "Почніть вводити назву...")
            {
                AppUtils.ShowWarning("Будь ласка, спочатку оберіть або введіть населений пункт!");
                return false;
            }

            string pattern = @"^[а-яА-ЯіІїЇєЄґҐa-zA-Z\s\-']+$";
            if (!Regex.IsMatch(locationText, pattern))
            {
                AppUtils.ShowWarning("Назва містить недопустимі символи.");
                return false;
            }

            return true;
        }

        private void BtnAddLandPlot_Click(object sender, RoutedEventArgs e)
        {
            if (!IsLocationValid()) return; 
            AppUtils.NavigateTo(this, new AddEditWindow(CbLocation.Text.Trim()));
        }

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            string locationText = CbLocation.Text?.Trim();
            bool hasLocation = !string.IsNullOrWhiteSpace(locationText)
                               && locationText != "Почніть вводити назву...";

            AppUtils.NavigateTo(this, new ViewWindow(hasLocation ? locationText : ""));
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e) => AppUtils.GoBack(this);
    }
}