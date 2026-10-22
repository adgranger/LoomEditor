// Ported from microsoft/vscode: src/vs/editor/test/common/viewModel/inlineDecorations.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout;

public class InjectedTextInlineDecorationsComputerTests
{
    private sealed record TestContext(
        IReadOnlyList<ModelDecorationInjectedTextOptions>? InjectionOptions,
        IReadOnlyList<int>? InjectionOffsets,
        IReadOnlyList<int> BreakOffsets,
        int WrappedTextIndentLength = 0,
        int BaseViewLineNumber = 1) : IInjectedTextInlineDecorationsComputerContext
    {
        public IReadOnlyList<ModelDecorationInjectedTextOptions>? GetInjectionOptions(int modelLineNumber) => InjectionOptions;

        public IReadOnlyList<int>? GetInjectionOffsets(int modelLineNumber) => InjectionOffsets;

        public IReadOnlyList<int> GetBreakOffsets(int modelLineNumber) => BreakOffsets;

        public int GetWrappedTextIndentLength(int modelLineNumber) => WrappedTextIndentLength;

        public int GetBaseViewLineNumber(int modelLineNumber) => BaseViewLineNumber;
    }

    private static readonly InlineDecoration[] None = [];

    private static ModelDecorationInjectedTextOptions Inj(string content, string? cls = null, bool letterSpacing = false, double? widthInEm = null) =>
        ModelDecorationInjectedTextOptions.From(new InjectedTextOptions
        {
            Content = content,
            InlineClassName = cls,
            InlineClassNameAffectsLetterSpacing = letterSpacing,
            WidthInEm = widthInEm,
        });

    private static InlineDecoration D(int line, int startColumn, int endColumn, string cls, InlineDecorationType type = InlineDecorationType.Regular) =>
        new(new Range(line, startColumn, line, endColumn), cls, type);

    private static TestContext Ctx(ModelDecorationInjectedTextOptions[] options, int[] offsets, int[] breaks, int indent = 0, int baseLine = 1) =>
        new(options, offsets, breaks, indent, baseLine);

    private static void Check(TestContext context, int modelLineNumber, params InlineDecoration[][] expected)
    {
        IReadOnlyList<IReadOnlyList<InlineDecoration>> result = new InjectedTextInlineDecorationsComputer(context).GetInlineDecorations(modelLineNumber);
        Assert.Equal(expected, result.Select(l => l.ToArray()).ToArray());
    }

    // Monaco: 'no injections returns empty'
    [Fact]
    public void NoInjectionsReturnsEmpty() => Check(new TestContext(null, null, [10]), 1);

    // Monaco: 'single injection with inlineClassName on a single output line'
    [Fact]
    public void SingleInjectionWithInlineClassNameOnASingleOutputLine() =>
        Check(Ctx([Inj("injected", "injected-class")], [5], [18]), 1, [D(1, 6, 14, "injected-class")]);

    // Monaco: 'injection without inlineClassName produces no inline decorations'
    [Fact]
    public void InjectionWithoutInlineClassNameProducesNoInlineDecorations() =>
        Check(Ctx([Inj("injected")], [5], [18]), 1, None);

    // Monaco: 'fixed width injection uses its regular inline decoration'
    [Fact]
    public void FixedWidthInjectionUsesItsRegularInlineDecoration() =>
        Check(Ctx([Inj(" ", "fixed-width", widthInEm: 3)], [5], [11]), 1, [D(1, 6, 7, "fixed-width")]);

    // Monaco: 'injection with inlineClassNameAffectsLetterSpacing'
    [Fact]
    public void InjectionWithInlineClassNameAffectsLetterSpacing() =>
        Check(Ctx([Inj("abc", "ls-class", letterSpacing: true)], [0], [13]), 1,
            [D(1, 1, 4, "ls-class", InlineDecorationType.RegularAffectingLetterSpacing)]);

    // Monaco: 'multiple injections on a single output line'
    [Fact]
    public void MultipleInjectionsOnASingleOutputLine() =>
        Check(Ctx([Inj("AA", "class-a"), Inj("BBB", "class-b")], [2, 5], [15]), 1,
            [D(1, 3, 5, "class-a"), D(1, 8, 11, "class-b")]);

