using OLED_Sleeper.Features.MonitorDimming.Services.Interfaces;

namespace OLED_Sleeper.Tests.TestDoubles
{
    /// <summary>
    /// An <see cref="IDdcCiSession"/> that stands in for a panel, holding a brightness value instead of
    /// talking to hardware. A write is reflected by the next read, so a restore confirms by default.
    /// </summary>
    public class FakeDdcCiSession : IDdcCiSession
    {
        private uint _brightness;

        /// <param name="brightness">The brightness the panel reports before anything is written.</param>
        public FakeDdcCiSession(uint brightness)
        {
            _brightness = brightness;
        }

        /// <summary>
        /// When true, <see cref="GetBrightness"/> reports that the monitor did not answer.
        /// </summary>
        public bool ReadsFail { get; set; }

        /// <summary>
        /// When true, <see cref="SetBrightness"/> records the attempt and reports that the monitor
        /// rejected it, leaving the held brightness untouched.
        /// </summary>
        public bool WritesAreRejected { get; set; }

        /// <summary>
        /// The value the panel reports after a write, whatever was written. Null reflects the written
        /// value back, which is what a monitor that applied it does.
        /// </summary>
        public uint? ReadBackOverride { get; set; }

        /// <summary>
        /// Every value passed to <see cref="SetBrightness"/>, in order, including rejected writes.
        /// </summary>
        public List<uint> WrittenBrightnessLevels { get; } = new();

        /// <summary>
        /// How many times <see cref="Dispose"/> has run. One per channel the service opened means
        /// no handle was leaked.
        /// </summary>
        public int DisposeCount { get; private set; }

        /// <inheritdoc />
        public uint? GetBrightness()
        {
            return ReadsFail ? null : _brightness;
        }

        /// <inheritdoc />
        public bool SetBrightness(uint brightness)
        {
            WrittenBrightnessLevels.Add(brightness);
            if (WritesAreRejected) return false;

            _brightness = ReadBackOverride ?? brightness;
            return true;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            DisposeCount++;
        }
    }
}
