using LoomEditor.Core.Text;
using LoomEditor.View.Features.LinesOperations;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Features.LinesOperations;

// Monaco: tests 'transpose' and 'toggle case' of 'Editor Contrib - Line Operations'
public partial class LinesOperationsTests
{
    private static void Transpose(TestCodeEditor editor) => LinesOperationsActions.Transpose(editor.ViewModel);

    private static void Transform(TestCodeEditor editor, Func<string, string> modifyText) =>
        LinesOperationsActions.TransformCase(editor.ViewModel, "test", modifyText);

    // Selects, transforms, then checks the line and (when given) the selection.
    private static void TestCase(TestCodeEditor editor, Func<string, string> modifyText, Selection selection, int lineNumber, string expectedLine, Selection? expectedSelection)
    {
        SetSelections(editor, selection);
        Transform(editor, modifyText);
        Assert.Equal(expectedLine, editor.Model.GetLineContent(lineNumber));
        if (expectedSelection is { } expected)
        {
            AssertSelection(editor, expected);
        }
    }

    // Monaco: 'transpose'
    [Fact]
    public void Transpose_SwapsTheCharactersAroundTheCursor()
    {
        TestCodeEditor.WithTestCodeEditor(["hello world", string.Empty, string.Empty, "   "], editor =>
        {
            SetSelections(editor, new Selection(1, 1, 1, 1));
            Transpose(editor);
            Assert.Equal("hello world", editor.Model.GetLineContent(1));
            AssertSelection(editor, new Selection(1, 2, 1, 2));

            SetSelections(editor, new Selection(1, 6, 1, 6));
            Transpose(editor);
            Assert.Equal("hell oworld", editor.Model.GetLineContent(1));
            AssertSelection(editor, new Selection(1, 7, 1, 7));

            SetSelections(editor, new Selection(1, 12, 1, 12));
            Transpose(editor);
            Assert.Equal("hell oworl", editor.Model.GetLineContent(1));
            AssertSelection(editor, new Selection(2, 2, 2, 2));

            SetSelections(editor, new Selection(3, 1, 3, 1));
            Transpose(editor);
            Assert.Equal(string.Empty, editor.Model.GetLineContent(3));
            AssertSelection(editor, new Selection(4, 1, 4, 1));

            SetSelections(editor, new Selection(4, 2, 4, 2));
            Transpose(editor);
            Assert.Equal("   ", editor.Model.GetLineContent(4));
            AssertSelection(editor, new Selection(4, 3, 4, 3));
        });

        // fix #16633
        TestCodeEditor.WithTestCodeEditor(["", string.Empty, "hello", "world", string.Empty, "hello world", string.Empty, "hello world"], editor =>
        {
            SetSelections(editor, new Selection(1, 1, 1, 1));
            Transpose(editor);
            Assert.Equal(string.Empty, editor.Model.GetLineContent(2));
            AssertSelection(editor, new Selection(2, 1, 2, 1));

            SetSelections(editor, new Selection(3, 6, 3, 6));
            Transpose(editor);
            Assert.Equal("oworld", editor.Model.GetLineContent(4));
            AssertSelection(editor, new Selection(4, 2, 4, 2));

            SetSelections(editor, new Selection(6, 12, 6, 12));
            Transpose(editor);
            Assert.Equal("d", editor.Model.GetLineContent(7));
            AssertSelection(editor, new Selection(7, 2, 7, 2));

            SetSelections(editor, new Selection(8, 12, 8, 12));
            Transpose(editor);
            Assert.Equal("hello world", editor.Model.GetLineContent(8));
            AssertSelection(editor, new Selection(8, 12, 8, 12));
        });
    }

