// File: MainWindow.xaml.cs
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace OLED_Sleeper
{
    public partial class MainWindow : Window
    {
        // Refactored: DataContext is now set from App.xaml.cs via DI.
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Hides the window instead of closing it, to keep the app running in the tray.
            e.Cancel = true;
            this.Hide();
            base.OnClosing(e);
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
            this.Hide();
        }
    }
}