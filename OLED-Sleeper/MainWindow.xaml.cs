using System.Windows;
using System.Windows.Input;

namespace OLED_Sleeper
{
    /// <summary>
    /// The main application window for OLED Sleeper.
    /// Handles window chrome and the custom title bar.
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Constructor

        public MainWindow()
        {
            InitializeComponent();
        }

        #endregion Constructor

        #region Title Bar & Window Controls

        /// <summary>
        /// Handles dragging the window when the custom title bar is clicked and dragged.
        /// </summary>
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        /// <summary>
        /// Minimizes the window when the minimize button is clicked.
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// Toggles between maximized and normal window state when the maximize button is clicked.
        /// </summary>
        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = (WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
        }

        /// <summary>
        /// Closes the window when the close button is clicked.
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion Title Bar & Window Controls
    }
}
