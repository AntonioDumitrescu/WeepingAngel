using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Text;


var startTime = DateTime.Now;
using var sourceImage = ScreenShotBitBlt();
using var targetImage = new Bitmap(sourceImage.Width / 2, sourceImage.Height / 2, PixelFormat.Format24bppRgb);
Console.WriteLine($"Scaling {sourceImage.Width}x{sourceImage.Height} to {targetImage.Width}x{targetImage.Height}");


/*
using var sourceImage = new Bitmap(sourceResolution.X, sourceResolution.Y, PixelFormat.Format24bppRgb);
var sourceImageData = sourceImage.LockBits(new Rectangle(0, 0, sourceImage.Width, sourceImage.Height),
    ImageLockMode.WriteOnly, sourceImage.PixelFormat);

unsafe
{   var ptr = (byte*)sourceImageData.Scan0.ToPointer();

    var rng = new Random();

    for (var i = 0; i < sourceImage.Width * sourceImage.Height * 3; i += 3)
    {
        var randomNumber = rng.Next(0, int.MaxValue);

        ptr[i] = (byte)(randomNumber & 0xFF);
        ptr[i + 1] = (byte)(randomNumber << 8 & 0xFF);
        ptr[i + 2] = (byte)(randomNumber << 16 & 0xFF);
    }
}

sourceImage.UnlockBits(sourceImageData);
*/

Bitmap ScreenShotBitBlt()
{
    var width = Screen.PrimaryScreen.WorkingArea.Width;
    var height = Screen.PrimaryScreen.WorkingArea.Height;
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


var tests = 0;
var results = new List<(int number, float milliseconds, CompositingQuality cc, InterpolationMode im, SmoothingMode sm)>();
const int iterations = 30;

foreach (var compQuality in Enum.GetValues<CompositingQuality>())
{
    foreach (var interpolationMode in Enum.GetValues<InterpolationMode>())
    {
        foreach (var smoothingMode in Enum.GetValues<SmoothingMode>())
        {
            var testNumber = tests++;
            Console.WriteLine($"Test {testNumber}\r\n" +
                              $"  Compositing quality: {compQuality}\r\n" +
                              $"  Interpolation mode : {interpolationMode}\r\n" +
                              $"  Smoothing mode     : {smoothingMode}\r\n\r\n");

            
            using var graphics = Graphics.FromImage(targetImage);
            try
            {
                graphics.CompositingQuality = compQuality;
                graphics.InterpolationMode = interpolationMode;
                graphics.SmoothingMode = smoothingMode;
            }
            catch (Exception e)
            {
                Console.WriteLine("test failed. skipping!");
                goto finish;
            }
           

            var sw = new Stopwatch();
            var millisecondsTotal = 0f;
            var il = new ImageList();
            //il.Draw();
            for (var i = 0; i < iterations; i++)
            {
                sw.Restart();
                graphics.DrawImage(
                    sourceImage,
                    new Rectangle(0, 0, targetImage.Width, targetImage.Height),
                    new Rectangle(0, 0, sourceImage.Width, sourceImage.Height),
                    GraphicsUnit.Pixel);
                sw.Stop();

                millisecondsTotal += sw.ElapsedTicks / 10000f;
            }

            Console.WriteLine($"total: {millisecondsTotal} ms, avg iteration: {millisecondsTotal / iterations:F}");

            results.Add((testNumber, millisecondsTotal, compQuality, interpolationMode, smoothingMode));

            Console.WriteLine("Streaming comparision to disk...");

            using (var destinationBmp = new Bitmap(sourceImage.Width + targetImage.Width, sourceImage.Height,
                       PixelFormat.Format24bppRgb))
            {
                using var destinationGraphics = Graphics.FromImage(destinationBmp);

                destinationGraphics.DrawImageUnscaled(sourceImage, new Point(0, 0));
                destinationGraphics.DrawImageUnscaled(targetImage, new Point(sourceImage.Width, 0));

                destinationBmp.Save($"{testNumber}.png", ImageFormat.Png);
            }

            
            finish:
            Console.WriteLine("\r\n------------------------\r\n\r\n");
        }
    }
}

Console.WriteLine("\r\nTests complete. Sorting numerical results!");

var sorted = results.OrderByDescending(x=>x.milliseconds).ToList();

Console.WriteLine($"Total results: {sorted.Count}");
Console.WriteLine("Creating report.");

var sb = new StringBuilder();

foreach (var (number, milliseconds, cc, im, sm) in sorted)
{
    sb.AppendLine($"Test {number} time: {milliseconds:F} ms for {iterations} ({milliseconds / iterations:F} ms avg/iteration)");
    sb.AppendLine($"    Compositing quality: {cc}");
    sb.AppendLine($"    Interpolation mode : {im}");
    sb.AppendLine($"    Smoothing mode     : {sm}");
    sb.AppendLine();
}

var report = sb.ToString();
Console.WriteLine("Writing to report.txt");
File.WriteAllText("report.txt", report);
Console.WriteLine($"Finished in {(DateTime.Now - startTime).TotalSeconds:F} seconds.");