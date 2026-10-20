// Ported from microsoft/vscode: src/vs/editor/common/model/textModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.Core.Model;

// Indentation options. There is no detectIndentation creation option: DetectIndentation applies the guess on demand.
public sealed partial class TextModel
{
    private TextModelOptions _options;

    /// <summary>Raised when <see cref="UpdateOptions"/> changed at least one option (Monaco: <c>onDidChangeOptions</c>).</summary>
    public event EventHandler<ModelOptionsChangedEventArgs>? OptionsChanged;

    /// <summary>The indentation and line-ending options (Monaco: <c>getOptions</c>).</summary>
    public TextModelOptions Options => _options;

    /// <summary>Changes some options; an option left <see langword="null"/> keeps its value (Monaco: <c>updateOptions</c>).</summary>
    /// <remarks>Does nothing when the resulting options equal the current ones; otherwise raises <see cref="OptionsChanged"/>.</remarks>
    public void UpdateOptions(TextModelUpdateOptions newOptions)
    {
        int tabSize = newOptions.TabSize ?? _options.TabSize;

        // Monaco: indentSize ?? originalIndentSize, where originalIndentSize is 'tabSize' or the number.
        int? indentSize;
        if (newOptions.IndentSize is not null)
        {
            indentSize = newOptions.IndentSize;
        }
        else if (newOptions.IndentSizeIsTabSize || _options.IndentSizeIsTabSize)
        {
            indentSize = null;
        }
        else
        {
            indentSize = _options.IndentSize;
        }

        bool insertSpaces = newOptions.InsertSpaces ?? _options.InsertSpaces;
        bool trimAutoWhitespace = newOptions.TrimAutoWhitespace ?? _options.TrimAutoWhitespace;

        var resolved = new TextModelOptions(tabSize, indentSize, insertSpaces, trimAutoWhitespace, _options.DefaultEol);

        if (_options.Equals(resolved))
        {
            return;
        }

        ModelOptionsChangedEventArgs e = _options.CreateChangeEvent(resolved);
        _options = resolved;
        OptionsChanged?.Invoke(this, e);
    }

    // Monaco: detectIndentation. Monaco's TODO: guess indentSize independent of tabSize.
    internal void DetectIndentation(bool defaultInsertSpaces, int defaultTabSize)
    {
        GuessedIndentation guessedIndentation = IndentationGuesser.GuessIndentation(_buffer, defaultTabSize, defaultInsertSpaces);
        UpdateOptions(new TextModelUpdateOptions(
            TabSize: guessedIndentation.TabSize,
            IndentSize: guessedIndentation.TabSize,
            InsertSpaces: guessedIndentation.InsertSpaces));
    }

    // Monaco: normalizeIndentation.
    internal string NormalizeIndentation(string str) =>
        Indentation.NormalizeIndentation(str, _options.IndentSize, _options.InsertSpaces);

    // Monaco: normalizePosition. The model has no injected text, so the position is already normalized. Static (CA1822): no model state is needed.
    internal static Position NormalizePosition(Position position, PositionAffinity affinity) => position;

    // Monaco: getLineIndentColumn + indentOfLine. Spaces and tabs are single code units, so a loop over chars is identical.
    internal int GetLineIndentColumn(int lineNumber)
    {
        string line = GetLineContent(lineNumber);
        int indent = 0;
        while (indent < line.Length && (line[indent] == ' ' || line[indent] == '\t'))
        {
            indent++;
        }

        // Columns start with 1.
        return indent + 1;
    }
}