    // Monaco: 'toggle case' (one test upstream, split here by block)
    [Fact]
    public void ToggleCase_UpperLowerTitleAndSnakeCase() =>
        TestCodeEditor.WithTestCodeEditor(
            [
                "hello world",
                "öçşğü",
                "parseHTMLString",
                "getElementById",
                "insertHTML",
                "PascalCase",
                "CSSSelectorsList",
                "iD",
                "tEST",
                "öçşÖÇŞğüĞÜ",
                "audioConverter.convertM4AToMP3();",
                "snake_case",
                "Capital_Snake_Case",
                "function helloWorld() {",
                "return someGlobalObject.printHelloWorld(\"en\", \"utf-8\");",
                "}",
                "helloWorld();",
                "'JavaScript'",
                "parseHTML4String",
                "_accessor: ServicesAccessor",
            ],
            editor =>
            {
                TestCase(editor, CaseTransforms.ToUpper, new Selection(1, 1, 1, 12), 1, "HELLO WORLD", new Selection(1, 1, 1, 12));
                TestCase(editor, CaseTransforms.ToLower, new Selection(1, 1, 1, 12), 1, "hello world", new Selection(1, 1, 1, 12));
                TestCase(editor, CaseTransforms.ToUpper, new Selection(1, 3, 1, 3), 1, "HELLO world", new Selection(1, 3, 1, 3));
                TestCase(editor, CaseTransforms.ToLower, new Selection(1, 4, 1, 4), 1, "hello world", new Selection(1, 4, 1, 4));
                TestCase(editor, CaseTransforms.ToTitleCase, new Selection(1, 1, 1, 12), 1, "Hello World", new Selection(1, 1, 1, 12));
                TestCase(editor, CaseTransforms.ToUpper, new Selection(2, 1, 2, 6), 2, "ÖÇŞĞÜ", new Selection(2, 1, 2, 6));
                TestCase(editor, CaseTransforms.ToLower, new Selection(2, 1, 2, 6), 2, "öçşğü", new Selection(2, 1, 2, 6));
                TestCase(editor, CaseTransforms.ToTitleCase, new Selection(2, 1, 2, 6), 2, "Öçşğü", new Selection(2, 1, 2, 6));
                TestCase(editor, CaseTransforms.ToSnakeCase, new Selection(3, 1, 3, 16), 3, "parse_html_string", new Selection(3, 1, 3, 18));
                TestCase(editor, CaseTransforms.ToSnakeCase, new Selection(4, 1, 4, 15), 4, "get_element_by_id", new Selection(4, 1, 4, 18));
                TestCase(editor, CaseTransforms.ToSnakeCase, new Selection(5, 1, 5, 11), 5, "insert_html", new Selection(5, 1, 5, 12));
                TestCase(editor, CaseTransforms.ToSnakeCase, new Selection(6, 1, 6, 11), 6, "pascal_case", new Selection(6, 1, 6, 12));
                TestCase(editor, CaseTransforms.ToSnakeCase, new Selection(7, 1, 7, 17), 7, "css_selectors_list", new Selection(7, 1, 7, 19));
                TestCase(editor, CaseTransforms.ToSnakeCase, new Selection(8, 1, 8, 3), 8, "i_d", new Selection(8, 1, 8, 4));
                TestCase(editor, CaseTransforms.ToSnakeCase, new Selection(9, 1, 9, 5), 9, "t_est", new Selection(9, 1, 9, 6));
                TestCase(editor, CaseTransforms.ToSnakeCase, new Selection(10, 1, 10, 11), 10, "öçş_öç_şğü_ğü", new Selection(10, 1, 10, 14));
                TestCase(editor, CaseTransforms.ToSnakeCase, new Selection(11, 1, 11, 34), 11, "audio_converter.convert_m4a_to_mp3();", new Selection(11, 1, 11, 38));
                TestCase(editor, CaseTransforms.ToSnakeCase, new Selection(12, 1, 12, 11), 12, "snake_case", new Selection(12, 1, 12, 11));
                TestCase(editor, CaseTransforms.ToSnakeCase, new Selection(13, 1, 13, 19), 13, "capital_snake_case", new Selection(13, 1, 13, 19));

                SetSelections(editor, new Selection(14, 1, 17, 14));
                Transform(editor, CaseTransforms.ToSnakeCase);
                Assert.Equal(
                    "function hello_world() {\nreturn some_global_object.print_hello_world(\"en\", \"utf-8\");\n}\nhello_world();",
                    editor.Model.GetValueInRange(new Range(14, 1, 17, 15)));
                AssertSelection(editor, new Selection(14, 1, 17, 15));

                TestCase(editor, CaseTransforms.ToSnakeCase, new Selection(18, 1, 18, 13), 18, "'java_script'", new Selection(18, 1, 18, 14));
                TestCase(editor, CaseTransforms.ToSnakeCase, new Selection(19, 1, 19, 17), 19, "parse_html4_string", new Selection(19, 1, 19, 19));
                TestCase(editor, CaseTransforms.ToSnakeCase, new Selection(20, 1, 20, 28), 20, "_accessor: services_accessor", new Selection(20, 1, 20, 29));
            });

