using OLED_Sleeper.Features.MonitorIdleDetection.Media.Interfaces;
using OLED_Sleeper.Native;
using Serilog;
using System.Runtime.InteropServices;

namespace OLED_Sleeper.Features.MonitorIdleDetection.Media
{
    /// <summary>
    /// Finds which process families are playing audio by reading the peak meter of every audio session on every active render endpoint.
    /// </summary>
    /// <remarks>
    /// Core Audio COM objects are used from whichever thread calls, so the caller must be on a multithreaded-apartment thread; the idle
    /// loop runs on the thread pool, which is. A muted video has a zero peak and so is not seen here.
    /// </remarks>
    public class CoreAudioActivityProbe : IAudioActivityProbe, IDisposable
    {
        /// <summary>
        /// A peak above this counts as audible. It is low enough to catch quiet scenes.
        /// </summary>
        private const float AudiblePeakThreshold = 0.001f;

        /// <summary>
        /// How long a family stays audible after its peak was last above the threshold, in seconds. It bridges silent moments in a video.
        /// </summary>
        private const double AudibleMemorySeconds = 3;

        /// <summary>
        /// How often the sessions are enumerated again, in seconds. Between enumerations the cached meters are read.
        /// </summary>
        private const double EnumerationIntervalSeconds = 1;

        /// <summary>
        /// Each cached session's meter, paired with the family of the process that owns the session.
        /// </summary>
        private readonly List<(string Family, CoreAudioInterop.IAudioMeterInformation Meter)> _meters = new();

        /// <summary>
        /// When each family was last audible.
        /// </summary>
        private readonly Dictionary<string, DateTime> _lastAudibleUtc = new();

        private DateTime _lastEnumerationUtc = DateTime.MinValue;
        private bool _apartmentWarningLogged;

        /// <inheritdoc />
        public IReadOnlySet<string> GetAudibleFamilies()
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.MTA)
            {
                if (!_apartmentWarningLogged)
                {
                    _apartmentWarningLogged = true;
                    Log.Warning("Audio detection needs a multithreaded-apartment thread and was called from another. Audio is treated as silent.");
                }
                return new HashSet<string>();
            }

            var now = DateTime.UtcNow;

            try
            {
                if ((now - _lastEnumerationUtc).TotalSeconds >= EnumerationIntervalSeconds)
                {
                    RefreshSessions();
                    _lastEnumerationUtc = now;
                }

                foreach (var (family, meter) in _meters)
                {
                    if (meter.GetPeakValue(out float peak) == 0 && peak > AudiblePeakThreshold)
                    {
                        _lastAudibleUtc[family] = now;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Audio session sampling failed.");
                ReleaseSessions();
                _lastEnumerationUtc = DateTime.MinValue;
            }

            foreach (var family in _lastAudibleUtc.Where(entry => (now - entry.Value).TotalSeconds > AudibleMemorySeconds).Select(entry => entry.Key).ToList())
            {
                _lastAudibleUtc.Remove(family);
            }
            return _lastAudibleUtc.Keys.ToHashSet(StringComparer.Ordinal);
        }

        /// <summary>
        /// Releases the cached sessions and enumerates them again across every active render endpoint.
        /// </summary>
        private void RefreshSessions()
        {
            ReleaseSessions();

            CoreAudioInterop.IMMDeviceEnumerator? enumerator = null;
            CoreAudioInterop.IMMDeviceCollection? devices = null;
            try
            {
                enumerator = (CoreAudioInterop.IMMDeviceEnumerator)new CoreAudioInterop.MMDeviceEnumeratorClass();
                if (enumerator.EnumAudioEndpoints(CoreAudioInterop.EDataFlowRender, CoreAudioInterop.DEVICE_STATE_ACTIVE, out devices) != 0) return;
                if (devices.GetCount(out uint deviceCount) != 0) return;

                for (uint i = 0; i < deviceCount; i++)
                {
                    if (devices.Item(i, out var device) != 0) continue;
                    try
                    {
                        AddSessionsOf(device);
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(device);
                    }
                }
            }
            finally
            {
                if (devices != null) Marshal.ReleaseComObject(devices);
                if (enumerator != null) Marshal.ReleaseComObject(enumerator);
            }
        }

        /// <summary>
        /// Adds the metered sessions of one endpoint device to the cache.
        /// </summary>
        private void AddSessionsOf(CoreAudioInterop.IMMDevice device)
        {
            var iid = CoreAudioInterop.IID_IAudioSessionManager2;
            if (device.Activate(ref iid, CoreAudioInterop.CLSCTX_ALL, nint.Zero, out object managerObject) != 0) return;

            var manager = (CoreAudioInterop.IAudioSessionManager2)managerObject;
            CoreAudioInterop.IAudioSessionEnumerator? sessions = null;
            try
            {
                if (manager.GetSessionEnumerator(out sessions) != 0) return;
                if (sessions.GetCount(out int count) != 0) return;

                for (int i = 0; i < count; i++)
                {
                    if (sessions.GetSession(i, out var control) != 0) continue;

                    // The session control and its meter are the same COM object, so the meter is the one reference kept.
                    if (control.GetProcessId(out uint processId) == 0 && processId != 0 && control is CoreAudioInterop.IAudioMeterInformation meter)
                    {
                        _meters.Add((ProcessFamily.Of(processId), meter));
                    }
                    else
                    {
                        Marshal.ReleaseComObject(control);
                    }
                }
            }
            finally
            {
                if (sessions != null) Marshal.ReleaseComObject(sessions);
                Marshal.ReleaseComObject(manager);
            }
        }

        /// <summary>
        /// Releases every cached session.
        /// </summary>
        private void ReleaseSessions()
        {
            foreach (var (_, meter) in _meters)
            {
                Marshal.ReleaseComObject(meter);
            }
            _meters.Clear();
        }

        /// <inheritdoc />
        public void Dispose() => ReleaseSessions();
    }
}
