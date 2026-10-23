using LoomEditor.Core.Text;

namespace LoomEditor.View.Accessibility;

/// <summary>The paragraph unit: every model line is a paragraph.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class ParagraphUnitStarts(AutomationTextDocument document) : AutomationUnitStarts(document)
{
    public override Position StartAtOrBefore(Position position) => new(Document.Validate(position).LineNumber, 1);

    public override Position? PreviousStart(Position position)
    {
        position = Document.Validate(position);
        if (position.Column > 1)
        {
            return new Position(position.LineNumber, 1);
        }

        return position.LineNumber > 1 ? new Position(position.LineNumber - 1, 1) : null;
    }

    public override Position? NextStartStrict(Position position)
    {
        position = Document.Validate(position);
        return position.LineNumber < Document.Model.LineCount ? new Position(position.LineNumber + 1, 1) : null;
    }
}
