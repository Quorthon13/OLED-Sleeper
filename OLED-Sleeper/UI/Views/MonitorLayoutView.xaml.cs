using System.Windows;
using System.Windows.Controls;
using OLED_Sleeper.UI.ViewModels;

namespace OLED_Sleeper.UI.Views
{
    /// <summary>
    /// Interaction logic for MonitorLayoutView.
    /// This UserControl displays the layout of all monitors and handles resizing events
    /// to keep the monitor layout in sync with the available space.
    /// </summary>
    public partial class MonitorLayoutView : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MonitorLayoutView"/> class.
        /// </summary>
        public MonitorLayoutView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the SizeChanged event for the UserControl.
        /// Notifies the MainViewModel to recalculate the monitor layout when the control is resized.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The size changed event arguments.</param>
        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel && e.NewSize.Height > 0)
            {
                // Call the method to recalculate the monitor layout with the new size.
                viewModel.RecalculateLayout(e.NewSize.Width, e.NewSize.Height);
            }
        }
    }
}