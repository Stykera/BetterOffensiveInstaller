using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace BetterOffensiveInstaller;

internal static class SystemTheme
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaUseImmersiveDarkModeBefore20H1 = 19;
    private const int DwmwaBorderColor = 34;
    private const int DwmwaCaptionColor = 35;
    private const int DwmwaTextColor = 36;
    private const int DwmwaColorDefault = unchecked((int)0xFFFFFFFF);
    private const int PbmSetBkColor = 0x2001;
    private const int PbmSetBarColor = 0x409;
    private const int PreferredAppModeAllowDark = 1;
    private const int PreferredAppModeForceDark = 2;
    private const int PreferredAppModeForceLight = 3;

    public static ThemePalette Current { get; private set; } = ThemePalette.Light();

    public static void UseSystemAppMode()
    {
        try
        {
            var mode = ShouldUseDarkColors() ? PreferredAppModeForceDark : PreferredAppModeForceLight;
            _ = SetPreferredAppMode(mode);
            FlushMenuThemes();
        }
        catch (Exception)
        {
        }
    }

    public static void AllowSystemAppMode()
    {
        try
        {
            _ = SetPreferredAppMode(PreferredAppModeAllowDark);
            FlushMenuThemes();
        }
        catch (Exception)
        {
        }
    }

    public static void Apply(Form form)
    {
        var dark = ShouldUseDarkColors();
        Current = dark ? ThemePalette.Dark(ReadAccentColor()) : ThemePalette.Light();

        form.BackColor = Current.Window;
        form.ForeColor = Current.Text;

        foreach (Control control in form.Controls)
        {
            ApplyControl(control, Current.Window, dark);
        }

        form.Invalidate(true);
    }

    public static void ApplyTitleBar(Form form)
    {
        if (!form.IsHandleCreated)
        {
            return;
        }

        try
        {
            var enabled = ShouldUseDarkColors() ? 1 : 0;
            _ = DwmSetWindowAttribute(form.Handle, DwmwaUseImmersiveDarkMode, ref enabled, sizeof(int));
            _ = DwmSetWindowAttribute(form.Handle, DwmwaUseImmersiveDarkModeBefore20H1, ref enabled, sizeof(int));

            var caption = enabled == 1 ? ToColorRef(Current.Window) : DwmwaColorDefault;
            var text = enabled == 1 ? ToColorRef(Current.Text) : DwmwaColorDefault;
            var border = enabled == 1 ? ToColorRef(Current.Card) : DwmwaColorDefault;
            _ = DwmSetWindowAttribute(form.Handle, DwmwaCaptionColor, ref caption, sizeof(int));
            _ = DwmSetWindowAttribute(form.Handle, DwmwaTextColor, ref text, sizeof(int));
            _ = DwmSetWindowAttribute(form.Handle, DwmwaBorderColor, ref border, sizeof(int));
        }
        catch (Exception)
        {
        }
    }

    public static void ApplyProgressBar(ProgressBar progressBar)
    {
        if (!progressBar.IsHandleCreated)
        {
            return;
        }

        try
        {
            if (!ShouldUseDarkColors())
            {
                _ = SetWindowTheme(progressBar.Handle, null, null);
                return;
            }

            _ = SetWindowTheme(progressBar.Handle, string.Empty, string.Empty);
            _ = SendMessage(progressBar.Handle, PbmSetBkColor, IntPtr.Zero, (IntPtr)ToColorRef(Current.Input));
            _ = SendMessage(progressBar.Handle, PbmSetBarColor, IntPtr.Zero, (IntPtr)ToColorRef(Current.Accent));
        }
        catch (Exception)
        {
        }
    }

    private static void ApplyControl(Control control, Color parentBackground, bool dark)
    {
        switch (control)
        {
            case Button button:
                StyleButton(button, dark);
                break;

            case TextBox textBox:
                StyleTextBox(textBox, dark);
                break;

            case ComboBox comboBox:
                StyleComboBox(comboBox, dark);
                break;

            case GroupBox groupBox:
                groupBox.BackColor = dark ? Current.Card : SystemColors.Control;
                groupBox.ForeColor = dark ? Current.Text : SystemColors.ControlText;
                foreach (Control child in groupBox.Controls)
                {
                    ApplyControl(child, groupBox.BackColor, dark);
                }

                break;

            case RadioButton radioButton:
                radioButton.UseVisualStyleBackColor = !dark;
                radioButton.BackColor = parentBackground;
                radioButton.ForeColor = dark ? Current.Text : SystemColors.ControlText;
                break;

            case Label label:
                label.BackColor = parentBackground;
                label.ForeColor = dark ? Current.Text : SystemColors.ControlText;
                break;

            case ProgressBar:
                break;

            default:
                control.BackColor = parentBackground;
                control.ForeColor = dark ? Current.Text : SystemColors.ControlText;
                foreach (Control child in control.Controls)
                {
                    ApplyControl(child, parentBackground, dark);
                }

                break;
        }
    }

    private static void StyleButton(Button button, bool dark)
    {
        if (!dark)
        {
            button.UseVisualStyleBackColor = true;
            button.FlatStyle = FlatStyle.Standard;
            button.BackColor = SystemColors.Control;
            button.ForeColor = SystemColors.ControlText;
            return;
        }

        button.UseVisualStyleBackColor = false;
        button.FlatStyle = FlatStyle.Flat;
        button.BackColor = Current.Button;
        button.ForeColor = Current.Text;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = Current.ButtonBorder;
        button.FlatAppearance.MouseOverBackColor = Current.ButtonHover;
        button.FlatAppearance.MouseDownBackColor = Current.ButtonPressed;
    }

    private static void StyleTextBox(TextBox textBox, bool dark)
    {
        if (!dark)
        {
            textBox.BorderStyle = BorderStyle.Fixed3D;
            textBox.BackColor = SystemColors.Window;
            textBox.ForeColor = SystemColors.WindowText;
            return;
        }

        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.BackColor = Current.Input;
        textBox.ForeColor = Current.Text;
    }

    private static void StyleComboBox(ComboBox comboBox, bool dark)
    {
        if (!dark)
        {
            comboBox.FlatStyle = FlatStyle.Standard;
            comboBox.DrawMode = DrawMode.Normal;
            comboBox.BackColor = SystemColors.Window;
            comboBox.ForeColor = SystemColors.WindowText;
            return;
        }

        comboBox.FlatStyle = FlatStyle.Flat;
        comboBox.DrawMode = DrawMode.OwnerDrawFixed;
        comboBox.ItemHeight = Math.Max(comboBox.Font.Height + 6, 20);
        comboBox.BackColor = Current.Input;
        comboBox.ForeColor = Current.Text;
    }

    private static bool ShouldUseDarkColors()
    {
        return !SystemInformation.HighContrast && !ReadAppsUseLightTheme();
    }

    private static bool ReadAppsUseLightTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is not int value || value != 0;
        }
        catch (Exception)
        {
            return true;
        }
    }

    private static Color ReadAccentColor()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM");
            if (key?.GetValue("AccentColor") is int accent)
            {
                return ColorFromAbgr(accent);
            }

            if (key?.GetValue("ColorizationColor") is int colorization)
            {
                return ColorFromAbgr(colorization);
            }
        }
        catch (Exception)
        {
        }

        return Color.FromArgb(0, 120, 215);
    }

    private static Color ColorFromAbgr(int value)
    {
        return Color.FromArgb(value & 0xFF, (value >> 8) & 0xFF, (value >> 16) & 0xFF);
    }

    private static int ToColorRef(Color color)
    {
        return color.R | (color.G << 8) | (color.B << 16);
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int valueSize);

    [DllImport("uxtheme.dll", EntryPoint = "#135")]
    private static extern int SetPreferredAppMode(int preferredAppMode);

    [DllImport("uxtheme.dll", EntryPoint = "#136")]
    private static extern void FlushMenuThemes();

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hwnd, string? subAppName, string? subIdList);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam);

    internal readonly record struct ThemePalette(
        Color Window,
        Color Card,
        Color Input,
        Color Text,
        Color MutedText,
        Color Button,
        Color ButtonBorder,
        Color ButtonHover,
        Color ButtonPressed,
        Color Accent,
        Color HighlightText)
    {
        public static ThemePalette Light()
        {
            return new ThemePalette(
                SystemColors.Control,
                SystemColors.Control,
                SystemColors.Window,
                SystemColors.ControlText,
                SystemColors.ControlText,
                SystemColors.Control,
                SystemColors.ControlDark,
                SystemColors.Control,
                SystemColors.Control,
                SystemColors.Highlight,
                SystemColors.HighlightText);
        }

        public static ThemePalette Dark(Color accent)
        {
            return new ThemePalette(
                Color.FromArgb(32, 32, 32),
                Color.FromArgb(44, 44, 44),
                Color.FromArgb(59, 59, 59),
                Color.White,
                Color.FromArgb(200, 200, 200),
                Color.FromArgb(59, 59, 59),
                Color.FromArgb(107, 107, 107),
                Color.FromArgb(72, 72, 72),
                Color.FromArgb(86, 86, 86),
                accent,
                ContrastText(accent));
        }

        private static Color ContrastText(Color background)
        {
            var luminance = ((0.299 * background.R) + (0.587 * background.G) + (0.114 * background.B)) / 255d;
            return luminance > 0.6 ? Color.Black : Color.White;
        }
    }
}
