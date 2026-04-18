using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using lab4_task3.DTO;
using lab4_task3.views;

namespace lab4_task3
{
    public class OwnerMock
    {
        public int Id { get; set; }
        public string FullName { get; set; }
    }

    public partial class AddEditWindow : Window
    {
        private bool unsaved = false;
        private Random random = new Random();

        // Зберігаємо список локально, щоб легко додавати
        private List<OwnerMock> ownersList = new List<OwnerMock>();

        public AddEditWindow()
        {
            InitializeComponent();

            LoadOwnersFromDatabase();

            TxtMarketValue.TextChanged += NumericInput_TextChanged;
            TxtGroundWater.TextChanged += NumericInput_TextChanged;
        }

        private void LoadOwnersFromDatabase()
        {
            // Тут можна замінити на реальний запит до БД
            ownersList = new List<OwnerMock>
            {
                new OwnerMock { Id = 1, FullName = "Іваненко І.І." },
                new OwnerMock { Id = 2, FullName = "Петренко П.П." },
                new OwnerMock { Id = 3, FullName = "Сидоренко С.С." }
            };

            RefreshOwnerComboBox();
        }

        // Окремий метод щоб не дублювати код оновлення ComboBox
        private void RefreshOwnerComboBox()
        {
            int selectedId = CbOwner.SelectedValue is int id ? id : -1;

            CbOwner.ItemsSource = null;
            CbOwner.ItemsSource = ownersList;

            // Відновлюємо вибір якщо був
            if (selectedId != -1)
            {
                foreach (var owner in ownersList)
                {
                    if (owner.Id == selectedId)
                    {
                        CbOwner.SelectedValue = selectedId;
                        break;
                    }
                }
            }
        }

        private void SavePlotToDatabase(int ownerId, string purpose, double marketValue, double groundWater,
            string soilType, bool river, bool flat, bool fertile, bool forest, bool road, List<string> coordinates)
        {
            // логіка збереження
        }

        private bool CheckForOverlapMock(List<string> coordinates)
        {
            return random.Next(100) < 30;
        }

        private void NumericInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                string text = textBox.Text;
                string cleanText = Regex.Replace(text, @"[^\d,.-]", "");
                if (text != cleanText)
                {
                    int caret = textBox.CaretIndex > 0 ? textBox.CaretIndex - 1 : 0;
                    textBox.Text = cleanText;
                    textBox.CaretIndex = caret;
                }
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            AppUtils.GoBack(this);
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (CbOwner.SelectedIndex == -1 ||
                CbPryznachennya.SelectedIndex == -1 ||
                CbSoilType.SelectedIndex == -1 ||
                string.IsNullOrWhiteSpace(TxtMarketValue.Text) ||
                string.IsNullOrWhiteSpace(TxtGroundWater.Text))
            {
                AppUtils.ShowWarning("Будь ласка, заповніть всі основні поля (Власник, Призначення, Вартість, Рівень вод, Тип ґрунту).");
                return;
            }

            if (!double.TryParse(TxtMarketValue.Text, out double marketValue) || marketValue < 0)
            {
                AppUtils.ShowWarning("Введіть коректну ринкову вартість.");
                return;
            }

            if (!double.TryParse(TxtGroundWater.Text, out double groundWater))
            {
                AppUtils.ShowWarning("Введіть коректний рівень ґрунтових вод.");
                return;
            }

            if (LbCoordinates.Items.Count < 3)
            {
                AppUtils.ShowWarning("Земельна ділянка повинна мати щонайменше 3 координати (вершини).");
                return;
            }

            Button btnSave = sender as Button;
            string originalContent = btnSave.Content?.ToString();
            btnSave.IsEnabled = false;
            btnSave.Content = "⏳ Перевірка меж...";

            await Task.Delay(2000);

            List<string> coordinates = new List<string>();
            foreach (var item in LbCoordinates.Items)
            {
                coordinates.Add(item.ToString());
            }

            bool isOverlapping = CheckForOverlapMock(coordinates);

