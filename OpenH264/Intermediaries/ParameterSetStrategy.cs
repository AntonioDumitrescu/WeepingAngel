namespace OpenH264.Intermediaries;

public enum ParameterSetStrategy
{
    CONSTANT_ID = 0,
    INCREASING_ID = 1,
    SPS_LISTING = 2,
    SPS_LISTING_AND_PPS_INCREASING = 3,
    SPS_PPS_LISTING = 6,
}