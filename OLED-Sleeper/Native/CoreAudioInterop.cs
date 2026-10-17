using System.Runtime.InteropServices;

namespace OLED_Sleeper.Native
{
    /// <summary>
    /// Hand-written COM declarations for the parts of the Windows Core Audio API that session metering needs.
    /// A [ComImport] declaration binds by slot position, so each interface lists its vtable in order up to the last method used here;
    /// unused methods before that one are kept as placeholders.
    /// Callers own the returned COM objects and release them with <see cref="Marshal.ReleaseComObject"/>.
    /// </summary>
    internal static class CoreAudioInterop
    {
        /// <summary>
        /// Selects the render (playback) data flow in <see cref="IMMDeviceEnumerator.EnumAudioEndpoints"/>.
        /// </summary>
        public const int EDataFlowRender = 0;

        /// <summary>
        /// Restricts <see cref="IMMDeviceEnumerator.EnumAudioEndpoints"/> to endpoints that are currently active.
        /// </summary>
        public const uint DEVICE_STATE_ACTIVE = 0x00000001;

        /// <summary>
        /// The activation context that allows any server type.
        /// </summary>
        public const uint CLSCTX_ALL = 0x17;

        /// <summary>
        /// The interface ID of <see cref="IAudioSessionManager2"/>, as <see cref="IMMDevice.Activate"/> needs it.
        /// </summary>
        public static readonly Guid IID_IAudioSessionManager2 = new("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F");

        /// <summary>
        /// The class that creates the device enumerator.
        /// </summary>
        [ComImport]
        [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        public class MMDeviceEnumeratorClass
        {
        }

        /// <summary>
        /// Enumerates the audio endpoint devices.
        /// </summary>
        [ComImport]
        [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IMMDeviceEnumerator
        {
            [PreserveSig]
            int EnumAudioEndpoints(int dataFlow, uint stateMask, out IMMDeviceCollection devices);
        }

        /// <summary>
        /// A collection of audio endpoint devices.
        /// </summary>
        [ComImport]
        [Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IMMDeviceCollection
        {
            [PreserveSig]
            int GetCount(out uint count);

            [PreserveSig]
            int Item(uint index, out IMMDevice device);
        }

        /// <summary>
        /// One audio endpoint device.
        /// </summary>
        [ComImport]
        [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IMMDevice
        {
            [PreserveSig]
            int Activate(ref Guid iid, uint clsCtx, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object interfacePointer);
        }

        /// <summary>
        /// Manages the audio sessions of one endpoint device.
        /// </summary>
        [ComImport]
        [Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IAudioSessionManager2
        {
            [PreserveSig]
            int GetAudioSessionControl(IntPtr audioSessionGuid, uint streamFlags, out IntPtr sessionControl);

            [PreserveSig]
            int GetSimpleAudioVolume(IntPtr audioSessionGuid, uint streamFlags, out IntPtr audioVolume);

            [PreserveSig]
            int GetSessionEnumerator(out IAudioSessionEnumerator sessionEnum);
        }

        /// <summary>
        /// Enumerates the audio sessions of an endpoint device.
        /// </summary>
        [ComImport]
        [Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IAudioSessionEnumerator
        {
            [PreserveSig]
            int GetCount(out int sessionCount);

            [PreserveSig]
            int GetSession(int sessionCount, out IAudioSessionControl2 session);
        }

        /// <summary>
        /// Controls and describes one audio session. The <c>IAudioSessionControl</c> slots come first.
        /// </summary>
        [ComImport]
        [Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IAudioSessionControl2
        {
            [PreserveSig]
            int GetState(out int state);

            [PreserveSig]
            int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string displayName);

            [PreserveSig]
            int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string value, ref Guid eventContext);

            [PreserveSig]
            int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string iconPath);

            [PreserveSig]
            int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string value, ref Guid eventContext);

            [PreserveSig]
            int GetGroupingParam(out Guid groupingParam);

            [PreserveSig]
            int SetGroupingParam(ref Guid overrideValue, ref Guid eventContext);

            [PreserveSig]
            int RegisterAudioSessionNotification(IntPtr newNotifications);

            [PreserveSig]
            int UnregisterAudioSessionNotification(IntPtr newNotifications);

            [PreserveSig]
            int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string retVal);

            [PreserveSig]
            int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string retVal);

            [PreserveSig]
            int GetProcessId(out uint retVal);
        }

        /// <summary>
        /// Reports the peak sample value of an audio session or endpoint, from 0.0 to 1.0.
        /// </summary>
        [ComImport]
        [Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IAudioMeterInformation
        {
            [PreserveSig]
            int GetPeakValue(out float peak);
        }
    }
}
