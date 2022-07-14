using System.Runtime.InteropServices;

namespace OpenH264.Interop
{
    internal static class CommonInterop
    {
        public const string LibX86 = "openh264-2.2.0-win32.dll";
        public const string LibX64 = "openh264-2.2.0-win64.dll";

        public static OpenH264Version WelsGetCodecVersion() 
            => Environment.Is64BitProcess ? WelsGetCodecVersion_X64() : WelsGetCodecVersion_X86();

        [DllImport(LibX86, EntryPoint = "WelsGetCodecVersion", CallingConvention = CallingConvention.Cdecl)]
        private static extern OpenH264Version WelsGetCodecVersion_X86();


        [DllImport(LibX64, EntryPoint = "WelsGetCodecVersion", CallingConvention = CallingConvention.Cdecl)]
        private static extern OpenH264Version WelsGetCodecVersion_X64();
    }
}
