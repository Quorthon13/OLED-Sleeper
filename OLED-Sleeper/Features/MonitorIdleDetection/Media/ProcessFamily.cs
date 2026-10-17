using OLED_Sleeper.Native;
using System.Text;

namespace OLED_Sleeper.Features.MonitorIdleDetection.Media
{
    /// <summary>
    /// Maps a process to its family, the lowercased path of its executable, so a browser's audio child process matches the browser's window.
    /// </summary>
    internal static class ProcessFamily
    {
        /// <summary>
        /// Buffer size, in characters, for an image path.
        /// </summary>
        private const int PathBufferLength = 1024;

        /// <summary>
        /// Gets the family of a process.
        /// </summary>
        /// <param name="processId">The process identifier.</param>
        /// <returns>The lowercased executable path, or a <c>pid:</c> placeholder when the path cannot be read.</returns>
        public static string Of(uint processId)
        {
            nint handle = NativeMethods.OpenProcess(NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
            if (handle == nint.Zero) return $"pid:{processId}";

            try
            {
                var buffer = new StringBuilder(PathBufferLength);
                uint size = (uint)buffer.Capacity;
                return NativeMethods.QueryFullProcessImageName(handle, 0, buffer, ref size)
                    ? buffer.ToString().ToLowerInvariant()
                    : $"pid:{processId}";
            }
            finally
            {
                NativeMethods.CloseHandle(handle);
            }
        }
    }
}
