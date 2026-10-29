// Ported from microsoft/vscode: src/vs/editor/common/viewLayout/viewLineRenderer.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>
/// The characters a line is drawn with when it holds control characters (Monaco: <c>isControlCharacter</c> and the
/// substitution of <c>renderViewLine</c>). Behavior difference: the directional formatting characters are not
/// rendered as <c>[U+202A]</c> (ADR 0013, decision 8).
/// </summary>
/// <remarks>Threading: pure, thread-safe.</remarks>
internal static class ControlCharacters
{
    // Numeric constants: a literal U+2028 or U+2029 in the source would end the line.
    private const char ReplacementCharacter = (char)0xFFFD;
    private const char DelPicture = (char)0x2421;
    private const char Bom = (char)0xFEFF;
    private const char LineSeparator = (char)0x2028;
    private const char ParagraphSeparator = (char)0x2029;
    private const char NextLine = (char)0x0085;
    private const char FirstPicture = (char)0x2400;
    private const char LastPicture = (char)0x241F;

    /// <summary>
    /// The text to lay out: control characters become their control pictures (when <paramref name="renderControlCharacters"/>)
    /// and the BOM, the line and paragraph separators and the next line character become U+FFFD (always).
    /// It has the same length as <paramref name="text"/>, and is the same instance when nothing changes.
    /// </summary>
    public static string Substitute(string text, bool renderControlCharacters)
    {
        ArgumentNullException.ThrowIfNull(text);

        for (int i = 0; i < text.Length; i++)
        {
            if (Replacement(text[i], renderControlCharacters) != text[i])
            {
                return string.Create(text.Length, (text, renderControlCharacters, i), static (span, state) =>
                {
                    state.text.AsSpan().CopyTo(span);
                    for (int j = state.i; j < span.Length; j++)
                    {
                        span[j] = Replacement(span[j], state.renderControlCharacters);
                    }
                });
            }
        }

        return text;
    }

    /// <summary>Whether <paramref name="c"/> is a control picture that <see cref="Substitute"/> produces (U+2400 to U+241F, U+2421).</summary>
    public static bool IsControlPicture(char c) => c is >= FirstPicture and <= LastPicture or DelPicture;

    private static char Replacement(char c, bool renderControlCharacters) => c switch
    {
        Bom or LineSeparator or ParagraphSeparator or NextLine => ReplacementCharacter,
        '\t' => c,
        < ' ' when renderControlCharacters => (char)(FirstPicture + c),
        (char)0x7F when renderControlCharacters => DelPicture,
        _ => c,
    };
}
