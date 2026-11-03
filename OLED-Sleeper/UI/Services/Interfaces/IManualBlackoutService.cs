using OLED_Sleeper.Features.UserSettings.Models;
using System.Windows;

namespace OLED_Sleeper.UI.Services.Interfaces
{

    /// <summary>Manual monitor blackout controls and their optional per-monitor global shortcuts.</summary>
    public interface IManualBlackoutService : IDisposable
    {
        event Action<string>? BlackoutStateChanged;
        event Action? ShortcutsChanged;
        void Attach(Window window);
        void SetCaptureActive(bool active);
        void Update(IEnumerable<MonitorSettings> settings);
        bool IsBlackoutActive(string hardwareId);
        string GetShortcutError(string hardwareId);
        Task ToggleAsync(string hardwareId);
    }
}
