using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout.TestHelpers;

/// <summary>An <see cref="ICharacterWidthReader"/> driven by a delegate; records every request.</summary>
internal sealed class FakeCharacterWidthReader : ICharacterWidthReader
{
    private readonly Func<string, CharacterStyle, double> _width;

    public FakeCharacterWidthReader(Func<string, CharacterStyle, double> width)
    {
        _width = width;
    }

    public List<(string Character, CharacterStyle Style)> Requests { get; } = [];

    /// <summary>Every character is 8.4 wide, full-width characters are twice as wide.</summary>
    public static FakeCharacterWidthReader Monospace()
    {
        return new FakeCharacterWidthReader((c, _) => c == "ｍ" ? 16.8 : 8.4);
    }

    public double GetWidth(string character, CharacterStyle style)
    {
        Requests.Add((character, style));
        return _width(character, style);
    }
}
