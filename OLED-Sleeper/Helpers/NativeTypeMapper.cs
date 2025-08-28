using OLED_Sleeper.Native;
using System.Windows;

namespace OLED_Sleeper.Helpers
{
    /// <summary>
    /// Provides simple conversion methods for native Windows types.
    /// </summary>
    internal static class NativeTypeMapper
    {
        /// <summary>
        /// Converts a native Rect (Left, Top, Right, Bottom) to a WPF Rect (X, Y, Width, Height).
        /// </summary>
        public static Rect ToWindowsRect(this NativeMethods.Rect nativeRect)
        {
            return new Rect(
                nativeRect.left,
                nativeRect.top,
                nativeRect.right - nativeRect.left,
                nativeRect.bottom - nativeRect.top
            );
        }
    }
}