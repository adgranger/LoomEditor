using LoomEditor.Core.Text;

namespace LoomEditor.View.Accessibility;

/// <summary>The character unit: a grapheme cluster, or a line break (one character, whatever the model's EOL).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class CharacterUnitStarts(AutomationTextDocument document) : AutomationUnitStarts(document)
{
    public override Position StartAtOrBefore(Position position)
    {
        position = Document.Validate(position);
        int line = position.LineNumber;
        int offset = position.Column - 1;
        string content = Document.Model.GetLineContent(line);
        if (offset < content.Length)
        {
            return new Position(line, Strings.GetCharContainingOffset(content, offset).Start + 1);
        }

        if (line < Document.Model.LineCount)
        {
            return position;
        }

        if (content.Length > 0)
        {
            return new Position(line, content.Length - Strings.PrevCharLength(content, content.Length) + 1);
        }

        return line > 1 ? new Position(line - 1, Document.Model.GetLineMaxColumn(line - 1)) : Document.DocumentStart;
    }

    public override Position? PreviousStart(Position position)
    {
        position = Document.Validate(position);
        int line = position.LineNumber;
        int offset = position.Column - 1;
        if (offset > 0)
        {
            string content = Document.Model.GetLineContent(line);
            if (offset < content.Length)
            {
                int start = Strings.GetCharContainingOffset(content, offset).Start;
                if (start < offset)
                {
                    // inside a grapheme: its own start is the greatest start before the position
                    return new Position(line, start + 1);
                }
            }

            return new Position(line, offset - Strings.PrevCharLength(content, offset) + 1);
        }

        return line > 1 ? new Position(line - 1, Document.Model.GetLineMaxColumn(line - 1)) : null;
    }

    public override Position? NextStartStrict(Position position)
    {
        position = Document.Validate(position);
        int line = position.LineNumber;
        int offset = position.Column - 1;
        int lineCount = Document.Model.LineCount;
        string content = Document.Model.GetLineContent(line);
        if (offset < content.Length)
        {
            int next = Strings.GetCharContainingOffset(content, offset).End;
            if (next < content.Length)
            {
                return new Position(line, next + 1);
            }

            return line < lineCount ? new Position(line, content.Length + 1) : null;
        }

        return line < lineCount && FirstStartOfLine(line + 1) is { } start ? start : null;
    }

    // (L, 1) when the line has content or is followed by a line break (a character); null for an empty last line.
    private Position? FirstStartOfLine(int line) =>
        Document.Model.GetLineLength(line) > 0 || line < Document.Model.LineCount ? new Position(line, 1) : null;
}
