// Ported from microsoft/vscode: src/vs/editor/common/viewModel/viewModelLines.ts (ViewModelLinesFromProjectedModel)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.Events;

namespace LoomEditor.View.Layout;

internal sealed partial class ViewModelLinesFromProjectedModel
{
    /// <summary>Returns whether a setting changed (the lines are built again, reusing the previous breaks when only the column changed).</summary>
    public bool SetWrappingSettings(FontInfo fontInfo, int wrappingColumn, WrappingIndent wrappingIndent, WordBreak wordBreak, bool useTwoCellFullwidthCharacters)
    {
        ArgumentNullException.ThrowIfNull(fontInfo);

        bool equalFontInfo = _fontInfo.Equals(fontInfo);
        bool equalWrappingColumn = _wrappingColumn == wrappingColumn;
        bool equalWrappingIndent = _wrappingIndent == wrappingIndent;
        bool equalWordBreak = _wordBreak == wordBreak;
        bool equalUseTwoCellFullwidthCharacters = _useTwoCellFullwidthCharacters == useTwoCellFullwidthCharacters;
        if (equalFontInfo && equalWrappingColumn && equalWrappingIndent && equalWordBreak && equalUseTwoCellFullwidthCharacters)
        {
            return false;
        }

        bool onlyWrappingColumnChanged = equalFontInfo && !equalWrappingColumn && equalWrappingIndent && equalWordBreak && equalUseTwoCellFullwidthCharacters;

        _fontInfo = fontInfo;
        _wrappingColumn = wrappingColumn;
        _wrappingIndent = wrappingIndent;
        _wordBreak = wordBreak;
        _useTwoCellFullwidthCharacters = useTwoCellFullwidthCharacters;

        ModelLineProjectionData?[]? previousLineBreaks = null;
        if (onlyWrappingColumnChanged)
        {
            previousLineBreaks = new ModelLineProjectionData?[_modelLineProjections.Count];
            for (int i = 0; i < previousLineBreaks.Length; i++)
            {
                previousLineBreaks[i] = _modelLineProjections[i].GetProjectionData();
            }
        }

        ConstructLines(resetHiddenAreas: false, previousLineBreaks);

        return true;
    }

    /// <summary>A line-breaks computer for the current settings; the default context reads the model and the injected text of this editor.</summary>
    public ILineBreaksComputer CreateLineBreaksComputer(ILineBreaksComputerContext? context = null) =>
        _monospaceLineBreaksComputerFactory.CreateLineBreaksComputer(
            context ?? new ModelLineBreaksContext(_model, _editorId),
            _fontInfo,
            _tabSize,
            _wrappingColumn,
            _wrappingIndent,
            _wordBreak,
            _wrapOnEscapedLineFeeds,
            _useTwoCellFullwidthCharacters);

    private sealed class ModelLineBreaksContext(Core.Model.TextModel model, int editorId) : ILineBreaksComputerContext
    {
        public string GetLineContent(int lineNumber) => model.GetLineContent(lineNumber);

        public IReadOnlyList<LineInjectedText>? GetLineInjectedText(int lineNumber) => model.GetLineInjectedText(lineNumber, editorId);
    }
}
