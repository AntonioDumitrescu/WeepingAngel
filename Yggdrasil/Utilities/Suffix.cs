namespace Yggdrasil.Utilities;

public static class Suffix
{
    private static readonly string[] SizeSuffixes =
    {
        "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"
    };

    public static string Convert(long value, int decimalPlaces = 1)
    {
        if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
        if (value < 0) { return "-" + Convert(-value); }
        if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

        var mag = (int)Math.Log(value, 1024);
        var adjustedSize = (decimal)value / (1L << mag * 10);

        if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
        {
            mag += 1;
            adjustedSize /= 1024;
        }

        return string.Format("{0:n" + decimalPlaces + "} {1}",
            adjustedSize,
            SizeSuffixes[mag]);
    }
}