    [Fact]
    public void ToggleCase_TitleCaseBoundaries() =>
        TestCodeEditor.WithTestCodeEditor(
            ["foO baR BaZ", "foO'baR'BaZ", "foO[baR]BaZ", "foO`baR~BaZ", "foO^baR%BaZ", "foO$baR!BaZ", "'physician's assistant'"],
            editor =>
            {
                TestCase(editor, CaseTransforms.ToTitleCase, new Selection(1, 1, 1, 12), 1, "Foo Bar Baz", null);
                TestCase(editor, CaseTransforms.ToTitleCase, new Selection(2, 1, 2, 12), 2, "Foo'bar'baz", null);
                TestCase(editor, CaseTransforms.ToTitleCase, new Selection(3, 1, 3, 12), 3, "Foo[Bar]Baz", null);
                TestCase(editor, CaseTransforms.ToTitleCase, new Selection(4, 1, 4, 12), 4, "Foo`Bar~Baz", null);
                TestCase(editor, CaseTransforms.ToTitleCase, new Selection(5, 1, 5, 12), 5, "Foo^Bar%Baz", null);
                TestCase(editor, CaseTransforms.ToTitleCase, new Selection(6, 1, 6, 12), 6, "Foo$Bar!Baz", null);
                TestCase(editor, CaseTransforms.ToTitleCase, new Selection(7, 1, 7, 23), 7, "'Physician's Assistant'", null);
            });

    [Fact]
    public void ToggleCase_CamelCase() =>
        TestCodeEditor.WithTestCodeEditor(
            [
                "camel from words",
                "from_snake_case",
                "from-kebab-case",
                "alreadyCamel",
                "ReTain_some_CAPitalization",
                "my_var.test_function()",
                "öçş_öç_şğü_ğü",
                "XMLHttpRequest",
                "\tfunction hello_world() {",
                "\t\treturn some_global_object;",
                "\t}",
            ],
            editor =>
            {
                TestCase(editor, CaseTransforms.ToCamelCase, new Selection(1, 1, 1, 18), 1, "camelFromWords", null);
                TestCase(editor, CaseTransforms.ToCamelCase, new Selection(2, 1, 2, 15), 2, "fromSnakeCase", null);
                TestCase(editor, CaseTransforms.ToCamelCase, new Selection(3, 1, 3, 15), 3, "fromKebabCase", null);
                TestCase(editor, CaseTransforms.ToCamelCase, new Selection(4, 1, 4, 12), 4, "alreadyCamel", null);
                TestCase(editor, CaseTransforms.ToCamelCase, new Selection(5, 1, 5, 26), 5, "reTainSomeCAPitalization", null);
                TestCase(editor, CaseTransforms.ToCamelCase, new Selection(6, 1, 6, 23), 6, "myVar.testFunction()", null);
                TestCase(editor, CaseTransforms.ToCamelCase, new Selection(7, 1, 7, 14), 7, "öçşÖçŞğüĞü", null);
                TestCase(editor, CaseTransforms.ToCamelCase, new Selection(8, 1, 8, 14), 8, "XMLHttpRequest", null);

                SetSelections(editor, new Selection(9, 1, 11, 2));
                Transform(editor, CaseTransforms.ToCamelCase);
                Assert.Equal("\tfunction helloWorld() {\n\t\treturn someGlobalObject;\n\t}", editor.Model.GetValueInRange(new Range(9, 1, 11, 3)));
            });

