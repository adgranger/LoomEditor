using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Snippets;
using LoomEditor.Core.Text;
using LoomEditor.Core.Undo;
using LoomEditor.View.Features;
using LoomEditor.View.Features.Snippet;

namespace LoomEditor.View.Tests.Features.Snippet;

// Monaco: suite 'SnippetSession' (snippetSession.test.ts). The tests that only move between placeholders are ported here, through a
// small harness that does what SnippetSession.insert / merge do (P10-24 ports the rest); the others are new.
public class OneSnippetTests
{
    private const string Text = "function foo() {\n    console.log(a);\n}";

    private sealed class Harness : IDisposable
    {
        public Harness(string text = Text, params Selection[] selections)
        {
            Context = new EditorContext();
            Context.SetText(text);
            Context.SetSelections(selections.Length > 0 ? selections : [new Selection(1, 1, 1, 1), new Selection(2, 5, 2, 5)]);
        }

        public EditorContext Context { get; }

        public List<OneSnippet> Snippets { get; private set; } = [];

        public string Value => Context.Model.GetValue();

        // Monaco: SnippetSession.insert: one snippet per selection, the edits, then the first move.
        public void Insert(string template) => Snippets = InsertSnippets(template, out _);

        // Monaco: SnippetSession.merge: nested snippets replace the active placeholders of the outer ones.
        public void Merge(string template)
        {
            List<OneSnippet> nested = InsertSnippets(template, out _);
            foreach (OneSnippet outer in Snippets)
            {
                int count = outer.ActivePlaceholderCount;
                outer.Merge(nested.GetRange(0, count));
                nested.RemoveRange(0, count);
            }

            MoveAll(null);
        }

        public void Move(bool? forward) => MoveAll(forward);

        public void Type(string text) => Context.ViewModel.Type(text, "test");

        public void AssertSelections(params Selection[] expected)
        {
            IReadOnlyList<Selection> actual = Context.Selections;
            Assert.Equal(expected.Length, actual.Count);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.True(actual[i] == expected[i], $"actual={actual[i]} <> expected={expected[i]}");
            }
        }

        public bool IsAtLastPlaceholder => Snippets.All(snippet => snippet.IsAtLastPlaceholder);

        public void Dispose()
        {
            foreach (OneSnippet snippet in Snippets)
            {
                snippet.Dispose();
            }

            Context.Dispose();
        }

        private List<OneSnippet> InsertSnippets(string template, out List<TextChange> changes)
        {
            IReadOnlyList<Selection> selections = Context.Selections;
            List<TextmateSnippet> parsed = selections.Select(_ => new SnippetParser().Parse(template, true)).ToList();
            var edits = selections.Select((selection, i) => new IdentifiedEditOperation(selection.Range, parsed[i].ToString())).ToList();
            var captured = new List<TextChange>();
            Context.ViewModel.ExecuteEdits("snippet", edits, operations =>
            {
                captured.AddRange(operations.Select(operation => operation.TextChange).OrderBy(change => change.NewPosition));
                return null;
            });
            changes = captured;

            var snippets = new List<OneSnippet>();
            for (int i = 0; i < parsed.Count; i++)
            {
                var snippet = new OneSnippet(Context, parsed[i], string.Empty);
                snippet.Initialize(captured[i]);
                snippets.Add(snippet);
            }

            if (Snippets.Count == 0)
            {
                Snippets = snippets;
                MoveAll(true);
            }
            else
            {
                // Nested snippets are only initialized: the outer snippets merge them and move.
            }

            return snippets;
        }

