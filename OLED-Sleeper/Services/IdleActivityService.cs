// File: Services/IdleActivityService.cs
using OLED_Sleeper.Extensions;
using OLED_Sleeper.Models;
using OLED_Sleeper.Native;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace OLED_Sleeper.Services
{
    /// <summary>
    /// A background service that monitors user activity and determines when monitors should be considered idle.
    /// It uses a combination of system-wide idle time, mouse position, and active window location.
    /// </summary>
    public class IdleActivityService : IIdleActivityService
    {
        #region Private Classes, Structs, and Enums
        /// <summary>
        /// A private class to hold the combined state and settings for a monitor being actively managed.
        /// </summary>
        private class ManagedMonitorState
        {
            public MonitorSettings Settings { get; set; }
            public Rect Bounds { get; set; }
        }

        /// <summary>
        /// A snapshot of the system's state at a point in time.
        /// </summary>
        private readonly struct SystemState
        {
            public readonly uint IdleTimeMilliseconds;
            public readonly Point CursorPosition;
            public readonly Rect ForegroundWindowRect;

            public SystemState(uint idleTime, Point cursorPosition, Rect windowRect)
            {
                IdleTimeMilliseconds = idleTime;
                CursorPosition = cursorPosition;
                ForegroundWindowRect = windowRect;
            }
        }

        /// <summary>
        /// --- Updated: Defines the reason for a monitor's active state. ---
        /// Defines the specific reason why a monitor is considered active.
        /// </summary>
        private enum ActivityReason
        {
            None, // Used when the monitor is idle.
            MousePosition,
            ActiveWindow,
            SystemInput
        }
        #endregion

        #region Fields
        private CancellationTokenSource _cancellationTokenSource;
        private List<ManagedMonitorState> _managedMonitors = new List<ManagedMonitorState>();
        private readonly Dictionary<string, bool> _idleStateNotified = new Dictionary<string, bool>();
        private readonly object _lock = new object();
        private readonly IMonitorService _monitorService;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="IdleActivityService"/> class.
        /// </summary>
        /// <param name="monitorService">The service used to get information about physical monitors.</param>
        public IdleActivityService(IMonitorService monitorService)
        {
            _monitorService = monitorService;
        }

        #region Public Methods
        /// <summary>
        /// Starts the background idle checking loop.
        /// </summary>
        public void Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => IdleCheckLoop(_cancellationTokenSource.Token));
            Log.Information("IdleActivityService started.");
        }

        /// <summary>
        /// Stops the background idle checking loop.
        /// </summary>
        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
            Log.Information("IdleActivityService stopped.");
        }

        /// <summary>
        /// Updates the list of monitors to be managed by the service.
        /// </summary>
        /// <param name="monitorSettings">The list of all available monitor settings.</param>
        public void UpdateSettings(List<MonitorSettings> monitorSettings)
        {
            var activeSettings = monitorSettings.Where(s => s.IsManaged).ToList();
            var allMonitors = _monitorService.GetMonitors();

            lock (_lock)
            {
                _managedMonitors = (from setting in activeSettings
                                    join monitorInfo in allMonitors on setting.HardwareId equals monitorInfo.HardwareId
                                    select new ManagedMonitorState
                                    {
                                        Settings = setting,
                                        Bounds = monitorInfo.Bounds
                                    }).ToList();

                var unmatchedSettings = activeSettings.Where(s => !_managedMonitors.Any(m => m.Settings.HardwareId == s.HardwareId)).ToList();
                if (unmatchedSettings.Any())
                {
                    Log.Warning("Found {UnmatchedCount} managed settings with no matching physical monitor attached.", unmatchedSettings.Count);
                }

                _idleStateNotified.Clear();
            }
            Log.Information("IdleActivityService settings updated. Now tracking {Count} monitors.", _managedMonitors.Count);
        }
        #endregion

        #region Private Core Logic
        /// <summary>
        /// The main background loop that periodically checks monitor states.
        /// </summary>
        /// <param name="token">A cancellation token to stop the loop.</param>
        private async Task IdleCheckLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    ProcessMonitors();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error occurred in the idle check loop.");
                }
                await Task.Delay(1000, token);
            }
        }

        /// <summary>
        /// Gathers system state and processes each managed monitor.
        /// </summary>
        private void ProcessMonitors()
        {
            var systemState = GetSystemState();

            lock (_lock)
            {
                foreach (var monitor in _managedMonitors)
                {
                    var reason = GetMonitorActivityReason(monitor, systemState);
                    UpdateMonitorState(monitor, reason, systemState);
                }
            }
        }

        /// <summary>
        /// Gathers all required system-wide state information at once.
        /// </summary>
        /// <returns>A snapshot of the current system state.</returns>
        private SystemState GetSystemState()
        {
            uint idleTime = GetSystemIdleTimeMilliseconds();

            NativeMethods.GetCursorPos(out var nativePoint);
            Point cursorPosition = new Point(nativePoint.X, nativePoint.Y);

            IntPtr foregroundWindowHandle = NativeMethods.GetForegroundWindow();
            Rect windowRect;
            if (NativeMethods.DwmGetWindowAttribute(foregroundWindowHandle, NativeMethods.DWMWA_EXTENDED_FRAME_BOUNDS, out var nativeWindowRect, Marshal.SizeOf(typeof(NativeMethods.Rect))) == 0)
            {
                windowRect = nativeWindowRect.ToWindowsRect();
            }
            else
            {
                NativeMethods.GetWindowRect(foregroundWindowHandle, out nativeWindowRect);
                windowRect = nativeWindowRect.ToWindowsRect();
            }

            return new SystemState(idleTime, cursorPosition, windowRect);
        }

        /// <summary>
        /// --- Updated: Returns the reason for activity, not just a boolean. ---
        /// Determines why a monitor is active based on its settings and the current system state.
        /// </summary>
        /// <param name="monitor">The managed monitor to check.</param>
        /// <param name="state">The current snapshot of the system state.</param>
        /// <returns>An <see cref="ActivityReason"/> indicating why the monitor is active, or <c>None</c> if it's idle.</returns>
        private ActivityReason GetMonitorActivityReason(ManagedMonitorState monitor, SystemState state)
        {
            if (monitor.Settings.IsActiveOnMousePosition && monitor.Bounds.Contains(state.CursorPosition))
            {
                return ActivityReason.MousePosition;
            }

            if (monitor.Settings.IsActiveOnActiveWindow)
            {
                Rect intersection = Rect.Intersect(monitor.Bounds, state.ForegroundWindowRect);
                if (!intersection.IsEmpty && intersection.Width > 0 && intersection.Height > 0)
                {
                    return ActivityReason.ActiveWindow;
                }
            }

            if (monitor.Settings.IsActiveOnInput && state.IdleTimeMilliseconds < monitor.Settings.IdleTimeMilliseconds)
            {
                return ActivityReason.SystemInput;
            }

            return ActivityReason.None;
        }

        /// <summary>
        /// --- Updated: Logs the specific cause of an activity state change. ---
        /// Updates the notified state of a monitor and logs with detailed cause information if a change has occurred.
        /// </summary>
        /// <param name="monitor">The monitor whose state is being updated.</param>
        /// <param name="reason">The reason for the monitor's current activity state.</param>
        /// <param name="state">The current system state, used for logging details.</param>
        private void UpdateMonitorState(ManagedMonitorState monitor, ActivityReason reason, SystemState state)
        {
            bool isCurrentlyActive = (reason != ActivityReason.None);
            bool hasBeenNotifiedAsIdle = _idleStateNotified.ContainsKey(monitor.Settings.HardwareId);

            if (isCurrentlyActive && hasBeenNotifiedAsIdle)
            {
                // State changed from idle to active, so log the cause.
                string causeMessage;
                switch (reason)
                {
                    case ActivityReason.MousePosition:
                        causeMessage = $"Mouse position {state.CursorPosition} detected on monitor bounds {monitor.Bounds}.";
                        break;
                    case ActivityReason.ActiveWindow:
                        causeMessage = $"Active window at {state.ForegroundWindowRect} intersects with monitor bounds {monitor.Bounds}.";
                        break;
                    case ActivityReason.SystemInput:
                        causeMessage = $"System-wide user input detected (idle time was {state.IdleTimeMilliseconds}ms).";
                        break;
                    default:
                        causeMessage = "Unknown activity detected.";
                        break;
                }
                Log.Information("Monitor {HardwareId} is now ACTIVE. Cause: {Cause}", monitor.Settings.HardwareId, causeMessage);
                _idleStateNotified.Remove(monitor.Settings.HardwareId);
            }
            else if (!isCurrentlyActive && !hasBeenNotifiedAsIdle)
            {
                // State changed from active to idle.
                _idleStateNotified[monitor.Settings.HardwareId] = true;
                Log.Information("Monitor {HardwareId} has become idle (no qualifying activity detected).", monitor.Settings.HardwareId);
            }
        }

        /// <summary>
        /// Gets the system-wide user idle time in milliseconds using the GetLastInputInfo API.
        /// </summary>
        /// <returns>The number of milliseconds since the last user input event.</returns>
        private static uint GetSystemIdleTimeMilliseconds()
        {
            var lastInputInfo = new NativeMethods.LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            if (NativeMethods.GetLastInputInfo(ref lastInputInfo))
            {
                uint lastInputTick = lastInputInfo.dwTime;
                uint currentTick = (uint)Environment.TickCount;
                return currentTick - lastInputTick;
            }
            return 0;
        }
        #endregion
    }
}