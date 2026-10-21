using LoomEditor.Core.Languages;
using LoomEditor.Core.Tests.Languages.TestHelpers;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Tests.Features.Indentation;

// Monaco: suite 'Auto Indent On Type - Visual Basic' (every test is named 'issue #118932: ...').
// Each test selects (when asked), types some text without a source, then some keyboard text, and checks the model.
public partial class AutoIndentOnTypeTests
{
    private static void TypeInVisualBasic(string[] initial, Selection? selection, string? typed, string? keyboard, string[] expected) =>
        Run(TestLanguages.VB, Join(initial), EditorAutoIndentStrategy.Full, (_, model, viewModel) =>
        {
            if (selection is { } s)
            {
                SetSelection(viewModel, s);
            }

            if (typed is not null)
            {
                viewModel.Type(typed);
            }

            if (keyboard is not null)
            {
                viewModel.Type(keyboard, "keyboard");
            }

            Assert.Equal(Join(expected), model.GetValue());
        });

    // Monaco: 'issue #118932: no indentation in visual basic files'
    [Fact]
    public void Issue118932_NoIndentationInVisualBasicFiles() =>
        TypeInVisualBasic(["If True Then", "    Some code", "    End I"], new Selection(3, 10, 3, 10), null, "f", ["If True Then", "    Some code", "End If"]);

    // Monaco: 'issue #118932: indent after Module declaration'
    [Fact]
    public void Issue118932_IndentAfterModuleDeclaration() =>
        TypeInVisualBasic([], null, "Module Test", "\n", ["Module Test", "    "]);

    // Monaco: 'issue #118932: indent after Sub declaration'
    [Fact]
    public void Issue118932_IndentAfterSubDeclaration() =>
        TypeInVisualBasic(["Module Test", "    "], new Selection(2, 5, 2, 5), "Sub Main()", "\n", ["Module Test", "    Sub Main()", "        "]);

    // Monaco: 'issue #118932: dedent on End Sub'
    [Fact]
    public void Issue118932_DedentOnEndSub() =>
        TypeInVisualBasic(
            ["Module Test", "    Sub Main()", "        Console.WriteLine(\"Hello\")", "        End Su"],
            new Selection(4, 15, 4, 15),
            null,
            "b",
            ["Module Test", "    Sub Main()", "        Console.WriteLine(\"Hello\")", "    End Sub"]);

    // Monaco: 'issue #118932: dedent on End Module' (End Module typed right after Module, no nested blocks)
    [Fact]
    public void Issue118932_DedentOnEndModule() =>
        TypeInVisualBasic(["Module Test", "    Private x As Integer", "    End Modul"], new Selection(3, 14, 3, 14), null, "e", ["Module Test", "    Private x As Integer", "End Module"]);

    // Monaco: 'issue #118932: indent after Function declaration'
    [Fact]
    public void Issue118932_IndentAfterFunctionDeclaration() =>
        TypeInVisualBasic(
            ["Module Test", "    "],
            new Selection(2, 5, 2, 5),
            "Function Add(a As Integer, b As Integer) As Integer",
            "\n",
            ["Module Test", "    Function Add(a As Integer, b As Integer) As Integer", "        "]);

    // Monaco: 'issue #118932: dedent on End Function'
    [Fact]
    public void Issue118932_DedentOnEndFunction() =>
        TypeInVisualBasic(
            ["Module Test", "    Function Add(a, b)", "        Return a + b", "        End Functio"],
            new Selection(4, 20, 4, 20),
            null,
            "n",
            ["Module Test", "    Function Add(a, b)", "        Return a + b", "    End Function"]);

    // Monaco: 'issue #118932: indent after If Then'
    [Fact]
    public void Issue118932_IndentAfterIfThen() =>
        TypeInVisualBasic(["Sub Test()", "    "], new Selection(2, 5, 2, 5), "If x > 0 Then", "\n", ["Sub Test()", "    If x > 0 Then", "        "]);

