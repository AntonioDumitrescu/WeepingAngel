using System.Runtime.InteropServices;

namespace OpenH264.Intermediaries;

public struct SysMemBuffer
{
    public int Width; 
    public int Height;
    [MarshalAs(UnmanagedType.I4)]
    public VideoFormatType Format;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    public int[] Stride;
}