using Avalonia.Automation;

namespace LoomEditor.Avalonia.Controls.Comments;

internal sealed partial class CommentThreadWidget
{
    private void SetAutomationName(string name) => AutomationProperties.SetName(this, name);

    private void SetAutomationHelp(string help) => AutomationProperties.SetHelpText(this, help);
}
