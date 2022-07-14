using System.Runtime.InteropServices;

namespace OpenH264.Intermediaries;

//[StructLayout(LayoutKind.Explicit)]
public struct BufferInfo
{
    //[FieldOffset(0)]
    public bool BufferStatus;
    //[FieldOffset(1)]
    public ulong InBsTimestamp;
    //[FieldOffset(9)]
    public ulong OutYuvTimestamp;
    //[FieldOffset(17)]
    public SysMemBuffer MemBuffer;
    //[FieldOffset(17)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public IntPtr[] Destination;
}