namespace WindowMover;

internal sealed class SettingsForm : Form
{
    private readonly CheckBox _control = new() { Text = "Ctrl", AutoSize = true };
    private readonly CheckBox _alt = new() { Text = "Alt", AutoSize = true };
    private readonly CheckBox _shift = new() { Text = "Shift", AutoSize = true };
    private readonly CheckBox _win = new() { Text = "Win", AutoSize = true };
    private readonly ComboBox _key = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 105
    };
    private readonly CheckBox _startWithWindows = new()
    {
        Text = Localization.Text("StartWithWindows"),
        AutoSize = true
    };
    private readonly ComboBox _language = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 230
    };
    private readonly Label _validationMessage = new()
    {
        AutoSize = true,
        ForeColor = Color.Firebrick,
        Visible = false
    };

    public SettingsForm(Hotkey currentHotkey, bool startWithWindows, string currentLanguage)
    {
        Text = Localization.Text("SettingsTitle");
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(500, 280);
        Font = SystemFonts.MessageBoxFont;

        _key.Items.AddRange(Hotkey.SupportedKeys
            .Select(key => new KeyChoice(key, Hotkey.KeyToDisplayName(key)))
            .Cast<object>()
            .ToArray());

        _control.Checked = currentHotkey.Modifiers.HasFlag(HotkeyModifiers.Control);
        _alt.Checked = currentHotkey.Modifiers.HasFlag(HotkeyModifiers.Alt);
        _shift.Checked = currentHotkey.Modifiers.HasFlag(HotkeyModifiers.Shift);
        _win.Checked = currentHotkey.Modifiers.HasFlag(HotkeyModifiers.Win);
        _key.SelectedItem = _key.Items.Cast<KeyChoice>().First(item => item.Key == currentHotkey.Key);
        _startWithWindows.Checked = startWithWindows;

        _language.Items.AddRange(
        [
            new LanguageChoice(
                Localization.SystemLanguage,
                Localization.Format("LanguageSystemDefault", Localization.SystemUiCulture.NativeName)),
            new LanguageChoice(Localization.EnglishLanguage, Localization.Text("LanguageEnglish")),
            new LanguageChoice(Localization.FinnishLanguage, Localization.Text("LanguageFinnish"))
        ]);
        var normalizedLanguage = Localization.NormalizeLanguage(currentLanguage);
        _language.SelectedItem = _language.Items
            .Cast<LanguageChoice>()
            .First(item => item.Code == normalizedLanguage);

        BuildLayout();
    }

    public Hotkey SelectedHotkey
    {
        get
        {
            var modifiers = HotkeyModifiers.None;
            if (_control.Checked) modifiers |= HotkeyModifiers.Control;
            if (_alt.Checked) modifiers |= HotkeyModifiers.Alt;
            if (_shift.Checked) modifiers |= HotkeyModifiers.Shift;
            if (_win.Checked) modifiers |= HotkeyModifiers.Win;
            return new Hotkey(modifiers, ((KeyChoice)_key.SelectedItem!).Key);
        }
    }

    public bool StartWithWindows => _startWithWindows.Checked;

    public string SelectedLanguage => ((LanguageChoice)_language.SelectedItem!).Code;

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 6
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var heading = new Label
        {
            Text = Localization.Text("HotkeyHeading"),
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 8)
        };

        var hotkeyRow = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 8)
        };
        hotkeyRow.Controls.AddRange([_control, _alt, _shift, _win, _key]);

        _validationMessage.Margin = new Padding(0, 0, 0, 8);
        _startWithWindows.Margin = new Padding(0, 10, 0, 0);

        var languageRow = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0, 6, 0, 8)
        };
        var languageLabel = new Label
        {
            Text = Localization.Text("LanguageLabel"),
            AutoSize = true,
            Margin = new Padding(0, 6, 10, 0)
        };
        languageRow.Controls.AddRange([languageLabel, _language]);

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            WrapContents = false
        };
        var saveButton = new Button { Text = Localization.Text("Save"), AutoSize = true };
        var cancelButton = new Button
        {
            Text = Localization.Text("Cancel"),
            AutoSize = true,
            DialogResult = DialogResult.Cancel
        };
        saveButton.Click += SaveButtonClick;
        buttons.Controls.AddRange([saveButton, cancelButton]);

        AcceptButton = saveButton;
        CancelButton = cancelButton;

        root.Controls.Add(heading, 0, 0);
        root.Controls.Add(hotkeyRow, 0, 1);
        root.Controls.Add(_validationMessage, 0, 2);
        root.Controls.Add(languageRow, 0, 3);
        root.Controls.Add(_startWithWindows, 0, 4);
        root.Controls.Add(buttons, 0, 5);
        Controls.Add(root);
    }

    private void SaveButtonClick(object? sender, EventArgs eventArgs)
    {
        var hotkey = SelectedHotkey;
        if (!hotkey.IsValid)
        {
            _validationMessage.Text = Localization.Text("InvalidHotkey");
            _validationMessage.Visible = true;
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private sealed record KeyChoice(Keys Key, string Name)
    {
        public override string ToString() => Name;
    }

    private sealed record LanguageChoice(string Code, string Name)
    {
        public override string ToString() => Name;
    }
}