        private void MoveAll(bool? forward) =>
            Context.SetSelections(Snippets.SelectMany(snippet => snippet.Move(forward)).ToList());
    }

    // Monaco: 'snippets, repeated tabstops'
    [Fact]
    public void RepeatedTabstops()
    {
        using var h = new Harness();
        h.Insert("${1:abc}foo${1:abc}$0");
        h.AssertSelections(new Selection(1, 1, 1, 4), new Selection(1, 7, 1, 10), new Selection(2, 5, 2, 8), new Selection(2, 11, 2, 14));

        h.Move(true);

        h.AssertSelections(new Selection(1, 10, 1, 10), new Selection(2, 14, 2, 14));
    }

    // Monaco: 'snippets, selections -> next/prev'
    [Fact]
    public void SelectionsNextPrev()
    {
        using var h = new Harness();
        h.Insert("f$1oo${2:bar}foo$0");

        h.AssertSelections(new Selection(1, 2, 1, 2), new Selection(2, 6, 2, 6));
        h.Move(true);
        h.AssertSelections(new Selection(1, 4, 1, 7), new Selection(2, 8, 2, 11));
        h.Move(false);
        h.AssertSelections(new Selection(1, 2, 1, 2), new Selection(2, 6, 2, 6));
        h.Move(true);
        h.AssertSelections(new Selection(1, 4, 1, 7), new Selection(2, 8, 2, 11));
        h.Move(true);
        h.AssertSelections(new Selection(1, 10, 1, 10), new Selection(2, 14, 2, 14));
    }

    // Monaco: 'snippets, selections & typing'
    [Fact]
    public void SelectionsAndTyping()
    {
        using var h = new Harness();
        h.Insert("f${1:oo}_$2_$0");

        h.Type("X");
        h.Move(true);
        h.Type("bar");

        // go back to ${1:oo} which is now just 'X'
        h.Move(false);
        h.AssertSelections(new Selection(1, 2, 1, 3), new Selection(2, 6, 2, 7));

        // go forward to $2 which is now 'bar'
        h.Move(true);
        h.AssertSelections(new Selection(1, 4, 1, 7), new Selection(2, 8, 2, 11));

        // go to the final tabstop
        h.Move(true);
        Assert.Equal("fX_bar_function foo() {\n    fX_bar_console.log(a);\n}", h.Value);
        h.AssertSelections(new Selection(1, 8, 1, 8), new Selection(2, 12, 2, 12));
    }

    // Monaco: 'snippets, don't grow final tabstop'
    [Fact]
    public void DoNotGrowFinalTabstop()
    {
        using var h = new Harness("foo_zzz_foo", new Selection(1, 5, 1, 8));
        h.Insert("$1bar$0");
        h.AssertSelections(new Selection(1, 5, 1, 5));

        h.Type("foo-");
        h.Move(true);
        Assert.Equal("foo_foo-bar_foo", h.Value);
        h.AssertSelections(new Selection(1, 12, 1, 12));

        h.Type("XXX");
        Assert.Equal("foo_foo-barXXX_foo", h.Value);
        h.Move(false);
        h.AssertSelections(new Selection(1, 5, 1, 9));
        h.Move(true);
        h.AssertSelections(new Selection(1, 15, 1, 15));
    }

    // Monaco: 'snippets, don't merge touching tabstops 1/2'
    [Fact]
    public void DoNotMergeTouchingTabstops_1()
    {
        using var h = new Harness();
        h.Insert("$1$2$3$0");
        h.AssertSelections(new Selection(1, 1, 1, 1), new Selection(2, 5, 2, 5));

        h.Move(true);
        h.AssertSelections(new Selection(1, 1, 1, 1), new Selection(2, 5, 2, 5));
        h.Move(true);
        h.AssertSelections(new Selection(1, 1, 1, 1), new Selection(2, 5, 2, 5));
        h.Move(true);
        h.AssertSelections(new Selection(1, 1, 1, 1), new Selection(2, 5, 2, 5));

        h.Move(false);
        h.Move(false);
        h.Move(false);
        h.AssertSelections(new Selection(1, 1, 1, 1), new Selection(2, 5, 2, 5));
        h.Type("111");

        h.Move(true);
        h.Type("222");

        h.Move(true);
        h.Type("333");

        h.Move(true);
        Assert.Equal("111222333function foo() {\n    111222333console.log(a);\n}", h.Value);
        h.AssertSelections(new Selection(1, 10, 1, 10), new Selection(2, 14, 2, 14));

        h.Move(false);
        h.AssertSelections(new Selection(1, 7, 1, 10), new Selection(2, 11, 2, 14));
        h.Move(false);
        h.AssertSelections(new Selection(1, 4, 1, 7), new Selection(2, 8, 2, 11));
        h.Move(false);
        h.AssertSelections(new Selection(1, 1, 1, 4), new Selection(2, 5, 2, 8));
    }

    // Monaco: 'snippets, don't merge touching tabstops 2/2'
    [Fact]
    public void DoNotMergeTouchingTabstops_2()
    {
        using var h = new Harness();
        h.Insert("$1$2$3$0");
        h.AssertSelections(new Selection(1, 1, 1, 1), new Selection(2, 5, 2, 5));

        h.Type("111");
        h.Move(true);
        h.AssertSelections(new Selection(1, 4, 1, 4), new Selection(2, 8, 2, 8));
        h.Type("222");

        h.Move(true);
        h.AssertSelections(new Selection(1, 7, 1, 7), new Selection(2, 11, 2, 11));
        h.Type("333");

        h.Move(true);
        Assert.True(h.IsAtLastPlaceholder);
    }

    // Monaco: 'snippets, gracefully move over final tabstop'
    [Fact]
    public void GracefullyMoveOverFinalTabstop()
    {
        using var h = new Harness();
        h.Insert("${1}bar$0");

        Assert.False(h.IsAtLastPlaceholder);
        h.AssertSelections(new Selection(1, 1, 1, 1), new Selection(2, 5, 2, 5));

        h.Move(true);
        Assert.True(h.IsAtLastPlaceholder);
        h.AssertSelections(new Selection(1, 4, 1, 4), new Selection(2, 8, 2, 8));

        h.Move(true);
        Assert.True(h.IsAtLastPlaceholder);
        h.AssertSelections(new Selection(1, 4, 1, 4), new Selection(2, 8, 2, 8));
    }

    // Monaco: 'snippets, overwriting nested placeholder'
    [Fact]
    public void OverwritingNestedPlaceholder()
    {
        using var h = new Harness();
        h.Insert("log(${1:\"$2\"});$0");
        h.AssertSelections(new Selection(1, 5, 1, 7), new Selection(2, 9, 2, 11));

        h.Type("XXX");
        Assert.Equal("log(XXX);function foo() {\n    log(XXX);console.log(a);\n}", h.Value);

        h.Move(true);
        Assert.False(h.IsAtLastPlaceholder);

        h.Move(true);
        Assert.True(h.IsAtLastPlaceholder);
        h.AssertSelections(new Selection(1, 10, 1, 10), new Selection(2, 14, 2, 14));
    }

    // Monaco: 'snippets, typing with nested placeholder' (the cut is a typing over the selection)
    [Fact]
    public void TypingWithNestedPlaceholder()
    {
        using var h = new Harness(Text, new Selection(1, 1, 1, 1));
        h.Insert("This ${1:is ${2:nested}}.$0");
        h.AssertSelections(new Selection(1, 6, 1, 15));

        h.Move(true);
        h.AssertSelections(new Selection(1, 9, 1, 15));

        h.Type("XXX");
        h.Move(false);
        h.AssertSelections(new Selection(1, 6, 1, 12));
    }

    // Monaco: 'snippets, merge'
    [Fact]
    public void Merge_Monaco()
    {
        using var h = new Harness(Text, new Selection(1, 1, 1, 1));
        h.Insert("This ${1:is ${2:nested}}.$0");
        h.Move(true);
        h.AssertSelections(new Selection(1, 9, 1, 15));

        h.Merge("really ${1:nested}$0");
        h.AssertSelections(new Selection(1, 16, 1, 22));

        h.Move(true);
        h.AssertSelections(new Selection(1, 22, 1, 22));
        Assert.False(h.IsAtLastPlaceholder);

        h.Move(true);
        Assert.True(h.IsAtLastPlaceholder);
        h.AssertSelections(new Selection(1, 23, 1, 23));

        h.Move(false);
        h.Type("AAA");

        // back to `really ${1:nested}`
        h.Move(false);
        h.AssertSelections(new Selection(1, 16, 1, 22));

        // back to `${1:is ...}` which now grew
        h.Move(false);
        h.AssertSelections(new Selection(1, 6, 1, 25));
    }

    // Monaco: 'snippets, next does not throw when placeholder decorations are missing'
    [Fact]
    public void NextDoesNotThrowWhenPlaceholderDecorationsAreMissing()
    {
        using var h = new Harness(string.Empty, new Selection(1, 1, 1, 1));
        h.Insert("${1:foo}$0");

        var ids = Enumerable.Range(1, h.Context.Model.LineCount)
            .SelectMany(line => h.Context.GetLineDecorations(line))
            .Where(decoration => decoration.Options.ClassName is "snippet-placeholder" or "finish-snippet-placeholder")
            .Select(decoration => decoration.Id)
            .ToList();
        Assert.NotEmpty(ids);
        h.Context.RemoveDecorations(ids);

        Assert.Empty(h.Snippets[0].Move(true));
    }

    // Monaco: 'snippets, deep merge does not produce phantom cursors (#279349)'
    [Fact]
    public void DeepMergeDoesNotProducePhantomCursors_279349()
    {
        using var h = new Harness(string.Empty, new Selection(1, 1, 1, 1));
        h.Insert("\\sin\\left(${1:x}\\right) ");
        for (int i = 0; i < 25; i++)
        {
            h.Merge("\\sin\\left(${1:x}\\right) ");

            Assert.True(h.Context.Selections.Count == 1, $"selection count after {i + 1} merges");
        }
    }

    [Fact]
    public void Move_Forward_VisitsPlaceholdersInOrderThenFinal()
    {
        using var h = new Harness("", new Selection(1, 1, 1, 1));
        h.Insert("${2:b}${1:a}$0");

        h.AssertSelections(new Selection(1, 2, 1, 3));
        h.Move(true);
        h.AssertSelections(new Selection(1, 1, 1, 2));
        h.Move(true);
        h.AssertSelections(new Selection(1, 3, 1, 3));
        Assert.True(h.IsAtLastPlaceholder);
    }

    [Fact]
    public void Move_Backward_AtFirst_StaysAtFirst()
    {
        using var h = new Harness("", new Selection(1, 1, 1, 1));
        h.Insert("${1:a}${2:b}$0");

        h.Move(false);

        Assert.True(h.Snippets[0].IsAtFirstPlaceholder);
        h.AssertSelections(new Selection(1, 1, 1, 2));
    }

    [Fact]
    public void Move_SameIndexPlaceholders_SelectedTogether()
    {
        using var h = new Harness("", new Selection(1, 1, 1, 1));
        h.Insert("${1:a}-${1:a}$0");

        h.AssertSelections(new Selection(1, 1, 1, 2), new Selection(1, 3, 1, 4));
        Assert.Equal(2, h.Snippets[0].ActivePlaceholderCount);
    }

    [Fact]
    public void Move_AppliesTransformOfLeftPlaceholder()
    {
        using var h = new Harness("", new Selection(1, 1, 1, 1));
        h.Insert("${1/(.*)/${1:/upcase}/}-$0");

        h.Type("ab");
        h.Move(true);

        Assert.Equal("AB-", h.Value);
        h.AssertSelections(new Selection(1, 4, 1, 4));
    }

    [Fact]
    public void Move_EmptyPlaceholder_ReturnsCaret()
    {
        using var h = new Harness("", new Selection(1, 1, 1, 1));
        h.Insert("a$1b$0");

        h.AssertSelections(new Selection(1, 2, 1, 2));
    }

    [Fact]
    public void Typing_InActivePlaceholder_GrowsDecoration()
    {
        using var h = new Harness("", new Selection(1, 1, 1, 1));
        h.Insert("$1$0");

        h.Type("XY");

        h.AssertSelections(new Selection(1, 3, 1, 3));
        Assert.Equal(new Selection(1, 1, 1, 3), h.Snippets[0].Move(null).Single());
    }

    [Fact]
    public void Typing_AfterInactivePlaceholder_DoesNotGrowIt()
    {
        using var h = new Harness("", new Selection(1, 1, 1, 1));
        h.Insert("${1:ab}${2:cd}$0");
        h.Move(true);

        h.Type("X");

        Assert.Equal("abX", h.Value);
        Assert.Equal(new Selection(1, 1, 1, 3), h.Snippets[0].Move(false).Single());
    }

    [Fact]
    public void Merge_NestedSnippet_RenumbersPlaceholders()
    {
        using var h = new Harness("", new Selection(1, 1, 1, 1));
        h.Insert("${1:a}$0");

        h.Merge("x${1:y}z${2:w}$0");

        Assert.Equal([1d, 2d, 3d], h.Snippets[0].ComputePossibleSelections().Keys.Order());
        Assert.Equal(1, h.Snippets[0].NestingLevel);
        h.AssertSelections(new Selection(1, 2, 1, 3));
    }

    [Fact]
    public void IsTrivialSnippet_OnlyFinalTabstop_True()
    {
        using var h = new Harness("", new Selection(1, 1, 1, 1));
        using var trivial = new OneSnippet(h.Context, new SnippetParser().Parse("abc$0"), string.Empty);
        using var plain = new OneSnippet(h.Context, new SnippetParser().Parse("abc"), string.Empty);
        using var notTrivial = new OneSnippet(h.Context, new SnippetParser().Parse("a$1"), string.Empty);
        using var middleFinal = new OneSnippet(h.Context, new SnippetParser().Parse("a$0b"), string.Empty);

        Assert.True(trivial.IsTrivialSnippet);
        Assert.True(plain.IsTrivialSnippet);
        Assert.False(notTrivial.IsTrivialSnippet);
        Assert.False(middleFinal.IsTrivialSnippet);
    }

    [Fact]
    public void ActiveChoice_OnChoicePlaceholder()
    {
        using var h = new Harness("", new Selection(1, 1, 1, 1));
        h.Insert("${1|a,b|}$0");

        (Choice Choice, Range Range)? active = h.Snippets[0].ActiveChoice;

        Assert.NotNull(active);
        Assert.Equal(2, active.Value.Choice.Options.Count);
        Assert.Equal(new Range(1, 1, 1, 2), active.Value.Range);
        Assert.True(h.Snippets[0].HasChoice);
    }

    [Fact]
    public void Dispose_RemovesDecorations()
    {
        var h = new Harness("", new Selection(1, 1, 1, 1));
        h.Insert("${1:a}$0");
        int Count() => h.Context.GetLineDecorations(1).Count(decoration => decoration.Options.ClassName is "snippet-placeholder" or "finish-snippet-placeholder");
        Assert.Equal(2, Count());

        h.Snippets[0].Dispose();

        Assert.Equal(0, Count());
        h.Context.Dispose();
    }
}
