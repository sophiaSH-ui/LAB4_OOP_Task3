using System.Windows;

namespace lab4_task3
{
    public static class AppUtils
    {
        public static void NavigateTo(Window currentWindow, Window targetWindow)
        {
            targetWindow.Owner = currentWindow;
            targetWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            currentWindow.Hide();
            targetWindow.Show();

            // Розумний обробник закриття
            targetWindow.Closed += (s, e) =>
            {
                // Якщо вікно закрилося, а його Owner (попереднє вікно) досі сховане, 
                // це означає, що натиснули "Хрестик". Тому закриваємо всю програму.
                if (!currentWindow.IsVisible)
                {
                    Application.Current.Shutdown();
                }
            };
        }

        public static void ShowDialog(Window currentWindow, Window targetWindow)
        {
            targetWindow.Owner = currentWindow;
            targetWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            targetWindow.ShowDialog();
        }

        // НОВИЙ МЕТОД ДЛЯ КНОПКИ "НАЗАД"
        public static void GoBack(Window currentWindow)
        {
            // Спочатку примусово показуємо попереднє вікно
            if (currentWindow.Owner != null)
            {
                currentWindow.Owner.Show();
            }
            // Тепер безпечно закриваємо поточне
            currentWindow.Close();
        }

        public static void ShowInfo(string message, string title = "Інформація")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void ShowWarning(string message, string title = "Увага")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public static bool AskConfirmation(string message, string title = "Підтвердження")
        {
            var res = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return res == MessageBoxResult.Yes;
        }
    }
}