            if (isOverlapping)
            {
                btnSave.IsEnabled = true;
                btnSave.Content = originalContent;
                AppUtils.ShowWarning("Збій збереження! Виявлено накладання меж на іншу зареєстровану ділянку в цій місцевості.");
                return;
            }

            int ownerId = (int)CbOwner.SelectedValue;
            string purpose = (CbPryznachennya.SelectedItem as ComboBoxItem)?.Content.ToString();
            string soilType = (CbSoilType.SelectedItem as ComboBoxItem)?.Content.ToString();

            bool hasRiver = ChkRiver.IsChecked == true;
            bool isFlat = ChkFlat.IsChecked == true;
            bool isFertile = ChkFertile.IsChecked == true;
            bool nearForest = ChkForest.IsChecked == true;
            bool nearRoad = ChkRoad.IsChecked == true;

            SavePlotToDatabase(ownerId, purpose, marketValue, groundWater, soilType,
                hasRiver, isFlat, isFertile, nearForest, nearRoad, coordinates);

            unsaved = false;
            AppUtils.ShowInfo("Дані земельної ділянки успішно збережено!");
            this.Close();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!unsaved) return;

            if (!AppUtils.AskConfirmation("Закрити без збереження?", "Увага"))
            {
                e.Cancel = true;
            }
        }

        private void BtnVisualize_Click(object sender, RoutedEventArgs e)
        {
            AppUtils.ShowDialog(this, new VisualizationWindow());
        }

        private void BtnAddNewOwner_Click(object sender, RoutedEventArgs e)
        {
            // ShowDialog — чекаємо поки вікно закриється і тоді читаємо результат
            var window = new CreatingAccountOwner();
            window.ShowDialog();

            if (window.CreatedOwner != null)
            {
                Owner created = window.CreatedOwner;

                // Додаємо нового власника в локальний список і оновлюємо ComboBox
                var newOwner = new OwnerMock
                {
                    Id = created.ID,
                    FullName = $"{created.LastName} {created.FirstName}"
                };

                ownersList.Add(newOwner);
                RefreshOwnerComboBox();

                // Одразу вибираємо щойно створеного власника
                CbOwner.SelectedValue = newOwner.Id;
            }
        }

        private void BtnAddCoord_Click(object sender, RoutedEventArgs e)
        {
            string xStr = TxtCoordX.Text.Trim();
            string yStr = TxtCoordY.Text.Trim();

            if (string.IsNullOrWhiteSpace(xStr) || string.IsNullOrWhiteSpace(yStr))
            {
                AppUtils.ShowWarning("Будь ласка, введіть значення X та Y для нової точки.");
                return;
            }

            if (!double.TryParse(xStr, out double x) || !double.TryParse(yStr, out double y))
            {
                AppUtils.ShowWarning("Координати повинні бути числовими значеннями.");
                return;
            }

            LbCoordinates.Items.Add($"X: {x}  |  Y: {y}");

            TxtCoordX.Clear();
            TxtCoordY.Clear();
            TxtCoordX.Focus();

            unsaved = true;
        }

        private void BtnRemoveCoord_Click(object sender, RoutedEventArgs e)
        {
            if (LbCoordinates.SelectedIndex != -1)
            {
                LbCoordinates.Items.RemoveAt(LbCoordinates.SelectedIndex);
                unsaved = true;
            }
        }

        private void LbCoordinates_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BtnRemoveCoord.IsEnabled = LbCoordinates.SelectedItem != null;
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            CbOwner.SelectedIndex = -1;
            CbPryznachennya.SelectedIndex = -1;
            CbSoilType.SelectedIndex = -1;

            TxtMarketValue.Clear();
            TxtGroundWater.Clear();
            TxtCoordX.Clear();
            TxtCoordY.Clear();

            LbCoordinates.Items.Clear();

            ChkRiver.IsChecked = false;
            ChkFlat.IsChecked = false;
            ChkFertile.IsChecked = false;
            ChkForest.IsChecked = false;
            ChkRoad.IsChecked = false;

            unsaved = true;
        }
    }
}