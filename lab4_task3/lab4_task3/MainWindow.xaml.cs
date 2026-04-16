using lab4_task3.views;
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