using LoomEditor.View.Suggest;

#if !LOOM_AVALONIA
namespace LoomEditor.WinUI.Controls.Suggest;
#elif LOOM_AVALONIA
namespace LoomEditor.Avalonia.Controls.Suggest;
#endif

// The sizes, the row controls and the state of the list; the host part builds and fills the rows.
internal sealed partial class SuggestWidget
{
    internal const double WidgetWidth = 430;
    internal const double RowHeight = 22;

    private readonly SuggestRow[] _rows = new SuggestRow[SuggestListState.MaxVisibleRows];
    private SuggestListState? _state;

    /// <summary>Raised with the visible row index (0-based) when a row is clicked.</summary>
    public event Action<int>? RowPressed;
}
