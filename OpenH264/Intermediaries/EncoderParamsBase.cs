namespace OpenH264.Intermediaries;

public struct EncoderParamsBase
{
    /// <summary>
    ///     Application type; please refer to <see cref="UsageType"/>
    /// </summary>
    public UsageType UsageType;

    /// <summary>
    ///     Width of picture in luminance samples (the maximum of all layers if multiple spatial layers presents)
    /// </summary>
    public int PictureWidth;

    /// <summary>
    ///     Height of picture in luminance samples (the maximum of all layers if multiple spatial layers presents)
    /// </summary>
    public int PictureHeight;
    /// <summary>
    ///     Target bitrate desired, in unit of bps
    /// </summary>
    public int Bitrate;

    public RateControlMode RateControlMode;
    public float MaxFrameRate;
}