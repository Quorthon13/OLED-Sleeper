namespace OLED_Sleeper.Infrastructure.Helpers
{
    /// <summary>
    /// Reports which distribution this build was published for.
    /// </summary>
    public static class BuildMode
    {
        /// <summary>
        /// Whether this build keeps its settings, state and logs beside the executable rather than under the
        /// user's application data directory. Publishing with <c>-p:Portable=true</c> sets it.
        /// </summary>
#if PORTABLE
        public const bool IsPortable = true;
#else
        public const bool IsPortable = false;
#endif
    }
}
