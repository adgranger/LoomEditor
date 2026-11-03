#if LOOM_WINUI
namespace LoomEditor.WinUI.Controls;
#elif LOOM_AVALONIA
namespace LoomEditor.Avalonia.Controls;
#else
#error This file is compiled into LoomEditor.WinUI or LoomEditor.Avalonia only.
#endif

public sealed partial class LoomEditor
{
    /// <summary>The text model of this editor: edit it with <c>PushEditOperations</c>, undo and redo through it.</summary>
    /// <remarks>Threading: use it on the UI thread only, like the control.</remarks>
    public Core.Model.TextModel Model => _context.Model;

    /// <summary>The selections of the cursors; the first one is the primary selection.</summary>
    public IReadOnlyList<Core.Text.Selection> Selections => _context.Selections;

    /// <summary>Replaces the selections, i.e. the cursors. There must be at least one.</summary>
    /// <param name="selections">The new selections.</param>
    public void SetSelections(IReadOnlyList<Core.Text.Selection> selections) => _context.SetSelections(selections);

    /// <summary>Raised after the selections changed.</summary>
    public event EventHandler? SelectionChanged
    {
        add => _context.SelectionChanged += value;
        remove => _context.SelectionChanged -= value;
    }

    /// <summary>The commands of this editor, e.g. to run <c>editor.action.selectAll</c> from a menu.</summary>
    public View.Commands.CommandRegistry Commands => _context.Commands;
}
