namespace OpenH264.Intermediaries;

public enum VideoFormatType
{
    videoFormatVFlip = -2147483648, // 0x80000000
    videoFormatRGB = 1,
    videoFormatRGBA = 2,
    videoFormatRGB555 = 3,
    videoFormatRGB565 = 4,
    videoFormatBGR = 5,
    videoFormatBGRA = 6,
    videoFormatABGR = 7,
    videoFormatARGB = 8,
    videoFormatYUY2 = 20, // 0x00000014
    videoFormatYVYU = 21, // 0x00000015
    videoFormatUYVY = 22, // 0x00000016
    videoFormatI420 = 23, // 0x00000017
    videoFormatYV12 = 24, // 0x00000018
    videoFormatInternal = 25, // 0x00000019
    videoFormatNV12 = 26, // 0x0000001A
}