using System.Runtime.InteropServices;

namespace OpenH264.Intermediaries;

public struct FrameInfo
{
    public int LayerNum;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
    public LayerInfo[] LayerInfo;
    public VideoFrameType FrameType;
    public int FrameSizeInBytes;
    public long TimeStamp;

    public static FrameInfo Create() => new()
    {
        LayerInfo = new LayerInfo[128]
    };
}