// Ported from microsoft/vscode: src/vs/editor/test/common/viewModel/monospaceLineBreaksComputer.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text;
using LoomEditor.Core.Model.Events;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout.TestHelpers;

internal static class LineBreaksTestHelpers
{
    private sealed class TestContext(string text, IReadOnlyList<LineInjectedText>? injectedText) : ILineBreaksComputerContext
    {
        public string GetLineContent(int lineNumber) => text;

        public IReadOnlyList<LineInjectedText>? GetLineInjectedText(int lineNumber) => injectedText;
    }

    public static (string Text, List<int> Indices) ParseAnnotatedText(string annotatedText)
    {
        var text = new StringBuilder();
        int currentLineIndex = 0;
        var indices = new List<int>();
        foreach (char c in annotatedText)
        {
            if (c == '|')
            {
                currentLineIndex++;
            }
            else
            {
                text.Append(c);
                while (indices.Count < text.Length)
                {
                    indices.Add(0);
                }

                indices[text.Length - 1] = currentLineIndex;
            }
        }

        return (text.ToString(), indices);
    }

    public static string ToAnnotatedText(string text, ModelLineProjectionData? lineBreakData)
    {
        // Insert line break markers again, according to algorithm
        if (lineBreakData is null)
        {
            // No wrapping
            return text;
        }

        var actualAnnotatedText = new StringBuilder();
        int previousLineIndex = 0;
        for (int i = 0; i < text.Length; i++)
        {
            OutputPosition r = lineBreakData.TranslateToOutputPosition(i);
            if (previousLineIndex != r.OutputLineIndex)
            {
                previousLineIndex = r.OutputLineIndex;
                actualAnnotatedText.Append('|');
            }

            actualAnnotatedText.Append(text[i]);
        }

        return actualAnnotatedText.ToString();
    }

    public static ModelLineProjectionData? GetLineBreakData(
        ILineBreaksComputerFactory factory,
        int tabSize,
        int breakAfter,
        double columnsForFullWidthChar,
        WrappingIndent wrappingIndent,
        WordBreak wordBreak,
        bool wrapOnEscapedLineFeeds,
        string text,
        ModelLineProjectionData? previousLineBreakData,
        IReadOnlyList<LineInjectedText>? injectedText = null,
        bool useTwoCellFullwidthCharacters = false)
    {
        var fontInfo = new FontInfo(
            PixelRatio: 1,
            FontFamily: "testFontFamily",
            FontWeight: "normal",
            FontSize: 14,
            LineHeight: 19,
            LetterSpacing: 0,
            IsMonospace: true,
            TypicalHalfwidthCharacterWidth: 7,
            TypicalFullwidthCharacterWidth: 7 * columnsForFullWidthChar,
            CanUseHalfwidthRightwardsArrow: true,
            SpaceWidth: 7,
            MiddotWidth: 7,
            WsmiddotWidth: 7,
            MaxDigitWidth: 7);
        var context = new TestContext(text, injectedText);
        ILineBreaksComputer lineBreaksComputer = factory.CreateLineBreaksComputer(context, fontInfo, tabSize, breakAfter, wrappingIndent, wordBreak, wrapOnEscapedLineFeeds, useTwoCellFullwidthCharacters);
        var previousLineBreakDataClone = previousLineBreakData is null
            ? null
            : new ModelLineProjectionData(null, null, [.. previousLineBreakData.BreakOffsets], [.. previousLineBreakData.BreakOffsetsVisibleColumn], previousLineBreakData.WrappedTextIndentLength);
        lineBreaksComputer.AddRequest(1, previousLineBreakDataClone);
        return lineBreaksComputer.Finalize()[0];
    }

    public static ModelLineProjectionData? AssertLineBreaks(
        ILineBreaksComputerFactory factory,
        int tabSize,
        int breakAfter,
        string annotatedText,
        WrappingIndent wrappingIndent = WrappingIndent.None,
        WordBreak wordBreak = WordBreak.Normal)
    {
        // Create version of `annotatedText` with line break markers removed
        string text = ParseAnnotatedText(annotatedText).Text;
        ModelLineProjectionData? lineBreakData = GetLineBreakData(factory, tabSize, breakAfter, 2, wrappingIndent, wordBreak, false, text, null);
        string actualAnnotatedText = ToAnnotatedText(text, lineBreakData);

        Assert.Equal(annotatedText, actualAnnotatedText);

        return lineBreakData;
    }
}
