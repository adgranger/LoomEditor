using LoomEditor.Themes;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Decorations;

/// <summary>
/// The decoration classes of the host, like a stylesheet: <see cref="Register"/> a class name with a style, then use the name in
/// <see cref="EditorDecorationOptions"/>.
/// </summary>
/// <remarks>
/// Styles are global to the process; a change shows at the next repaint of each editor, so register styles before setting decorations.
/// Prefix the names with the name of the application. The built-in names (<c>rangeHighlight</c>, <c>line-insert</c>, <c>line-delete</c>,
/// <c>char-insert</c>, <c>char-delete</c>, <c>findMatch</c>, ...) are used by the editor and cannot be replaced.
/// Threading: thread-safe.
/// </remarks>
public static class CustomDecorationStyles
{
    /// <summary>Adds a host class, or replaces the style of a class already registered.</summary>
    /// <param name="className">The class name: letters, digits, <c>_</c> and <c>-</c>; not a built-in name.</param>
    /// <param name="style">What the class draws.</param>
    /// <exception cref="ArgumentNullException"><paramref name="className"/> or <paramref name="style"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The name is empty, has another character, or is a built-in class name.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A border width is negative or not finite.</exception>
    public static void Register(string className, CustomDecorationStyle style)
    {
        ArgumentNullException.ThrowIfNull(className);
        ArgumentNullException.ThrowIfNull(style);
        if (className.Length == 0 || !IsValidName(className))
        {
            throw new ArgumentException("A class name is made of letters, digits, '_' and '-'.", nameof(className));
        }

        if (DecorationStyles.IsBuiltIn(className))
        {
            throw new ArgumentException($"'{className}' is a built-in class and cannot be replaced.", nameof(className));
        }

        CheckWidth(style.BorderWidth, nameof(style.BorderWidth));
        CheckWidth(style.LeftBorderWidth, nameof(style.LeftBorderWidth));

        DecorationStyles.SetCustom(className, new DecorationStyle(
            BorderWidth: (float)style.BorderWidth,
            LeftBorderWidth: (float)style.LeftBorderWidth,
            ForegroundResolver: Pick(style.Foreground, style.DarkForeground),
            BackgroundResolver: Pick(style.Background, style.DarkBackground),
            BorderResolver: Pick(style.Border, style.DarkBorder)));
    }

    /// <summary>Removes a host class.</summary>
    /// <param name="className">The class name.</param>
    /// <returns><see langword="false"/> when the class was not registered (or is built-in).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="className"/> is <see langword="null"/>.</exception>
    public static bool Unregister(string className)
    {
        ArgumentNullException.ThrowIfNull(className);
        return DecorationStyles.RemoveCustom(className);
    }

    private static bool IsValidName(string name)
    {
        foreach (char c in name)
        {
            if (!(c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_' or '-'))
            {
                return false;
            }
        }

        return true;
    }

    private static void CheckWidth(double width, string paramName)
    {
        if (!double.IsFinite(width) || width < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, width, "A border width is a finite number >= 0.");
        }
    }

    private static Func<EditorTheme, ThemeColor?>? Pick(ThemeColor? light, ThemeColor? dark) =>
        light is null && dark is null
            ? null
            : theme => theme.Type is ThemeType.Dark or ThemeType.HighContrastDark ? dark ?? light : light;
}