    // Monaco: 'issue #118932: indent after ElseIf Then'
    [Fact]
    public void Issue118932_IndentAfterElseIfThen() =>
        TypeInVisualBasic(
            ["Sub Test()", "    If x > 0 Then", "        DoSomething()", "    ElseIf x < 0 Then"],
            new Selection(4, 22, 4, 22),
            null,
            "\n",
            ["Sub Test()", "    If x > 0 Then", "        DoSomething()", "    ElseIf x < 0 Then", "        "]);

    // Monaco: 'issue #118932: dedent and indent on Else'
    [Fact]
    public void Issue118932_DedentAndIndentOnElse() =>
        TypeInVisualBasic(
            ["Sub Test()", "    If x > 0 Then", "        DoSomething()", "        Els"],
            new Selection(4, 12, 4, 12),
            null,
            "e",
            ["Sub Test()", "    If x > 0 Then", "        DoSomething()", "    Else"]);

    // Monaco: 'issue #118932: indent after While'
    [Fact]
    public void Issue118932_IndentAfterWhile() =>
        TypeInVisualBasic(["Sub Test()", "    "], new Selection(2, 5, 2, 5), "While x > 0", "\n", ["Sub Test()", "    While x > 0", "        "]);

    // Monaco: 'issue #118932: dedent on End While'
    [Fact]
    public void Issue118932_DedentOnEndWhile() =>
        TypeInVisualBasic(
            ["Sub Test()", "    While x > 0", "        x = x - 1", "        End Whil"],
            new Selection(4, 17, 4, 17),
            null,
            "e",
            ["Sub Test()", "    While x > 0", "        x = x - 1", "    End While"]);

    // Monaco: 'issue #118932: indent after For'
    [Fact]
    public void Issue118932_IndentAfterFor() =>
        TypeInVisualBasic(["Sub Test()", "    "], new Selection(2, 5, 2, 5), "For i = 1 To 10", "\n", ["Sub Test()", "    For i = 1 To 10", "        "]);

    // Monaco: 'issue #118932: dedent on Next'
    [Fact]
    public void Issue118932_DedentOnNext() =>
        TypeInVisualBasic(
            ["Sub Test()", "    For i = 1 To 10", "        DoSomething(i)", "        Nex"],
            new Selection(4, 12, 4, 12),
            null,
            "t",
            ["Sub Test()", "    For i = 1 To 10", "        DoSomething(i)", "    Next"]);

    // Monaco: 'issue #118932: indent after Do'
    [Fact]
    public void Issue118932_IndentAfterDo() =>
        TypeInVisualBasic(["Sub Test()", "    "], new Selection(2, 5, 2, 5), "Do", "\n", ["Sub Test()", "    Do", "        "]);

    // Monaco: 'issue #118932: dedent on Loop'
    [Fact]
    public void Issue118932_DedentOnLoop() =>
        TypeInVisualBasic(
            ["Sub Test()", "    Do", "        x = x + 1", "        Loo"],
            new Selection(4, 12, 4, 12),
            null,
            "p",
            ["Sub Test()", "    Do", "        x = x + 1", "    Loop"]);

    // Monaco: 'issue #118932: indent after Select Case'
    [Fact]
    public void Issue118932_IndentAfterSelectCase() =>
        TypeInVisualBasic(["Sub Test()", "    "], new Selection(2, 5, 2, 5), "Select Case x", "\n", ["Sub Test()", "    Select Case x", "        "]);

    // Monaco: 'issue #118932: dedent on End Select' (it dedents to match the Select Case level)
    [Fact]
    public void Issue118932_DedentOnEndSelect() =>
        TypeInVisualBasic(
            ["Sub Test()", "    Select Case x", "        End Selec"],
            new Selection(3, 18, 3, 18),
            null,
            "t",
            ["Sub Test()", "    Select Case x", "    End Select"]);

