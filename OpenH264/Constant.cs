namespace OpenH264;

internal static class Constant
{
    public const int MAX_TEMPORAL_LAYER_NUM = 4;
    public const int MAX_SPATIAL_LAYER_NUM = 4;
    public const int MAX_QUALITY_LAYER_NUM = 4;
    public const int MAX_LAYER_NUM_OF_FRAME = 128;
    public const int MAX_NAL_UNITS_IN_LAYER = 128;
    public const int MAX_RTP_PAYLOAD_LEN = 1000;
    public const int AVERAGE_RTP_PAYLOAD_LEN = 800;
    public const int SAVED_NALUNIT_NUM_TMP = 21;
    public const int MAX_SLICES_NUM_TMP = 35;
    public const int AUTO_REF_PIC_COUNT = -1;
}