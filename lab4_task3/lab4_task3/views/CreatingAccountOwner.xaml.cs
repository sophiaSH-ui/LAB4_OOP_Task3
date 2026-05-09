using System;
using System.Windows;
using lab4_task3.DTO;

namespace lab4_task3.views
{
    public partial class CreatingAccountOwner : Window
    {
        internal Owner CreatedOwner { get; private set; }

        public CreatingAccountOwner()
        {
            InitializeComponent();
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e) => AppUtils.GoBack(this);

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string lastName = TxtLastName.Text?.Trim();
            string firstName = TxtFirstName.Text?.Trim();

            if (string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(firstName) || DpBirthDate.SelectedDate == null)
            {
                AppUtils.ShowWarning("Будь ласка, заповніть всі поля.");
                return;
            }

            DateTime birthDate = DpBirthDate.SelectedDate.Value;
            SaveOwnerToDatabase(firstName, lastName, birthDate);
        }

        private void SaveOwnerToDatabase(string firstName, string lastName, DateTime birthDate)
        {
            try
            {
                CreatedOwner = new Owner(firstName, lastName, birthDate);
                AppUtils.ShowInfo("Нового власника успішно додано!");
                AppUtils.GoBack(this);
            }
            catch (Exception ex)
            {
                CreatedOwner = null;
                AppUtils.ShowWarning($"Помилка при збереженні: {ex.Message}");
            }
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            TxtLastName.Clear(); TxtFirstName.Clear(); DpBirthDate.SelectedDate = null;
        }
    }
}