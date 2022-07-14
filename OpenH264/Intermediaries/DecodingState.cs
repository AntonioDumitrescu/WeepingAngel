namespace OpenH264.Intermediaries;

public enum DecodingState
{
    DsErrorFree = 0x00,
    DsFramePending = 0x01,
    DsRefLost = 0x02,
    DsBitstreamError = 0x04,
    DsDepLayerLost = 0x08,
    DsNoParamSets = 0x10,
    DsDataErrorConcealed = 0x20,
    DsRefListNullPointers = 0x40,
    DsInvalidArgument = 0x1000,
    DsInitialOptExpected = 0x2000,
    DsOutOfMemory = 0x4000,
    DsDstBufNeedExpan = 0x8000
}