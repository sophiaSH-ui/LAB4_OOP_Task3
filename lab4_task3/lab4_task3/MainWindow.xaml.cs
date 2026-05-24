// MainWindow.xaml.cs
using lab4_task3.DTO;
using lab4_task3.views;
using System;
using System.Windows;

namespace lab4_task3
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                DB db = new DB();
                var allProperties = db.GetProperties();

                string dbValidationReport = db.Validate(allProperties);

                if (dbValidationReport != null)
                {
                    AppUtils.ShowWarning("Увага! У базі даних знайдено збережені записи, що порушують правила валідації DTO:\n\n" + dbValidationReport, "Порушення цілісності даних БД");
                }
            }
            catch (Exception ex)
            {
                AppUtils.ShowWarning($"Помилка зчитування або валідації БД: {ex.Message}", "Помилка");
            }
        }

        private void BtnLReg_Click(object sender, RoutedEventArgs e)
        {
            AppUtils.NavigateTo(this, new LandRegistry());
        }

        private void BtnAddOwner_Click(object sender, RoutedEventArgs e)
        {
            AppUtils.NavigateTo(this, new CreatingAccountOwner());

        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}