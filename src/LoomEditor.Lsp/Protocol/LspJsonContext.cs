using System.Text.Json.Serialization;
using LoomEditor.Lsp.JsonRpc;

namespace LoomEditor.Lsp.Protocol;

/// <summary>The source-generated JSON context of the project: no reflection, Native AOT safe.</summary>
/// <remarks>Threading: immutable, safe from any thread.</remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(JsonRpcMessage))]
[JsonSerializable(typeof(JsonRpcError))]
[JsonSerializable(typeof(InitializeParams))]
[JsonSerializable(typeof(InitializeResult))]
[JsonSerializable(typeof(InitializedParams))]
[JsonSerializable(typeof(DidOpenTextDocumentParams))]
[JsonSerializable(typeof(DidChangeTextDocumentParams))]
[JsonSerializable(typeof(DidCloseTextDocumentParams))]
[JsonSerializable(typeof(PublishDiagnosticsParams))]
[JsonSerializable(typeof(CompletionParams))]
[JsonSerializable(typeof(LspCompletionItem))]
[JsonSerializable(typeof(LspCompletionList))]
[JsonSerializable(typeof(TextDocumentPositionParams))]
[JsonSerializable(typeof(LspHover))]
[JsonSerializable(typeof(LspLocation))]
[JsonSerializable(typeof(LspLocationLink))]
[JsonSerializable(typeof(LspRange))]
[JsonSerializable(typeof(CancelParams))]
[JsonSerializable(typeof(LogMessageParams))]
[JsonSerializable(typeof(ShowMessageParams))]
internal sealed partial class LspJsonContext : JsonSerializerContext;
