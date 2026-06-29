using System.Runtime.InteropServices;

namespace DesktopGarden;

internal static class NativeMethods
{
    internal static readonly IntPtr HwndBroadcast = new(0xffff);
    internal static readonly int ShowExistingMessage = RegisterWindowMessage("LovelyPlants.ShowExisting");

    internal const int WsExLayered = 0x00080000;
    internal const int WsExToolWindow = 0x00000080;
    internal const int WsExNoActivate = 0x08000000;
    internal const int WsExTransparent = 0x00000020;
    internal const int WmNcHitTest = 0x0084;
    internal const int WmHotKey = 0x0312;
    internal const int HtClient = 1;
    internal const int HtTransparent = -1;
    internal const int UlwAlpha = 0x00000002;
    internal const int SrcOver = 0x00;
    internal const int AcSrcAlpha = 0x01;

    [StructLayout(LayoutKind.Sequential)]
    internal struct PointNative
    {
        public int X;
        public int Y;
        public PointNative(int x, int y) { X = x; Y = y; }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SizeNative
    {
        public int Width;
        public int Height;
        public SizeNative(int width, int height) { Width = width; Height = height; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct BlendFunction
    {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public byte AlphaFormat;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref PointNative destination, ref SizeNative size, IntPtr hdcSrc, ref PointNative source, int colorKey, ref BlendFunction blend, int flags);

    [DllImport("user32.dll")]
    internal static extern IntPtr GetDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    internal static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnregisterHotKey(IntPtr hwnd, int id);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    internal static extern int RegisterWindowMessage(string message);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool PostMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam);
}
