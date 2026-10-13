using LoomEditor.Core.Text;
using LoomEditor.Core.Tests.Model.TestHelpers;

namespace LoomEditor.Core.Tests.Model;

/// <summary>Ported from Monaco <c>editableTextModelAuto.test.ts</c>: suite <c>EditorModel Auto Tests</c>.</summary>
public class EditableTextModelAutoTests
{
    private static TextEdit EditOp(int startLineNumber, int startColumn, int endLineNumber, int endColumn, params string[] text) =>
        TextEdit.Replace(new Range(startLineNumber, startColumn, endLineNumber, endColumn), string.Join('\n', text));

    // Monaco: 'auto1'
    [Fact]
    public void Auto1() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["ioe", "", "yjct", "", ""],
            [EditOp(1, 2, 1, 2, "b", "r", "fq"), EditOp(1, 4, 2, 1, "", "")],
            ["ib", "r", "fqoe", "", "yjct", "", ""]);

    // Monaco: 'auto2'
    [Fact]
    public void Auto2() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["f", "littnhskrq", "utxvsizqnk", "lslqz", "jxn", "gmm"],
            [EditOp(1, 2, 1, 2, "", "o"), EditOp(2, 4, 2, 4, "zaq", "avb"), EditOp(2, 5, 6, 2, "jlr", "zl", "j")],
            ["f", "o", "litzaq", "avbtjlr", "zl", "jmm"]);

    // Monaco: 'auto3'
    [Fact]
    public void Auto3() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["ofw", "qsxmziuvzw", "rp", "qsnymek", "elth", "wmgzbwudxz", "iwsdkndh", "bujlbwb", "asuouxfv", "xuccnb"],
            [EditOp(4, 3, 4, 3, "")],
            ["ofw", "qsxmziuvzw", "rp", "qsnymek", "elth", "wmgzbwudxz", "iwsdkndh", "bujlbwb", "asuouxfv", "xuccnb"]);

    // Monaco: 'auto4'
    [Fact]
    public void Auto4() =>
        EditableTextModelAssert.TestApplyEditsWithSyncedModels(
            ["fefymj", "qum", "vmiwxxaiqq", "dz", "lnqdgorosf"],
            [EditOp(1, 3, 1, 5, "hp"), EditOp(1, 7, 2, 1, "kcg", "", "mpx"), EditOp(2, 2, 2, 2, "", "aw", ""), EditOp(2, 2, 2, 2, "vqr", "mo"), EditOp(4, 2, 5, 3, "xyc")],
            ["fehpmjkcg", "", "mpxq", "aw", "vqr", "moum", "vmiwxxaiqq", "dxycqdgorosf"]);

    // Monaco: the GENERATE_TESTS loop of editableTextModelAuto.test.ts (disabled upstream, uses Math.random).
    // Here it runs with fixed seeds; the seed is in the failure message.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void GeneratedEdits_AreAppliedAndInverted(int seed)
    {
        var random = new Random(seed);
        for (int iteration = 0; iteration < 200; iteration++)
        {
            var testModel = new GeneratedTestModel(random);
            try
            {
                EditableTextModelAssert.TestApplyEditsWithSyncedModels(
                    testModel.InitialContent.Split('\n'),
                    testModel.Edits,
                    testModel.ResultingContent.Split('\n'));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"seed {seed}, iteration {iteration}: {testModel}", ex);
            }
        }
    }

    // Monaco: getRandomInt
    private static int GetRandomInt(Random random, int min, int max) => random.Next(min, max + 1);

    // Monaco: getRandomString
    private static string GetRandomString(Random random, int minLength, int maxLength)
    {
        int length = GetRandomInt(random, minLength, maxLength);
        var chars = new char[length];
        for (int i = 0; i < length; i++)
        {
            chars[i] = (char)GetRandomInt(random, 'a', 'z');
        }

        return new string(chars);
    }

    // Monaco: generateFile
    private static string GenerateFile(Random random, bool small)
    {
        int lineCount = GetRandomInt(random, 1, small ? 3 : 10);
        var lines = new string[lineCount];
        for (int i = 0; i < lineCount; i++)
        {
            lines[i] = GetRandomString(random, 0, small ? 3 : 10);
        }

        return string.Join('\n', lines);
    }

    private readonly record struct GeneratedEdit(int Offset, int Length, string Text);

    // Monaco: generateEdits
    private static List<GeneratedEdit> GenerateEdits(Random random, string content)
    {
        var result = new List<GeneratedEdit>();
        int cnt = GetRandomInt(random, 1, 5);
        int maxOffset = content.Length;

        while (cnt > 0 && maxOffset > 0)
        {
            int offset = GetRandomInt(random, 0, maxOffset);
            int length = GetRandomInt(random, 0, maxOffset - offset);
            string text = GenerateFile(random, small: true);

            result.Add(new GeneratedEdit(offset, length, text));

            maxOffset = offset;
            cnt--;
        }

        result.Reverse();
        return result;
    }

    // Monaco: class TestModel
    private sealed class GeneratedTestModel
    {
        public GeneratedTestModel(Random random)
        {
            InitialContent = GenerateFile(random, small: false);

            List<GeneratedEdit> edits = GenerateEdits(random, InitialContent);

            Position[] offsetToPosition = GenerateOffsetToPosition(InitialContent);
            var textEdits = new List<TextEdit>();
            foreach (GeneratedEdit edit in edits)
            {
                Position start = offsetToPosition[edit.Offset];
                Position end = offsetToPosition[edit.Offset + edit.Length];
                textEdits.Add(TextEdit.Replace(new Range(start.LineNumber, start.Column, end.LineNumber, end.Column), edit.Text));
            }

            Edits = [.. textEdits];

            ResultingContent = InitialContent;
            for (int i = edits.Count - 1; i >= 0; i--)
            {
                ResultingContent =
                    ResultingContent[..edits[i].Offset] +
                    edits[i].Text +
                    ResultingContent[(edits[i].Offset + edits[i].Length)..];
            }
        }

        public string InitialContent { get; }

        public string ResultingContent { get; }

        public TextEdit[] Edits { get; }

        public override string ToString() =>
            $"initial: \"{InitialContent.Replace("\n", "\\n")}\", edits: [{string.Join("; ", Edits)}]";

        // Monaco: _generateOffsetToPosition
        private static Position[] GenerateOffsetToPosition(string content)
        {
            var result = new Position[content.Length + 1];
            int lineNumber = 1;
            int column = 1;

            for (int offset = 0; offset <= content.Length; offset++)
            {
                result[offset] = new Position(lineNumber, column);

                if (offset < content.Length && content[offset] == '\n')
                {
                    lineNumber++;
                    column = 1;
                }
                else
                {
                    column++;
                }
            }

            return result;
        }
    }
}
