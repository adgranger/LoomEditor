// Not ported from Monaco: LoomEditor property test comparing the piece tree with a naive reference buffer.

using LoomEditor.Core.Text;
using LoomEditor.Core.Tests.Model.PieceTree.TestHelpers;

namespace LoomEditor.Core.Tests.Model.PieceTree;

public class PieceTreePropertyTests
{
    private const string InsertAlphabet = "abc\r\n\U0001F600";

    public static TheoryData<int> Seeds => [101, 102, 103, 104, 105, 106, 107, 108, 109, 110];

    [Theory]
    [MemberData(nameof(Seeds))]
    public void RandomEdits_AgainstNaiveBuffer_StayEquivalent(int seed)
    {
        var random = new Random(seed);
        var naive = new NaiveTextBuffer();
        var tree = TestTextBuffers.Create([""], normalizeEol: false).GetPieceTree();

        for (int op = 0; op < 2000; op++)
        {
            string context = $"seed {seed}, operation {op}";
            if (naive.Length == 0 || random.Next(2) == 0)
            {
                int offset = random.Next(naive.Length + 1);
                var value = new char[random.Next(21)];
                for (int i = 0; i < value.Length; i++)
                {
                    value[i] = InsertAlphabet[random.Next(InsertAlphabet.Length)];
                }

                naive.Insert(offset, new string(value));
                tree.Insert(offset, new string(value));
            }
            else
            {
                int offset = random.Next(naive.Length);
                int count = random.Next(Math.Min(naive.Length - offset, 20) + 1);
                naive.Delete(offset, count);
                tree.Delete(offset, count);
            }

            Assert.True(naive.GetText() == tree.GetLinesRawContent(), $"Text differs ({context})");

            List<string> lines = naive.GetLines();
            Assert.True(lines.Count == tree.GetLineCount(), $"Line count differs ({context})");
            for (int i = 0; i < lines.Count; i++)
            {
                Assert.True(lines[i] == tree.GetLineContent(i + 1), $"Line {i + 1} differs ({context})");
            }

            for (int i = 0; i < 20; i++)
            {
                int offset = random.Next(naive.Length + 1);
                Position expected = naive.GetPositionAt(offset);
                Assert.True(expected == tree.GetPositionAt(offset), $"GetPositionAt({offset}) differs ({context})");
                Assert.True(
                    naive.GetOffsetAt(expected.LineNumber, expected.Column) == tree.GetOffsetAt(expected.LineNumber, expected.Column),
                    $"GetOffsetAt({expected.LineNumber}, {expected.Column}) differs ({context})");
            }

            TreeInvariants.AssertValid(tree.Tree);
        }
    }

    [Fact]
    public void InsertEmpty_InsideCrlf_KeepsLineCount()
    {
        var m = new MirroredPieceTree("", normalizeEol: false);
        m.Insert(0, "a\n\n\r\r\r\n");

        m.Insert(6, "");

        Assert.Equal(6, m.Tree.GetLineCount());
        m.AssertContent();
        PieceTreeAssert.LinesContent(m.Text, m.Tree);
        TreeInvariants.AssertValid(m.Tree.Tree);
    }
}
