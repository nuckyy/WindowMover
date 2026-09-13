using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WindowMover;

[Flags]
internal enum HotkeyModifiers : uint
{
    None = 0,
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Win = 0x0008,
    NoRepeat = 0x4000
}

internal sealed record Hotkey(HotkeyModifiers Modifiers, Keys Key)
{
    public static readonly Hotkey Default = new(HotkeyModifiers.Control | HotkeyModifiers.Alt, Keys.M);

    public bool IsValid =>
        Key is >= Keys.A and <= Keys.Z or >= Keys.D0 and <= Keys.D9 or >= Keys.F1 and <= Keys.F11
        && (Modifiers & ~HotkeyModifiers.NoRepeat) != HotkeyModifiers.None;

    public uint NativeModifiers => (uint)(Modifiers | HotkeyModifiers.NoRepeat);

    public override string ToString()
    {
        var parts = new List<string>(5);
        if (Modifiers.HasFlag(HotkeyModifiers.Control)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(HotkeyModifiers.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(HotkeyModifiers.Shift)) parts.Add("Shift");
        if (Modifiers.HasFlag(HotkeyModifiers.Win)) parts.Add("Win");
        parts.Add(KeyToDisplayName(Key));
        return string.Join(" + ", parts);
    }

    public static Keys[] SupportedKeys { get; } =
    [
        .. Enumerable.Range('A', 26).Select(value => (Keys)value),
        .. Enumerable.Range(0, 10).Select(value => (Keys)((int)Keys.D0 + value)),
        .. Enumerable.Range(1, 11).Select(value => (Keys)((int)Keys.F1 + value - 1))
    ];

    public static string KeyToDisplayName(Keys key)
    {
        if (key is >= Keys.D0 and <= Keys.D9)
        {
            return ((int)(key - Keys.D0)).ToString(CultureInfo.InvariantCulture);
        }

        return key.ToString();
    }
}

internal sealed class HotkeyRegistrationException : Win32Exception
{
    public HotkeyRegistrationException(Hotkey hotkey)
        : base(Marshal.GetLastWin32Error(), Localization.Format("HotkeyRegistrationFailed", hotkey))
    {
    }
}
