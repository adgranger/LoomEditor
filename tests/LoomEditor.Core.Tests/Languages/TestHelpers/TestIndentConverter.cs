using LoomEditor.Core.Languages;

namespace LoomEditor.Core.Tests.Languages.TestHelpers;

/// <summary>An <see cref="IIndentConverter"/> for tests without a cursor configuration: one level is <c>indentSize</c> spaces.</summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed class TestIndentConverter(int indentSize = 4) : IIndentConverter
{
    private readonly string _indent = new(' ', indentSize);

    public string ShiftIndent(string indentation) => indentation + _indent;

    public string UnshiftIndent(string indentation)
    {
        if (indentation.EndsWith(_indent, StringComparison.Ordinal))
        {
            return indentation[..^indentSize];
        }

        if (indentation.EndsWith('\t'))
        {
            return indentation[..^1];
        }

        return indentation;
    }
}
