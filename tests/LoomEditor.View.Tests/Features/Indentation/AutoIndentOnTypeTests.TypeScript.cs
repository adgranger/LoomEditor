using LoomEditor.Core.Languages;
using LoomEditor.Core.Tests.Languages.TestHelpers;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;

namespace LoomEditor.View.Tests.Features.Indentation;

// Monaco: suite 'Auto Indent On Type - TypeScript/JavaScript' (the 11 test.skip tests are not ported)
public partial class AutoIndentOnTypeTests
{
    private const string TypeScript = TestLanguages.TypeScript;

    // Monaco: 'issue #208215: indent after arrow function'
    [Fact]
    public void Issue208215_IndentAfterArrowFunction() =>
        Run(TypeScript, string.Empty, EditorAutoIndentStrategy.Full, (_, model, viewModel) =>
        {
            viewModel.Type("const add1 = (n) =>");
            viewModel.Type("\n", "keyboard");
            Assert.Equal(Join("const add1 = (n) =>", "    "), model.GetValue());
        });

    // Monaco: 'issue #208215: indent after arrow function 2'
    [Fact]
    public void Issue208215_IndentAfterArrowFunction2() =>
        Run(TypeScript, Join("const array = [1, 2, 3, 4, 5];", "array.map(", "    v =>"), EditorAutoIndentStrategy.Full, (_, model, viewModel) =>
        {
            SetSelection(viewModel, new Selection(3, 9, 3, 9));
            viewModel.Type("\n", "keyboard");
            Assert.Equal(Join("const array = [1, 2, 3, 4, 5];", "array.map(", "    v =>", "        "), model.GetValue());
        });

    // Monaco: 'issue #116843: indent after arrow function'
    [Fact]
    public void Issue116843_IndentAfterArrowFunction() =>
        Run(TypeScript, string.Empty, EditorAutoIndentStrategy.Full, (_, model, viewModel) =>
        {
            viewModel.Type(Join("const add1 = (n) =>", "    n + 1;"));
            viewModel.Type("\n", "keyboard");
            Assert.Equal(Join("const add1 = (n) =>", "    n + 1;", string.Empty), model.GetValue());
        });

    // Monaco: 'issue #29755: do not add indentation on enter if indentation is already valid'
    [Fact]
    public void Issue29755_DoNotAddIndentationOnEnterIfIndentationIsAlreadyValid() =>
        Run(
            TypeScript,
            Join("function f() {", "    const one = 1;", "    const two = 2;", "}"),
            EditorAutoIndentStrategy.Full,
            (_, model, viewModel) =>
            {
                SetSelection(viewModel, new Selection(3, 1, 3, 1));
                viewModel.Type("\n", "keyboard");
                Assert.Equal(Join("function f() {", "    const one = 1;", string.Empty, "    const two = 2;", "}"), model.GetValue());
            });

    // Monaco: 'issue #36090'
    [Fact]
    public void Issue36090() =>
        Run(
            TypeScript,
            Join(
                "class ItemCtrl {",
                "    getPropertiesByItemId(id) {",
                "        return this.fetchItem(id)",
                "            .then(item => {",
                "                return this.getPropertiesOfItem(item);",
                "            });",
                "    }",
                "}"),
            EditorAutoIndentStrategy.Advanced,
            (_, model, viewModel) =>
            {
                SetSelection(viewModel, new Selection(7, 6, 7, 6));
                viewModel.Type("\n", "keyboard");
                Assert.Equal(
                    Join(
                        "class ItemCtrl {",
                        "    getPropertiesByItemId(id) {",
                        "        return this.fetchItem(id)",
                        "            .then(item => {",
                        "                return this.getPropertiesOfItem(item);",
                        "            });",
                        "    }",
                        "    ",
                        "}"),
                    model.GetValue());
                Assert.Equal(new Selection(8, 5, 8, 5), viewModel.GetSelection());
            });

