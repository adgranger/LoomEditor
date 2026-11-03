using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using LoomEditor.View.Comments;

namespace LoomEditor.Avalonia.Controls.Comments;

/// <summary>The initials of a comment author in a colored circle.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal static class CommentAvatar
{
    private const double Size = 28;

    // Theme independent: white initials stay readable on each of them in light and dark themes.
    private static readonly Color[] Palette =
    [
        Color.FromArgb(255, 0x1F, 0x6F, 0xB2),
        Color.FromArgb(255, 0x2E, 0x7D, 0x32),
        Color.FromArgb(255, 0x8E, 0x24, 0xAA),
        Color.FromArgb(255, 0xC6, 0x28, 0x28),
        Color.FromArgb(255, 0x00, 0x83, 0x8F),
        Color.FromArgb(255, 0xAD, 0x14, 0x57),
        Color.FromArgb(255, 0x6D, 0x4C, 0x41),
        Color.FromArgb(255, 0x45, 0x5A, 0x64),
    ];

    /// <summary>A 28 px circle with the initials of <paramref name="name"/>.</summary>
    /// <remarks><c>CommentAuthor.AvatarUrl</c> is ignored, as in the WinUI twin: no image is downloaded.</remarks>
    public static Border Create(string name, double fontSize) => new()
    {
        Width = Size,
        Height = Size,
        CornerRadius = new CornerRadius(Size / 2),
        VerticalAlignment = VerticalAlignment.Top,
        Background = new SolidColorBrush(Palette[AvatarInitials.PaletteIndex(name, Palette.Length)]),
        Child = new TextBlock
        {
            Text = AvatarInitials.Get(name),
            Foreground = Brushes.White,
            FontSize = Math.Max(9, fontSize - 2),
            FontWeight = FontWeight.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        },
    };
}
