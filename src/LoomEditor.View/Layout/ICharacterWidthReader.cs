namespace LoomEditor.View.Layout;

/// <summary>Style of a character whose width is requested. Monaco: <c>CharWidthRequestType</c>.</summary>
internal enum CharacterStyle
{
    Regular,
    Italic,
    Bold,
}

/// <summary>Measures the advance width of a single character with a given font. Implemented with DirectWrite in P2-12.</summary>
/// <remarks>Threading: implementation-defined; <see cref="FontMeasurements"/> calls it from the calling thread only.</remarks>
internal interface ICharacterWidthReader
{
    /// <summary>Width of <paramref name="character"/>, in DIPs, for the font the reader was created for.</summary>
    double GetWidth(string character, CharacterStyle style);
}