    [Fact]
    public void ToggleCase_UpperAndLowerOnEmptyAndWhitespaceLines() =>
        TestCodeEditor.WithTestCodeEditor([string.Empty, "   "], editor =>
        {
            TestCase(editor, CaseTransforms.ToUpper, new Selection(1, 1, 1, 1), 1, string.Empty, new Selection(1, 1, 1, 1));
            TestCase(editor, CaseTransforms.ToLower, new Selection(1, 1, 1, 1), 1, string.Empty, new Selection(1, 1, 1, 1));
            TestCase(editor, CaseTransforms.ToUpper, new Selection(2, 2, 2, 2), 2, "   ", new Selection(2, 2, 2, 2));
            TestCase(editor, CaseTransforms.ToLower, new Selection(2, 2, 2, 2), 2, "   ", new Selection(2, 2, 2, 2));
        });

    [Fact]
    public void ToggleCase_KebabCase() =>
        TestCodeEditor.WithTestCodeEditor(
            [
                "hello world",
                "öçşğü",
                "parseHTMLString",
                "getElementById",
                "PascalCase",
                "öçşÖÇŞğüĞÜ",
                "audioConverter.convertM4AToMP3();",
                "Capital_Snake_Case",
                "parseHTML4String",
                "_accessor: ServicesAccessor",
                "Kebab-Case",
            ],
            editor =>
            {
                TestCase(editor, CaseTransforms.ToKebabCase, new Selection(1, 1, 1, 12), 1, "hello world", new Selection(1, 1, 1, 12));
                TestCase(editor, CaseTransforms.ToKebabCase, new Selection(2, 1, 2, 6), 2, "öçşğü", new Selection(2, 1, 2, 6));
                TestCase(editor, CaseTransforms.ToKebabCase, new Selection(3, 1, 3, 16), 3, "parse-html-string", new Selection(3, 1, 3, 18));
                TestCase(editor, CaseTransforms.ToKebabCase, new Selection(4, 1, 4, 15), 4, "get-element-by-id", new Selection(4, 1, 4, 18));
                TestCase(editor, CaseTransforms.ToKebabCase, new Selection(5, 1, 5, 11), 5, "pascal-case", new Selection(5, 1, 5, 12));
                TestCase(editor, CaseTransforms.ToKebabCase, new Selection(6, 1, 6, 11), 6, "öçş-öç-şğü-ğü", new Selection(6, 1, 6, 14));
                TestCase(editor, CaseTransforms.ToKebabCase, new Selection(7, 1, 7, 34), 7, "audio-converter.convert-m4a-to-mp3();", new Selection(7, 1, 7, 38));
                TestCase(editor, CaseTransforms.ToKebabCase, new Selection(8, 1, 8, 19), 8, "capital-snake-case", new Selection(8, 1, 8, 19));
                TestCase(editor, CaseTransforms.ToKebabCase, new Selection(9, 1, 9, 17), 9, "parse-html4-string", new Selection(9, 1, 9, 19));
                TestCase(editor, CaseTransforms.ToKebabCase, new Selection(10, 1, 10, 28), 10, "_accessor: services-accessor", new Selection(10, 1, 10, 29));
                TestCase(editor, CaseTransforms.ToKebabCase, new Selection(11, 1, 11, 11), 11, "kebab-case", new Selection(11, 1, 11, 11));
            });

