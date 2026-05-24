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
        private bool _unsaved = false;
        private bool _isInitializing = false;

        public CreatingAccountOwner()
        {
            InitializeComponent();
            LoadOwners();

            TxtLastName.TextChanged += MarkAsUnsaved;
            TxtFirstName.TextChanged += MarkAsUnsaved;
            DpBirthDate.SelectedDateChanged += MarkAsUnsaved;
            this.Closing += Window_Closing;
        }

        private void MarkAsUnsaved(object sender, RoutedEventArgs e)
        {
            if (!_isInitializing) _unsaved = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_unsaved && !AppUtils.AskConfirmation("Є незбережені зміни. Закрити без збереження?", "Увага"))
            {
                e.Cancel = true;
            }
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

            try
            {
                DB db = new DB();

                if (_selectedOwner == null)
                {
                    var newOwner = new Owner(firstName, lastName, birthDate);

                    var allOwners = db.GetOwners();
                    string dbValidationReport = db.Validate(allOwners);

                    if (dbValidationReport != null)
                    {
                        newOwner.Delete();
                        AppUtils.ShowWarning("Неможливо створити власника! Виявлено некоректні дані:\n" + dbValidationReport, "Помилка збереження");
                        return;
                    }

                    AppUtils.ShowInfo("Нового власника успішно додано!");
                }
                else
                {
                    string oldFirstName = _selectedOwner.FirstName;
                    string oldLastName = _selectedOwner.LastName;
                    DateTime oldBirthDate = _selectedOwner.BirthDate;

                    _selectedOwner.Update(firstName, lastName, birthDate);

                    var allOwners = db.GetOwners();
                    string dbValidationReport = db.Validate(allOwners);

                    if (dbValidationReport != null)
                    {
                        _selectedOwner.Update(oldFirstName, oldLastName, oldBirthDate);
                        AppUtils.ShowWarning("Неможливо оновити власника! Виявлено некоректні дані:\n" + dbValidationReport, "Помилка збереження");
                        return;
                    }

                    AppUtils.ShowInfo("Дані власника оновлено!");
                }

                _unsaved = false;
                LoadOwners();
                BtnReset_Click(null, null);
            }
            catch (Exception ex)
            {
                AppUtils.ShowWarning($"Операція відхилена! Перевірте правильність вводу.\n\nДеталі проблеми: {ex.Message}", "Критична помилка збереження");
            }
        }

        private void DgOwners_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _selectedOwner = DgOwners.SelectedItem as Owner;
            if (_selectedOwner != null)
            {
                _isInitializing = true;
                TxtFirstName.Text = _selectedOwner.FirstName;
                TxtLastName.Text = _selectedOwner.LastName;
                DpBirthDate.SelectedDate = _selectedOwner.BirthDate;
                BtnSave.Content = "✓ Оновити";
                BtnDelete.IsEnabled = true;
                _isInitializing = false;
                _unsaved = false;
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedOwner != null && AppUtils.AskConfirmation("Ви впевнені, що хочете видалити цього власника? Це автоматично видалить усі його ділянки!", "Увага!"))
            {
                try
                {
                    var db = new DB();
                    var allProperties = db.GetProperties();

                    foreach (var prop in allProperties.Where(p => p.Owner.ID == _selectedOwner.ID))
                    {
                        prop.Delete();
                        prop.Description.Delete();
                    }

                    _selectedOwner.Delete();
                    _unsaved = false;
                    LoadOwners();
                    BtnReset_Click(null, null);
                    AppUtils.ShowInfo("Власника та його ділянки успішно видалено.");
                }
                catch (Exception ex)
                {
                    AppUtils.ShowWarning($"Помилка при видаленні:\n{ex.Message}", "Помилка");
                }
            }
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            _isInitializing = true;
            TxtLastName.Clear();
            TxtFirstName.Clear();
            DpBirthDate.SelectedDate = null;
            _selectedOwner = null;
            DgOwners.SelectedItem = null;
            BtnSave.Content = "✓ Зберегти";
            BtnDelete.IsEnabled = false;
            _isInitializing = false;
            _unsaved = false;
        }
    }
}