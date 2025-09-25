using OLED_Sleeper.Features.MonitorIdleDetection.Models;
using OLED_Sleeper.Features.MonitorIdleDetection.Services.Interfaces;
using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.MonitorInformation.Services.Interfaces;
using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.Native;
using Serilog;
using System.Runtime.InteropServices;
using System.Windows;

namespace OLED_Sleeper.Features.MonitorIdleDetection.Services
{
    /// <summary>
    /// Monitors user activity and determines when managed monitors become idle or active.
    /// Manages a timer for each monitor, raising events on state transitions.
    /// </summary>
    public class MonitorIdleDetectionService : IMonitorIdleDetectionService
    {
        #region Events

        /// <inheritdoc/>
        public event EventHandler<MonitorIdleStateEventArgs> MonitorBecameIdle;

        /// <inheritdoc/>
        public event EventHandler<MonitorIdleStateEventArgs> MonitorBecameActive;

        #endregion Events

        #region Fields

        private CancellationTokenSource _cancellationTokenSource;
        private List<ManagedMonitorState> _managedMonitors = new();
        private readonly object _lock = new();
        private readonly IMonitorInfoManager _monitorManager;
        private readonly Dictionary<string, MonitorTimerState> _monitorStates = new();

        #endregion Fields

        #region Nested Types

        /// <summary>
        /// State machine for per-monitor activity.
        /// </summary>
        private enum MonitorStateMachine
        {
            Active,
            Counting,
            Idle
        }

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
        private readonly struct SystemState(uint idleTime, Point cursorPosition, Rect windowRect, nint windowHandle)
        {
            public readonly uint IdleTimeMilliseconds = idleTime;
            public readonly Point CursorPosition = cursorPosition;
            public readonly Rect ForegroundWindowRect = windowRect;
            public readonly nint ForegroundWindowHandle = windowHandle;
        }

        #endregion Nested Types

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitorIdleDetectionService"/> class.
        /// </summary>
        /// <param name="monitorManager">Service for monitor information.</param>
        public MonitorIdleDetectionService(IMonitorInfoManager monitorManager)
        {
            _monitorManager = monitorManager;
        }

        #region Public Methods

