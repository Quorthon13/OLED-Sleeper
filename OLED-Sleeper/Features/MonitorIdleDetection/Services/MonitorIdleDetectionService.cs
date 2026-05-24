using OLED_Sleeper.Core.Interfaces;
using OLED_Sleeper.Features.MonitorBehavior.Commands;
using OLED_Sleeper.Features.MonitorBlackout.Services.Interfaces;
using OLED_Sleeper.Features.MonitorIdleDetection.Models;
using OLED_Sleeper.Features.MonitorIdleDetection.Services.Interfaces;
using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.MonitorInformation.Services.Interfaces;
using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.Native;
using Serilog;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace OLED_Sleeper.Features.MonitorIdleDetection.Services
{
    /// <summary>
    /// Monitors user activity and determines when managed monitors become idle or active.
    /// Manages a timer for each monitor, raising events on state transitions.
    /// </summary>

    /// <summary>
    /// Service that monitors user activity and determines when managed monitors become idle or active.
    /// Handles per-monitor state machines and dispatches commands to apply idle/active behaviors.
    /// </summary>
    public class MonitorIdleDetectionService : IMonitorIdleDetectionService
    {
        // === Dependencies & State ===
        private readonly IMediator _mediator;

        private readonly IMonitorInfoManager _monitorManager;
        private readonly IMonitorBlackoutService _monitorBlackoutService;
        private CancellationTokenSource? _cancellationTokenSource;
        private List<ManagedMonitorState> _managedMonitors = new();
        private readonly object _lock = new();
        private readonly Dictionary<string, MonitorTimerState> _monitorStates = new();
        private static readonly HashSet<string> EmptyHardwareIdSet = new(StringComparer.Ordinal);
        private HashSet<string> _cachedVisibleWindowHardwareIds = new(StringComparer.Ordinal);
        private DateTime _visibleWindowCacheUtc = DateTime.MinValue;
        private const int VisibleWindowScanIntervalMs = 400;

        // === Construction ===

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitorIdleDetectionService"/> class.
        /// </summary>
        /// <param name="monitorManager">Service for monitor information.</param>
        /// <param name="mediator">Mediator for dispatching monitor behavior commands.</param>
        /// <param name="monitorBlackoutService">Service used to ignore blackout overlay windows when enumerating visible windows.</param>
        public MonitorIdleDetectionService(
            IMonitorInfoManager monitorManager,
            IMediator mediator,
            IMonitorBlackoutService monitorBlackoutService)
        {
            _monitorManager = monitorManager;
            _mediator = mediator;
            _monitorBlackoutService = monitorBlackoutService;
        }

        // === Service Lifecycle ===

        /// <summary>
        /// Starts the idle detection service and begins monitoring.
        /// </summary>
        public void Start()
        {
            lock (_lock)
            {
                if (_cancellationTokenSource != null)
                    return;

                _cancellationTokenSource = new CancellationTokenSource();
                var token = _cancellationTokenSource.Token;
                _ = Task.Run(() => IdleCheckLoop(token));
            }

            Log.Information("MonitorIdleDetectionService started.");
        }

        /// <summary>
        /// Stops the idle detection service and monitoring.
        /// </summary>
        public void Stop()
        {
            lock (_lock)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }

            Log.Information("MonitorIdleDetectionService stopped.");
        }

        /// <summary>
        /// Updates the settings for all managed monitors.
        /// </summary>
        /// <param name="monitorSettings">The list of monitor settings to manage.</param>
        public void UpdateSettings(List<MonitorSettings> monitorSettings)
        {
            var activeSettings = monitorSettings.Where(s => s.IsManaged).ToList();

            void OnMonitorsReady(object? sender, IReadOnlyList<MonitorInfo> allMonitors)
            {
                _monitorManager.MonitorListReady -= OnMonitorsReady;

                int count;
                lock (_lock)
                {
                    ApplyManagedMonitorsAndResetTimersUnlocked(activeSettings, allMonitors);
                    count = _managedMonitors.Count;
                }

                Log.Information("MonitorIdleDetectionService settings updated. Now tracking {Count} monitors.", count);
            }

            _monitorManager.MonitorListReady += OnMonitorsReady;
            _monitorManager.GetCurrentMonitorsAsync();
        }

        /// <inheritdoc />
        public void ApplyTopologyAndSettings(List<MonitorSettings> monitorSettings, IReadOnlyList<MonitorInfo> activeEnrichedMonitors)
        {
            var activeSettings = monitorSettings.Where(s => s.IsManaged).ToList();

            int count;
            lock (_lock)
            {
                ApplyManagedMonitorsAndResetTimersUnlocked(activeSettings, activeEnrichedMonitors);
                count = _managedMonitors.Count;
            }

            Log.Information("MonitorIdleDetectionService topology applied. Now tracking {Count} monitors.", count);
        }

        /// <summary>
        /// Rebuilds managed monitors and timer state. Caller must hold <c>_lock</c>.
        /// </summary>
        private void ApplyManagedMonitorsAndResetTimersUnlocked(
            List<MonitorSettings> activeSettings,
            IReadOnlyList<MonitorInfo> allMonitors)
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

            _visibleWindowCacheUtc = DateTime.MinValue;
        }

        // === Idle Detection Loop ===

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
            List<ManagedMonitorState> snapshot;
            lock (_lock)
            {
                snapshot = _managedMonitors.ToList();
            }

            var visibleOnHardwareIds = GetVisibleWindowHardwareIdsSnapshot(snapshot);
            var systemState = GetSystemState(visibleOnHardwareIds);

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

        // === State Machine Handlers ===

        /// <summary>
        /// Handles the Active state for a monitor. Transitions to Counting if no activity is detected.
        /// </summary>
        /// <param name="timerState">The timer state for the monitor.</param>
        /// <param name="hasActivityNow">Whether activity is currently detected.</param>
        private void HandleActiveState(MonitorTimerState timerState, bool hasActivityNow)
        {
            if (!hasActivityNow)
            {
                timerState.CurrentState = MonitorStateMachine.Counting;
                timerState.ActivityStoppedTimestamp = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Handles the Counting state for a monitor. If idle time is reached, transitions to Idle and dispatches idle behavior command.
        /// </summary>
        /// <param name="timerState">The timer state for the monitor.</param>
        /// <param name="monitor">The managed monitor.</param>
        /// <param name="hasActivityNow">Whether activity is currently detected.</param>
        /// <param name="eventArgs">Monitor idle state event arguments.</param>
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
                    _mediator.SendAsync(new ApplyMonitorIdleBehaviorCommand(eventArgs));
                }
            }
        }

        /// <summary>
        /// Handles the Idle state for a monitor. If activity is detected, transitions to Active and dispatches active behavior command.
        /// </summary>
        /// <param name="timerState">The timer state for the monitor.</param>
        /// <param name="monitor">The managed monitor.</param>
        /// <param name="hasActivityNow">Whether activity is currently detected.</param>
        /// <param name="eventArgs">Monitor idle state event arguments.</param>
        private void HandleIdleState(MonitorTimerState timerState, ManagedMonitorState monitor, bool hasActivityNow, MonitorIdleStateEventArgs eventArgs)
        {
            if (hasActivityNow)
            {
                _mediator.SendAsync(new ApplyMonitorActiveBehaviorCommand(eventArgs));
                if (!eventArgs.IsIgnored)
                {
                    timerState.CurrentState = MonitorStateMachine.Active;
                    Log.Information("Monitor #{DisplayNumber} is now ACTIVE.", monitor.DisplayNumber);
                }
            }
        }

        // === Activity Detection Helpers ===

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
            if (IsVisibleWindowActive(monitor, state))
                return ActivityReason.VisibleWindow;
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
        /// Checks whether a visible (non-foreground) window on this monitor should count as activity.
        /// </summary>
        private static bool IsVisibleWindowActive(ManagedMonitorState monitor, SystemState state)
        {
            return monitor.Settings.IsActiveOnVisibleWindows
                   && state.HardwareIdsWithVisibleWindows.Contains(monitor.Settings.HardwareId);
        }

        /// <summary>
        /// Returns cached or freshly enumerated hardware IDs for monitors that have a qualifying visible window.
        /// </summary>
        private HashSet<string> GetVisibleWindowHardwareIdsSnapshot(IReadOnlyList<ManagedMonitorState> monitors)
        {
            if (!monitors.Any(m => m.Settings.IsActiveOnVisibleWindows))
                return EmptyHardwareIdSet;

            var now = DateTime.UtcNow;
            if ((now - _visibleWindowCacheUtc).TotalMilliseconds < VisibleWindowScanIntervalMs)
                return _cachedVisibleWindowHardwareIds;

            _visibleWindowCacheUtc = now;
            _cachedVisibleWindowHardwareIds = ComputeHardwareIdsWithVisibleAppWindows(monitors);
            return _cachedVisibleWindowHardwareIds;
        }

        /// <summary>
        /// Enumerates top-level windows and maps monitors (with the option enabled) that intersect a visible, non-shell window.
        /// </summary>
        private HashSet<string> ComputeHardwareIdsWithVisibleAppWindows(IReadOnlyList<ManagedMonitorState> monitors)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            var targets = monitors.Where(m => m.Settings.IsActiveOnVisibleWindows).ToList();
            if (targets.Count == 0)
                return result;

            var distinctHardwareIds = targets.Select(m => m.Settings.HardwareId).Distinct(StringComparer.Ordinal).ToList();
            var monitorHandleByHardwareId = new Dictionary<string, nint>(StringComparer.Ordinal);
            foreach (var id in distinctHardwareIds)
            {
                var sample = targets.First(m => m.Settings.HardwareId == id);
                monitorHandleByHardwareId[id] = GetMonitorHandleFromBounds(sample.Bounds);
            }

            nint shellWindow = NativeMethods.GetShellWindow();

            NativeMethods.EnumWindows((hWnd, _) =>
            {
                if (result.Count >= distinctHardwareIds.Count)
                    return false;

                nint hwnd = hWnd;
                if (!NativeMethods.IsWindow(hwnd) || !NativeMethods.IsWindowVisible(hwnd) || NativeMethods.IsIconic(hwnd))
                    return true;
                if (hwnd == shellWindow || _monitorBlackoutService.IsOverlayWindow(hwnd))
                    return true;
                if (IsWindowCloaked(hwnd))
                    return true;

                var className = GetWindowClassName(hwnd);
                if (IsShellDesktopWindowClass(className))
                    return true;

                Rect windowBounds = GetWindowScreenBoundsForVisibleScan(hwnd);
                if (windowBounds.IsEmpty || windowBounds.Width <= 0 || windowBounds.Height <= 0)
                    return true;

                nint windowMonitor = (nint)NativeMethods.MonitorFromWindow((IntPtr)hwnd, NativeMethods.MONITOR_DEFAULTTONULL);

                foreach (var m in targets)
                {
                    if (result.Contains(m.Settings.HardwareId))
                        continue;
                    if (!monitorHandleByHardwareId.TryGetValue(m.Settings.HardwareId, out nint targetMonitor))
                        continue;

                    bool anchoredOnMonitor = windowMonitor != nint.Zero && windowMonitor == targetMonitor;
                    bool rectsOverlap = MonitorIntersectsWindow(m.Bounds, windowBounds);
                    if (!(anchoredOnMonitor || rectsOverlap))
                        continue;

                    // Require meaningful coverage on this monitor so shadows, DWM helpers, or huge rects
                    // that only clip a sliver of the display do not block blackout.
                    if (!HasSignificantVisibleOverlap(m.Bounds, windowBounds))
                        continue;

                    result.Add(m.Settings.HardwareId);
                }

                return true;
            }, IntPtr.Zero);

            return result;
        }

        private static nint GetMonitorHandleFromBounds(Rect bounds)
        {
            var nativeRect = new NativeMethods.Rect
            {
                left = (int)Math.Floor(bounds.Left),
                top = (int)Math.Floor(bounds.Top),
                right = (int)Math.Ceiling(bounds.Right),
                bottom = (int)Math.Ceiling(bounds.Bottom)
            };
            return (nint)NativeMethods.MonitorFromRect(ref nativeRect, NativeMethods.MONITOR_DEFAULTTONEAREST);
        }

        /// <summary>
        /// Window bounds for "is something covering this monitor" checks.
        /// Uses <see cref="NativeMethods.GetWindowRect"/> first because DWM extended frame bounds are often wrong or empty for fullscreen and GPU-presented windows.
        /// </summary>
        private static Rect GetWindowScreenBoundsForVisibleScan(nint hwnd)
        {
            if (NativeMethods.GetWindowRect(hwnd, out var nativeRect))
            {
                var r = nativeRect.ToWindowsRect();
                if (r.Width > 0 && r.Height > 0)
                    return r;
            }

            if (NativeMethods.DwmGetWindowAttribute(hwnd, NativeMethods.DWMWA_EXTENDED_FRAME_BOUNDS, out nativeRect, Marshal.SizeOf(typeof(NativeMethods.Rect))) == 0)
            {
                var r = nativeRect.ToWindowsRect();
                if (r.Width > 0 && r.Height > 0)
                    return r;
            }

            return Rect.Empty;
        }

        private static bool MonitorIntersectsWindow(Rect monitorBounds, Rect windowBounds)
        {
            Rect intersection = Rect.Intersect(monitorBounds, windowBounds);
            return !intersection.IsEmpty && intersection.Width > 0 && intersection.Height > 0;
        }

        /// <summary>
        /// True when the window covers enough of the monitor to count as real content (not a shadow, sliver, or stray overlap from another display).
        /// </summary>
        private static bool HasSignificantVisibleOverlap(Rect monitorBounds, Rect windowBounds)
        {
            Rect intersection = Rect.Intersect(monitorBounds, windowBounds);
            if (intersection.IsEmpty || intersection.Width <= 0 || intersection.Height <= 0)
                return false;

            double intersectionPixels = intersection.Width * intersection.Height;
            double monitorPixels = monitorBounds.Width * monitorBounds.Height;
            if (monitorPixels <= 0)
                return false;

            const double minFractionOfMonitor = 0.015;
            const double absoluteMinPixels = 40_000;
            double threshold = Math.Max(absoluteMinPixels, monitorPixels * minFractionOfMonitor);
            return intersectionPixels >= threshold;
        }

        private static string GetWindowClassName(nint hwnd)
        {
            var sb = new StringBuilder(256);
            return NativeMethods.GetClassName(hwnd, sb, sb.Capacity) > 0 ? sb.ToString() : string.Empty;
        }

        private static bool IsShellDesktopWindowClass(string className) =>
            className.Equals("Progman", StringComparison.OrdinalIgnoreCase)
            || className.Equals("WorkerW", StringComparison.OrdinalIgnoreCase)
            || className.Equals("Shell_TrayWnd", StringComparison.OrdinalIgnoreCase)
            || className.Equals("Shell_SecondaryTrayWnd", StringComparison.OrdinalIgnoreCase)
            || className.Equals("NotifyIconOverflowWindow", StringComparison.OrdinalIgnoreCase)
            || className.Equals("DummyDWMListenerWindow", StringComparison.OrdinalIgnoreCase)
            || className.Equals("XamlExplorerHostIslandWindow", StringComparison.OrdinalIgnoreCase)
            || className.Equals("SysShadow", StringComparison.OrdinalIgnoreCase)
            || className.Equals("Windows.UI.Composition.DesktopWindowContentBridge", StringComparison.OrdinalIgnoreCase);

        private static bool IsWindowCloaked(nint hwnd)
        {
            if (NativeMethods.DwmGetWindowAttributeInt(hwnd, NativeMethods.DWMWA_CLOAKED, out int cloaked, sizeof(int)) != 0)
                return false;
            return cloaked != 0;
        }

        // === System State Helpers ===

        /// <summary>
        /// Gathers all required system-wide state information at once.
        /// </summary>
        /// <param name="hardwareIdsWithVisibleWindows">Monitors that currently have a qualifying visible window.</param>
        /// <returns>System state snapshot.</returns>
        private static SystemState GetSystemState(HashSet<string> hardwareIdsWithVisibleWindows)
        {
            uint idleTime = GetSystemIdleTimeMilliseconds();
            NativeMethods.GetCursorPos(out var nativePoint);
            Point cursorPosition = new(nativePoint.X, nativePoint.Y);
            nint foregroundWindowHandle = NativeMethods.GetForegroundWindow();
            Rect windowRect = GetForegroundWindowRect(foregroundWindowHandle);
            return new SystemState(idleTime, cursorPosition, windowRect, foregroundWindowHandle, hardwareIdsWithVisibleWindows);
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

        // === Internal Types ===

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
            public required MonitorSettings Settings { get; set; }
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
            public readonly nint ForegroundWindowHandle;
            public readonly HashSet<string> HardwareIdsWithVisibleWindows;

            public SystemState(
                uint idleTime,
                Point cursorPosition,
                Rect windowRect,
                nint windowHandle,
                HashSet<string> hardwareIdsWithVisibleWindows)
            {
                IdleTimeMilliseconds = idleTime;
                CursorPosition = cursorPosition;
                ForegroundWindowRect = windowRect;
                ForegroundWindowHandle = windowHandle;
                HardwareIdsWithVisibleWindows = hardwareIdsWithVisibleWindows;
            }
        }
    }
}