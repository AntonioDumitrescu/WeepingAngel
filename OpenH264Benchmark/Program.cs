using System.Diagnostics;
using System.Drawing.Imaging;
using OpenH264;
using OpenH264.Intermediaries;
using Yggdrasil.Utilities;

const int spatialLayerNum = 1;
const int frameRate = 60;
const int bitRate = 5_000_000;
const int idrInterval = 60;

var width = Screen.PrimaryScreen.Bounds.Width;
var height = Screen.PrimaryScreen.Bounds.Height;

var encParams = new EncoderParamsExt
{
    BaseParams = new EncoderParamsBase
    {
        PictureWidth = width,
        PictureHeight = height,
        Bitrate = bitRate,
        MaxFrameRate = frameRate,
        UsageType = UsageType.ScreenContentRealTime,
        RateControlMode = RateControlMode.Quality
    },
    EnableSceneChangeDetect = true,
    EnableFrameSkip = true,
    MaxNalSize = 1514,
    bIsLosslessLink = false,
    SpatialLayers = new SpatialLayerConfig[4],
    MultipleThreadIdc = 0,
    EnableAdaptiveQuant = true,
};

for (var i = 0; i < spatialLayerNum; i++)
{
    encParams.SpatialLayers[i] = new SpatialLayerConfig
    {
        VideoWidth = width >> (spatialLayerNum - 1 - i),
        VideoHeight = height >> (spatialLayerNum - 1 - i),
        FrameRate = frameRate,
        MaxSpatialBitrate = encParams.MaxBitrate,
        SpatialBitrate = encParams.MaxBitrate,
        SliceArgument = new SliceArgument
        {
            SliceMode = SliceMode.SM_SIZELIMITED_SLICE,
            SliceSizeConstraint = encParams.MaxNalSize
        }
    };
}

using var encoder = new EncoderWrapper(encParams);
using var decoder = new DecoderWrapper();

var sw = new Stopwatch();
var startTime = DateTime.Now;
var frames = 0;

var bytesTransferred = 0L;

var form = new Form();
form.Text = "Decoded image";
var pictureBox = new PictureBox();
pictureBox.Dock = DockStyle.Fill;
pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
form.Controls.Add(pictureBox);

var formThread = new Thread(() =>
{
    form.ShowDialog();
});
formThread.SetApartmentState(ApartmentState.STA);
formThread.Start();

while ((DateTime.Now - startTime).TotalSeconds < 5)
{
    using var bmp = ScreenShotBitBlt();
    var bmpData = bmp.LockBits(
        new Rectangle(0, 0, bmp.Width, bmp.Height),
        ImageLockMode.ReadOnly,
        PixelFormat.Format24bppRgb);

    sw.Restart();

    if (frames % idrInterval == 0)
    {
        encoder.Encoder.ForceIntraFrame(true);
    }

    encoder.EncodeRgb24(bmpData.Scan0, out var nals);
    bmp.UnlockBits(bmpData);
    sw.Stop();

    bytesTransferred += nals.Sum(x => x.Length);

    Console.WriteLine($"Frame {frames++}:");
    Console.WriteLine("  Encode");
    Console.WriteLine($"    {sw.ElapsedTicks / 10000f:F} milliseconds");
    Console.WriteLine("    NALs:");

    foreach (var nal in nals)
    {
        Console.WriteLine($"      {Suffix.Convert(nal.Length, 2)}");
    }

    Console.WriteLine("\r\n  Decode:");

    sw.Reset();
    foreach (var nal in nals)
    {
        unsafe
        {
            fixed (byte* nalPtr = &nal[0])
            {
                sw.Start();
                var output = decoder.DecodeToRgb(nalPtr, nal.Length, out var decodingState, out var w, out var h);
                sw.Stop();
                if (decodingState != DecodingState.DsErrorFree)
                {
                    Environment.FailFast($"Decoding failed: {decodingState}");
                }
                
                if(output.Length == 0) continue;

                var decodedBmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
                var decodedBmpData = decodedBmp.LockBits(new Rectangle(0, 0, decodedBmp.Width, decodedBmp.Height),
                    ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

                unsafe
                {
                    fixed (byte* rgbPtr = &output[0])
                    {
                        Buffer.MemoryCopy(rgbPtr, decodedBmpData.Scan0.ToPointer(), output.Length, output.Length);
                    }
                }

                decodedBmp.UnlockBits(decodedBmpData);

                form.Invoke(() =>
                {
                    pictureBox.Image?.Dispose();
                    pictureBox.Image = decodedBmp;
                });
            }
        }
       
    }

    Console.WriteLine($"    {sw.ElapsedTicks / 10000f:F} milliseconds");
 
   

    ++frames;
    
    Console.WriteLine("\r\n-----------\r\n");
}

Console.WriteLine($"\r\nData transferred in {(DateTime.Now - startTime).TotalSeconds:F} seconds: {Suffix.Convert(bytesTransferred, 2)}");

Bitmap ScreenShotBitBlt()
{
    var screenShot = new Bitmap(width, height, PixelFormat.Format24bppRgb);

    using var graphics = Graphics.FromImage(screenShot);

    graphics.CopyFromScreen(
        0,
        0,
        0,
        0,
        new Size(width, height),
        CopyPixelOperation.SourceCopy);

    return screenShot;
}