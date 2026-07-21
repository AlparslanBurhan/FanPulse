using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace FanPulse.App.Common;

/// <summary>Windows 10/11 koyu başlık çubuğu (DWM immersive dark mode).</summary>
public static class DarkTitleBar
{
    private const int DwmwaUseImmersiveDarkMode = 20;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    public static void Apply(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd != IntPtr.Zero)
        {
            Set(hwnd);
            return;
        }

        window.SourceInitialized += (_, _) =>
            Set(new WindowInteropHelper(window).Handle);
    }

    private static void Set(IntPtr hwnd)
    {
        var enabled = 1;
        _ = DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref enabled, sizeof(int));
    }
}
