namespace LoomEditor.Rendering.Tests.TestHelpers;

/// <summary>An <see cref="ILineLayout"/> with a fixed width per character; <c>\t</c> jumps to the next tab stop.</summary>
/// <remarks>Threading: not thread-safe; one test, one thread.</remarks>
internal sealed class FakeLineLayout(string text, double characterWidth, int tabSize) : ILineLayout
{
    public string Text { get; } = text;

    public bool IsDisposed { get; private set; }

    public LineStyle Style { get; init; } = LineStyle.Plain;

    public double Width => GetCharacterX(Text.Length);

    public double GetCharacterX(int index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, Text.Length);

        double tabStop = tabSize * characterWidth;
        double x = 0;
        for (int i = 0; i < index; i++)
        {
            x = Text[i] == '\t' ? (Math.Floor(x / tabStop) + 1) * tabStop : x + characterWidth;
        }

        return x;
    }

    public int HitTest(double x)
    {
        // The boundary nearest to x; a point in the middle of a character goes to the right boundary.
        int best = 0;
        double bestDistance = Math.Abs(x - GetCharacterX(0));
        for (int index = 1; index <= Text.Length; index++)
        {
            if (char.IsHighSurrogate(Text[index - 1]))
            {
                continue;
            }

            double distance = Math.Abs(x - GetCharacterX(index));
            if (distance <= bestDistance)
            {
                best = index;
                bestDistance = distance;
            }
        }

        return best;
    }

    public void Dispose() => IsDisposed = true;
}
