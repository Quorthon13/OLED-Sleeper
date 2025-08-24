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
    /// A background service that monitors user activity and determines when monitors should be considered idle.
    /// It manages a dedicated timer for each monitor that only starts counting when qualifying activity stops.
    /// </summary>
    public class IdleActivityService : IIdleActivityService
    {
        #region Public Events

        /// <summary>
        /// Raised when a managed monitor transitions from an active to an idle state.
        /// </summary>
        public event EventHandler<MonitorStateEventArgs> MonitorBecameIdle;

        /// <summary>
        /// Raised when a managed monitor transitions from an idle to an active state.
        /// </summary>
        public event EventHandler<MonitorStateEventArgs> MonitorBecameActive;

        #endregion Public Events

        #region Private State Management

        /// <summary>
        /// Defines the states for our per-monitor state machine.
        /// </summary>
        private enum MonitorStateMachine
        { Active, Counting, Idle }

        /// <summary>
        /// A private class to hold the combined state and settings for a monitor being actively managed.
        /// </summary>
        private class ManagedMonitorState
        {
            public int DisplayNumber { get; set; }
            public MonitorSettings Settings { get; set; }
            public Rect Bounds { get; set; }
        }

        /// <summary>
        /// Tracks the timer and state for a single monitor.
        /// </summary>
        private class MonitorTimerState
        {
            public MonitorStateMachine CurrentState { get; set; } = MonitorStateMachine.Active;
            public DateTime ActivityStoppedTimestamp { get; set; }
        }

        /// <summary>
        /// A snapshot of the system's state at a point in time.
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

        /// <summary>
        /// A dictionary to hold the state machine for each managed monitor.
        /// </summary>
        private readonly Dictionary<string, MonitorTimerState> _monitorStates = new();

        #endregion Private State Management

        #region Fields

        private CancellationTokenSource _cancellationTokenSource;
        private List<ManagedMonitorState> _managedMonitors = new List<ManagedMonitorState>();
        private readonly object _lock = new object();
        private readonly IMonitorService _monitorService;

        #endregion Fields

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
                                // Activity has stopped. Transition to the Counting state.
                                timerState.CurrentState = MonitorStateMachine.Counting;
                                timerState.ActivityStoppedTimestamp = DateTime.UtcNow;
                            }
                            break;

                        case MonitorStateMachine.Counting:
                            if (hasActivityNow)
                            {
                                // Activity resumed during the countdown. Transition back to Active.
                                timerState.CurrentState = MonitorStateMachine.Active;
                            }
                            else
                            {
                                // Still no activity. Check if the timer has expired.
                                var elapsed = DateTime.UtcNow - timerState.ActivityStoppedTimestamp;
                                if (elapsed.TotalMilliseconds >= monitor.Settings.IdleTimeMilliseconds)
                                {
                                    // Timer expired. Transition to Idle and raise the event.
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
                                // Activity resumed from an idle state. Raise the event.
                                MonitorBecameActive?.Invoke(this, eventArgs);

                                // Only transition back to Active if the event wasn't ignored by a handler.
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
        /// Determines the reason for any qualifying activity on a monitor at this exact moment.
        /// </summary>
        private ActivityReason GetActivityReason(ManagedMonitorState monitor, SystemState state)
        {
            // The user-defined idle time is the main gatekeeper. If the system has been idle for less
            // than this time, we consider it active due to general input, provided the setting is enabled.
            if (monitor.Settings.IsActiveOnInput && state.IdleTimeMilliseconds < monitor.Settings.IdleTimeMilliseconds)
            {
                return ActivityReason.SystemInput;
            }

            // If the system-wide timer has expired, we then check for monitor-specific "keep-alive" overrides.
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

            // If no conditions are met, there is no activity.
            return ActivityReason.None;
        }

        /// <summary>
        /// Gathers all required system-wide state information at once.
        /// </summary>
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
            return new SystemState(idleTime, cursorPosition, windowRect, foregroundWindowHandle);
        }

        /// <summary>
        /// Gets the system-wide user idle time in milliseconds using the GetLastInputInfo API.
        /// </summary>
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

        #endregion Private Core Logic
    }
}