        /// <inheritdoc/>
        public void Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => IdleCheckLoop(_cancellationTokenSource.Token));
            Log.Information("MonitorIdleDetectionService started.");
        }

        /// <inheritdoc/>
        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            Log.Information("MonitorIdleDetectionService stopped.");
        }

        /// <inheritdoc/>
        public void UpdateSettings(List<MonitorSettings> monitorSettings)
        {
            var activeSettings = monitorSettings.Where(s => s.IsManaged).ToList();

            void OnMonitorsReady(object? sender, IReadOnlyList<MonitorInfo> allMonitors)
            {
                _monitorManager.MonitorListReady -= OnMonitorsReady;

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
                Log.Information("MonitorIdleDetectionService settings updated. Now tracking {Count} monitors.", _managedMonitors.Count);
            }

            _monitorManager.MonitorListReady += OnMonitorsReady;
            _monitorManager.GetCurrentMonitorsAsync();
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
                    ProcessSingleMonitor(monitor, systemState);
                }
            }
        }

        /// <summary>
        /// Processes a single managed monitor according to the state machine logic.
        /// </summary>
        /// <param name="monitor">The managed monitor.</param>
        /// <param name="systemState">Current system state.</param>
        private void ProcessSingleMonitor(ManagedMonitorState monitor, SystemState systemState)
        {
            var timerState = _monitorStates[monitor.Settings.HardwareId];
            var activityReason = GetActivityReason(monitor, systemState);
            bool hasActivityNow = activityReason != ActivityReason.None;

            var eventArgs = new MonitorIdleStateEventArgs(
                monitor.Settings.HardwareId, monitor.DisplayNumber, monitor.Bounds,
                monitor.Settings, systemState.ForegroundWindowHandle, activityReason);

            switch (timerState.CurrentState)
            {
                case MonitorStateMachine.Active:
                    HandleActiveState(timerState, hasActivityNow);
                    break;

                case MonitorStateMachine.Counting:
                    HandleCountingState(timerState, monitor, hasActivityNow, eventArgs);
                    break;

                case MonitorStateMachine.Idle:
                    HandleIdleState(timerState, monitor, hasActivityNow, eventArgs);
                    break;
            }
        }

        /// <summary>
        /// Handles the Active state for a monitor.
        /// </summary>
        private void HandleActiveState(MonitorTimerState timerState, bool hasActivityNow)
        {
            if (!hasActivityNow)
            {
                timerState.CurrentState = MonitorStateMachine.Counting;
                timerState.ActivityStoppedTimestamp = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Handles the Counting state for a monitor.
        /// </summary>
        private void HandleCountingState(MonitorTimerState timerState, ManagedMonitorState monitor, bool hasActivityNow, MonitorIdleStateEventArgs eventArgs)
        {
            if (hasActivityNow)
            {
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
        }

        /// <summary>
        /// Handles the Idle state for a monitor.
        /// </summary>
        private void HandleIdleState(MonitorTimerState timerState, ManagedMonitorState monitor, bool hasActivityNow, MonitorIdleStateEventArgs eventArgs)
        {
            if (hasActivityNow)
            {
                MonitorBecameActive?.Invoke(this, eventArgs);
                if (!eventArgs.IsIgnored)
                {
                    timerState.CurrentState = MonitorStateMachine.Active;
                    Log.Information("Monitor #{DisplayNumber} is now ACTIVE.", monitor.DisplayNumber);
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
            if (IsSystemInputActive(monitor, state))
                return ActivityReason.SystemInput;

            if (IsMousePositionActive(monitor, state))
                return ActivityReason.MousePosition;

            if (IsActiveWindowActive(monitor, state))
                return ActivityReason.ActiveWindow;

            return ActivityReason.None;
        }

        /// <summary>
        /// Checks if system input should be considered activity for the monitor.
        /// </summary>
        private static bool IsSystemInputActive(ManagedMonitorState monitor, SystemState state)
        {
            return monitor.Settings.IsActiveOnInput && state.IdleTimeMilliseconds < monitor.Settings.IdleTimeMilliseconds;
        }

        /// <summary>
        /// Checks if mouse position should be considered activity for the monitor.
        /// </summary>
        private static bool IsMousePositionActive(ManagedMonitorState monitor, SystemState state)
        {
            return monitor.Settings.IsActiveOnMousePosition && monitor.Bounds.Contains(state.CursorPosition);
        }

        /// <summary>
        /// Checks if the active window should be considered activity for the monitor.
        /// </summary>
        private static bool IsActiveWindowActive(ManagedMonitorState monitor, SystemState state)
        {
            if (monitor.Settings.IsActiveOnActiveWindow)
            {
                Rect intersection = Rect.Intersect(monitor.Bounds, state.ForegroundWindowRect);
                if (!intersection.IsEmpty && intersection.Width > 0 && intersection.Height > 0)
                    return true;
            }
            return false;
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
            nint foregroundWindowHandle = NativeMethods.GetForegroundWindow();
            Rect windowRect = GetForegroundWindowRect(foregroundWindowHandle);
            return new SystemState(idleTime, cursorPosition, windowRect, foregroundWindowHandle);
        }

        /// <summary>
        /// Gets the rectangle of the foreground window.
        /// </summary>
        /// <param name="foregroundWindowHandle">The handle to the foreground window.</param>
        /// <returns>The window rectangle.</returns>
        private static Rect GetForegroundWindowRect(nint foregroundWindowHandle)
        {
            if (NativeMethods.DwmGetWindowAttribute(foregroundWindowHandle, NativeMethods.DWMWA_EXTENDED_FRAME_BOUNDS, out var nativeWindowRect, Marshal.SizeOf(typeof(NativeMethods.Rect))) == 0)
            {
                return nativeWindowRect.ToWindowsRect();
            }
            else
            {
                NativeMethods.GetWindowRect(foregroundWindowHandle, out nativeWindowRect);
                return nativeWindowRect.ToWindowsRect();
            }
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