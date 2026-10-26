namespace LoomEditor.Lsp.Client;

/// <summary>A <c>window/logMessage</c> or <c>window/showMessage</c> notification.</summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed class LspLogMessageEventArgs(int type, string message, bool isShowMessage) : EventArgs
{
    public int Type { get; } = type;

    public string Message { get; } = message;

    public bool IsShowMessage { get; } = isShowMessage;
}
