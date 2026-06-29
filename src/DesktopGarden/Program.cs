using System.Threading;

namespace DesktopGarden;

internal static class Program
{
    private const string MutexName = "Local\\LovelyPlants.SingleInstance";

    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args.Length == 2 && string.Equals(args[0], "--render-preview", StringComparison.OrdinalIgnoreCase))
        {
            PreviewExporter.Export(args[1]);
            return;
        }
        if (args.Length == 2 && string.Equals(args[0], "--render-ui-preview", StringComparison.OrdinalIgnoreCase))
        {
            PreviewExporter.ExportUi(args[1]);
            return;
        }
        AppMode.QaWindow = args.Contains("--qa-window", StringComparer.OrdinalIgnoreCase);
#if DEBUG
        AppMode.QaWindow = true;
#endif

        using var mutex = new Mutex(true, MutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            NativeMethods.PostMessage(NativeMethods.HwndBroadcast, NativeMethods.ShowExistingMessage, IntPtr.Zero, IntPtr.Zero);
            return;
        }

        using (var splash = new SplashForm())
        {
            splash.ShowDialog();
        }

        Application.Run(new GardenApplicationContext());
    }
}

internal static class AppMode
{
    internal static bool QaWindow { get; set; }
}
