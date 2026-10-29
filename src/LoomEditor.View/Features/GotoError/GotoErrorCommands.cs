namespace LoomEditor.View.Features.GotoError;

/// <summary>The ids of the go to problem commands (the Monaco ids).</summary>
internal static class GotoErrorCommands
{
    /// <summary>Goes to the next problem (<c>Alt+F8</c>).</summary>
    public const string Next = "editor.action.marker.next";

    /// <summary>Goes to the previous problem (<c>Shift+Alt+F8</c>).</summary>
    public const string Prev = "editor.action.marker.prev";

    /// <summary>Goes to the next problem, in every file in Monaco (<c>F8</c>): the same as <see cref="Next"/> with one model.</summary>
    public const string NextInFiles = "editor.action.marker.nextInFiles";

    /// <summary>Goes to the previous problem, in every file in Monaco (<c>Shift+F8</c>): the same as <see cref="Prev"/> with one model.</summary>
    public const string PrevInFiles = "editor.action.marker.prevInFiles";

    /// <summary>Ends the go to problem session (<c>Escape</c>).</summary>
    public const string Close = "closeMarkersNavigation";
}
