using System;
using System.Text.RegularExpressions;
using System.Windows;

namespace lab4_task3.views
{
    public partial class CreatingAccountOwner : Window
    {
        public CreatingAccountOwner()
        {
            InitializeComponent();
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            AppUtils.GoBack(this);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string lastName = TxtLastName.Text?.Trim();
            string firstName = TxtFirstName.Text?.Trim();

            if (string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(firstName) ||
                DpBirthDate.SelectedDate == null)
            {
                AppUtils.ShowWarning("Будь ласка, заповніть всі поля.");
                return;
            }

            if (lastName.Length < 2 || lastName.Length > 50 ||
                firstName.Length < 2 || firstName.Length > 50)
            {
                AppUtils.ShowWarning("Ім'я та прізвище повинні містити від 2 до 50 символів.");
                return;
            }

            string namePattern = @"^[а-яА-ЯіІїЇєЄґҐa-zA-Z\-']+$";

            if (!Regex.IsMatch(lastName, namePattern) || !Regex.IsMatch(firstName, namePattern))
            {
                AppUtils.ShowWarning("Ім'я та прізвище можуть містити лише літери, дефіс або апостроф.");
                return;
            }

            DateTime birthDate = DpBirthDate.SelectedDate.Value;

            if (birthDate > DateTime.Now)
            {
                AppUtils.ShowWarning("Дата народження не може бути в майбутньому.");
                return;
            }

            if (birthDate < new DateTime(1900, 1, 1))
            {
                AppUtils.ShowWarning("Введіть коректну дату народження.");
                return;
            }

            int age = DateTime.Now.Year - birthDate.Year;
            if (birthDate.Date > DateTime.Now.Date.AddYears(-age))
            {
                age--;
            }

            if (age < 18)
            {
                AppUtils.ShowWarning("Власник повинен бути повнолітнім (від 18 років).");
                return;
            }

            TxtLastName.Text = lastName;
            TxtFirstName.Text = firstName;

            SaveOwnerToDatabase(firstName, lastName, birthDate);

            AppUtils.ShowInfo("Нового власника успішно додано!");
            this.Close();
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            TxtLastName.Clear();
            TxtFirstName.Clear();
            DpBirthDate.SelectedDate = null;
        }

        private void SaveOwnerToDatabase(string firstName, string lastName, DateTime birthDate)
        {
            // метод під зберігання в БД
        }
    }
}