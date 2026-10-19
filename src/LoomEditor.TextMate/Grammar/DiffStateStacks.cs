// Ported from microsoft/vscode-textmate: src/diffStateStacks.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.TextMate.Grammar;

/// <summary><c>StackDiff</c>: how to turn one state stack into another.</summary>
/// <param name="Pops"><c>pops</c>: how many elements to pop first.</param>
/// <param name="NewFrames"><c>newFrames</c>: the frames to push afterwards, outermost first.</param>
/// <remarks>Threading: treat as immutable.</remarks>
internal sealed record StackDiff(int Pops, List<StateStackFrame> NewFrames);

/// <summary>The functions of <c>diffStateStacks.ts</c>.</summary>
/// <remarks>Threading: stateless.</remarks>
internal static class DiffStateStacks
{
    /// <summary>
    /// <c>diffStateStacksRefEq</c>: the diff between two state stacks, which share elements by reference (the stacks
    /// of consecutive lines do).
    /// </summary>
    public static StackDiff DiffStateStacksRefEq(StateStack first, StateStack second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        int pops = 0;
        var newFrames = new List<StateStackFrame>();

        StateStackImpl? curFirst = (StateStackImpl)first;
        StateStackImpl? curSecond = (StateStackImpl)second;

        while (!ReferenceEquals(curFirst, curSecond))
        {
            if (curFirst is not null && (curSecond is null || curFirst.Depth >= curSecond.Depth))
            {
                // curFirst is certainly not contained in curSecond
                pops++;
                curFirst = curFirst.Parent;
            }
            else
            {
                // curSecond is certainly not contained in curFirst.
                // Also, curSecond must be defined, as otherwise a previous case would match
                newFrames.Add(curSecond!.ToStateStackFrame());
                curSecond = curSecond.Parent;
            }
        }

        newFrames.Reverse();
        return new StackDiff(pops, newFrames);
    }

    /// <summary><c>applyStateStackDiff</c>.</summary>
    public static StateStackImpl? ApplyStateStackDiff(StateStack? stack, StackDiff diff)
    {
        ArgumentNullException.ThrowIfNull(diff);
        var curStack = (StateStackImpl?)stack;
        for (int i = 0; i < diff.Pops; i++)
        {
            curStack = curStack!.Parent;
        }

        foreach (StateStackFrame frame in diff.NewFrames)
        {
            curStack = StateStackImpl.PushFrame(curStack, frame);
        }

        return curStack;
    }
}
