using System.Text;

namespace WindowMover;

internal static class AppLog
{
    private static readonly object SyncRoot = new();
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WindowMover");
    private static readonly string LogPath = Path.Combine(LogDirectory, "WindowMover.log");

    public static void Error(string message, Exception? exception = null)
    {
        try
        {
            var entry = new StringBuilder()
                .Append(DateTimeOffset.Now.ToString("O"))
                .Append(" ERROR ")
                .Append(message);

            if (exception is not null)
            {
                entry.AppendLine().Append(exception);
            }

            entry.AppendLine();

            lock (SyncRoot)
            {
                Directory.CreateDirectory(LogDirectory);
                File.AppendAllText(LogPath, entry.ToString(), Encoding.UTF8);
            }
        }
        catch
        {
            // Lokitus ei saa koskaan kaataa taustasovellusta.
        }
    }
}
