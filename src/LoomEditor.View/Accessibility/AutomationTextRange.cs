using LoomEditor.Core.Text;

namespace LoomEditor.View.Accessibility;

/// <summary>A mutable range over an <see cref="AutomationTextDocument"/> (UI Automation ranges are moved in place).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed partial class AutomationTextRange
{
    private Range _range;

    public AutomationTextRange(AutomationTextDocument document, Range range)
    {
        ArgumentNullException.ThrowIfNull(document);
        Document = document;
        _range = document.Validate(range);
    }

    /// <summary>The document the range belongs to.</summary>
    public AutomationTextDocument Document { get; }

    /// <summary>The range, re-validated against the model on every read.</summary>
    public Range Range => _range = Document.Validate(_range);

    /// <summary>True when start equals end.</summary>
    public bool IsDegenerate => Range.IsEmpty;

    /// <summary>A range over the whole document.</summary>
    public static AutomationTextRange ForDocument(AutomationTextDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return new AutomationTextRange(document, document.DocumentRange);
    }

    internal void Set(Range range)
    {
        _range = Document.Validate(range);
    }

    public AutomationTextRange Clone() => new(Document, Range);

    /// <summary>True when <paramref name="other"/> is on the same document and has the same endpoints.</summary>
    public bool Compare(AutomationTextRange other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return ReferenceEquals(Document, other.Document) && Range.Start == other.Range.Start && Range.End == other.Range.End;
    }

    /// <summary>-1, 0 or 1: this endpoint compared with the endpoint of <paramref name="other"/>.</summary>
    public int CompareEndpoints(AutomationTextEndpoint endpoint, AutomationTextRange other, AutomationTextEndpoint otherEndpoint)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (!ReferenceEquals(Document, other.Document))
        {
            throw new ArgumentException("The ranges belong to different documents.", nameof(other));
        }

        return Math.Sign(Get(endpoint).CompareTo(other.Get(otherEndpoint)));
    }

    public string GetText(int maxLength = -1) => Document.GetText(Range, maxLength);

    /// <summary>
    /// Finds <paramref name="text"/> (ordinal) inside the range, first match going forward or last going backward;
    /// returns a new range over the match, or <see langword="null"/> (also for an empty text).
    /// </summary>
    public AutomationTextRange? FindText(string text, bool backward, bool ignoreCase)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length == 0)
        {
            return null;
        }

        string content = GetText();
        StringComparison comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        int index = backward ? content.LastIndexOf(text, comparison) : content.IndexOf(text, comparison);
        if (index < 0)
        {
            return null;
        }

        Position start = Range.Start;
        return new AutomationTextRange(Document, new Range(Document.PositionAfter(start, index), Document.PositionAfter(start, index + text.Length)));
    }

    private Position Get(AutomationTextEndpoint endpoint) => endpoint == AutomationTextEndpoint.Start ? Range.Start : Range.End;
}
