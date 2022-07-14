namespace OpenH264.Intermediaries;

public enum VideoFrameType
{
    videoFrameTypeInvalid,
    videoFrameTypeIDR,
    videoFrameTypeI,
    videoFrameTypeP,
    videoFrameTypeSkip,
    videoFrameTypeIPMixed,
}