namespace LoomEditor.Gallery;

public sealed partial class DiffDemoWindow
{
    private void OnDiffChanged(object? sender, EventArgs e) =>
        StatusText.Text = $"{DiffEditor.ChangeCount} change(s) — diff up to date: {DiffEditor.IsDiffUpToDate}";
}
