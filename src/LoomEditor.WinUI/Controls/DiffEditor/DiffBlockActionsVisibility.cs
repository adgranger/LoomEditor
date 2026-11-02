namespace LoomEditor.WinUI.Controls;

/// <summary>When the block action buttons of a side-by-side <see cref="LoomDiffEditor"/> show.</summary>
public enum DiffBlockActionsVisibility
{
    /// <summary>Never: no lane, no button.</summary>
    Off,

    /// <summary>Only for the block under the pointer (over its lines' gutter or text, on the side that shows its buttons).</summary>
    OnHover,

    /// <summary>For every visible block.</summary>
    Always,
}
