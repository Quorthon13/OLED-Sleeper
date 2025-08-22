using OLED_Sleeper.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace OLED_Sleeper.Views
{
    public partial class MonitorLayoutView : UserControl
    {
        public MonitorLayoutView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.LoadMonitors(this.ActualWidth, this.ActualHeight);
            }
        }
    }
}