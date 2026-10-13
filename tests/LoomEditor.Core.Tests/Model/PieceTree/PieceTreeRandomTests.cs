// Ported from microsoft/vscode: src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts (suite 'random is unsupervised')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Tests.Model.PieceTree.TestHelpers;

namespace LoomEditor.Core.Tests.Model.PieceTree;

public class PieceTreeRandomTests
{
    public static TheoryData<int> Seeds => [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

    // Monaco: 'splitting large change buffer'
    [Fact]
    public void SplittingLargeChangeBuffer()
    {
        var m = new MirroredPieceTree("", normalizeEol: false);
        m.Insert(0, "WUZ\nXVZY\n");
        m.Insert(8, "\r\r\nZXUWVW");
        m.Delete(10, 7);
        m.Delete(10, 1);
        m.Insert(4, "VX\r\r\nWZVZ");
        m.Delete(11, 3);
        m.Delete(12, 4);
        m.Delete(8, 0);
        m.Delete(10, 2);
        m.Insert(0, "VZXXZYZX\r");

        AssertAll(m, 0);
    }

    // Monaco: 'random insert delete'
    [Theory]
    [MemberData(nameof(Seeds))]
    public void RandomInsertDelete(int seed)
    {
        var random = new Random(seed);
        var m = new MirroredPieceTree("", normalizeEol: false);

        RunRandomEdits(random, m, 1000, 100);

        AssertAll(m, seed);
    }

    // Monaco: 'random chunks'
    [Theory]
    [MemberData(nameof(Seeds))]
    public void RandomChunks(int seed)
    {
        var random = new Random(seed);
        var chunks = new string[5];
        for (int i = 0; i < chunks.Length; i++)
        {
            chunks[i] = RandomText.RandomStr(random, 1000);
        }

        var m = new MirroredPieceTree(chunks, normalizeEol: false);

        RunRandomEdits(random, m, 1000, 100);

        AssertAll(m, seed);
    }

    // Monaco: 'random chunks 2'
    [Theory]
    [MemberData(nameof(Seeds))]
    public void RandomChunks2(int seed)
    {
        var random = new Random(seed);
        var m = new MirroredPieceTree([RandomText.RandomStr(random, 1000)], normalizeEol: false);

        RunRandomEdits(random, m, 50, 30, () => PieceTreeAssert.LinesContent(m.Text, m.Tree));

        AssertAll(m, seed);
    }

    private static void RunRandomEdits(Random random, MirroredPieceTree m, int count, int maxInsertLength, Action? afterEach = null)
    {
        for (int i = 0; i < count; i++)
        {
            if (random.NextDouble() < 0.6)
            {
                string text = RandomText.RandomStr(random, maxInsertLength);
                m.Insert(RandomText.RandomInt(random, m.Text.Length + 1), text);
            }
            else
            {
                int pos = RandomText.RandomInt(random, m.Text.Length);
                int length = Math.Min(m.Text.Length - pos, (int)Math.Floor(random.NextDouble() * 10));
                m.Delete(pos, length);
            }

            afterEach?.Invoke();
        }
    }

    private static void AssertAll(MirroredPieceTree m, int seed)
    {
        try
        {
            m.AssertContent();
            PieceTreeAssert.LineStarts(m.Text, m.Tree);
            PieceTreeAssert.LinesContent(m.Text, m.Tree);
            TreeInvariants.AssertValid(m.Tree.Tree);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException($"Random test failed for seed {seed}", e);
        }
    }
}
