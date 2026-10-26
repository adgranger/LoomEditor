namespace LoomEditor.Lsp.Protocol;

/// <summary>LSP <c>CompletionItemKind</c> values.</summary>
/// <remarks>Threading: constants, safe from any thread.</remarks>
internal static class LspCompletionItemKind
{
    public const int Text = 1;
    public const int Method = 2;
    public const int Function = 3;
    public const int Constructor = 4;
    public const int Field = 5;
    public const int Variable = 6;
    public const int Class = 7;
    public const int Interface = 8;
    public const int Module = 9;
    public const int Property = 10;
    public const int Unit = 11;
    public const int Value = 12;
    public const int Enum = 13;
    public const int Keyword = 14;
    public const int Snippet = 15;
    public const int Color = 16;
    public const int File = 17;
    public const int Reference = 18;
    public const int Folder = 19;
    public const int EnumMember = 20;
    public const int Constant = 21;
    public const int Struct = 22;
    public const int Event = 23;
    public const int Operator = 24;
    public const int TypeParameter = 25;
}

/// <summary>LSP <c>InsertTextFormat</c> values.</summary>
/// <remarks>Threading: constants, safe from any thread.</remarks>
internal static class LspInsertTextFormat
{
    public const int PlainText = 1;
    public const int Snippet = 2;
}

/// <summary>LSP <c>DiagnosticSeverity</c> values.</summary>
/// <remarks>Threading: constants, safe from any thread.</remarks>
internal static class LspDiagnosticSeverity
{
    public const int Error = 1;
    public const int Warning = 2;
    public const int Information = 3;
    public const int Hint = 4;
}

/// <summary>LSP <c>DiagnosticTag</c> values.</summary>
/// <remarks>Threading: constants, safe from any thread.</remarks>
internal static class LspDiagnosticTag
{
    public const int Unnecessary = 1;
    public const int Deprecated = 2;
}

/// <summary>LSP <c>CompletionItemTag</c> values.</summary>
/// <remarks>Threading: constants, safe from any thread.</remarks>
internal static class LspCompletionItemTag
{
    public const int Deprecated = 1;
}

/// <summary>LSP <c>CompletionTriggerKind</c> values.</summary>
/// <remarks>Threading: constants, safe from any thread.</remarks>
internal static class LspCompletionTriggerKind
{
    public const int Invoked = 1;
    public const int TriggerCharacter = 2;
    public const int TriggerForIncompleteCompletions = 3;
}

/// <summary>LSP <c>MessageType</c> values.</summary>
/// <remarks>Threading: constants, safe from any thread.</remarks>
internal static class LspMessageType
{
    public const int Error = 1;
    public const int Warning = 2;
    public const int Info = 3;
    public const int Log = 4;
}

/// <summary>LSP <c>TextDocumentSyncKind</c> values.</summary>
/// <remarks>Threading: constants, safe from any thread.</remarks>
internal static class LspTextDocumentSyncKind
{
    public const int None = 0;
    public const int Full = 1;
    public const int Incremental = 2;
}

/// <summary>LSP <c>MarkupKind</c> values.</summary>
/// <remarks>Threading: constants, safe from any thread.</remarks>
internal static class LspMarkupKind
{
    public const string PlainText = "plaintext";
    public const string Markdown = "markdown";
}

/// <summary>LSP <c>PositionEncodingKind</c> values.</summary>
/// <remarks>Threading: constants, safe from any thread.</remarks>
internal static class LspPositionEncoding
{
    public const string Utf16 = "utf-16";
}
