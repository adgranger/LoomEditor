// Ported from microsoft/vscode: src/vs/editor/test/common/viewModel/monospaceLineBreaksComputer.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text;
using LoomEditor.Core.Decorations;
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

    public static LineInjectedText Injected(int column, string content, double? widthInEm = null, int order = 0) =>
        new(0, 1, column, ModelDecorationInjectedTextOptions.From(new InjectedTextOptions { Content = content, WidthInEm = widthInEm }), order);

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
            FontFeatureSettings: EditorFontOptions.LigaturesOff,
            FontVariationSettings: EditorFontOptions.VariationsOff,
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

    public static void AssertLineBreakDataEqual(ModelLineProjectionData? a, ModelLineProjectionData? b)
    {
        if (a is null || b is null)
        {
            Assert.Equal(a is null, b is null);
            return;
        }

        Assert.Equal(a.BreakOffsets, b.BreakOffsets);
        Assert.Equal(a.WrappedTextIndentLength, b.WrappedTextIndentLength);
        for (int i = 0; i < a.BreakOffsetsVisibleColumn.Length; i++)
        {
            double diff = Math.Abs(a.BreakOffsetsVisibleColumn[i] - b.BreakOffsetsVisibleColumn[i]);
            Assert.True(diff < 0.001);
        }
    }

    public static void AssertIncrementalLineBreaks(
        ILineBreaksComputerFactory factory,
        string text,
        int tabSize,
        int breakAfter1,
        string annotatedText1,
        int breakAfter2,
        string annotatedText2,
        WrappingIndent wrappingIndent = WrappingIndent.None,
        double columnsForFullWidthChar = 2,
        bool useTwoCellFullwidthCharacters = false)
    {
        // sanity check the test
        Assert.Equal(text, ParseAnnotatedText(annotatedText1).Text);
        Assert.Equal(text, ParseAnnotatedText(annotatedText2).Text);

        // check that the direct mapping is ok for 1
        ModelLineProjectionData? directLineBreakData1 = GetLineBreakData(factory, tabSize, breakAfter1, columnsForFullWidthChar, wrappingIndent, WordBreak.Normal, false, text, null, null, useTwoCellFullwidthCharacters);
        Assert.Equal(annotatedText1, ToAnnotatedText(text, directLineBreakData1));

        // check that the direct mapping is ok for 2
        ModelLineProjectionData? directLineBreakData2 = GetLineBreakData(factory, tabSize, breakAfter2, columnsForFullWidthChar, wrappingIndent, WordBreak.Normal, false, text, null, null, useTwoCellFullwidthCharacters);
        Assert.Equal(annotatedText2, ToAnnotatedText(text, directLineBreakData2));

        // check that going from 1 to 2 is ok
        ModelLineProjectionData? lineBreakData2From1 = GetLineBreakData(factory, tabSize, breakAfter2, columnsForFullWidthChar, wrappingIndent, WordBreak.Normal, false, text, directLineBreakData1, null, useTwoCellFullwidthCharacters);
        Assert.Equal(annotatedText2, ToAnnotatedText(text, lineBreakData2From1));
        AssertLineBreakDataEqual(lineBreakData2From1, directLineBreakData2);

        // check that going from 2 to 1 is ok
        ModelLineProjectionData? lineBreakData1From2 = GetLineBreakData(factory, tabSize, breakAfter1, columnsForFullWidthChar, wrappingIndent, WordBreak.Normal, false, text, directLineBreakData2, null, useTwoCellFullwidthCharacters);
        Assert.Equal(annotatedText1, ToAnnotatedText(text, lineBreakData1From2));
        AssertLineBreakDataEqual(lineBreakData1From2, directLineBreakData1);
    }
}
