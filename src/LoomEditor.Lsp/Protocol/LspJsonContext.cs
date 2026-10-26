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
internal sealed partial class LspJsonContext : JsonSerializerContext;
