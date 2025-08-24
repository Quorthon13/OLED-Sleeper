// File: MainWindow.xaml.cs
using OLED_Sleeper.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace OLED_Sleeper
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            var viewModel = DataContext as MainViewModel;
            if (viewModel != null && viewModel.IsDirty)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Would you like to save them before hiding the window?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true; // Prevents the window from closing/hiding.
                    return;
                }

                if (result == MessageBoxResult.Yes)
                {
                    viewModel.SaveSettingsCommand.Execute(null);
                }
            }

            this.Hide();
            e.Cancel = true;
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = (this.WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}