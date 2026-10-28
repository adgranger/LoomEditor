using LoomEditor.View.Commands;
using LoomEditor.View.DiffEditor;

namespace LoomEditor.View.Features.DiffNavigation;

/// <summary>What <see cref="DiffNavigationFeature"/> asks the diff control to do.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal interface IDiffNavigationTarget
{
    /// <summary>Moves the cursor of the modified editor to the next or previous change and reveals it.</summary>
    void GoToChange(NavigationDirection direction);

    /// <summary>Opens the accessible diff viewer, or goes to its next group when it is open (<c>F7</c>).</summary>
    void AccessibleDiffViewerNext();

    /// <summary>Opens the accessible diff viewer and goes to its previous group (<c>Shift+F7</c>).</summary>
    void AccessibleDiffViewerPrevious();
}

/// <summary>
/// Adds the next / previous change commands of a diff editor. Not in <see cref="EditorFeatures.Default"/>: the diff control
/// appends it to the features of its two editors (<see cref="EditorFeatures.ForDiffEditor"/>).
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class DiffNavigationFeature : IEditorFeature
{
    private readonly List<IDisposable> _registrations = [];

    /// <summary>The diff control the commands act on; set before <see cref="Attach"/>.</summary>
    public IDiffNavigationTarget? Target { get; init; }

    /// <inheritdoc />
    public string Id => "loom.diffNavigation";

    /// <inheritdoc />
    public void Attach(IEditorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _registrations.Add(context.Commands.Register(
            new EditorCommand(DiffNavigationCommands.NextChange, () => Target?.GoToChange(NavigationDirection.Next))));
        _registrations.Add(context.Commands.Register(
            new EditorCommand(DiffNavigationCommands.PreviousChange, () => Target?.GoToChange(NavigationDirection.Previous))));
        _registrations.Add(context.Commands.Register(
            new EditorCommand(DiffNavigationCommands.AccessibleViewerNext, () => Target?.AccessibleDiffViewerNext())));
        _registrations.Add(context.Commands.Register(
            new EditorCommand(DiffNavigationCommands.AccessibleViewerPrevious, () => Target?.AccessibleDiffViewerPrevious())));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (IDisposable registration in _registrations)
        {
            registration.Dispose();
        }

        _registrations.Clear();
    }
}
