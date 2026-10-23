using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout.TestHelpers;

/// <summary>Wraps a real factory and records the <c>previousLineBreakData</c> of every request, in order.</summary>
/// <remarks>Threading: use from one test thread.</remarks>
internal sealed class RecordingLineBreaksComputerFactory(ILineBreaksComputerFactory inner) : ILineBreaksComputerFactory
{
    /// <summary>The previous break data received by <c>AddRequest</c> since the last <see cref="Clear"/>.</summary>
    public List<ModelLineProjectionData?> PreviousLineBreaks { get; } = [];

    public void Clear() => PreviousLineBreaks.Clear();

    public ILineBreaksComputer CreateLineBreaksComputer(
        ILineBreaksComputerContext context,
        FontInfo fontInfo,
        int tabSize,
        int wrappingColumn,
        WrappingIndent wrappingIndent,
        WordBreak wordBreak,
        bool wrapOnEscapedLineFeeds,
        bool useTwoCellFullwidthCharacters) =>
        new Recording(
            PreviousLineBreaks,
            inner.CreateLineBreaksComputer(context, fontInfo, tabSize, wrappingColumn, wrappingIndent, wordBreak, wrapOnEscapedLineFeeds, useTwoCellFullwidthCharacters));

    private sealed class Recording(List<ModelLineProjectionData?> log, ILineBreaksComputer computer) : ILineBreaksComputer
    {
        public void AddRequest(int lineNumber, ModelLineProjectionData? previousLineBreakData)
        {
            log.Add(previousLineBreakData);
            computer.AddRequest(lineNumber, previousLineBreakData);
        }

        public ModelLineProjectionData?[] Finalize() => computer.Finalize();
    }
}
