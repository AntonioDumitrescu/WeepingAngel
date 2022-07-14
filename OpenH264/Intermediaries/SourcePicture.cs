using System.Runtime.InteropServices;

namespace OpenH264.Intermediaries;

public struct SourcePicture
{
    public VideoFormatType ColorFormat;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public int[] Stride;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public IntPtr[] Data;
    public int PictureWidth;
    public int PictureHeight;
    public long TimeStamp;

    public static SourcePicture Create() => new()
    {
        Stride = new int[4],
        Data = new IntPtr[4]
    };
}