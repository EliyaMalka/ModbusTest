using System.Windows;
using ModbusTestClient.ViewModels;

namespace ModbusTestClient
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.Dispose();
            }
        }
    }
}
