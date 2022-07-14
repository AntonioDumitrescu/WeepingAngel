using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenH264.Intermediaries;

namespace OpenH264;

public class EncoderWrapper : IDisposable
{
    private readonly EncoderParamsExt _params;

    public WelsSvcEncoder Encoder { get; }

    public int YuvBufferSize => _params.BaseParams.PictureWidth * _params.BaseParams.PictureHeight * 3 / 2;

    public EncoderWrapper(WelsSvcEncoder encoder, EncoderParamsExt @params)
    {
        Encoder = encoder;
        _params = @params;
    }

    public EncoderWrapper(EncoderParamsExt @params)
    {
        InstallDll.EnsureInstalled();

        _params = @params;
        Encoder = new WelsSvcEncoder();
        Encoder.InitializeExt(ref _params);
    }

    public EncoderWrapper(
        int width,
        int height,
        int bitRate,
        int frameRate,
        UsageType usage,
        RateControlMode rateControlMode) : 
        this(
            new EncoderParamsExt
            {
                BaseParams = new EncoderParamsBase
                {
                    PictureWidth = width,
                    PictureHeight = height,
                    Bitrate = bitRate,
                    MaxFrameRate = frameRate,
                    UsageType = usage,
                    RateControlMode = rateControlMode
                },
                EnableSceneChangeDetect = true
            }) { }

    public unsafe bool Encode(byte* yuv420, out List<byte[]> nals)
    {
        var width = _params.BaseParams.PictureWidth;
        var height = _params.BaseParams.PictureHeight;

        var picture = SourcePicture.Create();
        picture.PictureWidth = width;
        picture.PictureHeight = height;
        picture.ColorFormat = VideoFormatType.videoFormatI420;
        picture.Stride[0] = width;
        picture.Stride[1] = picture.Stride[2] = width >> 1;

        picture.Data[0] = new IntPtr(yuv420);
        picture.Data[1] = new IntPtr(yuv420 + width * height);
        picture.Data[2] = new IntPtr(yuv420 + width * height + (width * height >> 2));

        var frameInfo = FrameInfo.Create();
        
        var success = Encoder.EncodeFrame(ref picture, ref frameInfo);

        nals = new List<byte[]>();

        if (!success)
        {
            return false;
        }

        for (var layerIdx = 0; layerIdx < frameInfo.LayerNum; layerIdx++)
        {
            var layerInfo = frameInfo.LayerInfo[layerIdx];

            var index = 0;

            for (var nalIdx = 0; nalIdx < layerInfo.NalCount; nalIdx++)
            {
                var nalSize = ((int*)layerInfo.NalLengthInByte.ToPointer())[nalIdx];

                var buffer = new byte[nalSize];

                fixed (byte* dstPtr = &buffer[0])
                {
                    Buffer.MemoryCopy((byte*)layerInfo.BsBuf.ToPointer() + index, dstPtr, buffer.Length, buffer.Length);
                }

                nals.Add(buffer);

                index += nalSize;
            }
        }

        return true;
    }

    public unsafe bool Encode(ReadOnlySpan<byte> yuv420, out List<byte[]> nals)
    {
        fixed (byte* ptr = &yuv420[0])
        {
            return Encode(ptr, out nals);
        }
    }

    public unsafe bool EncodeRgb24(byte* rgb24, out List<byte[]> data)
    {
        var yuvBuffer = Marshal.AllocHGlobal(YuvBufferSize);
        var yuvPtr = (byte*)yuvBuffer.ToPointer();
        try
        {
            Rgb24ToYuv420P(yuvPtr, rgb24, _params.BaseParams.PictureWidth, _params.BaseParams.PictureHeight);
            return Encode(yuvPtr, out data);
        }
        finally
        {
            Marshal.FreeHGlobal(yuvBuffer);
        }
    }

    public unsafe bool EncodeRgb24(ReadOnlySpan<byte> rgb24, out List<byte[]> data)
    {
        fixed (byte* ptr = &rgb24[0])
        {
            return EncodeRgb24(ptr, out data);
        }
    }

    public unsafe bool EncodeRgb24(IntPtr scan0, out List<byte[]> data)
    {
        return EncodeRgb24((byte*)scan0.ToPointer(), out data);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private unsafe void Rgb24ToYuv420P(byte* destination, byte* rgb, int width, int height)
    {
        var imageSize = width * height;
        var uPos = imageSize;
        var vPos = uPos + uPos / 4;
        var i = 0;
        for (var line = 0; line < height; ++line)
        {
            if (line % 2 == 0)
            {
                for (var x = 0; x < width; x += 2)
                {
                    var r = rgb[3 * i];
                    var g = rgb[3 * i + 1];
                    var b = rgb[3 * i + 2];

                    destination[i++] = (byte)(((66 * r + 129 * g + 25 * b) >> 8) + 16);
                    destination[uPos++] = (byte)(((-38 * r + -74 * g + 112 * b) >> 8) + 128);
                    destination[vPos++] = (byte)(((112 * r + -94 * g + -18 * b) >> 8) + 128);

                    r = rgb[3 * i];
                    g = rgb[3 * i + 1];
                    b = rgb[3 * i + 2];

                    destination[i++] = (byte)(((66 * r + 129 * g + 25 * b) >> 8) + 16);
                }
            }

            else
            {
                for (var x = 0; x < width; x += 1)
                {
                    var r = rgb[3 * i];
                    var g = rgb[3 * i + 1];
                    var b = rgb[3 * i + 2];
                    destination[i++] = (byte)(((66 * r + 129 * g + 25 * b) >> 8) + 16);
                }
            }
        }
    }

    public void Dispose()
    {
        Encoder.Dispose();
    }
}