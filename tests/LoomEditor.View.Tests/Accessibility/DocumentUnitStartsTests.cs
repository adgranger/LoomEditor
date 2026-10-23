using LoomEditor.View.Accessibility;
using static LoomEditor.View.Tests.Accessibility.TestHelpers.AutomationTestHelpers;

namespace LoomEditor.View.Tests.Accessibility;

public class DocumentUnitStartsTests
{
    private const string Text = "abc\ndef";

    private static void Run(Action<AutomationUnitStarts> test) => WithDocument(Text, d => test(new DocumentUnitStarts(d)));

    [Fact]
    public void S_Anywhere_IsDocumentStart() => Run(u => Assert.Equal(Pos(1, 1), u.StartAtOrBefore(Pos(2, 2))));

    [Fact]
    public void P_AtDocumentStart_IsNull() => Run(u => Assert.Null(u.PreviousStart(Pos(1, 1))));

    [Fact]
    public void P_Elsewhere_IsDocumentStart() => Run(u => Assert.Equal(Pos(1, 1), u.PreviousStart(Pos(2, 2))));

    [Fact]
    public void N_Anywhere_IsNull() => Run(u => Assert.Null(u.NextStartStrict(Pos(1, 1))));

    [Fact]
    public void NS_Anywhere_IsDocumentEnd() => Run(u => Assert.Equal(Pos(2, 4), u.NextStart(Pos(1, 1))));
}
