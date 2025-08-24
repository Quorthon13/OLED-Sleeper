// File: Services/IdleActivityService.cs
using OLED_Sleeper.Events;
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
    /// Monitors user activity and determines when managed monitors become idle or active.
    /// Manages a timer for each monitor, raising events on state transitions.
    /// </summary>
    public class IdleActivityService : IIdleActivityService
    {
        #region Events

        /// <summary>
        /// Raised when a managed monitor transitions from active to idle.
        /// </summary>
        public event EventHandler<MonitorStateEventArgs> MonitorBecameIdle;

        /// <summary>
        /// Raised when a managed monitor transitions from idle to active.
        /// </summary>
        public event EventHandler<MonitorStateEventArgs> MonitorBecameActive;

        #endregion Events

        #region Fields

        private CancellationTokenSource _cancellationTokenSource;
        private List<ManagedMonitorState> _managedMonitors = new();
        private readonly object _lock = new();
        private readonly IMonitorService _monitorService;
        private readonly Dictionary<string, MonitorTimerState> _monitorStates = new();

        #endregion Fields

        #region Nested Types

        /// <summary>
        /// State machine for per-monitor activity.
        /// </summary>
        private enum MonitorStateMachine
        { Active, Counting, Idle }

        /// <summary>
        /// Holds state and settings for a managed monitor.
        /// </summary>
        private class ManagedMonitorState
        {
            public int DisplayNumber { get; set; }
            public MonitorSettings Settings { get; set; }
            public Rect Bounds { get; set; }
        }

        /// <summary>
        /// Tracks timer and state for a monitor.
        /// </summary>
        private class MonitorTimerState
        {
            public MonitorStateMachine CurrentState { get; set; } = MonitorStateMachine.Active;
            public DateTime ActivityStoppedTimestamp { get; set; }
        }

        /// <summary>
        /// Snapshot of system state at a point in time.
        /// </summary>
        private readonly struct SystemState
        {
            public readonly uint IdleTimeMilliseconds;
            public readonly Point CursorPosition;
            public readonly Rect ForegroundWindowRect;
            public readonly IntPtr ForegroundWindowHandle;

            public SystemState(uint idleTime, Point cursorPosition, Rect windowRect, IntPtr windowHandle)
            {
                IdleTimeMilliseconds = idleTime;
                CursorPosition = cursorPosition;
                ForegroundWindowRect = windowRect;
                ForegroundWindowHandle = windowHandle;
            }
        }

        #endregion Nested Types

        /// <summary>
        /// Initializes a new instance of the <see cref="IdleActivityService"/> class.
        /// </summary>
        /// <param name="monitorService">Service for monitor information.</param>
        public IdleActivityService(IMonitorService monitorService)
        {
            _monitorService = monitorService;
        }

        #region Public Methods

        /// <inheritdoc/>
        public void Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => IdleCheckLoop(_cancellationTokenSource.Token));
            Log.Information("IdleActivityService started.");
        }

        /// <inheritdoc/>
        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
            Log.Information("IdleActivityService stopped.");
        }

        /// <inheritdoc/>
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
                                        Bounds = monitorInfo.Bounds,
                                        DisplayNumber = monitorInfo.DisplayNumber
                                    }).ToList();

                _monitorStates.Clear();
                foreach (var monitor in _managedMonitors)
                {
                    _monitorStates[monitor.Settings.HardwareId] = new MonitorTimerState();
                }
            }
            Log.Information("IdleActivityService settings updated. Now tracking {Count} monitors.", _managedMonitors.Count);
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Main background loop that periodically checks monitor states.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
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
                await Task.Delay(200, token);
            }
        }

        /// <summary>
        /// Gathers system state and processes each managed monitor according to the state machine logic.
        /// </summary>
        private void ProcessMonitors()
        {
            var systemState = GetSystemState();

            lock (_lock)
            {
                foreach (var monitor in _managedMonitors)
                {
                    var timerState = _monitorStates[monitor.Settings.HardwareId];
                    var activityReason = GetActivityReason(monitor, systemState);
                    bool hasActivityNow = activityReason != ActivityReason.None;

                    var eventArgs = new MonitorStateEventArgs(
                        monitor.Settings.HardwareId, monitor.DisplayNumber, monitor.Bounds,
                        monitor.Settings, systemState.ForegroundWindowHandle, activityReason);

                    switch (timerState.CurrentState)
                    {
                        case MonitorStateMachine.Active:
                            if (!hasActivityNow)
                            {
                                // Activity stopped. Start counting.
                                timerState.CurrentState = MonitorStateMachine.Counting;
                                timerState.ActivityStoppedTimestamp = DateTime.UtcNow;
                            }
                            break;

                        case MonitorStateMachine.Counting:
                            if (hasActivityNow)
                            {
                                // Activity resumed. Back to active.
                                timerState.CurrentState = MonitorStateMachine.Active;
                            }
                            else
                            {
                                var elapsed = DateTime.UtcNow - timerState.ActivityStoppedTimestamp;
                                if (elapsed.TotalMilliseconds >= monitor.Settings.IdleTimeMilliseconds)
                                {
                                    timerState.CurrentState = MonitorStateMachine.Idle;
                                    Log.Information("Monitor #{DisplayNumber} has become idle after {Seconds}s of inactivity.",
                                        monitor.DisplayNumber, Math.Round(elapsed.TotalSeconds));
                                    MonitorBecameIdle?.Invoke(this, eventArgs);
                                }
                            }
                            break;

                        case MonitorStateMachine.Idle:
                            if (hasActivityNow)
                            {
                                MonitorBecameActive?.Invoke(this, eventArgs);
                                if (!eventArgs.IsIgnored)
                                {
                                    timerState.CurrentState = MonitorStateMachine.Active;
                                    Log.Information("Monitor #{DisplayNumber} is now ACTIVE.", monitor.DisplayNumber);
                                }
                            }
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Determines the reason for any qualifying activity on a monitor at this moment.
        /// </summary>
        /// <param name="monitor">The managed monitor.</param>
        /// <param name="state">Current system state.</param>
        /// <returns>The activity reason.</returns>
        private static ActivityReason GetActivityReason(ManagedMonitorState monitor, SystemState state)
        {
            if (monitor.Settings.IsActiveOnInput && state.IdleTimeMilliseconds < monitor.Settings.IdleTimeMilliseconds)
                return ActivityReason.SystemInput;

            if (monitor.Settings.IsActiveOnMousePosition && monitor.Bounds.Contains(state.CursorPosition))
                return ActivityReason.MousePosition;

            if (monitor.Settings.IsActiveOnActiveWindow)
            {
                Rect intersection = Rect.Intersect(monitor.Bounds, state.ForegroundWindowRect);
                if (!intersection.IsEmpty && intersection.Width > 0 && intersection.Height > 0)
                    return ActivityReason.ActiveWindow;
            }

            return ActivityReason.None;
        }

        /// <summary>
        /// Gathers all required system-wide state information at once.
        /// </summary>
        /// <returns>System state snapshot.</returns>
        private static SystemState GetSystemState()
        {
            uint idleTime = GetSystemIdleTimeMilliseconds();
            NativeMethods.GetCursorPos(out var nativePoint);
            Point cursorPosition = new(nativePoint.X, nativePoint.Y);
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
            return new SystemState(idleTime, cursorPosition, windowRect, foregroundWindowHandle);
        }

        /// <summary>
        /// Gets the system-wide user idle time in milliseconds using the GetLastInputInfo API.
        /// </summary>
        /// <returns>Idle time in milliseconds.</returns>
        private static uint GetSystemIdleTimeMilliseconds()
        {
            var lastInputInfo = new NativeMethods.LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf(typeof(NativeMethods.LASTINPUTINFO)) };
            if (NativeMethods.GetLastInputInfo(ref lastInputInfo))
            {
                uint lastInputTick = lastInputInfo.dwTime;
                uint currentTick = (uint)Environment.TickCount;
                return currentTick - lastInputTick;
            }
            return 0;
        }

        #endregion Private Methods
    }
}