using System.Runtime.InteropServices;

namespace OpenH264.Interop;

internal static class DecoderInterop
{

    [DllImport(CommonInterop.LibX86, EntryPoint = "WelsCreateDecoder", CallingConvention = CallingConvention.Cdecl)]
    private static extern int WelsCreateDecoder_X86(ref IntPtr ppEncoder);

    [DllImport(CommonInterop.LibX86, EntryPoint = "WelsDestroyDecoder", CallingConvention = CallingConvention.Cdecl)]
    private static extern void WelsDestroyDecoder_X86(IntPtr ppEncoder);


    [DllImport(CommonInterop.LibX64, EntryPoint = "WelsCreateDecoder", CallingConvention = CallingConvention.Cdecl)]
    private static extern int WelsCreateDecoder_X64(ref IntPtr ppEncoder);

    [DllImport(CommonInterop.LibX64, EntryPoint = "WelsDestroyDecoder", CallingConvention = CallingConvention.Cdecl)]
    private static extern void WelsDestroyDecoder_X64(IntPtr ppEncoder);


    public static IntPtr WelsCreateDecoder()
    {
        var zero = IntPtr.Zero;

        if (Environment.Is64BitProcess)
        {
            return WelsCreateDecoder_X64(ref zero) == 0 ? zero : throw new InvalidOperationException("Failed to create encoder");
        }

        return WelsCreateDecoder_X86(ref zero) == 0 ? zero : throw new InvalidOperationException("Failed to create encoder");
    }

    public static void WelsDestroyDecoder(IntPtr ptr)
    {
        if (Environment.Is64BitProcess)
        {
            WelsDestroyDecoder_X64(ptr);
        }
        else
        {
            WelsDestroyDecoder_X86(ptr);
        }
    }
}