using OLED_Sleeper.Features.MonitorIdleDetection.Media.Helpers;
using OLED_Sleeper.Features.MonitorIdleDetection.Media.Interfaces;
using OLED_Sleeper.Native;
using Serilog;
using System.Runtime.InteropServices;
using System.Windows;

namespace OLED_Sleeper.Features.MonitorIdleDetection.Media
{
    /// <summary>
    /// Captures a tiny downscaled copy of a monitor once a second with GDI and compares it with the previous one.
    /// </summary>
    /// <remarks>
    /// DRM-protected video often captures as black and so reads as static; audio is the fallback for that case. Pixels that the
    /// compositor does not expose to GDI, such as some hardware overlay planes, have the same effect.
    /// </remarks>
    public class GdiScreenMotionDetector : IScreenMotionDetector
    {
        /// <summary>
        /// Width of the downscaled capture, in pixels.
        /// </summary>
        private const int CaptureWidth = 64;

        /// <summary>
        /// Height of the downscaled capture, in pixels.
        /// </summary>
        private const int CaptureHeight = 36;

        /// <summary>
        /// How often a monitor is sampled, in seconds.
        /// </summary>
        private const double SampleIntervalSeconds = 1;

        private const int BytesPerPixel = 4;

        private readonly Dictionary<string, MonitorMotionState> _states = new();

        /// <summary>
        /// The previous frame, the history and the sampling time of one monitor.
        /// </summary>
        private sealed class MonitorMotionState
        {
            public byte[]? PreviousFrame { get; set; }
            public MotionHistory History { get; } = new();
            public DateTime LastSampleUtc { get; set; } = DateTime.MinValue;
        }

        /// <inheritdoc />
        public bool IsMoving(string hardwareId, Rect bounds)
        {
            if (!_states.TryGetValue(hardwareId, out var state))
            {
                state = new MonitorMotionState();
                _states[hardwareId] = state;
            }

            var now = DateTime.UtcNow;
            if ((now - state.LastSampleUtc).TotalSeconds >= SampleIntervalSeconds)
            {
                state.LastSampleUtc = now;
                Sample(state, bounds);
            }

            return state.History.IsMoving;
        }

        /// <inheritdoc />
        public void Forget(string hardwareId) => _states.Remove(hardwareId);

        /// <summary>
        /// Captures the monitor and adds the comparison with the previous frame to its history.
        /// </summary>
        private static void Sample(MonitorMotionState state, Rect bounds)
        {
            var frame = Capture(bounds);
            if (frame == null) return;

            if (state.PreviousFrame != null)
            {
                state.History.Add(FrameDifference.ChangedFraction(state.PreviousFrame, frame));
            }
            state.PreviousFrame = frame;
        }

        /// <summary>
        /// Copies a monitor's pixels, shrunk with halftone averaging, into a BGRA buffer.
        /// </summary>
        /// <returns>The pixels, or null when the capture failed.</returns>
        private static byte[]? Capture(Rect bounds)
        {
            nint screenDc = nint.Zero;
            nint memoryDc = nint.Zero;
            nint bitmap = nint.Zero;
            nint previous = nint.Zero;

            try
            {
                screenDc = NativeMethods.GetDC(nint.Zero);
                if (screenDc == nint.Zero) return null;

                memoryDc = NativeMethods.CreateCompatibleDC(screenDc);
                if (memoryDc == nint.Zero) return null;

                var info = new NativeMethods.BITMAPINFO
                {
                    bmiHeader = new NativeMethods.BITMAPINFOHEADER
                    {
                        biSize = (uint)Marshal.SizeOf<NativeMethods.BITMAPINFOHEADER>(),
                        biWidth = CaptureWidth,
                        biHeight = -CaptureHeight,
                        biPlanes = 1,
                        biBitCount = 32
                    }
                };

                bitmap = NativeMethods.CreateDIBSection(screenDc, ref info, 0, out nint bits, nint.Zero, 0);
                if (bitmap == nint.Zero || bits == nint.Zero) return null;

                previous = NativeMethods.SelectObject(memoryDc, bitmap);
                NativeMethods.SetStretchBltMode(memoryDc, NativeMethods.HALFTONE);
                NativeMethods.SetBrushOrgEx(memoryDc, 0, 0, nint.Zero);

                if (!NativeMethods.StretchBlt(
                        memoryDc, 0, 0, CaptureWidth, CaptureHeight,
                        screenDc, (int)bounds.Left, (int)bounds.Top, (int)bounds.Width, (int)bounds.Height,
                        NativeMethods.SRCCOPY))
                {
                    return null;
                }

                NativeMethods.GdiFlush();

                var pixels = new byte[CaptureWidth * CaptureHeight * BytesPerPixel];
                Marshal.Copy(bits, pixels, 0, pixels.Length);
                return pixels;
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "The screen capture for motion detection failed.");
                return null;
            }
            finally
            {
                if (memoryDc != nint.Zero && previous != nint.Zero) NativeMethods.SelectObject(memoryDc, previous);
                if (bitmap != nint.Zero) NativeMethods.DeleteObject(bitmap);
                if (memoryDc != nint.Zero) NativeMethods.DeleteDC(memoryDc);
                if (screenDc != nint.Zero) NativeMethods.ReleaseDC(nint.Zero, screenDc);
            }
        }
    }
}
