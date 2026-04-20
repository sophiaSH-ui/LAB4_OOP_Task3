using lab4_task3.views;
using System.Windows;

namespace lab4_task3
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
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