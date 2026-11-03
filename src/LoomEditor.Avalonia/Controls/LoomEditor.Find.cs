using Avalonia.Controls;
using Avalonia.VisualTree;
using LoomEditor.Avalonia.Controls.Find;
using LoomEditor.Avalonia.Input;

namespace LoomEditor.Avalonia.Controls;

// Find: the optional PART_FindWidget in the template is connected to the find controller that FindFeature creates.
// Without the part (or without the feature) find has no UI.
public sealed partial class LoomEditor
{
    private const string FindWidgetPartName = "PART_FindWidget";

    private FindWidget? _findWidget;

    private void InitializeFind() => _context.SelectionChanged += (_, _) => _findWidget?.UpdateFindInSelectionToggle();

    private void AttachFindWidget(INameScope nameScope)
    {
        _findWidget?.Disconnect();
        _findWidget = nameScope.Find<FindWidget>(FindWidgetPartName);
        ConnectFindWidget();
    }

    // Called when the template gives a widget and when the feature host (which creates the find controller) starts.
    private void ConnectFindWidget()
    {
        if (_findWidget is null || _context.FindController is not { } controller)
        {
            return;
        }

        _findWidget.Connect(controller, _viewModel, () => Focus());
        _findWidget.TryHandleKey = e => _keyboard.TryHandle(
            e.Key,
            KeyMapping.ToModifiers(e.KeyModifiers, OperatingSystem.IsMacOS()),
            CreateKeybindingContext(),
            FindWidget.RoutedCommandIds.Contains);
        _findWidget.ApplyTheme(EffectiveTheme);
    }

    private bool IsFromFindWidget(object? source)
    {
        for (var element = source as global::Avalonia.Visual; element is not null && !ReferenceEquals(element, this); element = element.GetVisualParent())
        {
            if (ReferenceEquals(element, _findWidget))
            {
                return true;
            }
        }

        return false;
    }
}
