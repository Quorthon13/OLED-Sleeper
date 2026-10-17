using OLED_Sleeper.Features.MonitorBehavior.Commands;
using OLED_Sleeper.Features.MonitorIdleDetection.Media.Interfaces;
using OLED_Sleeper.Features.MonitorIdleDetection.Media.Models;
using OLED_Sleeper.Features.MonitorIdleDetection.Models;
using OLED_Sleeper.Features.MonitorIdleDetection.Services.Interfaces;
using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.Messaging.Interfaces;
using OLED_Sleeper.Native;
using Serilog;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace OLED_Sleeper.Features.MonitorIdleDetection.Services
{
    /// <summary>
    /// Monitors user activity and determines when managed monitors become idle or active.
    /// Handles per-monitor state machines and dispatches commands to apply idle/active behaviors.
    /// </summary>
    public class MonitorIdleDetectionService : IMonitorIdleDetectionService
    {
        /// <summary>
        /// Window classes that make up the shell desktop. The desktop spans the whole virtual screen, so
        /// it never counts as active-window activity for any monitor.
        /// </summary>
        private static readonly HashSet<string> DesktopWindowClasses = new(StringComparer.Ordinal)
        {
            "Progman",
            "WorkerW"
        };

        /// <summary>
        /// Buffer size, in characters, for a window class name.
        /// </summary>
        private const int ClassNameBufferLength = 256;

        /// <summary>
        /// The longest a monitor is kept awake by video playback alone. Some games hold a display request even when paused on a
        /// static screen, which would otherwise burn that image in. Once the cap is reached, only real activity (input, the mouse
        /// or the active window) keeps the monitor awake, and it resets the cap.
        /// </summary>
        internal static readonly TimeSpan MaxMediaKeepAwake = TimeSpan.FromHours(4);

        // === Dependencies & State ===
        private readonly IMediator _mediator;
        private readonly IMediaActivityMonitor _mediaActivityMonitor;
        private readonly TimeProvider _timeProvider;

        private CancellationTokenSource? _cancellationTokenSource;
        private List<ManagedMonitorState> _managedMonitors = new();
        private readonly object _lock = new();
        private readonly Dictionary<string, MonitorTimerState> _monitorStates = new();

        // === Construction ===

        public MonitorIdleDetectionService(IMediator mediator, IMediaActivityMonitor mediaActivityMonitor, TimeProvider timeProvider)
        {
            _mediator = mediator;
            _mediaActivityMonitor = mediaActivityMonitor;
            _timeProvider = timeProvider;
        }

        // === Service Lifecycle ===

        /// <summary>
        /// Starts the idle detection service and begins monitoring. Any previous loop is stopped first.
        /// </summary>
        public void Start()
        {
            Stop();

            var cancellationTokenSource = new CancellationTokenSource();
            _cancellationTokenSource = cancellationTokenSource;
            Task.Run(() => IdleCheckLoop(cancellationTokenSource.Token));
            Log.Information("MonitorIdleDetectionService started.");
        }

        /// <summary>
        /// Stops the idle detection service and monitoring. A second call does nothing.
        /// </summary>
        public void Stop()
        {
            var cancellationTokenSource = Interlocked.Exchange(ref _cancellationTokenSource, null);
            if (cancellationTokenSource == null) return;

            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            Log.Information("MonitorIdleDetectionService stopped.");
        }

        /// <summary>
        /// Updates the settings for all managed monitors.
        /// </summary>
        /// <param name="monitorSettings">The list of monitor settings to manage.</param>
        /// <param name="monitors">The monitors to join the settings against, supplying bounds and display numbers.</param>
        /// <remarks>
        /// Callers supply the monitor list; this class never reads the shared cache and does not depend on
        /// the monitor manager, so no lock that manager owns can be taken while <see cref="_lock"/> is held.
        /// </remarks>
        public Task UpdateSettingsAsync(List<MonitorSettings> monitorSettings, IReadOnlyList<MonitorInfo> monitors)
        {
            var activeSettings = monitorSettings.Where(s => s.IsManaged).ToList();

            int trackedCount;
            lock (_lock)
            {
                _managedMonitors = (from setting in activeSettings
                                    join monitorInfo in monitors on setting.HardwareId equals monitorInfo.HardwareId
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

                trackedCount = _managedMonitors.Count;
            }

            Log.Information("MonitorIdleDetectionService settings updated. Now tracking {Count} monitors.", trackedCount);
            return Task.CompletedTask;
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
            ProcessTick(GetSystemState());
        }

        /// <summary>
        /// Runs one pass of the state machine over every managed monitor.
        /// </summary>
        /// <param name="systemState">The system state for this pass.</param>
        internal void ProcessTick(SystemState systemState)
        {
            lock (_lock)
            {
                var mediaDecisions = EvaluateMedia();

                foreach (var monitor in _managedMonitors)
                {
                    mediaDecisions.TryGetValue(monitor.Settings.HardwareId, out var mediaDecision);
                    ProcessSingleMonitor(monitor, systemState, mediaDecision);
                }
            }
        }

        /// <summary>
        /// Asks the media monitor about every monitor that is not idle and has the video option on.
        /// A monitor that is idle is never asked, so media can neither wake it nor cost a capture.
        /// </summary>
        /// <returns>The decisions, keyed by hardware ID.</returns>
        private IReadOnlyDictionary<string, MediaDecision> EvaluateMedia()
        {
            var requests = _managedMonitors
                .Where(monitor => monitor.Settings.IsActiveOnVideo &&
                                  _monitorStates[monitor.Settings.HardwareId].CurrentState != MonitorStateMachine.Idle)
                .Select(monitor => new MediaMonitorRequest(monitor.Settings.HardwareId, monitor.Bounds))
                .ToList();

            return _mediaActivityMonitor.Evaluate(requests);
        }

        /// <summary>
        /// Processes a single managed monitor according to the state machine logic.
        /// </summary>
        /// <param name="monitor">The managed monitor.</param>
        /// <param name="systemState">Current system state.</param>
        /// <param name="mediaDecision">The video decision for the monitor, or null when it was not evaluated.</param>
        private void ProcessSingleMonitor(ManagedMonitorState monitor, SystemState systemState, MediaDecision? mediaDecision)
        {
            var timerState = _monitorStates[monitor.Settings.HardwareId];
            var activityReason = GetActivityReason(monitor, systemState, mediaDecision);
            activityReason = ApplyMediaKeepAwakeCap(timerState, monitor, activityReason, mediaDecision);
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
                    // Video playback never wakes a monitor that is already idle; only real activity does.
                    HandleIdleState(timerState, monitor, hasActivityNow && activityReason != ActivityReason.VideoPlayback, eventArgs);
                    break;
            }
        }

        // === Media Keep-Awake ===

        /// <summary>
        /// Tracks how long video playback alone has kept a monitor awake and withdraws it once the cap is reached.
        /// Real activity resets the tracking; so does the monitor going idle.
        /// </summary>
        /// <param name="timerState">The timer state for the monitor.</param>
        /// <param name="monitor">The managed monitor.</param>
        /// <param name="reason">The reason found for this pass.</param>
        /// <param name="mediaDecision">The video decision, for the log.</param>
        /// <returns>The reason to act on: the one given, or <see cref="ActivityReason.None"/> when the cap withdrew a media reason.</returns>
        private ActivityReason ApplyMediaKeepAwakeCap(MonitorTimerState timerState, ManagedMonitorState monitor, ActivityReason reason, MediaDecision? mediaDecision)
        {
            if (reason != ActivityReason.VideoPlayback)
            {
                if (timerState.LastMediaSignal != null)
                {
                    Log.Debug("Monitor #{DisplayNumber} media keep-awake ended.", monitor.DisplayNumber);
                    timerState.LastMediaSignal = null;
                }

                if (reason != ActivityReason.None || timerState.CurrentState == MonitorStateMachine.Idle)
                {
                    timerState.MediaKeepAwakeSince = null;
                    timerState.MediaCapLogged = false;
                }
                return reason;
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            timerState.MediaKeepAwakeSince ??= now;

            if (now - timerState.MediaKeepAwakeSince.Value >= MaxMediaKeepAwake)
            {
                if (!timerState.MediaCapLogged)
                {
                    timerState.MediaCapLogged = true;
                    Log.Information("Monitor #{DisplayNumber} has been kept awake by video playback for {Hours}h. It may now go idle until there is real activity.",
                        monitor.DisplayNumber, MaxMediaKeepAwake.TotalHours);
                }
                return ActivityReason.None;
            }

            string signal = mediaDecision?.Signal ?? string.Empty;
            if (timerState.LastMediaSignal != signal)
            {
                Log.Debug("Monitor #{DisplayNumber} kept awake by {Reason} ({Signal}).", monitor.DisplayNumber, reason, signal);
            }
            timerState.LastMediaSignal = signal;
            return reason;
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
                timerState.ActivityStoppedTimestamp = _timeProvider.GetUtcNow().UtcDateTime;
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
                var elapsed = _timeProvider.GetUtcNow().UtcDateTime - timerState.ActivityStoppedTimestamp;
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
        /// <param name="mediaDecision">The video decision for the monitor, or null when it was not evaluated.</param>
        /// <returns>The activity reason. Real activity is checked first, so a media reason means nothing else applied.</returns>
        private static ActivityReason GetActivityReason(ManagedMonitorState monitor, SystemState state, MediaDecision? mediaDecision)
        {
            if (IsSystemInputActive(monitor, state))
                return ActivityReason.SystemInput;
            if (IsMousePositionActive(monitor, state))
                return ActivityReason.MousePosition;
            if (IsActiveWindowActive(monitor, state))
                return ActivityReason.ActiveWindow;
            if (monitor.Settings.IsActiveOnVideo && mediaDecision is { IsVideoPlaying: true })
                return ActivityReason.VideoPlayback;
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

        // === System State Helpers ===

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
            Rect windowRect = IsDesktopWindow(foregroundWindowHandle)
                ? Rect.Empty
                : GetForegroundWindowRect(foregroundWindowHandle);
            return new SystemState(idleTime, cursorPosition, windowRect, foregroundWindowHandle);
        }

        /// <summary>
        /// Determines whether a window is the shell desktop rather than an application window.
        /// </summary>
        /// <param name="hwnd">The handle to test.</param>
        /// <returns>True when the window belongs to one of the <see cref="DesktopWindowClasses"/>; false for a null handle or a class name the shell would not answer.</returns>
        private static bool IsDesktopWindow(nint hwnd)
        {
            if (hwnd == nint.Zero) return false;

            var className = new StringBuilder(ClassNameBufferLength);
            if (NativeMethods.GetClassName(hwnd, className, className.Capacity) == 0) return false;

            return DesktopWindowClasses.Contains(className.ToString());
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

            /// <summary>When video playback started keeping the monitor awake, or null while it has not (since real activity or idling).</summary>
            public DateTime? MediaKeepAwakeSince { get; set; }

            /// <summary>Whether the cap on media keep-awake has been logged for the current stretch.</summary>
            public bool MediaCapLogged { get; set; }

            /// <summary>The signal logged for the current media keep-awake, or null while media is not keeping the monitor awake.</summary>
            public string? LastMediaSignal { get; set; }
        }

        /// <summary>
        /// Snapshot of system state at a point in time.
        /// </summary>
        internal readonly struct SystemState
        {
            public readonly uint IdleTimeMilliseconds;
            public readonly Point CursorPosition;
            public readonly Rect ForegroundWindowRect;
            public readonly nint ForegroundWindowHandle;

            public SystemState(uint idleTime, Point cursorPosition, Rect windowRect, nint windowHandle)
            {
                IdleTimeMilliseconds = idleTime;
                CursorPosition = cursorPosition;
                ForegroundWindowRect = windowRect;
                ForegroundWindowHandle = windowHandle;
            }
        }
    }
}