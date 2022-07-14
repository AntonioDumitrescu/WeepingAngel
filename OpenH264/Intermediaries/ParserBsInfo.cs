namespace OpenH264.Intermediaries;

public struct ParserBsInfo
{
    public int NalNum;
    public IntPtr NalLenInByte;
    public unsafe byte* DestinationBuffer; // output
    public int SpsWidthInPixel;
    public int SpsHeightInPixel;
    public ulong InBsTimeStamp;
    public ulong OutBsTimeStamp;

}