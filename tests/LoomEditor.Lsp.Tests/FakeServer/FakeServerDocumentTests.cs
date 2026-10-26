using System.Text;
using LoomEditor.Lsp.Protocol;
using LoomEditor.Lsp.Tests.TestHelpers;

namespace LoomEditor.Lsp.Tests.FakeServer;

public class FakeServerDocumentTests
{
    private static async Task<FakeServerHost> OpenAsync(string text)
    {
        FakeServerHost host = FakeServerHost.StartInProcess();
        await host.InitializeAsync();
        host.Open(text);
        await host.BarrierAsync();
        return host;
    }

    private static async Task<string> ChangedAsync(string text, params string[] changes)
    {
        await using FakeServerHost host = await OpenAsync(text);
        await host.ChangeAsync(2, string.Join(",", changes));
        return host.Server!.DocumentText;
    }

    [Fact]
    public async Task DidOpen_StoresDocument_AndPublishesDiagnostics()
    {
        await using FakeServerHost host = await OpenAsync("plain text");

        System.Text.Json.JsonElement published = await host.WaitForDiagnosticsAsync();

        Assert.Equal(0, published.GetProperty("diagnostics").GetArrayLength());
        Assert.Equal(1, published.GetProperty("version").GetInt32());
        Assert.Equal("file:///t.txt", published.GetProperty("uri").GetString());
        Assert.Equal("plain text", host.Server!.DocumentText);
        Assert.Equal(1, host.Server.DocumentVersion);
        Assert.Equal("file:///t.txt", host.Server.DocumentUri);
    }

    [Fact]
    public async Task DidChange_Full_ReplacesText()
        => Assert.Equal("new", await ChangedAsync("old text", FakeServerHost.Full("new")));

    [Fact]
    public async Task DidChange_Incremental_InsertDeleteReplace()
    {
        Assert.Equal("hello, world", await ChangedAsync("hello world", FakeServerHost.Edit(0, 5, 0, 5, ",")));
        Assert.Equal("helloworld", await ChangedAsync("hello world", FakeServerHost.Edit(0, 5, 0, 6, "")));
        Assert.Equal("hello there", await ChangedAsync("hello world", FakeServerHost.Edit(0, 6, 0, 11, "there")));
    }

    [Fact]
    public async Task DidChange_Incremental_MultiByte()
    {
        // 'é' and '€' are one UTF-16 unit, the emoji is two.
        Assert.Equal("éE😀x", await ChangedAsync("é€😀x", FakeServerHost.Edit(0, 1, 0, 2, "E")));
        Assert.Equal("é€😀y", await ChangedAsync("é€😀x", FakeServerHost.Edit(0, 4, 0, 5, "y")));
        Assert.Equal("é€x", await ChangedAsync("é€😀x", FakeServerHost.Edit(0, 2, 0, 4, "")));
    }

    [Fact]
    public async Task DidChange_Incremental_MultiLineRange()
        => Assert.Equal("aXf", await ChangedAsync("ab\ncd\nef", FakeServerHost.Edit(0, 1, 2, 1, "X")));

    [Fact]
    public async Task DidChange_Incremental_CrLfDocument()
    {
        Assert.Equal("a\r\nB\r\nc", await ChangedAsync("a\r\nb\r\nc", FakeServerHost.Edit(1, 0, 1, 1, "B")));
        Assert.Equal("ab\r\nc", await ChangedAsync("a\r\nb\r\nc", FakeServerHost.Edit(0, 1, 1, 0, "")));
        Assert.Equal("a\rB\rc", await ChangedAsync("a\rb\rc", FakeServerHost.Edit(1, 0, 1, 1, "B")));
    }

    [Fact]
    public async Task DidChange_Incremental_ChangesApplyInOrder()
    {
        string text = await ChangedAsync(
            "abc",
            FakeServerHost.Edit(0, 3, 0, 3, "d"),
            FakeServerHost.Edit(0, 0, 0, 1, "X"),
            FakeServerHost.Edit(0, 4, 0, 4, "!"));

        Assert.Equal("Xbcd!", text);
    }

    [Fact]
    public async Task DidChange_CharacterBeyondLine_Clamped()
        => Assert.Equal("abX\ncd", await ChangedAsync("ab\ncd", FakeServerHost.Edit(0, 99, 0, 99, "X")));