    // Monaco: 'issue #118932: indent after Try'
    [Fact]
    public void Issue118932_IndentAfterTry() =>
        TypeInVisualBasic(["Sub Test()", "    "], new Selection(2, 5, 2, 5), "Try", "\n", ["Sub Test()", "    Try", "        "]);

    // Monaco: 'issue #118932: dedent and indent on Catch'
    [Fact]
    public void Issue118932_DedentAndIndentOnCatch() =>
        TypeInVisualBasic(
            ["Sub Test()", "    Try", "        DoSomething()", "        Catc"],
            new Selection(4, 13, 4, 13),
            null,
            "h",
            ["Sub Test()", "    Try", "        DoSomething()", "    Catch"]);

    // Monaco: 'issue #118932: dedent and indent on Finally'
    [Fact]
    public void Issue118932_DedentAndIndentOnFinally() =>
        TypeInVisualBasic(
            ["Sub Test()", "    Try", "        DoSomething()", "    Catch", "        HandleError()", "        Finall"],
            new Selection(6, 15, 6, 15),
            null,
            "y",
            ["Sub Test()", "    Try", "        DoSomething()", "    Catch", "        HandleError()", "    Finally"]);

    // Monaco: 'issue #118932: dedent on End Try'
    [Fact]
    public void Issue118932_DedentOnEndTry() =>
        TypeInVisualBasic(
            ["Sub Test()", "    Try", "        DoSomething()", "    Catch", "        HandleError()", "    Finally", "        Cleanup()", "        End Tr"],
            new Selection(8, 15, 8, 15),
            null,
            "y",
            ["Sub Test()", "    Try", "        DoSomething()", "    Catch", "        HandleError()", "    Finally", "        Cleanup()", "    End Try"]);

    // Monaco: 'issue #118932: indent after Class'
    [Fact]
    public void Issue118932_IndentAfterClass() =>
        TypeInVisualBasic([], null, "Class MyClass", "\n", ["Class MyClass", "    "]);

    // Monaco: 'issue #118932: dedent on End Class'
    [Fact]
    public void Issue118932_DedentOnEndClass() =>
        TypeInVisualBasic(["Class MyClass", "    Private x As Integer", "    End Clas"], new Selection(3, 14, 3, 14), null, "s", ["Class MyClass", "    Private x As Integer", "End Class"]);

    // Monaco: 'issue #118932: full program indentation flow'
    [Fact]
    public void Issue118932_FullProgramIndentationFlow() =>
        Run(TestLanguages.VB, string.Empty, EditorAutoIndentStrategy.Full, (_, model, viewModel) =>
        {
            // Type Module Test and press Enter
            viewModel.Type("Module Test");
            viewModel.Type("\n", "keyboard");
            Assert.Equal(Join("Module Test", "    "), model.GetValue());

            // Type Sub Main() and press Enter
            viewModel.Type("Sub Main()");
            viewModel.Type("\n", "keyboard");
            Assert.Equal(Join("Module Test", "    Sub Main()", "        "), model.GetValue());

            // Type Console.WriteLine and press Enter
            viewModel.Type("Console.WriteLine(\"Hello, World!\")");
            viewModel.Type("\n", "keyboard");
            Assert.Equal(Join("Module Test", "    Sub Main()", "        Console.WriteLine(\"Hello, World!\")", "        "), model.GetValue());

            // Type End Su then 'b' to complete End Sub (auto-indent triggers on last char)
            viewModel.Type("End Su");
            viewModel.Type("b", "keyboard");
            Assert.Equal(Join("Module Test", "    Sub Main()", "        Console.WriteLine(\"Hello, World!\")", "    End Sub"), model.GetValue());

            // Press Enter - should maintain same indent level after End Sub
            viewModel.Type("\n", "keyboard");
            Assert.Equal(Join("Module Test", "    Sub Main()", "        Console.WriteLine(\"Hello, World!\")", "    End Sub", "    "), model.GetValue());
        });
}
