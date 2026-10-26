using System.Buffers;
using System.Globalization;
using System.Text;

namespace LoomEditor.Lsp.JsonRpc;

/// <summary>Writes <c>Content-Length</c> framed messages (LSP base protocol) to a byte stream.</summary>
/// <remarks>Threading: not thread-safe, the connection serializes writes.</remarks>
internal sealed class LspMessageWriter(Stream output)
{
    private const int SingleWriteLimit = 64 * 1024;

    /// <summary>Writes the header and the body, then flushes.</summary>
    public async ValueTask WriteMessageAsync(ReadOnlyMemory<byte> body, CancellationToken token)
    {
        string header = string.Create(CultureInfo.InvariantCulture, $"Content-Length: {body.Length}\r\n\r\n");
        int headerLength = header.Length;

        if (body.Length <= SingleWriteLimit)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(headerLength + body.Length);
            try
            {
                Encoding.ASCII.GetBytes(header, buffer);
                body.CopyTo(buffer.AsMemory(headerLength));
                await output.WriteAsync(buffer.AsMemory(0, headerLength + body.Length), token).ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        else
        {
            await output.WriteAsync(Encoding.ASCII.GetBytes(header), token).ConfigureAwait(false);
            await output.WriteAsync(body, token).ConfigureAwait(false);
        }

        await output.FlushAsync(token).ConfigureAwait(false);
    }
}
