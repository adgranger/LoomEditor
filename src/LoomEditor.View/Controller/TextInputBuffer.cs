namespace LoomEditor.View.Controller;

/// <summary>
/// Turns the characters of <c>CharacterReceived</c> into the text to type (ADR 0006, decision 11: the plain typing path).
/// </summary>
/// <remarks>
/// <para>
/// Rules: <c>'\r'</c> (Enter) gives <c>"\n"</c> (Monaco types <c>"\n"</c>, the model converts it to its EOL);
/// control characters below U+0020 and U+007F are ignored (Tab, Backspace and Ctrl+letter also arrive as characters, but
/// they are handled as keys); a high surrogate waits for the next character, and the pair is returned when a low
/// surrogate follows. A lone surrogate (a low surrogate with no high one before it, or a high surrogate followed by
/// anything else) is dropped; the character that follows it is still processed.
/// </para>
/// <para>Threading: UI thread only; keeps the pending high surrogate between calls.</para>
/// </remarks>
internal sealed class TextInputBuffer
{
    private char _pendingHighSurrogate;

    /// <summary>Gives the next character; returns the text to type, or <c>null</c> if there is nothing to type yet.</summary>
    public string? Accept(char ch)
    {
        char high = _pendingHighSurrogate;
        _pendingHighSurrogate = '\0';

        if (char.IsLowSurrogate(ch))
        {
            // A low surrogate completes a pending high one; on its own it is dropped.
            return high != '\0' ? string.Concat(high.ToString(), ch.ToString()) : null;
        }

        // Anything else: a pending high surrogate was alone and is dropped.
        if (char.IsHighSurrogate(ch))
        {
            _pendingHighSurrogate = ch;
            return null;
        }

        if (ch == '\r')
        {
            return "\n";
        }

        if (ch < ' ' || ch == '\u007F')
        {
            return null;
        }

        return ch.ToString();
    }
}
