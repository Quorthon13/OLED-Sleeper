using OLED_Sleeper.Native;
using OLED_Sleeper.UI.Models;
using OLED_Sleeper.UI.Services.Interfaces;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace OLED_Sleeper.UI.Services
{

    /// <summary>Routes WM_HOTKEY from the settings window even while that window is hidden.</summary>
    public sealed class GlobalHotkeyRegistrar : IGlobalHotkeyRegistrar
    {
        private HwndSource? _source;
        public event Action<int>? HotkeyPressed;
        public bool IsReady => _source != null;
        public Point CursorPosition => NativeMethods.GetCursorPos(out var cursor) ? new Point(cursor.X, cursor.Y) : new Point(double.NaN, double.NaN);

        public void Attach(Window window)
        {
            _source?.RemoveHook(WindowProc);
            _source = HwndSource.FromHwnd(new WindowInteropHelper(window).EnsureHandle());
            _source?.AddHook(WindowProc);
        }

        public bool TryRegister(int id, ManualHotkeyGesture gesture, out int error)
        {
            bool success = _source != null && NativeMethods.RegisterHotKey(_source.Handle, id, gesture.Modifiers | 0x4000, gesture.VirtualKey);
            error = success ? 0 : Marshal.GetLastWin32Error();
            return success;
        }

        public void Unregister(int id)
        {
            if (_source != null) NativeMethods.UnregisterHotKey(_source.Handle, id);
        }

        private nint WindowProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
        {
            if (msg == 0x0312)
            {
                handled = true;
                HotkeyPressed?.Invoke(wParam.ToInt32());
            }
            return nint.Zero;
        }

        public void Dispose()
        {
            _source?.RemoveHook(WindowProc);
            _source = null;
        }
    }
}
