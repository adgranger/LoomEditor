using LoomEditor.Core.Text;

namespace LoomEditor.View.Accessibility;

/// <summary>
/// The word (and format) unit: a run of regular characters or a run of word separators, followed by the whitespace that trails it;
/// whitespace at the start of a line is a word of its own. Classes come from Monaco's <see cref="WordCharacterClassifier"/> with the default separators.
/// Surrogate pairs are two regular units and CJK text is one regular run (no <c>Intl.Segmenter</c>, see <see cref="WordCharacterClassifier"/>).
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class WordUnitStarts(AutomationTextDocument document) : AutomationUnitStarts(document)
{
    private static readonly WordCharacterClassifier Classifier = new(WordHelper.UsualWordSeparators, []);

    public override Position StartAtOrBefore(Position position)
    {
        position = Document.Validate(position);
        var starts = GetTokenStartColumns(Document.Model.GetLineContent(position.LineNumber));
        int column = 1;
        foreach (int start in starts)
        {
            if (start > position.Column)
            {
                break;
            }

            column = start;
        }

        return new Position(position.LineNumber, column);
    }

    public override Position? PreviousStart(Position position)
    {
        position = Document.Validate(position);
        var starts = GetTokenStartColumns(Document.Model.GetLineContent(position.LineNumber));
        for (int i = starts.Count - 1; i >= 0; i--)
        {
            if (starts[i] < position.Column)
            {
                return new Position(position.LineNumber, starts[i]);
            }
        }

        if (position.LineNumber == 1)
        {
            return null;
        }

        var previous = GetTokenStartColumns(Document.Model.GetLineContent(position.LineNumber - 1));
        return new Position(position.LineNumber - 1, previous[^1]);
    }

    public override Position? NextStartStrict(Position position)
    {
        position = Document.Validate(position);
        foreach (int start in GetTokenStartColumns(Document.Model.GetLineContent(position.LineNumber)))
        {
            if (start > position.Column)
            {
                return new Position(position.LineNumber, start);
            }
        }

        return position.LineNumber < Document.Model.LineCount ? new Position(position.LineNumber + 1, 1) : null;
    }

    // The 1-based start columns of the tokens of a line; always at least one (column 1).
    private static List<int> GetTokenStartColumns(string content)
    {
        var starts = new List<int> { 1 };
        for (int i = 1; i < content.Length; i++)
        {
            var current = Classifier.Get(content[i]);
            if (current != WordCharacterClass.Whitespace && current != Classifier.Get(content[i - 1]))
            {
                starts.Add(i + 1);
            }
        }

        return starts;
    }
}