    [Fact]
    public async Task DidChange_LineBeyondDocument_ClampedToEnd()
        => Assert.Equal("abX", await ChangedAsync("ab", FakeServerHost.Edit(5, 0, 5, 0, "X")));

    [Fact]
    public async Task DidChange_StaleVersion_Ignored()
    {
        await using FakeServerHost host = await OpenAsync("abc");

        await host.ChangeAsync(1, FakeServerHost.Full("same version"));
        await host.ChangeAsync(0, FakeServerHost.Full("older version"));

        Assert.Equal("abc", host.Server!.DocumentText);
        Assert.Equal(1, host.Server.DocumentVersion);
        Assert.Equal(2, host.Server.ReceivedMethods.Count(m => m == "didChange(ignored)"));
    }

    [Fact]
    public async Task DidChange_OtherUri_Ignored()
    {
        await using FakeServerHost host = await OpenAsync("abc");

        await host.ChangeAsync(2, FakeServerHost.Full("other"), "file:///other.txt");

        Assert.Equal("abc", host.Server!.DocumentText);
        Assert.Contains("didChange(ignored)", host.Server.ReceivedMethods);
    }

    [Fact]
    public async Task DidClose_ClearsAndPublishesEmpty()
    {
        await using FakeServerHost host = await OpenAsync("ERROR");
        Assert.Equal(1, (await host.WaitForDiagnosticsAsync()).GetProperty("diagnostics").GetArrayLength());

        host.Notify(LspMethods.DidClose, """{"textDocument":{"uri":"file:///t.txt"}}""");
        System.Text.Json.JsonElement closed = await host.WaitForDiagnosticsAsync();

        Assert.Equal(0, closed.GetProperty("diagnostics").GetArrayLength());
        Assert.Equal("file:///t.txt", closed.GetProperty("uri").GetString());
        Assert.Equal("", host.Server!.DocumentText);
        Assert.Null(host.Server.DocumentUri);
    }

    [Fact]
    public async Task RandomEdits_ServerTextMatchesReferenceModel()
    {
        const int Seed = 12345;
        Random random = new(Seed);
        string[] pieces = ["a", "b", "\n", "\r\n", "\r", " ", "é", "😀", "xyz"];
        StringBuilder reference = new("line one\nline two\r\nthree\rfour");
        await using FakeServerHost host = await OpenAsync(reference.ToString());

        for (int i = 0; i < 500; i++)
        {
            string current = reference.ToString();
            int lines = current.Count(c => c is '\n' or '\r') + 2;
            int line1 = random.Next(lines);
            int char1 = random.Next(12);
            int line2 = random.Next(lines);
            int char2 = random.Next(12);
            string text = string.Concat(Enumerable.Range(0, random.Next(4)).Select(_ => pieces[random.Next(pieces.Length)]));

            int a = ReferenceOffset(current, line1, char1);
            int b = ReferenceOffset(current, line2, char2);
            (a, b) = (Math.Min(a, b), Math.Max(a, b));
            reference.Remove(a, b - a).Insert(a, text);

            await host.ChangeAsync(i + 2, FakeServerHost.Edit(line1, char1, line2, char2, text));

            Assert.True(
                reference.ToString() == host.Server!.DocumentText,
                $"seed {Seed}, edit {i}: expected '{reference}' but the server has '{host.Server.DocumentText}'");
        }
    }

    /// <summary>The position rules written a second way: walk the text line by line.</summary>
    private static int ReferenceOffset(string text, int line, int character)
    {
        int start = 0;
        for (int l = 0; l < line; l++)
        {
            int end = start;
            while (end < text.Length && text[end] is not ('\r' or '\n'))
            {
                end++;
            }

            if (end == text.Length)
            {
                return text.Length;
            }

            start = text[end] == '\r' && end + 1 < text.Length && text[end + 1] == '\n' ? end + 2 : end + 1;
        }

        int lineEnd = start;
        while (lineEnd < text.Length && text[lineEnd] is not ('\r' or '\n'))
        {
            lineEnd++;
        }

        return start + Math.Min(character, lineEnd - start);
    }
}
