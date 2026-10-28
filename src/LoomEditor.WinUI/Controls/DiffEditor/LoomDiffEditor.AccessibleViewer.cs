// Ported from microsoft/vscode: src/vs/editor/browser/widget/diffEditor/components/accessibleDiffViewer.ts (AccessibleDiffViewer, AccessibleDiffViewerModelFromEditors), commands.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Diff;
using LoomEditor.Core.Text;
using LoomEditor.View.DiffEditor;
using LoomEditor.View.Features.DiffNavigation;
using LoomEditor.WinUI.Accessibility;
using LoomEditor.WinUI.Controls.DiffEditor;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace LoomEditor.WinUI.Controls;

// The accessible diff viewer: opened by F7 / Shift+F7 or by code, it covers both editors. Not ported: the audio cues and the
// onlyShowAccessibleDiffViewer option.
#pragma warning disable CA1001 // WinUI controls are not IDisposable: disposable fields are released in OnUnloaded.
public sealed partial class LoomDiffEditor
#pragma warning restore CA1001
{
    private const string ViewerHostPartName = "PART_ViewerHost";

    private Grid? _viewerHost;
    private AccessibleDiffViewer? _accessibleViewer;
    private AccessibleDiffViewerModel? _viewerModel;

    /// <summary>Whether the accessible diff viewer is open.</summary>
    public bool IsAccessibleDiffViewerOpen => _accessibleViewer is not null;

    /// <summary>Opens the accessible diff viewer on the group of changes nearest to the cursor of the modified editor; does nothing when it is open.</summary>
    public void OpenAccessibleDiffViewer()
    {
        if (_accessibleViewer is not null || _viewerHost is null || _original is null || _modified is null)
        {
            return;
        }

        var host = new ViewerHost(this);
        _viewerModel = new AccessibleDiffViewerModel(host, canClose: true);
        _viewerModel.SetDiffs(CurrentViewerDiffs());
        _accessibleViewer = new AccessibleDiffViewer(_viewerModel, host, Theme, _modified.FontFamily, _modified.FontSize, _modified.TabSize);
        _viewerHost.Children.Add(_accessibleViewer);

        // The two editors are hidden from automation while the viewer is open.
        AutomationProperties.SetAccessibilityView(_original, AccessibilityView.Raw);
        AutomationProperties.SetAccessibilityView(_modified, AccessibilityView.Raw);
    }

    /// <summary>Closes the accessible diff viewer and focuses the modified editor; does nothing when it is closed.</summary>
    public void CloseAccessibleDiffViewer() => _viewerModel?.Close();

    void IDiffNavigationTarget.AccessibleDiffViewerNext()
    {
        bool wasOpen = IsAccessibleDiffViewerOpen;
        OpenAccessibleDiffViewer();
        if (wasOpen)
        {
            _viewerModel?.NextGroup();
        }
    }

    void IDiffNavigationTarget.AccessibleDiffViewerPrevious()
    {
        // Monaco goes to the previous group even when it has just opened the viewer.
        OpenAccessibleDiffViewer();
        _viewerModel?.PreviousGroup();
    }

    private void ConnectViewerHost(Grid host) => _viewerHost = host;

    private void RefreshScreenReader() => _options.IsScreenReaderOptimized = ScreenReaderDetector.IsScreenReaderRunning();

    private List<DetailedLineRangeMapping>? CurrentViewerDiffs() =>
        _viewModel?.Diff?.Mappings.Select(m => m.LineRangeMapping).ToList();

    private void UpdateViewerDiffs() => _viewerModel?.SetDiffs(CurrentViewerDiffs());

    private void DestroyAccessibleViewer()
    {
        if (_accessibleViewer is null)
        {
            return;
        }

        _accessibleViewer.Detach();
        _viewerHost?.Children.Remove(_accessibleViewer);
        _accessibleViewer = null;
        _viewerModel = null;
        if (_original is not null && _modified is not null)
        {
            AutomationProperties.SetAccessibilityView(_original, AccessibilityView.Content);
            AutomationProperties.SetAccessibilityView(_modified, AccessibilityView.Content);
        }
    }

    // Monaco: AccessibleDiffViewerModelFromEditors.
    private sealed class ViewerHost(LoomDiffEditor owner) : IAccessibleDiffViewerHost
    {
        public int OriginalLineCount => owner._original!.Model.LineCount;

        public int ModifiedLineCount => owner._modified!.Model.LineCount;

        public int? ModifiedCursorLine => owner._modified!.Selections[0].PositionLineNumber;

        public string GetOriginalLine(int lineNumber) => owner._original!.Model.GetLineContent(lineNumber);

        public string GetModifiedLine(int lineNumber) => owner._modified!.Model.GetLineContent(lineNumber);

        public void OriginalReveal(Core.Text.Range range)
        {
            if (!owner._options.RenderSideBySideEffective)
            {
                // The original editor is hidden in the inline view (Monaco has the same limit): go to the block in the modified editor.
                LineRangeMapping? mapping = owner._viewModel?.Diff?.Mappings.Select(m => m.LineRangeMapping).FirstOrDefault(m => m.Original.Contains(range.StartLineNumber));
                int line = Math.Clamp(mapping?.Modified.StartLineNumber ?? 1, 1, owner._modified!.Model.LineCount);
                Show(owner._modified!, Core.Text.Range.Collapsed(new Position(line, 1)));
                return;
            }

            Show(owner._original!, range);
        }

        public void ModifiedReveal(Core.Text.Range? range)
        {
            if (range is { } r)
            {
                Show(owner._modified!, r);
            }
            else
            {
                owner._modified!.Focus(FocusState.Programmatic);
            }
        }

        public void ModifiedSetSelection(Core.Text.Range range) =>
            owner._modified!.SetSelections(new[] { Selection.FromRange(range, SelectionDirection.LeftToRight) });

        public void ModifiedFocus() => owner._modified!.Focus(FocusState.Programmatic);

        public void SetViewerVisible(bool visible)
        {
            if (!visible)
            {
                owner.DestroyAccessibleViewer();
            }
        }

        // Monaco: revealRange, setSelection and focus.
        private static void Show(LoomEditor editor, Core.Text.Range range)
        {
            editor.SetSelections(new[] { Selection.FromRange(range, SelectionDirection.LeftToRight) });
            editor.RevealRange(range);
            editor.Focus(FocusState.Programmatic);
        }
    }
}
