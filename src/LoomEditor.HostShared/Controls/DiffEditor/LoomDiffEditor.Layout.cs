using LoomEditor.View.DiffEditor;

#if LOOM_WINUI
using LoomEditor.WinUI.Controls.DiffEditor;
namespace LoomEditor.WinUI.Controls;
#elif LOOM_AVALONIA
using LoomEditor.Avalonia.Controls.DiffEditor;
namespace LoomEditor.Avalonia.Controls;
#else
#error This file is compiled into LoomEditor.WinUI or LoomEditor.Avalonia only.
#endif

// The reactions to the options and the sash. Monaco: `diffEditorSash.ts` and the layout of `diffEditorWidget.ts`.
public sealed partial class LoomDiffEditor
{
    private void OnOptionsChanged(object? sender, DiffEditorOptionsChangedEventArgs e)
    {
        IReadOnlySet<string> names = e.ChangedOptions;
        if (names.Contains(nameof(DiffEditorOptions.RenderIndicators)) || names.Contains(nameof(DiffEditorOptions.ShowEmptyDecorations)))
        {
            UpdateDecorations();
        }

        _sashLayout.EnableSplitViewResizing = _options.EnableSplitViewResizing;
        _sashLayout.SplitViewDefaultRatio = _options.SplitViewDefaultRatio;
        ApplyLayout();
        UpdateViewZones();
    }

    private void OnSashMoved(object? sender, double sashLeft)
    {
        _sashLayout.SetSashLeft(sashLeft);
        ApplyLayout();
    }

    private void OnSashReset(object? sender, EventArgs e)
    {
        _sashLayout.ResetSash();
        ApplyLayout();
    }

    // Monaco: the zones of diffEditorViewZones.ts; side by side they pad both editors, inline the modified one gets the deleted lines.
    private void UpdateViewZones()
    {
        bool wanted = _viewModel is not null && _original is not null && _modified is not null;
        bool inline = !_options.RenderSideBySideEffective;
        if (!wanted || (_viewZones is not null && _viewZonesInline != inline))
        {
            _viewZones?.Dispose();
            _viewZones = null;
        }

        if (!wanted)
        {
            return;
        }

        _hideUnchangedRegions?.Invalidate();

        if (_viewZones is null)
        {
            _viewZonesInline = inline;
            _viewZones = new DiffEditorViewZones(_original!, _modified!, _viewModel!, inline, () => _options.RenderIndicators);
        }
        else
        {
            _viewZones.Invalidate();
        }
    }
}
