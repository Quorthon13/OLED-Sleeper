using OLED_Sleeper.UI.Models;
using System.Windows;

namespace OLED_Sleeper.UI.Services.Interfaces
{

    /// <summary>Window-bound Windows global shortcut registration, replaceable in tests.</summary>
    public interface IGlobalHotkeyRegistrar : IDisposable
    {
        event Action<int>? HotkeyPressed;
        bool IsReady { get; }
        Point CursorPosition { get; }
        void Attach(Window window);
        bool TryRegister(int id, ManualHotkeyGesture gesture, out int error);
        void Unregister(int id);
    }
}
