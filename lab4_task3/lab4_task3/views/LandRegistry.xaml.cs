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
        }

        private void UpdatePlotsCount()
        {
            if (CountText == null || CbLocation == null) return;

            DB db = new DB();
            CountText.Text = db.GetPropertiesCount().ToString();
        }

        private bool IsLocationValid()
        {
            string locationText = CbLocation.Text?.Trim();

            if (string.IsNullOrWhiteSpace(locationText) || locationText == "Почніть вводити назву...")
            {
                AppUtils.ShowWarning("Будь ласка, спочатку оберіть або введіть населений пункт!");
                return false;
            }

            if (locationText.Length < 2 || locationText.Length > 50)
            {
                AppUtils.ShowWarning("Назва населеного пункту має містити від 2 до 50 символів.");
                return false;
            }

            string pattern = @"^[а-яА-ЯіІїЇєЄґҐa-zA-Z\s\-']+$";
            if (!Regex.IsMatch(locationText, pattern))
            {
                AppUtils.ShowWarning("Назва містить недопустимі символи. Використовуйте лише літери, пробіли, дефіси або апостроф.");
                return false;
            }

            CbLocation.Text = locationText;
            return true;
        }

        private void BtnAddLandPlot_Click(object sender, RoutedEventArgs e)
        {
            if (!IsLocationValid()) return;
            AppUtils.NavigateTo(this, new AddEditWindow(CbLocation.Text));
        }
        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            if (IsLocationValid()) AppUtils.NavigateTo(this, new ViewWindow(CbLocation.Text));
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e) => AppUtils.GoBack(this);
    }
}