using System.Text.Json;
using Microsoft.Win32;

namespace CapsLockIndicator;

/// <summary>
/// User settings persisted as JSON in %AppData%\CapsLockIndicator\settings.json.
/// </summary>
internal sealed class AppSettings
{
    public const float MinFontSize = 8f;
    public const float MaxFontSize = 200f;

    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "CapsLockIndicator";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public float FontSize { get; set; } = 36f;
    public string FontFamily { get; set; } = "Segoe UI";
    public bool Bold { get; set; } = true;
    public string BackgroundColor { get; set; } = "#505050";
    public string TextColor { get; set; } = "#FFFFFF";
    public int Opacity { get; set; } = 90;              // percent, 20-100
    public int DisplayDurationMs { get; set; } = 1500;
    public int Margin { get; set; } = 16;               // pixels from the screen / taskbar edge
    public string OnText { get; set; } = "Caps Lock ON";
    public string OffText { get; set; } = "Caps Lock OFF";

    public static string SettingsPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CapsLockIndicator", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath), JsonOptions);
                if (loaded != null)
                {
                    loaded.Normalize();
                    return loaded;
                }
            }
        }
        catch (Exception)
        {
            // Corrupted file: fall back to defaults.
        }

        return new AppSettings();
    }

    public void Save()
    {
        Normalize();
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, JsonOptions));
    }

    public AppSettings Clone() => (AppSettings)MemberwiseClone();

    public Color GetBackgroundColor() => ParseColor(BackgroundColor, Color.FromArgb(0x50, 0x50, 0x50));

    public Color GetTextColor() => ParseColor(TextColor, Color.White);

    public static string ToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    private void Normalize()
    {
        FontSize = Math.Clamp(FontSize, MinFontSize, MaxFontSize);
        Opacity = Math.Clamp(Opacity, 20, 100);
        DisplayDurationMs = Math.Clamp(DisplayDurationMs, 300, 10000);
        Margin = Math.Clamp(Margin, 0, 500);
        if (string.IsNullOrWhiteSpace(FontFamily)) FontFamily = "Segoe UI";
        OnText ??= "Caps Lock ON";
        OffText ??= "Caps Lock OFF";
    }

    private static Color ParseColor(string? value, Color fallback)
    {
        try
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : ColorTranslator.FromHtml(value);
        }
        catch (Exception)
        {
            return fallback;
        }
    }

    // Start-with-Windows is stored in the registry rather than the JSON file,
    // so it always reflects the real state.
    public static bool IsStartWithWindowsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        return key?.GetValue(RunValueName) is string;
    }

    public static void SetStartWithWindows(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true)
                        ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (enabled)
        {
            key.SetValue(RunValueName, $"\"{Environment.ProcessPath ?? Application.ExecutablePath}\"");
        }
        else if (key.GetValue(RunValueName) != null)
        {
            key.DeleteValue(RunValueName, false);
        }
    }
}
