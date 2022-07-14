using System.Drawing.Imaging;
using OpenH264;
using OpenH264.Intermediaries;

namespace RemoteDesktop.Client;

internal class OpenH264Encoder : IEncoder
{
    public OpenH264Encoder(int bitRate, int maxBitRate, UsageType usageType, int idrInterval)
    {
        var encoder = new WelsSvcEncoder();

        var @params = new EncoderParamsExt
        {
            BaseParams = new EncoderParamsBase
            {
                PictureWidth = Screen.PrimaryScreen.Bounds.Width,
                PictureHeight = Screen.PrimaryScreen.Bounds.Height,
                Bitrate = bitRate,
                MaxFrameRate = 60,
                UsageType = UsageType.ScreenContentRealTime,
                RateControlMode = RateControlMode.Quality
            },
            MaxBitrate = maxBitRate,
            EnableSceneChangeDetect = true,
            EnableFrameSkip = true,
            bIsLosslessLink = true,
            SpatialLayers = new SpatialLayerConfig[4],
            MultipleThreadIdc = 0,
            EnableAdaptiveQuant = true
        };

        @params.SpatialLayers[0] = new SpatialLayerConfig
        {
            VideoWidth = @params.BaseParams.PictureWidth,
            VideoHeight = @params.BaseParams.PictureHeight,
            FrameRate = @params.BaseParams.MaxFrameRate,
            MaxSpatialBitrate = @params.MaxBitrate,
            SpatialBitrate = @params.BaseParams.Bitrate,
            SliceArgument = new SliceArgument
            {
                SliceMode = SliceMode.SM_FIXEDSLCNUM_SLICE,
            }
        };

        encoder.InitializeExt(ref @params);
        _encoder = new EncoderWrapper(encoder, @params);
    }

    private readonly EncoderWrapper _encoder;

    public bool Encode(Bitmap image, out byte[][] results)
    {
        var bmpData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly,
            PixelFormat.Format24bppRgb);

        var success = _encoder.EncodeRgb24(bmpData.Scan0, out var list);
        
        image.UnlockBits(bmpData);

        results = success ? list.ToArray() : Array.Empty<byte[]>();

        return success;
    }
}