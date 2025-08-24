// File: Services/OverlayService.cs
using OLED_Sleeper.Native;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace OLED_Sleeper.Services
{
    public class OverlayService : IOverlayService
    {
        private readonly Dictionary<string, Window> _overlayWindows = new();
        private readonly HashSet<IntPtr> _overlayHandles = new();

        public void ShowBlackoutOverlay(string hardwareId, Rect bounds)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_overlayWindows.ContainsKey(hardwareId)) return;

                var overlay = new Window
                {
                    WindowStyle = WindowStyle.None,
                    ResizeMode = ResizeMode.NoResize,
                    AllowsTransparency = true,
                    Background = Brushes.Black,
                    ShowInTaskbar = false,
                    Topmost = true,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = bounds.Left,
                    Top = bounds.Top
                };

                overlay.Show();

                // Get the window handle (HWND) for style manipulation and tracking
                IntPtr hwnd = new WindowInteropHelper(overlay).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    // Apply the non-activating style to prevent focus stealing
                    IntPtr extendedStyle = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE);
                    NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE,
                        new IntPtr(extendedStyle.ToInt64() | NativeMethods.WS_EX_NOACTIVATE));

                    // Track this handle as one of our overlays
                    _overlayHandles.Add(hwnd);
                }

                // Apply DPI scaling to ensure correct size
                var source = PresentationSource.FromVisual(overlay);
                if (source != null)
                {
                    double dpiX = source.CompositionTarget.TransformToDevice.M11;
                    double dpiY = source.CompositionTarget.TransformToDevice.M22;

                    overlay.Left = bounds.Left / dpiX;
                    overlay.Top = bounds.Top / dpiY;
                    overlay.Width = bounds.Width / dpiX;
                    overlay.Height = bounds.Height / dpiY;
                }

                _overlayWindows[hardwareId] = overlay;
            });
        }

        public void HideOverlay(string hardwareId)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_overlayWindows.TryGetValue(hardwareId, out var overlay))
                {
                    // Untrack the handle before closing
                    IntPtr hwnd = new WindowInteropHelper(overlay).Handle;
                    if (hwnd != IntPtr.Zero)
                    {
                        _overlayHandles.Remove(hwnd);
                    }
                    overlay.Close();
                    _overlayWindows.Remove(hardwareId);
                }
            });
        }

        public bool IsOverlayWindow(IntPtr windowHandle)
        {
            return windowHandle != IntPtr.Zero && _overlayHandles.Contains(windowHandle);
        }
    }
}