    [Fact]
    public void ToggleCase_PascalCase() =>
        TestCodeEditor.WithTestCodeEditor(
            [
                "hello world",
                "öçşğü",
                "parseHTMLString",
                "getElementById",
                "PascalCase",
                "öçşÖÇŞğüĞÜ",
                "audioConverter.convertM4AToMP3();",
                "Capital_Snake_Case",
                "parseHTML4String",
                "Kebab-Case",
                "FOO_BAR",
                "FOO BAR A",
                "xML_HTTP-reQUEsT",
                "ÉCOLE",
                "ΩMEGA_CASE",
                "ДОМ_ТЕСТ",
            ],
            editor =>
            {
                TestCase(editor, CaseTransforms.ToPascalCase, new Selection(1, 1, 1, 12), 1, "HelloWorld", new Selection(1, 1, 1, 11));
                TestCase(editor, CaseTransforms.ToPascalCase, new Selection(2, 1, 2, 6), 2, "Öçşğü", new Selection(2, 1, 2, 6));
                TestCase(editor, CaseTransforms.ToPascalCase, new Selection(3, 1, 3, 16), 3, "ParseHTMLString", new Selection(3, 1, 3, 16));
                TestCase(editor, CaseTransforms.ToPascalCase, new Selection(4, 1, 4, 15), 4, "GetElementById", new Selection(4, 1, 4, 15));
                TestCase(editor, CaseTransforms.ToPascalCase, new Selection(5, 1, 5, 11), 5, "PascalCase", new Selection(5, 1, 5, 11));
                TestCase(editor, CaseTransforms.ToPascalCase, new Selection(6, 1, 6, 11), 6, "ÖçşÖÇŞğüĞÜ", new Selection(6, 1, 6, 11));
                TestCase(editor, CaseTransforms.ToPascalCase, new Selection(7, 1, 7, 34), 7, "AudioConverter.ConvertM4AToMP3();", new Selection(7, 1, 7, 34));
                TestCase(editor, CaseTransforms.ToPascalCase, new Selection(8, 1, 8, 19), 8, "CapitalSnakeCase", new Selection(8, 1, 8, 17));
                TestCase(editor, CaseTransforms.ToPascalCase, new Selection(9, 1, 9, 17), 9, "ParseHTML4String", new Selection(9, 1, 9, 17));
                TestCase(editor, CaseTransforms.ToPascalCase, new Selection(10, 1, 10, 11), 10, "KebabCase", new Selection(10, 1, 10, 10));

                SetSelections(editor, new Selection(9, 1, 10, 11));
                Transform(editor, CaseTransforms.ToPascalCase);
                Assert.Equal("ParseHTML4String\nKebabCase", editor.Model.GetValueInRange(new Range(9, 1, 10, 11)));
                AssertSelection(editor, new Selection(9, 1, 10, 10));

                TestCase(editor, CaseTransforms.ToPascalCase, new Selection(11, 1, 11, 8), 11, "FooBar", new Selection(11, 1, 11, 7));
                TestCase(editor, CaseTransforms.ToPascalCase, new Selection(12, 1, 12, 10), 12, "FooBarA", new Selection(12, 1, 12, 8));
                TestCase(editor, CaseTransforms.ToPascalCase, new Selection(13, 1, 13, 17), 13, "XmlHttpReQUEsT", new Selection(13, 1, 13, 15));
                TestCase(editor, CaseTransforms.ToPascalCase, new Selection(14, 1, 14, 6), 14, "École", new Selection(14, 1, 14, 6));
                TestCase(editor, CaseTransforms.ToPascalCase, new Selection(15, 1, 15, 11), 15, "ΩmegaCase", new Selection(15, 1, 15, 10));
                TestCase(editor, CaseTransforms.ToPascalCase, new Selection(16, 1, 16, 9), 16, "ДомТест", new Selection(16, 1, 16, 8));
            });

    [Fact]
    public void ToTitleCase_Apostrophes_FollowMonaco() =>
        Assert.Equal("O'neil Isn't", CaseTransforms.ToTitleCase("o'neil isn't"));

    [Fact]
    public void CaseTransforms_AllRegexesCompile()
    {
        System.Text.RegularExpressions.Regex[] regexes =
        [
            CaseTransforms.TitleBoundary,
            CaseTransforms.SnakeCaseBoundary,
            CaseTransforms.SnakeSingleLetters,
            CaseTransforms.CamelSingleLineWordBoundary,
            CaseTransforms.CamelMultiLineWordBoundary,
            CaseTransforms.CamelValidWordStart,
            CaseTransforms.PascalWordBoundary,
            CaseTransforms.PascalWordBoundaryToMaintain,
            CaseTransforms.PascalUpperCaseWordMatcher,
            CaseTransforms.KebabCaseBoundary,
            CaseTransforms.KebabSingleLetters,
            CaseTransforms.KebabUnderscoreBoundary,
        ];

        Assert.All(regexes, regex => Assert.NotNull(regex.ToString()));
    }
}
