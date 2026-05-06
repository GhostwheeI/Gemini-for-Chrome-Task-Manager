using Microsoft.Win32;

namespace GeminiForChromeManager;

internal enum ThemeMode
{
    Auto,
    Light,
    Dark
}

internal enum ResolvedTheme
{
    Light,
    Dark
}

internal static class AppTheme
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private static ThemeMode configuredTheme = ThemeMode.Auto;

    public static void Configure(AppSettings settings)
    {
        configuredTheme = Enum.IsDefined(settings.Theme) ? settings.Theme : ThemeMode.Auto;
    }

    public static ResolvedTheme CurrentResolvedTheme => Resolve(configuredTheme);

    public static void Apply(Form form)
    {
        Apply(form, CurrentResolvedTheme);
    }

    public static void Apply(Form form, ResolvedTheme theme)
    {
        ThemePalette palette = ThemePalette.For(theme);
        ApplyControl(form, palette);
    }

    public static void Apply(ContextMenuStrip menu)
    {
        ThemePalette palette = ThemePalette.For(CurrentResolvedTheme);
        menu.BackColor = palette.MenuBackColor;
        menu.ForeColor = palette.ForeColor;
        menu.Renderer = new ToolStripProfessionalRenderer(new AppThemeColorTable(palette));

        foreach (ToolStripItem item in menu.Items)
        {
            ApplyToolStripItem(item, palette);
        }
    }

    public static void ApplyMenuCheckBox(CheckBox checkBox)
    {
        ThemePalette palette = ThemePalette.For(CurrentResolvedTheme);
        checkBox.BackColor = palette.MenuBackColor;
        checkBox.ForeColor = palette.ForeColor;
        checkBox.FlatStyle = FlatStyle.Standard;
    }

    public static ResolvedTheme Resolve(ThemeMode themeMode)
    {
        return themeMode switch
        {
            ThemeMode.Dark => ResolvedTheme.Dark,
            ThemeMode.Light => ResolvedTheme.Light,
            _ => IsWindowsAppThemeDark() ? ResolvedTheme.Dark : ResolvedTheme.Light
        };
    }

    private static bool IsWindowsAppThemeDark()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
            object? value = key?.GetValue("AppsUseLightTheme");
            return value is int intValue && intValue == 0;
        }
        catch
        {
            return false;
        }
    }

    private static void ApplyControl(Control control, ThemePalette palette)
    {
        control.BackColor = palette.BackColor;
        control.ForeColor = palette.ForeColor;

        switch (control)
        {
            case TextBoxBase textBox:
                textBox.BackColor = palette.InputBackColor;
                textBox.ForeColor = palette.ForeColor;
                break;

            case ComboBox comboBox:
                comboBox.BackColor = palette.InputBackColor;
                comboBox.ForeColor = palette.ForeColor;
                break;

            case NumericUpDown numeric:
                numeric.BackColor = palette.InputBackColor;
                numeric.ForeColor = palette.ForeColor;
                break;

            case Button button:
                button.BackColor = palette.ButtonBackColor;
                button.ForeColor = palette.ForeColor;
                button.FlatStyle = FlatStyle.System;
                break;

            case CheckBox checkBox:
                checkBox.BackColor = palette.BackColor;
                checkBox.ForeColor = palette.ForeColor;
                break;

            case DataGridView grid:
                ApplyGrid(grid, palette);
                break;
        }

        foreach (Control child in control.Controls)
        {
            ApplyControl(child, palette);
        }
    }

    private static void ApplyGrid(DataGridView grid, ThemePalette palette)
    {
        grid.BackgroundColor = palette.BackColor;
        grid.BorderStyle = BorderStyle.FixedSingle;
        grid.EnableHeadersVisualStyles = false;
        grid.GridColor = palette.GridColor;
        grid.DefaultCellStyle.BackColor = palette.InputBackColor;
        grid.DefaultCellStyle.ForeColor = palette.ForeColor;
        grid.DefaultCellStyle.SelectionBackColor = palette.SelectionBackColor;
        grid.DefaultCellStyle.SelectionForeColor = palette.SelectionForeColor;
        grid.ColumnHeadersDefaultCellStyle.BackColor = palette.HeaderBackColor;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = palette.ForeColor;
        grid.RowHeadersDefaultCellStyle.BackColor = palette.HeaderBackColor;
        grid.RowHeadersDefaultCellStyle.ForeColor = palette.ForeColor;
    }

    private static void ApplyToolStripItem(ToolStripItem item, ThemePalette palette)
    {
        item.BackColor = palette.MenuBackColor;
        item.ForeColor = palette.ForeColor;

        if (item is ToolStripDropDownItem dropDownItem)
        {
            dropDownItem.DropDown.BackColor = palette.MenuBackColor;
            dropDownItem.DropDown.ForeColor = palette.ForeColor;
            dropDownItem.DropDown.Renderer = new ToolStripProfessionalRenderer(new AppThemeColorTable(palette));

            foreach (ToolStripItem child in dropDownItem.DropDownItems)
            {
                ApplyToolStripItem(child, palette);
            }
        }
    }

    private sealed record ThemePalette(
        Color BackColor,
        Color ForeColor,
        Color InputBackColor,
        Color ButtonBackColor,
        Color HeaderBackColor,
        Color GridColor,
        Color SelectionBackColor,
        Color SelectionForeColor,
        Color MenuBackColor)
    {
        public static ThemePalette For(ResolvedTheme theme)
        {
            return theme == ResolvedTheme.Dark
                ? new ThemePalette(
                    Color.FromArgb(32, 32, 32),
                    Color.FromArgb(242, 242, 242),
                    Color.FromArgb(45, 45, 48),
                    Color.FromArgb(56, 56, 60),
                    Color.FromArgb(45, 45, 48),
                    Color.FromArgb(70, 70, 74),
                    Color.FromArgb(0, 120, 215),
                    Color.White,
                    Color.FromArgb(38, 38, 38))
                : new ThemePalette(
                    SystemColors.Control,
                    SystemColors.ControlText,
                    SystemColors.Window,
                    SystemColors.Control,
                    SystemColors.Control,
                    SystemColors.ControlDark,
                    SystemColors.Highlight,
                    SystemColors.HighlightText,
                    SystemColors.Menu);
        }
    }

    private sealed class AppThemeColorTable : ProfessionalColorTable
    {
        private readonly ThemePalette palette;

        public AppThemeColorTable(ThemePalette palette)
        {
            this.palette = palette;
        }

        public override Color ToolStripDropDownBackground => palette.MenuBackColor;
        public override Color ImageMarginGradientBegin => palette.MenuBackColor;
        public override Color ImageMarginGradientMiddle => palette.MenuBackColor;
        public override Color ImageMarginGradientEnd => palette.MenuBackColor;
        public override Color MenuItemSelected => palette.SelectionBackColor;
        public override Color MenuItemSelectedGradientBegin => palette.SelectionBackColor;
        public override Color MenuItemSelectedGradientEnd => palette.SelectionBackColor;
        public override Color MenuItemBorder => palette.SelectionBackColor;
        public override Color SeparatorDark => palette.GridColor;
        public override Color SeparatorLight => palette.GridColor;
    }
}
