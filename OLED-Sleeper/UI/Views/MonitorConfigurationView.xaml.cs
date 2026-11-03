using OLED_Sleeper.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OLED_Sleeper.UI.Views
{
    /// <summary>
    /// Represents the code-behind for the MonitorConfigurationView UserControl.
    /// Handles initialization and interaction logic for the monitor configuration UI.
    /// </summary>
    public partial class MonitorConfigurationView : UserControl
    {
        public MonitorConfigurationView()
        {
            InitializeComponent();
        }

        private void ManualHotkey_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (Window.GetWindow(this)?.DataContext is MainViewModel main) main.SetShortcutCaptureActive(true);
        }

        private void ManualHotkey_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (Window.GetWindow(this)?.DataContext is MainViewModel main) main.SetShortcutCaptureActive(false);
        }

        private void ManualHotkey_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            if (DataContext is not MonitorConfigurationViewModel viewModel) return;
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (key == Key.Tab && Keyboard.Modifiers == ModifierKeys.None) { e.Handled = false; return; }
            if (key == Key.Escape)
            {
                viewModel.ManualHotkey = string.Empty;
                return;
            }

            var shortcut = OLED_Sleeper.UI.Models.ManualHotkeyGesture.Capture(key, Keyboard.Modifiers);
            if (!string.IsNullOrEmpty(shortcut)) viewModel.ManualHotkey = shortcut;
        }

    }
}
