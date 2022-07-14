using System.Runtime.InteropServices;

namespace OpenH264.Intermediaries;

public struct SliceArgument
{
    public SliceMode SliceMode;
    public uint SliceNum;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 35)]
    public uint[] SliceMbNum;
    public uint SliceSizeConstraint;
}