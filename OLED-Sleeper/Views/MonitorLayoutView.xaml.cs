// File: Views/MonitorLayoutView.xaml.cs
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

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel && e.NewSize.Height > 0)
            {
                // Call the new, specific method for handling resizes.
                viewModel.RecalculateLayout(e.NewSize.Width, e.NewSize.Height);
            }
        }
    }
}