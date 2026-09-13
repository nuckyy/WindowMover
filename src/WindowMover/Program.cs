using System.Diagnostics;

namespace WindowMover;

internal static class Program
{
    private const string MutexName = @"Local\WindowMover-1D387A30-E3B5-49CB-B4B6-5D3CF1F48E77";

    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, args) => AppLog.Error(Localization.Text("UnhandledUiError"), args.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            AppLog.Error(Localization.Text("UnhandledAppError"), args.ExceptionObject as Exception);

        try
        {
            Application.Run(new TrayApplicationContext());
        }
        catch (Exception exception)
        {
            AppLog.Error(Localization.Text("AppStartFailedLog"), exception);
            MessageBox.Show(
                Localization.Format("AppStartFailed", exception.Message),
                "WindowMover",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
