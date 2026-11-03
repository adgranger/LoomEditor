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

// The per-block action buttons in the glyph margin of the side-by-side view (phase 14). The editor knows nothing about what an action does: it raises an event.
public sealed partial class LoomDiffEditor
{
    // Called when the template gives the two editors: each creates its gutter controls on its gutter layer when it has one.
    private void AttachBlockActions()
    {
        LoomEditor original = _original!;
        LoomEditor modified = _modified!;
        original.SetDiffGutterFactory((layer, hitTest) => CreateDiffGutter(original, layer, false, hitTest));
        modified.SetDiffGutterFactory((layer, hitTest) => CreateDiffGutter(modified, layer, true, hitTest));
    }

    private void DetachBlockActions()
    {
        _original?.SetDiffGutterFactory(null);
        _modified?.SetDiffGutterFactory(null);
    }

    private DiffGutterControllers CreateDiffGutter(LoomEditor editor, Overlay.OverlayHost layer, bool modifiedSide, Func<double, double, View.Controller.MouseTarget> hitTest)
    {
        var blockActions = new DiffBlockActionsController(editor, layer, modifiedSide, hitTest);
        blockActions.Invoked += (block, action) => BlockActionInvoked?.Invoke(this, new DiffBlockActionInvokedEventArgs(block, action));
        blockActions.SetBlocks(_changes);
        var lineCheckBoxes = CreateLineCheckBoxes(editor, layer, modifiedSide, hitTest);
        var controllers = new DiffGutterControllers(blockActions, lineCheckBoxes);
        ConfigureGutter(controllers, GetGutterLanes());
        return controllers;
    }

    // Decision 1: the same lane count on both editors, so the two gutters keep the same width. Called for every phase 14 property and every layout change.
    private void UpdateGutterLanes()
    {
        if (_original is null || _modified is null)
        {
            return;
        }

        DiffGutterLanes lanes = GetGutterLanes();
        foreach (LoomEditor editor in new[] { _original, _modified })
        {
            editor.GlyphMarginLaneCount = lanes.Count;
            if (editor.DiffGutter is { } controllers)
            {
                ConfigureGutter(controllers, lanes);
            }
        }
    }

    private DiffGutterLanes GetGutterLanes() => DiffGutterLanes.Compute(
        _options.RenderSideBySideEffective,
        LineSelectionEnabled,
        LineActionGlyph is not null,
        BlockActionsVisibility != DiffBlockActionsVisibility.Off,
        PrimaryBlockActionGlyph is not null,
        SecondaryBlockActionGlyph is not null);

    private void ConfigureGutter(DiffGutterControllers controllers, DiffGutterLanes lanes)
    {
        ConfigureGutterFonts(controllers);
        controllers.BlockActions.Configure(
            BlockActionsVisibility,
            PrimaryBlockActionGlyph,
            PrimaryBlockActionToolTip,
            SecondaryBlockActionGlyph,
            SecondaryBlockActionToolTip,
            lanes.PrimaryBlockAction,
            lanes.SecondaryBlockAction);
        controllers.LineCheckBoxes.Configure(lanes.Select, lanes.LineAction, LineActionGlyph, LineActionToolTip);
    }

    private void UpdateBlockActionBlocks(IReadOnlyList<DiffChangeBlock> blocks)
    {
        _original?.DiffGutter?.BlockActions.SetBlocks(blocks);
        _modified?.DiffGutter?.BlockActions.SetBlocks(blocks);
    }
}
