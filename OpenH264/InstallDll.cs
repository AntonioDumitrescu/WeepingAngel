using OpenH264.Interop;
using OpenH264.Properties;

namespace OpenH264
{
    public static class InstallDll
    {
        public static void EnsureInstalled()
        {
            if (Environment.Is64BitProcess)
            {
                if (!File.Exists(CommonInterop.LibX64))
                {
                    File.WriteAllBytes(CommonInterop.LibX64, Resources.openh264_2_2_0_win64);
                }
            }
            else if (!File.Exists(CommonInterop.LibX86))
            {
                File.WriteAllBytes(CommonInterop.LibX86, Resources.openh264_2_2_0_win32);
            }
        }
    }
}