    // Monaco: 'issue #115304: indent block comment onEnter'
    [Fact]
    public void Issue115304_IndentBlockCommentOnEnter() =>
        Run(TypeScript, Join("/** */", "function f() {}"), EditorAutoIndentStrategy.Advanced, (_, model, viewModel) =>
        {
            SetSelection(viewModel, new Selection(1, 4, 1, 4));
            viewModel.Type("\n", "keyboard");
            Assert.Equal(Join("/**", " * ", " */", "function f() {}"), model.GetValue());
            Assert.Equal(new Selection(2, 4, 2, 4), viewModel.GetSelection());
        });

    // Monaco: 'issue #43244: indent when lambda arrow function is detected, outdent when end is reached'
    [Fact]
    public void Issue43244_IndentWhenLambdaArrowFunctionIsDetectedOutdentWhenEndIsReached() =>
        Run(TypeScript, Join("const array = [1, 2, 3, 4, 5];", "array.map(_)"), EditorAutoIndentStrategy.Full, (_, model, viewModel) =>
        {
            SetSelection(viewModel, new Selection(2, 12, 2, 12));
            viewModel.Type("\n", "keyboard");
            Assert.Equal(Join("const array = [1, 2, 3, 4, 5];", "array.map(_", "    ", ")"), model.GetValue());
        });

    // Monaco: 'issue #43244: incorrect indentation after if/for/while without braces'
    [Fact]
    public void Issue43244_IncorrectIndentationAfterIfForWhileWithoutBraces() =>
        Run(TypeScript, Join("function f() {", "    if (condition)", "}"), EditorAutoIndentStrategy.Full, (_, model, viewModel) =>
        {
            SetSelection(viewModel, new Selection(2, 19, 2, 19));
            viewModel.Type("\n", "keyboard");
            Assert.Equal(Join("function f() {", "    if (condition)", "        ", "}"), model.GetValue());

            viewModel.Type("return;");
            viewModel.Type("\n", "keyboard");
            Assert.Equal(Join("function f() {", "    if (condition)", "        return;", "    ", "}"), model.GetValue());
        });

    // Monaco: 'issue #208232: incorrect indentation inside of comments'
    [Fact]
    public void Issue208232_IncorrectIndentationInsideOfComments() =>
        Run(
            TypeScript,
            Join("/**", "indentation done for {", "*/"),
            EditorAutoIndentStrategy.Full,
            (_, model, viewModel) =>
            {
                SetSelection(viewModel, new Selection(2, 23, 2, 23));
                viewModel.Type("\n", "keyboard");
                Assert.Equal(Join("/**", "indentation done for {", string.Empty, "*/"), model.GetValue());
            },
            tokens:
            [
                [new StandardTokenTypeData(0, StandardTokenType.Comment)],
                [new StandardTokenTypeData(0, StandardTokenType.Comment)],
                [new StandardTokenTypeData(0, StandardTokenType.Comment)],
            ]);

    // Monaco: 'issue #209802: allman style braces in JavaScript'
    [Fact]
    public void Issue209802_AllmanStyleBracesInJavaScript() =>
        Run(TypeScript, "if (/*condition*/)", EditorAutoIndentStrategy.Full, (_, model, viewModel) =>
        {
            SetSelection(viewModel, new Selection(1, 19, 1, 19));
            viewModel.Type("\n", "keyboard");
            Assert.Equal(Join("if (/*condition*/)", "    "), model.GetValue());

            viewModel.Type("{", "keyboard");
            Assert.Equal(Join("if (/*condition*/)", "{}"), model.GetValue());

            SetSelection(viewModel, new Selection(2, 2, 2, 2));
            viewModel.Type("\n", "keyboard");
            Assert.Equal(Join("if (/*condition*/)", "{", "    ", "}"), model.GetValue());
        });

    // Monaco: 'issue #225916: surrounding with brackets works after arrow function'
    [Theory]
    [InlineData("{", "}")]
    [InlineData("[", "]")]
    [InlineData("(", ")")]
    public void Issue225916_SurroundingWithBracketsWorksAfterArrowFunction(string open, string close) =>
        Run(TypeScript, Join("const fn = () =>", "    doSomething()"), EditorAutoIndentStrategy.Full, (_, model, viewModel) =>
        {
            SetSelection(viewModel, new Selection(2, 5, 2, 18));
            viewModel.Type(open, "keyboard");
            Assert.Equal(Join("const fn = () =>", $"    {open}doSomething(){close}"), model.GetValue());
        });
}