    // Monaco: 'injection spanning across wrapped lines'
    [Fact]
    public void InjectionSpanningAcrossWrappedLines() =>
        Check(Ctx([Inj("1234567890", "injected")], [8], [15, 30], baseLine: 5), 1,
            [D(5, 9, 16, "injected")], [D(6, 1, 4, "injected")]);

    // Monaco: 'injection with wrappedTextIndentLength on wrapped lines'
    [Fact]
    public void InjectionWithWrappedTextIndentLengthOnWrappedLines() =>
        Check(Ctx([Inj("12345678901234567890", "injected")], [0], [15, 30], indent: 4), 1,
            [D(1, 1, 16, "injected")], [D(2, 5, 10, "injected")]);

    // Monaco: 'injection starting in later wrapped line'
    [Fact]
    public void InjectionStartingInLaterWrappedLine() =>
        Check(Ctx([Inj("ab", "late-class")], [20], [15, 32]), 1, None, [D(2, 6, 8, "late-class")]);

    // Monaco: 'base view line number offsets correctly'
    [Fact]
    public void BaseViewLineNumberOffsetsCorrectly() =>
        Check(Ctx([Inj("test", "test-class")], [0], [14], baseLine: 10), 1, [D(10, 1, 5, "test-class")]);

    // Monaco: 'range uses view line number, not model line number'
    [Fact]
    public void RangeUsesViewLineNumber_NotModelLineNumber() =>
        Check(Ctx([Inj("ghost", "ghost-class")], [0], [15], baseLine: 7), 3, [D(7, 1, 6, "ghost-class")]);

    // Monaco: 'range uses view line number on wrapped lines, not model line number'
    [Fact]
    public void RangeUsesViewLineNumberOnWrappedLines_NotModelLineNumber() =>
        Check(Ctx([Inj("1234567890", "wrap-class")], [0], [8, 20], baseLine: 5), 2,
            [D(5, 1, 9, "wrap-class")], [D(6, 1, 3, "wrap-class")]);

    // Monaco: 'spacing-only injection in the middle of a line'
    [Fact]
    public void SpacingOnlyInjectionInTheMiddleOfALine() =>
        Check(Ctx([Inj("", "spacer", widthInEm: 1)], [5], [10]), 1, [D(1, 6, 6, "spacer", InlineDecorationType.WidthOnly)]);

    // Monaco: 'spacing-only injection at the beginning of a line'
    [Fact]
    public void SpacingOnlyInjectionAtTheBeginningOfALine() =>
        Check(Ctx([Inj("", "spacer", widthInEm: 1)], [0], [10]), 1, [D(1, 1, 1, "spacer", InlineDecorationType.WidthOnly)]);

    // Monaco: 'spacing-only injection on an empty line'
    [Fact]
    public void SpacingOnlyInjectionOnAnEmptyLine() =>
        Check(Ctx([Inj("", "spacer", widthInEm: 1)], [0], [0]), 1, [D(1, 1, 1, "spacer", InlineDecorationType.WidthOnly)]);

    // Monaco: 'spacing-only injection at a wrap boundary belongs to the following line'
    [Fact]
    public void SpacingOnlyInjectionAtAWrapBoundaryBelongsToTheFollowingLine() =>
        Check(Ctx([Inj("", "spacer", widthInEm: 1)], [5], [5, 10]), 1, None, [D(2, 1, 1, "spacer", InlineDecorationType.WidthOnly)]);

    // Monaco: 'spacing-only injection at the end of a line'
    [Fact]
    public void SpacingOnlyInjectionAtTheEndOfALine() =>
        Check(Ctx([Inj("", "spacer", widthInEm: 1)], [10], [10]), 1, [D(1, 11, 11, "spacer", InlineDecorationType.WidthOnly)]);

    // Monaco: 'spacing-only injection next to an injection with content'
    [Fact]
    public void SpacingOnlyInjectionNextToAnInjectionWithContent() =>
        Check(Ctx([Inj("", "spacer", widthInEm: 1), Inj("AB", "text-class")], [2, 5], [12]), 1,
            [D(1, 3, 3, "spacer", InlineDecorationType.WidthOnly), D(1, 6, 8, "text-class")]);

    // Monaco: 'spacing-only injection without inlineClassName produces no inline decorations'
    [Fact]
    public void SpacingOnlyInjectionWithoutInlineClassNameProducesNoInlineDecorations() =>
        Check(Ctx([Inj("", widthInEm: 1)], [5], [10]), 1, None);
}
