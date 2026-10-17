namespace OLED_Sleeper.Features.MonitorIdleDetection.Media.Helpers
{
    /// <summary>
    /// Compares two small captures of a screen. Pure, so it is testable without capturing anything.
    /// </summary>
    public static class FrameDifference
    {
        /// <summary>
        /// The default amount, on a 0-255 luma scale, by which a cell must change to count as changed.
        /// It ignores dithering and compression noise.
        /// </summary>
        public const int DefaultLumaThreshold = 10;

        private const int BytesPerPixel = 4;

        /// <summary>
        /// Gets the fraction of cells whose brightness changed by more than a threshold.
        /// </summary>
        /// <param name="previousBgra">The earlier frame, as 32-bit BGRA pixels.</param>
        /// <param name="currentBgra">The later frame, in the same layout and size.</param>
        /// <param name="lumaThreshold">The luma change a cell needs to count as changed.</param>
        /// <returns>A value from 0 to 1; 0 when the frames are empty or differ in size.</returns>
        public static double ChangedFraction(ReadOnlySpan<byte> previousBgra, ReadOnlySpan<byte> currentBgra, int lumaThreshold = DefaultLumaThreshold)
        {
            if (previousBgra.Length != currentBgra.Length || currentBgra.Length < BytesPerPixel) return 0;

            int cells = currentBgra.Length / BytesPerPixel;
            int changed = 0;
            for (int i = 0; i < cells; i++)
            {
                int offset = i * BytesPerPixel;
                if (Math.Abs(Luma(previousBgra, offset) - Luma(currentBgra, offset)) > lumaThreshold)
                {
                    changed++;
                }
            }

            return (double)changed / cells;
        }

        /// <summary>
        /// Approximates a pixel's brightness with integer weights for B, G and R that sum to 256.
        /// </summary>
        private static int Luma(ReadOnlySpan<byte> bgra, int offset)
        {
            return (29 * bgra[offset] + 150 * bgra[offset + 1] + 77 * bgra[offset + 2]) >> 8;
        }
    }
}
