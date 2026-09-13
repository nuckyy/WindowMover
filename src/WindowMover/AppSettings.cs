using System.Text.Json;
using System.Windows.Forms;

namespace WindowMover;

internal sealed class AppSettings
{
    public bool Enabled { get; set; } = true;
    public HotkeyModifiers Modifiers { get; set; } = Hotkey.Default.Modifiers;
    public Keys HotkeyKey { get; set; } = Hotkey.Default.Key;
    public string Language { get; set; } = Localization.SystemLanguage;

    public Hotkey GetHotkey()
    {
        var hotkey = new Hotkey(Modifiers & ~HotkeyModifiers.NoRepeat, HotkeyKey);
        return hotkey.IsValid ? hotkey : Hotkey.Default;
    }
}

internal sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _directory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WindowMover");

    private string SettingsPath => Path.Combine(_directory, "settings.json");

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return new AppSettings();
            }

            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath), JsonOptions)
                ?? new AppSettings();
            settings.Language = Localization.NormalizeLanguage(settings.Language);
            return settings;
        }
        catch (Exception exception)
        {
            AppLog.Error(Localization.Text("SettingsLoadFailed"), exception);
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(_directory);
        var temporaryPath = SettingsPath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, JsonOptions));
        File.Move(temporaryPath, SettingsPath, overwrite: true);
    }
}
