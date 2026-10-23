// Ported from microsoft/vscode: src/vs/editor/common/modelLineProjectionData.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.Events;

namespace LoomEditor.View.Layout;

/// <summary>What a line-breaks computer reads from the model (Monaco: <c>ILineBreaksComputerContext</c>).</summary>
/// <remarks>Threading: implementation defined; the view model calls it on the UI thread.</remarks>
internal interface ILineBreaksComputerContext
{
    string GetLineContent(int lineNumber);

    IReadOnlyList<LineInjectedText>? GetLineInjectedText(int lineNumber);
}

/// <summary>Creates line-breaks computers (Monaco: <c>ILineBreaksComputerFactory</c>).</summary>
/// <remarks>Threading: implementation defined; the view model calls it on the UI thread.</remarks>
internal interface ILineBreaksComputerFactory
{
    ILineBreaksComputer CreateLineBreaksComputer(
        ILineBreaksComputerContext context,
        FontInfo fontInfo,
        int tabSize,
        int wrappingColumn,
        WrappingIndent wrappingIndent,
        WordBreak wordBreak,
        bool wrapOnEscapedLineFeeds,
        bool useTwoCellFullwidthCharacters);
}

/// <summary>Collects line numbers, then breaks them all at once (Monaco: <c>ILineBreaksComputer</c>).</summary>
/// <remarks>Threading: not thread-safe; single use per <see cref="Finalize"/>.</remarks>
internal interface ILineBreaksComputer
{
    /// <summary>Pass in <paramref name="previousLineBreakData"/> if the only difference is in breaking columns!!!</summary>
    void AddRequest(int lineNumber, ModelLineProjectionData? previousLineBreakData);

    ModelLineProjectionData?[] Finalize();
}
