using Avalonia.Platform;
using Avalonia.Threading;
using LoomEditor.Themes;
using LoomEditor.Themes.HighContrast;

namespace LoomEditor.Avalonia.Accessibility;

/// <summary>
/// Reads the high contrast preference of the platform (Windows high contrast, macOS "increase contrast") from Avalonia's
/// <see cref="IPlatformSettings"/>, and reports when it changes.
/// </summary>
/// <remarks>
/// Avalonia gives no scheme colors, only the accent and whether the scheme is dark: the other colors are fixed, those of the
/// Windows "High Contrast Black" and "High Contrast White" schemes.
/// Threading: UI thread only; <see cref="Changed"/> is raised on the UI thread.
/// </remarks>
internal sealed class SystemHighContrast : IDisposable
{
    private readonly IPlatformSettings _settings;
    private PlatformColorValues _values;

    public SystemHighContrast(IPlatformSettings settings)
    {
        _settings = settings;
        _values = settings.GetColorValues();
        _settings.ColorValuesChanged += OnColorValuesChanged;
    }

    /// <summary>Raised on the UI thread when the contrast preference, the theme variant or the accent changes.</summary>
    public event Action? Changed;

    /// <summary>Whether the platform asks for high contrast.</summary>
    public bool IsActive => _values.ContrastPreference == ColorContrastPreference.High;

    /// <summary>The colors of the high contrast theme: the accent is the highlight, the rest is fixed.</summary>
    public SystemHighContrastColors ReadColors()
    {
        global::Avalonia.Media.Color accent = _values.AccentColor1;
        ThemeColor highlight = ThemeColor.FromRgb(accent.R, accent.G, accent.B);
        return _values.ThemeVariant == PlatformThemeVariant.Light
            ? new(Rgb("#FFFFFF"), Rgb("#000000"), highlight, Rgb("#FFFFFF"), Rgb("#600000"), Rgb("#00009F"), Rgb("#FFFFFF"), Rgb("#000000"))
            : new(Rgb("#000000"), Rgb("#FFFFFF"), highlight, Rgb("#000000"), Rgb("#3FF23F"), Rgb("#FFFF00"), Rgb("#000000"), Rgb("#FFFFFF"));
    }

    public void Dispose() => _settings.ColorValuesChanged -= OnColorValuesChanged;

    private static ThemeColor Rgb(string hex) => ThemeColor.Parse(hex);

    /// <summary>Takes the new values of the platform and raises <see cref="Changed"/> (the tests call it: there is no fake platform).</summary>
    public void Update(PlatformColorValues values)
    {
        _values = values;
        Changed?.Invoke();
    }

    // The event carries the new values.
    private void OnColorValuesChanged(object? sender, PlatformColorValues e)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            Update(e);
        }
        else
        {
            Dispatcher.UIThread.Post(() => Update(e));
        }
    }
}
