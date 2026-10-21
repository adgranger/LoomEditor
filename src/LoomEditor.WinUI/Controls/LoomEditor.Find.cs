using Microsoft.UI.Xaml;

namespace LoomEditor.WinUI.Controls;

// Find: the optional PART_FindWidget in the template is connected to the find controller that FindFeature creates.
// Without the part (or without the feature) find has no UI.
public sealed partial class LoomEditor
{
    private void ConnectFindWidget()
    {
        if (_findWidget is null || _context.FindController is not { } controller)
        {
            return;
        }

        _findWidget.Connect(controller, _viewModel, () => Focus(FocusState.Programmatic));
        _findWidget.ApplyTheme(Theme);
    }
}
