namespace OpenH264.Intermediaries;

public enum RateControlMode
{
    Off = -1,
    Quality = 0,
    Bitrate = 1,
    BufferBased = 2,
    TimeStamp = 3,
    BitrateSkip = 4
}