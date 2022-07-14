namespace OpenH264.Intermediaries;

public struct SpatialLayerConfig
{
    public int VideoWidth;
    public int VideoHeight;
    public float FrameRate;
    public int SpatialBitrate;
    public int MaxSpatialBitrate;
    public AvcProfile Profile;
    public AvcLevel LevelIdc;
    public int DLayerQp;
    public SliceArgument SliceArgument;
    public bool VideoSignalTypePresent;
    public byte VideoFormat;
    public bool FullRange;
    public bool ColorDescriptionPresent;
    public ColorPrimaries ColorPrimaries;
    public TransferCharacteristics TransferCharacteristics;
    public ColorMatrix ColorMatrix;
    public bool AspectRatioPresent;
    public SampleAspectRatio AspectRatio;
    public ushort AspectRatioExtWidth;
    public ushort AspectRatioExtHeight;
}