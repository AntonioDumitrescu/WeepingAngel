using System.Runtime.InteropServices;

namespace OpenH264.Intermediaries;

public struct EncoderParamsExt
{
    public EncoderParamsBase BaseParams;
    public int TemploralLayerNumber;
    public int SpatialLayerNumber;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public SpatialLayerConfig[] SpatialLayers;
    public ComplexityMode ComplexityMode;
    public uint IntraPeriod;
    public int NumRefFrame;
    public ParameterSetStrategy SpsPpsIdStrategy;
    public bool PrefixNalAddingCtrl;
    public bool EnableSSEI;
    public bool SimulcastAVC;
    public int PaddingFlag;
    public int EntropyCodingModeFlag;
    public bool EnableFrameSkip;
    public int MaxBitrate;
    public int MaxQp;
    public int MinQp;
    public uint MaxNalSize;
    public bool EnableLongTermReference;
    public int LTRRefNum;
    public uint LtrMarkPeriod;
    public ushort MultipleThreadIdc;
    public bool UseLoadBalancing;
    public int LoopFilterDisableIdc;
    public int LoopFilterAlphaC0Offset;
    public int LoopFilterBetaOffset;
    public bool EnableDenoise;
    public bool EnableBackgroundDetection;
    public bool EnableAdaptiveQuant;
    public bool EnableFrameCroppingFlag;
    public bool EnableSceneChangeDetect;
    public bool bIsLosslessLink;

    public static EncoderParamsExt Create() => new EncoderParamsExt()
    {
        SpatialLayers = new SpatialLayerConfig[4]
    };
}