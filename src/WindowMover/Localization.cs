using System.Globalization;
using System.Resources;

namespace WindowMover;

internal static class Localization
{
    public const string SystemLanguage = "system";
    public const string EnglishLanguage = "en";
    public const string FinnishLanguage = "fi";

    private static readonly CultureInfo DetectedSystemUiCulture = CultureInfo.CurrentUICulture;
    private static readonly ResourceManager ResourceManager = new(
        "WindowMover.Resources.Strings",
        typeof(Localization).Assembly);

    public static CultureInfo SystemUiCulture => DetectedSystemUiCulture;

    public static string NormalizeLanguage(string? language)
    {
        return language?.Trim().ToLowerInvariant() switch
        {
            EnglishLanguage => EnglishLanguage,
            FinnishLanguage => FinnishLanguage,
            _ => SystemLanguage
        };
    }

    public static void ApplyLanguage(string? language)
    {
        var culture = NormalizeLanguage(language) switch
        {
            EnglishLanguage => CultureInfo.GetCultureInfo(EnglishLanguage),
            FinnishLanguage => CultureInfo.GetCultureInfo(FinnishLanguage),
            _ => DetectedSystemUiCulture
        };

        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    public static string Text(string name)
    {
        return Text(name, CultureInfo.CurrentUICulture);
    }

    internal static string Text(string name, CultureInfo culture)
    {
        return ResourceManager.GetString(name, culture)
            ?? throw new MissingManifestResourceException($"Missing UI resource: {name}");
    }

    public static string Format(string name, params object?[] arguments)
    {
        return string.Format(CultureInfo.CurrentUICulture, Text(name), arguments);
    }
}
