using System.Globalization;

namespace LoomEditor.Lsp.JsonRpc;

/// <summary>Reads <c>Content-Length</c> framed messages (LSP base protocol) from a byte stream and returns their bodies.</summary>
/// <remarks>
/// Threading: not thread-safe, one reader loop only. After an <see cref="LspProtocolException"/> the reader is faulted
/// and must be discarded: every later call throws again.
/// </remarks>
internal sealed class LspMessageReader(Stream input, int maxMessageSize = 64 * 1024 * 1024)
{
    private const int MaxHeaderLine = 8192;
    private const int MaxHeaders = 32;

    private readonly byte[] _buffer = new byte[8192];
    private readonly byte[] _line = new byte[MaxHeaderLine + 1];
    private int _start;
    private int _end;
    private bool _faulted;

    /// <summary>Returns the body of the next message, or <c>null</c> when the stream ends cleanly between messages.</summary>
    public async ValueTask<byte[]?> ReadMessageAsync(CancellationToken token)
    {
        if (_faulted)
        {
            throw new LspProtocolException("The reader is faulted");
        }

        try
        {
            return await ReadCoreAsync(token).ConfigureAwait(false);
        }
        catch (LspProtocolException)
        {
            _faulted = true;
            throw;
        }
    }

    private async ValueTask<byte[]?> ReadCoreAsync(CancellationToken token)
    {
        int contentLength = -1;
        int headerCount = 0;
        bool started = false;

        while (true)
        {
            int lineLength = await ReadLineAsync(started, token).ConfigureAwait(false);
            if (lineLength < 0)
            {
                return null;
            }

            started = true;
            if (lineLength == 0)
            {
                break;
            }

            if (++headerCount > MaxHeaders)
            {
                throw new LspProtocolException("Too many headers");
            }

            ReadOnlySpan<byte> line = _line.AsSpan(0, lineLength);
            int colon = line.IndexOf((byte)':');
            if (colon < 0)
            {
                throw new LspProtocolException("Malformed header line");
            }

            ReadOnlySpan<char> name = System.Text.Encoding.Latin1.GetString(line[..colon]).AsSpan().Trim(" \t");
            if (name.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
            {
                if (contentLength >= 0)
                {
                    throw new LspProtocolException("Duplicate Content-Length");
                }

                string value = System.Text.Encoding.Latin1.GetString(line[(colon + 1)..]).Trim(' ', '\t');
                if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out contentLength)
                    || contentLength > maxMessageSize)
                {
                    throw new LspProtocolException("Invalid Content-Length");
                }
            }
        }

        if (contentLength < 0)
        {
            throw new LspProtocolException("Missing Content-Length");
        }

        byte[] body = new byte[contentLength];
        int copied = Math.Min(contentLength, _end - _start);
        Array.Copy(_buffer, _start, body, 0, copied);
        _start += copied;
        while (copied < contentLength)
        {
            int read = await input.ReadAsync(body.AsMemory(copied), token).ConfigureAwait(false);
            if (read == 0)
            {
                throw new LspProtocolException("Unexpected end of stream");
            }

            copied += read;
        }

        return body;
    }

    /// <summary>Reads one header line into <c>_line</c> and returns its length without the terminator, or -1 on a clean EOF.</summary>
    private async ValueTask<int> ReadLineAsync(bool started, CancellationToken token)
    {
        int length = 0;
        while (true)
        {
            if (_start == _end)
            {
                _start = 0;
                _end = await input.ReadAsync(_buffer, token).ConfigureAwait(false);
                if (_end == 0)
                {
                    if (!started && length == 0)
                    {
                        return -1;
                    }

                    throw new LspProtocolException("Unexpected end of stream");
                }
            }

            byte b = _buffer[_start++];
            if (b == (byte)'\n')
            {
                if (length > 0 && _line[length - 1] == (byte)'\r')
                {
                    length--;
                }

                if (length > MaxHeaderLine)
                {
                    throw new LspProtocolException("Header line too long");
                }

                return length;
            }

            if (length > MaxHeaderLine)
            {
                throw new LspProtocolException("Header line too long");
            }

            _line[length++] = b;
        }
    }
}
