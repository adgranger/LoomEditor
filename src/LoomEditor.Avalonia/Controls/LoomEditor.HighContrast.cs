using Avalonia;
using LoomEditor.Avalonia.Accessibility;
using LoomEditor.Themes;
using LoomEditor.Themes.HighContrast;

namespace LoomEditor.Avalonia.Controls;

// Follows the high contrast preference of the platform (Windows high contrast, macOS "increase contrast"). No Monaco counterpart.
public sealed partial class LoomEditor
{
    /// <summary>Identifies the <see cref="UseSystemHighContrast"/> styled property.</summary>
    public static readonly StyledProperty<bool> UseSystemHighContrastProperty =
        AvaloniaProperty.Register<LoomEditor, bool>(nameof(UseSystemHighContrast), true);

    private SystemHighContrast? _highContrast;
    private EditorTheme? _systemHighContrastTheme;

    /// <summary>
    /// Whether the editor draws with high contrast colors while the platform asks for high contrast, instead of
    /// <see cref="Theme"/>. Defaults to <c>true</c>.
    /// </summary>
    public bool UseSystemHighContrast
    {
        get => GetValue(UseSystemHighContrastProperty);
        set => SetValue(UseSystemHighContrastProperty, value);
    }

    // The high contrast state, for the tests; null while the control is not attached.
    internal SystemHighContrast? SystemHighContrast => _highContrast;

    private EditorTheme EffectiveTheme => _systemHighContrastTheme ?? _theme;

    private static void InitializeHighContrastProperties() =>
        UseSystemHighContrastProperty.Changed.AddClassHandler<LoomEditor>(static (editor, _) => editor.RefreshHighContrast());

    private void StartHighContrast()
    {
        if (_highContrast is null && Application.Current?.PlatformSettings is { } settings)
        {
            _highContrast = new SystemHighContrast(settings);
            _highContrast.Changed += RefreshHighContrast;
        }

        RefreshHighContrast();
    }

    private void StopHighContrast()
    {
        if (_highContrast is { } highContrast)
        {
            highContrast.Changed -= RefreshHighContrast;
            highContrast.Dispose();
            _highContrast = null;
        }
    }

    // Does nothing before the control is attached: StartHighContrast applies then.
    private void RefreshHighContrast()
    {
        if (_highContrast is null)
        {
            return;
        }

        _systemHighContrastTheme = UseSystemHighContrast && _highContrast.IsActive
            ? SystemHighContrastThemeFactory.Create(_highContrast.ReadColors())
            : null;
        ApplyTheme(EffectiveTheme);
    }
}
