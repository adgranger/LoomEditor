namespace LoomEditor.Lsp.Protocol;

/// <summary>LSP <c>LogMessageParams</c> (<c>window/logMessage</c>).</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class LogMessageParams
{
    public required int Type { get; set; }

    public required string Message { get; set; }
}

/// <summary>LSP <c>ShowMessageParams</c> (<c>window/showMessage</c>).</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class ShowMessageParams
{
    public required int Type { get; set; }

    public required string Message { get; set; }
}
