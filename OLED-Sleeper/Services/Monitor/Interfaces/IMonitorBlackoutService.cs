// File: Services/IMonitorBlackoutService.cs
using System;
using System.Windows;

namespace OLED_Sleeper.Services.Monitor.Interfaces
{
    public interface IMonitorBlackoutService
    {
        void ShowBlackoutOverlay(string hardwareId, Rect bounds);

        void HideOverlay(string hardwareId);

        bool IsOverlayWindow(nint windowHandle);
    }
}