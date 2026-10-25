using LoomEditor.Core.Text;
using LoomEditor.View.Suggest;

namespace LoomEditor.View.Tests.Suggest.TestHelpers;

/// <summary>Records the calls of a <see cref="SuggestController"/>; <see cref="IsVisible"/> follows them.</summary>
/// <remarks>Threading: use from one test thread.</remarks>
internal sealed class FakeSuggestPresenter : ISuggestPresenter
{
    public List<Position> Shows { get; } = [];

    public int Hides { get; private set; }

    public bool IsVisible { get; private set; }

    public void Show(SuggestListState state, Position anchor)
    {
        Shows.Add(anchor);
        IsVisible = true;
    }

    public void Hide()
    {
        Hides++;
        IsVisible = false;
    }
}
