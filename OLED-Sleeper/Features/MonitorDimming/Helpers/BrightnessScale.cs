namespace OLED_Sleeper.Features.MonitorDimming.Helpers
{
    /// <summary>
    /// Maps dim levels expressed as percentages onto a monitor's own brightness range.
    /// This is a static helper class and does not hold any state.
    /// </summary>
    public static class BrightnessScale
    {
        /// <summary>Upper bound of the percentage scale that the settings and the UI express the dim level on.</summary>
        private const uint DimLevelPercentageMax = 100;

        /// <summary>
        /// Maps a percentage onto a monitor's brightness range.
        /// </summary>
        /// <param name="dimLevelPercentage">The dim level as a percentage. Values outside 0-100 are clamped.</param>
        /// <param name="maxBrightness">The monitor's highest accepted brightness value.</param>
        /// <returns>
        /// The percentage itself when the monitor reported no range or already runs on a 0-100 scale;
        /// otherwise the percentage of <paramref name="maxBrightness"/>.
        /// </returns>
        public static uint ToRawBrightness(int dimLevelPercentage, uint maxBrightness)
        {
            var percentage = (uint)Math.Clamp(dimLevelPercentage, 0, (int)DimLevelPercentageMax);
            if (maxBrightness == 0 || maxBrightness == DimLevelPercentageMax) return percentage;

            return (uint)Math.Round(percentage * maxBrightness / (double)DimLevelPercentageMax, MidpointRounding.AwayFromZero);
        }
    }
}
