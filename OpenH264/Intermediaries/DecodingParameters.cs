namespace OpenH264.Intermediaries;

public struct DecodingParameters
{
    public string FileNameRestructured;
    public uint CpuLoad;
    public byte TargetDqLayer;
    public ErrorConIdc ActiveIdc;
    public bool ParseOnly;
    public VideoProperty VideoProperty;
}