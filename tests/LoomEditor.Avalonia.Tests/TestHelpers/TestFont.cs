using LoomEditor.Avalonia.Rendering;
using LoomEditor.Core.Tokens;
using LoomEditor.Rendering;
using LoomEditor.View.Layout;

namespace LoomEditor.Avalonia.Tests.TestHelpers;

/// <summary>The embedded test font (JetBrains Mono, with ligatures) and measurers built on it.</summary>
internal static class TestFont
{
    public const string Family = "avares://LoomEditor.Avalonia.Tests/TestFonts#JetBrains Mono";

    public static AvaloniaTextMeasurer CreateMeasurer(int tabSize = 4, string fontFeatureSettings = EditorFontOptions.LigaturesOff) =>
        new(Family, 14, tabSize, 1, new EditorFontOptions(FontFeatureSettings: fontFeatureSettings));

    /// <summary>A style that gives the whole of a <paramref name="length"/>-character line one font style.</summary>
    public static LineStyle Style(int length, FontStyle fontStyle)
    {
        uint metadata = (3u << MetadataConsts.ForegroundOffset) | ((uint)fontStyle << MetadataConsts.FontStyleOffset);
        LineTokens tokens = LineTokens.CreateFromTextAndMetadata([(new string('x', length), metadata)], new LanguageIdCodec());
        return LineStyle.Create(tokens, length, [0, 0xFFCCCCCC, 0xFF1F1F1F, 0xFFFF0000], 0xFFCCCCCC);
    }
}
