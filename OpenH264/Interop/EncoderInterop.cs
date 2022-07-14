using System.Runtime.InteropServices;

namespace OpenH264.Interop;

internal static class EncoderInterop
{
    [DllImport(CommonInterop.LibX86, EntryPoint = "WelsCreateSVCEncoder", CallingConvention = CallingConvention.Cdecl)]
    private static extern int WelsCreateSVCEncoder_X86(ref IntPtr ppEncoder);

    [DllImport(CommonInterop.LibX86, EntryPoint = "WelsDestroySVCEncoder", CallingConvention = CallingConvention.Cdecl)]
    private static extern void WelsDestroySVCEncoder_X86(IntPtr ppEncoder);


    [DllImport(CommonInterop.LibX64, EntryPoint = "WelsCreateSVCEncoder", CallingConvention = CallingConvention.Cdecl)]
    private static extern int WelsCreateSVCEncoder_X64(ref IntPtr ppEncoder);

    [DllImport(CommonInterop.LibX64, EntryPoint = "WelsDestroySVCEncoder", CallingConvention = CallingConvention.Cdecl)]
    private static extern void WelsDestroySVCEncoder_X64(IntPtr ppEncoder);

    public static IntPtr WelsCreateSVCEncoder()
    {
        var zero = IntPtr.Zero;

        if (Environment.Is64BitProcess)
        {
            return WelsCreateSVCEncoder_X64(ref zero) == 0 ? zero : throw new InvalidOperationException("Failed to create encoder");
        }

        return WelsCreateSVCEncoder_X86(ref zero) == 0 ? zero : throw new InvalidOperationException("Failed to create encoder");
    }

    public static void WelsDestroySVCEncoder(IntPtr ptr)
    {
        if (Environment.Is64BitProcess)
        {
            WelsDestroySVCEncoder_X64(ptr);
        }
        else
        {
            WelsDestroySVCEncoder_X86(ptr);
        }
    }
}