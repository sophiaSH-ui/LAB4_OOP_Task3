using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using lab4_task3.DTO;

namespace lab4_task3.views
{
    public partial class CreatingAccountOwner : Window
    {
        private ObservableCollection<Owner> _owners;
        private Owner _selectedOwner;

        public CreatingAccountOwner()
        {
            InitializeComponent();
            LoadOwners();
        }

        private void LoadOwners()
        {
            _owners = new DB().GetOwners();
            DgOwners.ItemsSource = _owners;
        }

        private class TempOwnerData
        {
            [Required(ErrorMessage = "Прізвище є обов'язковим.")]
            [StringLength(50, MinimumLength = 2, ErrorMessage = "Прізвище має містити від 2 до 50 символів.")]
            [RegularExpression(@"^[а-яА-ЯіІїЇєЄґҐa-zA-Z\s\-']+$", ErrorMessage = "Прізвище містить недопустимі символи.")]
            public string LastName { get; set; }

            [Required(ErrorMessage = "Ім'я є обов'язковим.")]
            [StringLength(50, MinimumLength = 2, ErrorMessage = "Ім'я має містити від 2 до 50 символів.")]
            [RegularExpression(@"^[а-яА-ЯіІїЇєЄґҐa-zA-Z\s\-']+$", ErrorMessage = "Ім'я містить недопустимі символи.")]
            public string FirstName { get; set; }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e) => AppUtils.GoBack(this);

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string lastName = TxtLastName.Text?.Trim();
            string firstName = TxtFirstName.Text?.Trim();

            // поточна локальна валідація через анотації для GUI
            var tempOwner = new TempOwnerData { LastName = lastName, FirstName = firstName };
            var errors = InputValidator.ValidateByAnnotations(tempOwner);

            if (errors.Any())
            {
                AppUtils.ShowWarning(string.Join("\n", errors.Select(err => "• " + err)), "Помилки валідації");
                return;
            }

            if (DpBirthDate.SelectedDate == null)
            {
                AppUtils.ShowWarning("Будь ласка, вкажіть дату народження.");
                return;
            }

            DateTime birthDate = DpBirthDate.SelectedDate.Value;

            /* Коли у DTO буде метод Validate, треба бцде прописати щось типу
             * * string dbErrors = new DB().ValidateOwnerData(lastName, firstName, birthDate);
             * if (dbErrors != null)
             * {
             * AppUtils.ShowWarning(dbErrors, "Помилка валідації DTO / Бази Даних");
             * return; // Зупиняємо збереження, якщо DTO знайшло помилки
             * }
             */

            if (_selectedOwner == null)
            {
                new Owner(firstName, lastName, birthDate);
                AppUtils.ShowInfo("Нового власника успішно додано!");
            }
            else
            {
                _selectedOwner.Update(firstName, lastName, birthDate);
                AppUtils.ShowInfo("Дані власника оновлено!");
            }

            LoadOwners();
            BtnReset_Click(null, null);
        }

        private void DgOwners_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _selectedOwner = DgOwners.SelectedItem as Owner;
            if (_selectedOwner != null)
            {
                TxtFirstName.Text = _selectedOwner.FirstName;
                TxtLastName.Text = _selectedOwner.LastName;
                DpBirthDate.SelectedDate = _selectedOwner.BirthDate;
                BtnSave.Content = "✓ Оновити";
                BtnDelete.IsEnabled = true;
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedOwner != null && AppUtils.AskConfirmation("Ви впевнені, що хочете видалити цього власника? Це автоматично видалить усі його ділянки!", "Увага!"))
            {
                var db = new DB();
                var allProperties = db.GetProperties();

                foreach (var prop in allProperties.Where(p => p.Owner.ID == _selectedOwner.ID))
                {
                    prop.Delete();
                    prop.Description.Delete();
                }

                _selectedOwner.Delete();
                LoadOwners();
                BtnReset_Click(null, null);
                AppUtils.ShowInfo("Власника та його ділянки успішно видалено.");
            }
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            TxtLastName.Clear();
            TxtFirstName.Clear();
            DpBirthDate.SelectedDate = null;
            _selectedOwner = null;
            DgOwners.SelectedItem = null;
            BtnSave.Content = "✓ Зберегти";
            BtnDelete.IsEnabled = false;
        }
    }
}