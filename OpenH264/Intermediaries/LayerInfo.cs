namespace OpenH264.Intermediaries;

public struct LayerInfo
{
    public byte TemporalId;
    public byte SpatialId;
    public byte QualityId;
    public VideoFrameType FrameType;
    public byte LayerType;
    public int SubSeqId;
    public int NalCount;
    public IntPtr NalLengthInByte;
    public IntPtr BsBuf;
}