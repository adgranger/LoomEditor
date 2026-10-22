// Ported from microsoft/vscode: src/vs/editor/common/viewModel/viewModelImpl.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.Events;

namespace LoomEditor.View.Layout;

internal sealed partial class ViewModel
{
    // Monaco: _onConfigurationChanged (the setWrappingSettings block; _updateConfigurationViewLineCount is not ported)
    private void UpdateWrappingSettings()
    {
        EditorWrappingOptions wrapping = _layoutOptions.Wrapping;
        if (wrapping.WordWrapBreakBeforeCharacters != _breakBeforeCharacters || wrapping.WordWrapBreakAfterCharacters != _breakAfterCharacters)
        {
            _breakBeforeCharacters = wrapping.WordWrapBreakBeforeCharacters;
            _breakAfterCharacters = wrapping.WordWrapBreakAfterCharacters;
            _lines.SetLineBreaksComputerFactory(MonospaceLineBreaksComputerFactory.Create(wrapping));
        }

        StableViewport stableViewport = CaptureStableViewport();

        bool lineMappingChanged = false;
        EmitViewEvent(eventsCollector =>
        {
            lineMappingChanged = _lines.SetWrappingSettings(
                _fontInfo,
                LayoutInfo.WrappingColumn,
                wrapping.WrappingIndent,
                wrapping.WordBreak,
                wrapping.UseTwoCellFullwidthCharacters);
            if (lineMappingChanged)
            {
                _cursor.OnLineMappingChanged(eventsCollector);
                _decorations.OnLineMappingChanged();
                ViewLayout.OnFlushed(GetLineCount(), []);
                ViewLayout.OnHeightMaybeChanged();
                stableViewport.RecoverViewportStart(CoordinatesConverter, ViewLayout);
            }
        });

        if (lineMappingChanged)
        {
            UpdateLayoutViewLineCount();
            HandleVisibleLinesChanged();
        }
    }

    // Monaco: the first pass of onDidChangeContentOrInjectedText
    private Queue<ModelLineProjectionData?> ComputeLineBreaks(IReadOnlyList<ModelRawChange> changes)
    {
        ILineBreaksComputer computer = _lines.CreateLineBreaksComputer();
        foreach (ModelRawChange change in changes)
        {
            switch (change)
            {
                case ModelRawLinesInserted inserted:
                    for (int i = 0; i < inserted.Count; i++)
                    {
                        computer.AddRequest(inserted.FromLineNumberPostEdit + i, null);
                    }

                    break;
                case ModelRawLineChanged changed:
                    computer.AddRequest(changed.LineNumberPostEdit, null);
                    break;
            }
        }

        return new Queue<ModelLineProjectionData?>(computer.Finalize());
    }

    // Monaco: ArrayQueue.takeCount
    private static ModelLineProjectionData?[] TakeCount(Queue<ModelLineProjectionData?> queue, int count)
    {
        var result = new ModelLineProjectionData?[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = queue.Dequeue();
        }

        return result;
    }
}
