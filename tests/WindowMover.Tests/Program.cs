using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Text.Json;
using WindowMover;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Contains("--test-window", StringComparer.Ordinal))
        {
            return RunTestWindow();
        }

        var tests = new List<(string Name, Action Execute)>
        {
            ("Sama resoluutio säilyttää sijainnin ja koon", SameResolutionPreservesGeometry),
            ("Negatiiviset näyttökoordinaatit toimivat", NegativeCoordinatesAreSupported),
            ("Eri resoluutio skaalaa suhteellisesti", DifferentResolutionScalesProportionally),
            ("Ruudun ulkopuolinen ikkuna rajataan työalueelle", OffscreenWindowIsClamped),
            ("Virheellinen lähdealue keskittää ikkunan", InvalidSourceCentersWindow),
            ("Virheellinen kohdealue hylätään", InvalidDestinationIsRejected),
            ("Suomenkielinen resurssi latautuu", FinnishResourceLoads),
            ("Tuntematon kieli käyttää englantia", UnsupportedCultureUsesEnglishFallback),
            ("Kieliasetus normalisoidaan", LanguageSettingIsNormalized),
            ("Kieli voidaan vaihtaa ajon aikana", LanguageCanChangeAtRuntime),
            ("Vanha asetustiedosto käyttää järjestelmän kieltä", LegacySettingsUseSystemLanguage)
        };

        if (args.Contains("--integration", StringComparer.Ordinal))
        {
            tests.Add(("Oikea ikkuna siirtyy toiselle näytölle", RealWindowMovesBetweenMonitors));
        }

        var failed = 0;
        foreach (var test in tests)
        {
            try
            {
                test.Execute();
                Console.WriteLine($"PASS  {test.Name}");
            }
            catch (Exception exception)
            {
                failed++;
                Console.Error.WriteLine($"FAIL  {test.Name}\n      {exception.Message}");
            }
        }

        Console.WriteLine($"\n{tests.Count - failed}/{tests.Count} testiä läpäistiin.");
        return failed == 0 ? 0 : 1;
    }

    private static int RunTestWindow()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        var primaryArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
        using var form = new Form
        {
            Text = "WindowMover integraatiotesti",
            StartPosition = FormStartPosition.Manual,
            Location = new System.Drawing.Point(primaryArea.Left + 100, primaryArea.Top + 100),
            Size = new Size(800, 600),
            ShowInTaskbar = true
        };
        form.Shown += (_, _) => form.Activate();
        Application.Run(form);
        return 0;
    }

    private static void SameResolutionPreservesGeometry()
    {
        var actual = GeometryMapper.MapToMonitor(
            new Rectangle(100, 100, 1000, 800),
            new Rectangle(0, 0, 1920, 1040),
            new Rectangle(1920, 0, 1920, 1040));
        Equal(new Rectangle(2020, 100, 1000, 800), actual);
    }

    private static void NegativeCoordinatesAreSupported()
    {
        var actual = GeometryMapper.MapToMonitor(
            new Rectangle(100, 100, 1000, 800),
            new Rectangle(0, 0, 1920, 1040),
            new Rectangle(-1920, 0, 1920, 1040));
        Equal(new Rectangle(-1820, 100, 1000, 800), actual);
    }

    private static void DifferentResolutionScalesProportionally()
    {
        var actual = GeometryMapper.MapToMonitor(
            new Rectangle(384, 208, 1920, 1040),
            new Rectangle(0, 0, 3840, 2080),
            new Rectangle(1920, 0, 1920, 1040));
        Equal(new Rectangle(2112, 104, 960, 520), actual);
    }

    private static void OffscreenWindowIsClamped()
    {
        var destination = new Rectangle(-1280, -200, 1280, 720);
        var actual = GeometryMapper.MapToMonitor(
            new Rectangle(1800, 900, 600, 500),
            new Rectangle(0, 0, 1920, 1040),
            destination);

        True(destination.Contains(actual), $"{actual} ei mahdu alueelle {destination}.");
    }

    private static void InvalidSourceCentersWindow()
    {
        var actual = GeometryMapper.MapToMonitor(
            new Rectangle(0, 0, 640, 480),
            Rectangle.Empty,
            new Rectangle(-1920, 0, 1920, 1040));
        Equal(new Rectangle(-1280, 280, 640, 480), actual);
    }

    private static void InvalidDestinationIsRejected()
    {
        try
        {
            GeometryMapper.MapToMonitor(
                new Rectangle(0, 0, 500, 500),
                new Rectangle(0, 0, 1000, 1000),
                Rectangle.Empty);
        }
        catch (ArgumentOutOfRangeException)
        {
            return;
        }

        throw new InvalidOperationException("Tyhjä kohdealue hyväksyttiin odottamatta.");
    }

    private static void FinnishResourceLoads()
    {
        Equal("Asetukset…", Localization.Text("MenuSettings", CultureInfo.GetCultureInfo("fi-FI")));
    }

    private static void UnsupportedCultureUsesEnglishFallback()
    {
        Equal("Settings…", Localization.Text("MenuSettings", CultureInfo.GetCultureInfo("de-DE")));
    }

    private static void LanguageSettingIsNormalized()
    {
        Equal(Localization.FinnishLanguage, Localization.NormalizeLanguage(" FI "));
        Equal(Localization.SystemLanguage, Localization.NormalizeLanguage("de-DE"));
        Equal(Localization.SystemLanguage, Localization.NormalizeLanguage(null));
    }

    private static void LanguageCanChangeAtRuntime()
    {
        try
        {
            Localization.ApplyLanguage(Localization.FinnishLanguage);
            Equal("fi", CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
            Equal("Asetukset…", Localization.Text("MenuSettings"));

            Localization.ApplyLanguage(Localization.EnglishLanguage);
            Equal("en", CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
            Equal("Settings…", Localization.Text("MenuSettings"));
        }
        finally
        {
            Localization.ApplyLanguage(Localization.SystemLanguage);
        }
    }

    private static void LegacySettingsUseSystemLanguage()
    {
        var settings = JsonSerializer.Deserialize<AppSettings>(
            "{\"Enabled\":true,\"Modifiers\":3,\"HotkeyKey\":77}")
            ?? throw new InvalidOperationException("Asetuksia ei voitu lukea testissä.");

        Equal(Localization.SystemLanguage, settings.Language);
    }

    private static void RealWindowMovesBetweenMonitors()
    {
        if (Screen.AllScreens.Length < 2)
        {
            Console.WriteLine("SKIP  Integraatiotesti vaatii vähintään kaksi näyttöä.");
            return;
        }

        var executable = Environment.ProcessPath
            ?? throw new InvalidOperationException("Testiohjelman polkua ei voitu selvittää.");
        using var child = Process.Start(new ProcessStartInfo(executable, "--test-window")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        }) ?? throw new InvalidOperationException("Testi-ikkunaa ei voitu käynnistää.");

        try
        {
            var stopwatch = Stopwatch.StartNew();
            nint window = nint.Zero;
            while (stopwatch.Elapsed < TimeSpan.FromSeconds(8) && !child.HasExited)
            {
                child.Refresh();
                window = child.MainWindowHandle;
                if (window != nint.Zero)
                {
                    break;
                }

                Thread.Sleep(50);
            }

            True(window != nint.Zero, "Testi-ikkunan kahvaa ei saatu.");
            var sourceMonitor = NativeMethods.MonitorFromWindow(window, NativeMethods.MonitorDefaultToNearest);
            var result = new WindowMoverService().MoveWindowToNextMonitor(window);
            var destinationMonitor = NativeMethods.MonitorFromWindow(window, NativeMethods.MonitorDefaultToNearest);

            True(result.IsSuccess, result.Message);
            True(sourceMonitor != destinationMonitor, "Ikkuna jäi samalle näytölle.");
        }
        finally
        {
            if (!child.HasExited)
            {
                child.CloseMainWindow();
                if (!child.WaitForExit(3000))
                {
                    child.Kill(entireProcessTree: true);
                    child.WaitForExit();
                }
            }
        }
    }

    private static void Equal(Rectangle expected, Rectangle actual)
    {
        if (expected != actual)
        {
            throw new InvalidOperationException($"Odotettiin {expected}, saatiin {actual}.");
        }
    }

    private static void Equal(string expected, string actual)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Odotettiin '{expected}', saatiin '{actual}'.");
        }
    }

    private static void True(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
