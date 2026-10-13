// Not ported from Monaco: Monaco tests the tree only through PieceTreeBase. All tests here are LoomEditor additions.

using LoomEditor.Core.Model.PieceTree;
using LoomEditor.Core.Tests.Model.PieceTree.TestHelpers;

namespace LoomEditor.Core.Tests.Model.PieceTree;

public class RedBlackTreeTests
{
    private static Piece MakePiece(int length, int lineFeeds) => new(0, default, default, lineFeeds, length);

    private static List<TreeNode> Nodes(RedBlackTree tree)
    {
        var result = new List<TreeNode>();
        if (ReferenceEquals(tree.Root, tree.Sentinel))
        {
            return result;
        }

        for (TreeNode n = tree.Leftest(tree.Root); !ReferenceEquals(n, tree.Sentinel); n = tree.Next(n))
        {
            result.Add(n);
        }

        return result;
    }

    private static List<Piece> InOrder(RedBlackTree tree) => Nodes(tree).Select(n => n.Piece).ToList();

    [Fact]
    public void Constructor_EmptyTree_RootIsSentinelAndInvariantsHold()
    {
        var tree = new RedBlackTree();

        Assert.Same(tree.Sentinel, tree.Root);
        TreeInvariants.AssertValid(tree);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(100)]
    public void InsertRight_Append_KeepsOrderAndInvariants(int count)
    {
        var tree = new RedBlackTree();
        var expected = new List<Piece>();
        TreeNode? last = null;
        for (int i = 0; i < count; i++)
        {
            Piece piece = MakePiece(i + 1, i % 3);
            last = tree.InsertRight(last, piece);
            expected.Add(piece);
            TreeInvariants.AssertValid(tree);
        }

        Assert.Equal(expected, InOrder(tree));
    }

    [Fact]
    public void InsertLeft_BeforeFirstNode_KeepsOrderAndInvariants()
    {
        var tree = new RedBlackTree();
        var expected = new List<Piece>();
        TreeNode? first = null;
        for (int i = 0; i < 50; i++)
        {
            Piece piece = MakePiece(i + 1, i % 2);
            first = tree.InsertLeft(first, piece);
            expected.Insert(0, piece);
            TreeInvariants.AssertValid(tree);
        }

        Assert.Equal(expected, InOrder(tree));
    }

    [Fact]
    public void Insert_RandomPositions_MatchesListReference()
    {
        const int Seed = 12345;
        var random = new Random(Seed);
        var tree = new RedBlackTree();
        var reference = new List<Piece>();

        for (int i = 0; i < 500; i++)
        {
            Piece piece = MakePiece(random.Next(1, 20), random.Next(0, 4));
            if (reference.Count == 0)
            {
                tree.InsertRight(null, piece);
                reference.Add(piece);
            }
            else
            {
                int index = random.Next(reference.Count);
                TreeNode target = Nodes(tree)[index];
                if (random.Next(2) == 0)
                {
                    tree.InsertRight(target, piece);
                    reference.Insert(index + 1, piece);
                }
                else
                {
                    tree.InsertLeft(target, piece);
                    reference.Insert(index, piece);
                }
            }

            TreeInvariants.AssertValid(tree);
            Assert.True(reference.SequenceEqual(InOrder(tree)), $"order mismatch after insert {i} (seed {Seed})");
        }
    }

    [Fact]
    public void Delete_RandomNodesUntilEmpty_MatchesListReference()
    {
        const int Seed = 987;
        var random = new Random(Seed);
        var tree = new RedBlackTree();
        var reference = new List<Piece>();
        TreeNode? last = null;
        for (int i = 0; i < 200; i++)
        {
            Piece piece = MakePiece(random.Next(1, 20), random.Next(0, 4));
            last = tree.InsertRight(last, piece);
            reference.Add(piece);
        }

        while (reference.Count > 0)
        {
            int index = random.Next(reference.Count);
            TreeNode target = Nodes(tree)[index];
            tree.Delete(target);
            reference.RemoveAt(index);

            Assert.True(target.IsDetached);
            TreeInvariants.AssertValid(tree);
            Assert.True(reference.SequenceEqual(InOrder(tree)), $"order mismatch, {reference.Count} left (seed {Seed})");
        }

        Assert.Same(tree.Sentinel, tree.Root);
    }

    [Fact]
    public void UpdateTreeMetadata_AfterPieceChange_FixesAncestors()
    {
        var tree = new RedBlackTree();
        TreeNode? last = null;
        for (int i = 0; i < 40; i++)
        {
            last = tree.InsertRight(last, MakePiece(5, 1));
        }

        TreeNode node = Nodes(tree)[17];
        node.Piece = MakePiece(12, 3);
        tree.UpdateTreeMetadata(node, 7, 2);

        TreeInvariants.AssertValid(tree);
    }

    [Fact]
    public void Operations_TwoTreesInParallel_BothStayValid()
    {
        var trees = new[] { new RedBlackTree(), new RedBlackTree() };

        Parallel.For(0, 2, t =>
        {
            RedBlackTree tree = trees[t];
            var random = new Random(t + 1);
            var live = new List<TreeNode>();
            for (int i = 0; i < 3000; i++)
            {
                if (live.Count > 0 && random.Next(3) == 0)
                {
                    int index = random.Next(live.Count);
                    tree.Delete(live[index]);
                    live.RemoveAt(index);
                }
                else
                {
                    TreeNode? after = live.Count == 0 ? null : live[random.Next(live.Count)];
                    live.Add(tree.InsertRight(after, MakePiece(random.Next(1, 9), random.Next(0, 3))));
                }
            }
        });

        foreach (RedBlackTree tree in trees)
        {
            TreeInvariants.AssertValid(tree);
        }
    }
}
