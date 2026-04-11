using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace lab4_task3
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private bool IsLocationValid()
        {
            if (CbLocation.SelectedIndex == -1 &&
                (string.IsNullOrWhiteSpace(CbLocation.Text) || CbLocation.Text == "Почніть вводити назву..."))
            {
                MessageBox.Show("Будь ласка, спочатку оберіть або введіть населений пункт!",
                                "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        private void NavigateTo(Window targetWindow)
        {
            targetWindow.Owner = this;
            targetWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            targetWindow.Closed += (s, e) => this.Show();

            this.Hide();
            targetWindow.Show();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (IsLocationValid())
            {
                NavigateTo(new AddEditWindow());
            }
        }

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            if (IsLocationValid())
            {
                NavigateTo(new ViewWindow());
            }
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}