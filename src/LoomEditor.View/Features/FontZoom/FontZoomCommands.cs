namespace LoomEditor.View.Features.FontZoom;

/// <summary>Ids of the commands exposed by <see cref="FontZoomFeature"/>. None has a default key: the host binds them.</summary>
public static class FontZoomCommands
{
    /// <summary>Zooms in by one level. Same id as in Monaco.</summary>
    public const string FontZoomIn = "editor.action.fontZoomIn";

    /// <summary>Zooms out by one level. Same id as in Monaco.</summary>
    public const string FontZoomOut = "editor.action.fontZoomOut";

    /// <summary>Goes back to 100 %. Same id as in Monaco.</summary>
    public const string FontZoomReset = "editor.action.fontZoomReset";
}
