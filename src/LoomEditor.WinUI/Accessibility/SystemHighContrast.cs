using LoomEditor.Themes;
using LoomEditor.Themes.HighContrast;
using LoomEditor.WinUI.Interop;
using Microsoft.UI.Dispatching;
using Windows.UI.ViewManagement;

namespace LoomEditor.WinUI.Accessibility;

/// <summary>Reads the Windows high contrast state and scheme, and reports when the state changes.</summary>
/// <remarks>Threading: UI thread only; <see cref="Changed"/> is always raised on the dispatcher queue given to the constructor.</remarks>
internal sealed class SystemHighContrast : IDisposable
{
    private const int ColorWindow = 5;
    private const int ColorWindowText = 8;
    private const int ColorHighlight = 13;
    private const int ColorHighlightText = 14;
    private const int ColorButtonFace = 15;
    private const int ColorGrayText = 17;
    private const int ColorButtonText = 18;
    private const int ColorHotlight = 26;

    private readonly AccessibilitySettings _settings = new();
    private readonly DispatcherQueue _dispatcher;
    private bool _disposed;

    public SystemHighContrast(DispatcherQueue dispatcher)
    {
        _dispatcher = dispatcher;
        _settings.HighContrastChanged += OnHighContrastChanged;
    }

    /// <summary>Raised on the UI thread when high contrast is switched on or off or its scheme changes.</summary>
    public event Action? Changed;

    /// <summary>Whether a high contrast scheme is on.</summary>
    public bool IsActive => _settings.HighContrast;

    /// <summary>The colors of the current scheme.</summary>
    public static SystemHighContrastColors ReadColors() => new(
        Color(ColorWindow),
        Color(ColorWindowText),
        Color(ColorHighlight),
        Color(ColorHighlightText),
        Color(ColorGrayText),
        Color(ColorHotlight),
        Color(ColorButtonFace),
        Color(ColorButtonText));

    public void Dispose()
    {
        _disposed = true;
        _settings.HighContrastChanged -= OnHighContrastChanged;
    }

    // COLORREF is 0x00BBGGRR.
    private static ThemeColor Color(int index)
    {
        uint value = NativeMethods.GetSysColor(index);
        return ThemeColor.FromRgb((byte)(value & 0xFF), (byte)((value >> 8) & 0xFF), (byte)((value >> 16) & 0xFF));
    }

    // The WinRT event may arrive on any thread.
    private void OnHighContrastChanged(AccessibilitySettings sender, object args) =>
        _dispatcher.TryEnqueue(() =>
        {
            if (!_disposed)
            {
                Changed?.Invoke();
            }
        });
}
