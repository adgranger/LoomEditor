using LoomEditor.Themes;
using LoomEditor.Themes.HighContrast;
using LoomEditor.WinUI.Accessibility;
using Microsoft.UI.Xaml;

namespace LoomEditor.WinUI.Controls;

// Follows the Windows high contrast scheme. No Monaco counterpart.
#pragma warning disable CA1001 // Reported on the first partial declaration; see the suppression in LoomEditor.cs.
public sealed partial class LoomEditor
#pragma warning restore CA1001
{
    /// <summary>Identifies the <see cref="UseSystemHighContrast"/> dependency property.</summary>
    public static readonly DependencyProperty UseSystemHighContrastProperty = DependencyProperty.Register(
        nameof(UseSystemHighContrast), typeof(bool), typeof(LoomEditor), new PropertyMetadata(true, OnUseSystemHighContrastChanged));

    private SystemHighContrast? _highContrast;
    private EditorTheme? _systemHighContrastTheme;

    /// <summary>
    /// Whether the editor draws with the colors of the Windows high contrast scheme while one is on, instead of
    /// <see cref="Theme"/>. Defaults to <c>true</c>.
    /// </summary>
    public bool UseSystemHighContrast
    {
        get => (bool)GetValue(UseSystemHighContrastProperty);
        set => SetValue(UseSystemHighContrastProperty, value);
    }

    private EditorTheme EffectiveTheme => _systemHighContrastTheme ?? _theme;

    private static void OnUseSystemHighContrastChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) =>
        ((LoomEditor)sender).RefreshHighContrast();

    private void StartHighContrast()
    {
        _highContrast ??= new SystemHighContrast(DispatcherQueue);
        _highContrast.Changed += RefreshHighContrast;
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

    // Does nothing before OnLoaded: StartHighContrast applies then.
    private void RefreshHighContrast()
    {
        if (_highContrast is null)
        {
            return;
        }

        _systemHighContrastTheme = UseSystemHighContrast && _highContrast.IsActive
            ? SystemHighContrastThemeFactory.Create(SystemHighContrast.ReadColors())
            : null;
        ApplyTheme(EffectiveTheme);
    }
}
