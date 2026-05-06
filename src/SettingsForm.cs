namespace GeminiForChromeManager;

internal sealed class SettingsForm : Form
{
    private readonly AppSettings settings;
    private readonly CheckBox startWithWindowsCheckBox = new();
    private readonly CheckBox diagnosticsCheckBox = new();
    private readonly ComboBox themeComboBox = new();

    public SettingsForm(AppSettings settings)
    {
        this.settings = settings;
        Text = "Settings";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(430, 190);

        BuildLayout();
        LoadSettings();
        AppTheme.Apply(this, AppTheme.Resolve(settings.Theme));
    }

    private void BuildLayout()
    {
        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 2,
            RowCount = 4
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddLabel(root, "Start with Windows", 0);
        startWithWindowsCheckBox.Text = "Launch when you sign in";
        root.Controls.Add(startWithWindowsCheckBox, 1, 0);

        AddLabel(root, "Diagnostic logging", 1);
        diagnosticsCheckBox.Text = "Enabled";
        root.Controls.Add(diagnosticsCheckBox, 1, 1);

        AddLabel(root, "Theme", 2);
        themeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        themeComboBox.Items.AddRange(["Auto", "Light", "Dark"]);
        themeComboBox.Dock = DockStyle.Left;
        themeComboBox.Width = 160;
        themeComboBox.SelectedIndexChanged += (_, _) => AppTheme.Apply(this, AppTheme.Resolve(GetSelectedTheme()));
        root.Controls.Add(themeComboBox, 1, 2);

        FlowLayoutPanel buttons = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };

        Button saveButton = new()
        {
            Text = "Save",
            DialogResult = DialogResult.OK,
            Width = 90
        };
        saveButton.Click += (_, _) => SaveSettings();

        Button cancelButton = new()
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Width = 90
        };

        buttons.Controls.Add(saveButton);
        buttons.Controls.Add(cancelButton);
        root.Controls.Add(buttons, 1, 3);

        for (int row = 0; row < 3; row++)
        {
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        }

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        Controls.Add(root);
        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    private static void AddLabel(TableLayoutPanel root, string text, int row)
    {
        Label label = new()
        {
            Text = text,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            TextAlign = ContentAlignment.MiddleLeft
        };
        root.Controls.Add(label, 0, row);
    }

    private void LoadSettings()
    {
        startWithWindowsCheckBox.Checked = settings.StartWithWindows;
        diagnosticsCheckBox.Checked = settings.DiagnosticLoggingEnabled;
        themeComboBox.SelectedIndex = settings.Theme switch
        {
            ThemeMode.Light => 1,
            ThemeMode.Dark => 2,
            _ => 0
        };
    }

    private void SaveSettings()
    {
        settings.StartWithWindows = startWithWindowsCheckBox.Checked;
        settings.DiagnosticLoggingEnabled = diagnosticsCheckBox.Checked;
        settings.Theme = GetSelectedTheme();
    }

    private ThemeMode GetSelectedTheme()
    {
        return themeComboBox.SelectedIndex switch
        {
            1 => ThemeMode.Light,
            2 => ThemeMode.Dark,
            _ => ThemeMode.Auto
        };
    }
}
