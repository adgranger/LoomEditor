using LoomEditor.Core.Model.BracketPairs.Tree;

namespace LoomEditor.Core.Tests.Model.BracketPairs.Tree.TestHelpers;

internal static class AstAssert
{
    // Monaco: 'equals' helper of concat23Trees.test.ts
    public static bool StructurallyEqual(AstNode node1, AstNode node2)
    {
        if (node1.Length != node2.Length || node1.Children.Count != node2.Children.Count)
        {
            return false;
        }

        for (var i = 0; i < node1.Children.Count; i++)
        {
            if (!StructurallyEqual(node1.Children[i], node2.Children[i]))
            {
                return false;
            }
        }

        return node1.MissingOpeningBracketIds.Equals(node2.MissingOpeningBracketIds) && node1.Kind == node2.Kind;
    }
}
