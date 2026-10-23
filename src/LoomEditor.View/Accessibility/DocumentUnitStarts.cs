using LoomEditor.Core.Text;

namespace LoomEditor.View.Accessibility;

/// <summary>The document unit: the only start is the document start.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class DocumentUnitStarts(AutomationTextDocument document) : AutomationUnitStarts(document)
{
    public override Position StartAtOrBefore(Position position)
    {
        Document.Validate(position);
        return Document.DocumentStart;
    }

    public override Position? PreviousStart(Position position) =>
        Document.Validate(position) > Document.DocumentStart ? Document.DocumentStart : null;

    public override Position? NextStartStrict(Position position)
    {
        Document.Validate(position);
        return null;
    }
}
