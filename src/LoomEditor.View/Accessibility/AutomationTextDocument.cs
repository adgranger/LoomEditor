using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Accessibility;

/// <summary>What UI Automation sees of the editor: the model text as <c>\n</c>-joined lines.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class AutomationTextDocument
{
    public AutomationTextDocument(ViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ViewModel = viewModel;
    }

    /// <summary>The view model the document reads from.</summary>
    public ViewModel ViewModel { get; }

    /// <summary>The text model.</summary>
    public TextModel Model => ViewModel.Model;

    /// <summary>The start of the text: <c>(1, 1)</c>.</summary>
    public Position DocumentStart { get; } = new(1, 1);

    /// <summary>The end of the last line, read from the model each time.</summary>
    public Position DocumentEnd => new(Model.LineCount, Model.GetLineMaxColumn(Model.LineCount));

    /// <summary>The whole text.</summary>
    public Range DocumentRange => new(DocumentStart, DocumentEnd);

    /// <summary>Clamps <paramref name="range"/> to the model.</summary>
    public Range Validate(Range range) => Model.ValidateRange(range);

    /// <summary>Clamps <paramref name="position"/> to the model.</summary>
    public Position Validate(Position position) => Model.ValidatePosition(position);

    /// <summary>
    /// The position reached by walking <paramref name="textOffset"/> UTF-16 units of the LF-joined text from <paramref name="start"/>
    /// (a line break counts as one unit); an offset past the end gives <see cref="DocumentEnd"/>.
    /// </summary>
    public Position PositionAfter(Position start, int textOffset)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(textOffset);
        start = Validate(start);
        int line = start.LineNumber;
        int col = start.Column;
        int remaining = textOffset;
        while (true)
        {
            int available = Model.GetLineMaxColumn(line) - col;
            if (remaining <= available)
            {
                return new Position(line, col + remaining);
            }

            if (line == Model.LineCount)
            {
                return DocumentEnd;
            }

            remaining -= available + 1;
            line++;
            col = 1;
        }
    }

    /// <summary>
    /// The text of <paramref name="range"/> with <c>\n</c> line breaks. <paramref name="maxLength"/> is -1 (everything) or a maximum in UTF-16 units;
    /// a cut between the two halves of a surrogate pair drops the high surrogate too.
    /// </summary>
    public string GetText(Range range, int maxLength = -1)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, -1);
        string text = Model.GetValueInRange(Validate(range), EndOfLinePreference.LF);
        if (maxLength == -1 || text.Length <= maxLength)
        {
            return text;
        }

        // shortcut: reads the whole range even for a small maxLength, optimize if huge ranges are queried often
        if (maxLength > 0 && char.IsHighSurrogate(text[maxLength - 1]))
        {
            maxLength--;
        }

        return text[..maxLength];
    }
}
