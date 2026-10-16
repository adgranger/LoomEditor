// Ported from microsoft/vscode: src/vs/editor/contrib/wordOperations/test/browser/wordTestUtils.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text;
using LoomEditor.Core.Text;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Cursors.TestHelpers;

/// <summary>Helpers of the word operation tests: positions written as pipes in the text.</summary>
internal static class WordTestUtils
{
    public static (string Text, Position[] Positions) DeserializePipePositions(string text)
    {
        var resultText = new StringBuilder();
        int lineNumber = 1;
        int charIndex = 0;
        var positions = new List<Position>();
        foreach (char chr in text)
        {
            if (chr == '\n')
            {
                resultText.Append(chr);
                lineNumber++;
                charIndex = 0;
                continue;
            }

            if (chr == '|')
            {
                positions.Add(new Position(lineNumber, charIndex + 1));
            }
            else
            {
                resultText.Append(chr);
                charIndex++;
            }
        }

        return (resultText.ToString(), positions.ToArray());
    }

    public static string SerializePipePositions(string text, IReadOnlyList<Position> positionList)
    {
        // Monaco sorts the array in place with Position.compare (a stable sort) and consumes it with shift().
        var positions = new Queue<Position>(positionList.OrderBy(p => p));
        var resultText = new StringBuilder();
        int lineNumber = 1;
        int charIndex = 0;
        foreach (char chr in text)
        {
            if (positions.Count > 0 && positions.Peek().LineNumber == lineNumber && positions.Peek().Column == charIndex + 1)
            {
                resultText.Append('|');
                positions.Dequeue();
            }

            resultText.Append(chr);
            if (chr == '\n')
            {
                lineNumber++;
                charIndex = 0;
            }
            else
            {
                charIndex++;
            }
        }

        if (positions.Count > 0 && positions.Peek().LineNumber == lineNumber && positions.Peek().Column == charIndex + 1)
        {
            resultText.Append('|');
            positions.Dequeue();
        }

        if (positions.Count > 0)
        {
            throw new InvalidOperationException("Unexpected left over positions!!!");
        }

        return resultText.ToString();
    }

    public static List<Position> TestRepeatedActionAndExtractPositions(
        string text,
        Position initialPosition,
        Action<TestCodeEditor> action,
        Func<TestCodeEditor, Position> record,
        Func<TestCodeEditor, bool> stopCondition,
        TestCodeEditorOptions? options = null)
    {
        var actualStops = new List<Position>();
        TestCodeEditor.WithTestCodeEditor(
            text,
            editor =>
            {
                // Monaco: editor.setPosition(initialPosition), validated by the model
                editor.ViewModel.SetSelections("test", [new Selection(initialPosition, initialPosition)]);
                while (true)
                {
                    action(editor);
                    actualStops.Add(record(editor));
                    if (stopCondition(editor))
                    {
                        break;
                    }

                    if (actualStops.Count > 1000)
                    {
                        throw new InvalidOperationException($"Endless loop detected involving position {editor.ViewModel.GetPosition()}!");
                    }
                }
            },
            options);
        return actualStops;
    }
}
