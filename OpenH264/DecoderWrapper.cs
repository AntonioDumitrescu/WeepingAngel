using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using OpenH264.Intermediaries;

namespace OpenH264;

public class DecoderWrapper : IDisposable
{
    private readonly DecodingParameters _params;

    public WelsDecoder Decoder { get; }

    public DecoderWrapper(DecodingParameters @params)
    {
        InstallDll.EnsureInstalled();

        _params = @params;
        Decoder = new WelsDecoder();
        Decoder.Initialize(ref @params);
    }

    public DecoderWrapper() : this(new DecodingParameters
    {
        ActiveIdc = ErrorConIdc.ERROR_CON_SLICE_COPY,
        VideoProperty = new VideoProperty
        {
            BitstreamType = VideoBitstreamType.VIDEO_BITSTREAM_AVC
        },
        CpuLoad = 100
    }) { }

    public unsafe BufferInfo DecodeToYuv(byte* source, int length, out DecodingState result)
    {
        var bufferInfo = new BufferInfo();
        var dstPp = new byte[4];
            
        fixed (byte* outputPtr = &dstPp[0])
        {
            result = Decoder.DecodeFrame2(new IntPtr(source), length, new IntPtr(outputPtr), ref bufferInfo);
        }

        return bufferInfo;
    }

    public unsafe IMemoryOwner<byte>? DecodeToRgb(
        byte* source, 
        int length, 
        out DecodingState result,
        out bool success,
        out int width, 
        out int height)
    {
        success = false;

        var bufferInfo = DecodeToYuv(source, length, out result);
        width = bufferInfo.MemBuffer.Width;
        height = bufferInfo.MemBuffer.Height;

        if (result != DecodingState.DsErrorFree)
        {
            return default;
        }

        var yPlane = (byte*)(bufferInfo.Destination[0].ToPointer());
          
        var yS = bufferInfo.MemBuffer.Stride[0];

        var uPlane = (byte*)(bufferInfo.Destination[1].ToPointer());
        var vPlane = (byte*)(bufferInfo.Destination[2].ToPointer());

        if (width == 0 || height == 0)
        {
            return default;
        }

        var requiredSize = width * height * 3;

        Trace.Assert(requiredSize > 0);

        var rgb = MemoryPool<byte>.Shared.Rent(requiredSize);
        fixed (byte* ptr = &rgb.Memory.Span[0])
        {
            Yuv420PtoRgb(yPlane, uPlane, vPlane, width, height, yS, ptr);
        }

        success = true;
        return rgb;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private byte Clamp(int value)
    {
        return (byte)(value < 0 ? 0 : (value > 255 ? 255 : value));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
    private unsafe void Yuv420PtoRgb(
        byte* yPlane,
        byte* uPlane,
        byte* vPlane,
        int width,
        int height,
        int stride,
        byte* result)
    {
        if (width < 1) throw new ArgumentException(nameof(width));
        if (height < 1) throw new ArgumentException(nameof(height));
        if (stride < 1) throw new ArgumentException(nameof(stride));

        var rgb = result;

        for (var y = 0; y < height; y++)
        {
            var rowIdx = (stride * y);
            var uvpIdx = (stride / 2) * (y / 2);

            var pYp = yPlane + rowIdx;
            var pUp = uPlane + uvpIdx;
            var pVp = vPlane + uvpIdx;

            for (var x = 0; x < width; x += 2)
            {
                var d = *pUp - 128;
                var e = *pVp - 128;

                var c1X298 = 298 * (pYp[0] - 16);
                var c2X298 = 298 * (pYp[1] - 16);

                rgb[0] = Clamp((c1X298 + (409 * e) + 128) >> 8);
                rgb[1] = Clamp((c1X298 - (100 * d) - (208 * e) + 128) >> 8);
                rgb[2] = Clamp((c1X298 + (516 * d) + 128) >> 8);
                             
                rgb[3] = Clamp((c2X298 + (409 * e) + 128) >> 8);
                rgb[4] = Clamp((c2X298 - (100 * d) - (208 * e) + 128) >> 8);
                rgb[5] = Clamp((c2X298 + (516 * d) + 128) >> 8);

                rgb += 6;
                pYp += 2;

                ++pUp;
                ++pVp;
            }
        }
    }



    public void Dispose()
    {
        Decoder.Dispose();
    }
}