using System;
using System.Collections.ObjectModel;
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