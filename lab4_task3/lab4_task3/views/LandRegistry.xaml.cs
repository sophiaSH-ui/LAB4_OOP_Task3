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

            CbLocation.SelectionChanged += (s, e) => UpdatePlotsCount();
            CbLocation.AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent, new TextChangedEventHandler((s, e) => UpdatePlotsCount()));
        }

        private void UpdatePlotsCount()
        {
            if (CountText == null || CbLocation == null) return;

            var allPlots = TestDataStore.GetTestPlots();
            string loc = CbLocation.Text?.Trim();

            if (string.IsNullOrWhiteSpace(loc) || loc == "Почніть вводити назву...")
            {
                CountText.Text = allPlots.Count.ToString();
            }
            else
            {
                int count = allPlots.Count(p =>
                    !string.IsNullOrWhiteSpace(p.Location) &&
                    p.Location.IndexOf(loc, StringComparison.OrdinalIgnoreCase) >= 0);

                CountText.Text = count.ToString();
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

            AppUtils.NavigateTo(this, new AddEditWindow());
        }

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            string loc = CbLocation.Text?.Trim();

            if (string.IsNullOrWhiteSpace(loc) || loc == "Почніть вводити назву...")
            {
                loc = "";
            }
            else
            {
                if (!IsLocationValid()) return;
            }
 AppUtils.NavigateTo(this, new ViewWindow(loc));
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            AppUtils.GoBack(this);
        }
    }
}