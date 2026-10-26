using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Lsp.FakeServer;

namespace LoomEditor.Lsp.Tests.Editor;

public class LspDocumentSyncRandomTests
{
    private static readonly string[] Snippets = ["x", "\n", "\r\n", "é€😀", "foo bar", ""];

    private static async Task RunAsync(int seed, DefaultEndOfLine eol, FakeServerOptions? options)
    {
        Random random = new(seed);
        await using LspDocumentSyncTests.Rig rig = await LspDocumentSyncTests.Rig.StartAsync("start\nline two\n", options, eol: eol);
        TextModel model = rig.Model;
        for (int step = 0; step < 300; step++)
        {
            if (step % 50 == 49)
            {
                if (step % 100 == 49)
                {
                    model.SetValue(Snippets[random.Next(Snippets.Length)] + "reset\nvalue");
                }
                else
                {
                    model.SetEol(model.Eol == "\n" ? EndOfLineSequence.CRLF : EndOfLineSequence.LF);
                }
            }
            else
            {
                switch (random.Next(5))
                {
                    case 0:
                        model.ApplyEdits([TextEdit.Insert(RandomPosition(model, random), Snippets[random.Next(Snippets.Length)])]);
                        break;
                    case 1:
                    case 2:
                        model.ApplyEdits(RandomEdits(model, random, random.Next(1, 5)));
                        break;
                    case 3:
                        model.Undo();
                        break;
                    default:
                        model.Redo();
                        break;
                }
            }

            await rig.Host.BarrierAsync();
            Assert.True(model.GetValue() == rig.ServerText, $"seed {seed}, step {step}");
        }
    }

    private static Position RandomPosition(TextModel model, Random random)
    {
        int line = random.Next(1, model.LineCount + 1);
        return new Position(line, random.Next(1, model.GetLineMaxColumn(line) + 1));
    }

    private static List<TextEdit> RandomEdits(TextModel model, Random random, int count)
    {
        List<Position> points = [];
        for (int i = 0; i < count * 2; i++)
        {
            points.Add(RandomPosition(model, random));
        }

        points.Sort();
        List<TextEdit> edits = [];
        for (int i = 0; i < count; i++)
        {
            if (edits.Count == 0 || points[i * 2] > edits[^1].Range.End)
            {
                edits.Add(new TextEdit(new Range(points[i * 2], points[i * 2 + 1]), Snippets[random.Next(Snippets.Length)]));
            }
        }

        return edits;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(12345)]
    public Task RandomEdits_ServerTextMatchesModel(int seed) => RunAsync(seed, DefaultEndOfLine.LF, null);

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public Task RandomEdits_CrLfModel(int seed) => RunAsync(seed, DefaultEndOfLine.CRLF, null);

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public Task RandomEdits_ServerWantsFull(int seed) => RunAsync(seed, DefaultEndOfLine.LF, new FakeServerOptions { Sync = "full" });
}
