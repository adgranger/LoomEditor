using LoomEditor.View.Comments;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.WinUI.Controls.Comments;

/// <summary>The initials of a comment author in a colored circle.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal static class CommentAvatar
{
    private const double Size = 28;

    // Theme independent: white initials stay readable on each of them in light and dark themes.
    private static readonly Windows.UI.Color[] Palette =
    [
        Windows.UI.Color.FromArgb(255, 0x1F, 0x6F, 0xB2),
        Windows.UI.Color.FromArgb(255, 0x2E, 0x7D, 0x32),
        Windows.UI.Color.FromArgb(255, 0x8E, 0x24, 0xAA),
        Windows.UI.Color.FromArgb(255, 0xC6, 0x28, 0x28),
        Windows.UI.Color.FromArgb(255, 0x00, 0x83, 0x8F),
        Windows.UI.Color.FromArgb(255, 0xAD, 0x14, 0x57),
        Windows.UI.Color.FromArgb(255, 0x6D, 0x4C, 0x41),
        Windows.UI.Color.FromArgb(255, 0x45, 0x5A, 0x64),
    ];

    /// <summary>A 28 px circle with the initials of <paramref name="name"/>.</summary>
    /// <remarks><c>CommentAuthor.AvatarUrl</c> is ignored in phase 11: no image is downloaded.</remarks>
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
            Foreground = new SolidColorBrush(Colors.White),
            FontSize = Math.Max(9, fontSize - 2),
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        },
    };
}
