using LoomEditor.Core.Model;
using LoomEditor.Core.Tests.Model.PieceTree.TestHelpers;

namespace LoomEditor.Core.Tests.Model;

/// <summary>
/// Ported from Monaco <c>textModel.test.ts</c>, suite <c>Editor Model - TextModel</c>: indentation guessing
/// (<see cref="IndentationGuesser"/>, <see cref="TextModel.DetectIndentation"/>) and <see cref="TextModel.NormalizeIndentation"/>.
/// </summary>
public partial class TextModelIndentationTests
{
    // Monaco: testGuessIndentation. Monaco creates the model with detectIndentation: true; there is no such creation
    // option here, so the model is created with the defaults and DetectIndentation applies the guess.
    private static void TestGuessIndentation(bool defaultInsertSpaces, int defaultTabSize, bool expectedInsertSpaces, int expectedTabSize, string[] text, string? msg = null)
    {
        var m = new TextModel(string.Join('\n', text), options: new TextModelOptions(tabSize: defaultTabSize, insertSpaces: defaultInsertSpaces));
        m.DetectIndentation(defaultInsertSpaces, defaultTabSize);
        TextModelOptions r = m.Options;

        Assert.True(expectedInsertSpaces == r.InsertSpaces, $"{msg}: insertSpaces {r.InsertSpaces}, expected {expectedInsertSpaces}");
        Assert.True(expectedTabSize == r.TabSize, $"{msg}: tabSize {r.TabSize}, expected {expectedTabSize}");
    }

    // Monaco: assertGuess. Monaco's undefined is null; its [number] tab size is the number with tabSizeOnlyWithSpaces.
    private static void AssertGuess(bool? expectedInsertSpaces, int? expectedTabSize, string[] text, string? msg = null, bool tabSizeOnlyWithSpaces = false)
    {
        if (expectedInsertSpaces is null)
        {
            // cannot guess insertSpaces
            if (expectedTabSize is null)
            {
                // cannot guess tabSize
                TestGuessIndentation(true, 13370, true, 13370, text, msg);
                TestGuessIndentation(false, 13371, false, 13371, text, msg);
            }
            else if (!tabSizeOnlyWithSpaces)
            {
                // can guess tabSize
                TestGuessIndentation(true, 13370, true, expectedTabSize.Value, text, msg);
                TestGuessIndentation(false, 13371, false, expectedTabSize.Value, text, msg);
            }
            else
            {
                // can only guess tabSize when insertSpaces is true
                TestGuessIndentation(true, 13370, true, expectedTabSize.Value, text, msg);
                TestGuessIndentation(false, 13371, false, 13371, text, msg);
            }
        }
        else
        {
            // can guess insertSpaces
            bool insertSpaces = expectedInsertSpaces.Value;
            if (expectedTabSize is null)
            {
                // cannot guess tabSize
                TestGuessIndentation(true, 13370, insertSpaces, 13370, text, msg);
                TestGuessIndentation(false, 13371, insertSpaces, 13371, text, msg);
            }
            else if (!tabSizeOnlyWithSpaces)
            {
                // can guess tabSize
                TestGuessIndentation(true, 13370, insertSpaces, expectedTabSize.Value, text, msg);
                TestGuessIndentation(false, 13371, insertSpaces, expectedTabSize.Value, text, msg);
            }
            else
            {
                // can only guess tabSize when insertSpaces is true
                if (insertSpaces)
                {
                    TestGuessIndentation(true, 13370, insertSpaces, expectedTabSize.Value, text, msg);
                    TestGuessIndentation(false, 13371, insertSpaces, expectedTabSize.Value, text, msg);
                }
                else
                {
                    TestGuessIndentation(true, 13370, insertSpaces, 13370, text, msg);
                    TestGuessIndentation(false, 13371, insertSpaces, 13371, text, msg);
                }
            }
        }
    }

    // Monaco: 'issue #44991: Wrong indentation size auto-detection'
    [Fact]
    public void Issue44991_WrongIndentationSizeAutoDetection()
    {
        AssertGuess(true, 4,
        [
            "a = 10             # 0 space indent",
            "b = 5              # 0 space indent",
            "if a > 10:         # 0 space indent",
            "    a += 1         # 4 space indent      delta 4 spaces",
            "    if b > 5:      # 4 space indent",
            "        b += 1     # 8 space indent      delta 4 spaces",
            "        b += 1     # 8 space indent",
            "        b += 1     # 8 space indent",
            "# comment line 1   # 0 space indent      delta 8 spaces",
            "# comment line 2   # 0 space indent",
            "# comment line 3   # 0 space indent",
            "        b += 1     # 8 space indent      delta 8 spaces",
            "        b += 1     # 8 space indent",
            "        b += 1     # 8 space indent",
        ]);
    }


    // Monaco: 'issue #55818: Broken indentation detection'
    [Fact]
    public void Issue55818_BrokenIndentationDetection()
    {
        AssertGuess(true, 2,
        [
            "",
            "/* REQUIRE */",
            "",
            "const foo = require ( 'foo' ),",
            "      bar = require ( 'bar' );",
            "",
            "/* MY FN */",
            "",
            "function myFn () {",
            "",
            "  const asd = 1,",
            "        dsa = 2;",
            "",
            "  return bar ( foo ( asd ) );",
            "",
            "}",
            "",
            "/* EXPORT */",
            "",
            "module.exports = myFn;",
            "",
        ]);
    }


    // Monaco: 'issue #70832: Broken indentation detection'
    [Fact]
    public void Issue70832_BrokenIndentationDetection()
    {
        AssertGuess(false, null,
        [
            "x",
            "x",
            "x",
            "x",
            "\tx",
            "\t\tx",
            "    x",
            "\t\tx",
            "\tx",
            "\t\tx",
            "\tx",
            "\tx",
            "\tx",
            "\tx",
            "x",
        ]);
    }


    // Monaco: 'issue #62143: Broken indentation detection'
    [Fact]
    public void Issue62143_BrokenIndentationDetection()
    {
        // works before the fix
        AssertGuess(true, 2,
        [
            "x",
            "x",
            "  x",
            "  x",
        ]);

        // works before the fix
        AssertGuess(true, 2,
        [
            "x",
            "  - item2",
            "  - item3",
        ]);

        // works before the fix
        TestGuessIndentation(true, 2, true, 2,
        [
            "x x",
            "  x",
            "  x",
        ]);

        // fails before the fix
        // empty space inline breaks the indentation guess
        TestGuessIndentation(true, 2, true, 2,
        [
            "x x",
            "  x",
            "  x",
            "    x",
        ]);

        TestGuessIndentation(true, 2, true, 2,
        [
            "<!--test1.md -->",
            "- item1",
            "  - item2",
            "    - item3",
        ]);
    }


    // Monaco: 'issue #84217: Broken indentation detection'
    [Fact]
    public void Issue84217_BrokenIndentationDetection()
    {
        AssertGuess(true, 4,
        [
            "def main():",
            "    print('hello')",
        ]);
        AssertGuess(true, 4,
        [
            "def main():",
            "    with open('foo') as fp:",
            "        print(fp.read())",
        ]);
    }


    // Monaco: 'issue #65668: YAML file indented with 2 spaces'
    [Fact]
    public void Issue65668_YamlFileIndentedWith2Spaces()
    {
        // Full YAML file from the issue - should detect as 2 spaces
        AssertGuess(true, 2,
        [
            "version: 2",
            "",
            "jobs:",
            "  build:",
            "    docker:",
            "      - circleci/golang:1.11",
            "",
            "  environment:",
            "    TEST_RESULTS: /tmp/test-results",
            "",
            "  steps:",
            "    - checkout",
            "    - run: mkdir -p $TEST_RESULTS",
            "",
            "    - restore_cache:",
            "        keys:",
            "          - v1-pkg-cache",
            "",
            "    - run:",
            "        name: dep ensure",
            "        command: dep ensure -v",
            "",
            "    - run:",
            "        name: Run unit tests",
            "        command: |",
            "          trap \"go-junit-report <${TEST_RESULTS}/go-test.out > ${TEST_RESULTS}/go-test-report.xml\" EXIT",
            "          go test -v ./... | tee ${TEST_RESULTS}/go-test.out",
            "",
            "    - run:",
            "        name: Build",
            "        command: go build -v",
            "",
            "    - save_cache:",
            "        key: v1-pkg-cache",
            "        paths:",
            "          - \"/go/pkg\"",
            "",
            "    - store_artifacts:",
            "        path: /tmp/test-results",
            "        destination: raw-test-output",
            "",
            "    - store_test_results:",
            "        path: /tmp/test-results",
        ]);
    }


    // Monaco: 'issue #249040: 4-space indent should win over 2-space when predominant'
    [Fact]
    public void Issue249040_FourSpaceIndentWinsOverTwoSpaceWhenPredominant()
    {
        // File with mostly 4-space indents but some 2-space indents should detect as 4 spaces
        AssertGuess(true, 4,
        [
            "function foo() {",
            "    let a = 1;",
            "    let b = 2;",
            "    if (true) {",
            "        console.log(a);",
            "        console.log(b);",
            "    }",
            "    const obj = {",
            "      x: 1,",  // 2-space indent here
            "      y: 2",  // 2-space indent here
            "    };",
            "    return obj;",
            "}",
        ]);
    }

    // Monaco: 'normalizeIndentation 1'
    [Fact]
    public void NormalizeIndentation1()
    {
        var model = new TextModel(string.Empty, options: new TextModelOptions(tabSize: 4, indentSize: 4, insertSpaces: false));

        Assert.Equal("\t", model.NormalizeIndentation("\t"));
        Assert.Equal("\t", model.NormalizeIndentation("    "));
        Assert.Equal("   ", model.NormalizeIndentation("   "));
        Assert.Equal("  ", model.NormalizeIndentation("  "));
        Assert.Equal(" ", model.NormalizeIndentation(" "));
        Assert.Equal("", model.NormalizeIndentation(""));
        Assert.Equal("\t\t", model.NormalizeIndentation(" \t    "));
        Assert.Equal("\t   ", model.NormalizeIndentation(" \t   "));
        Assert.Equal("\t  ", model.NormalizeIndentation(" \t  "));
        Assert.Equal("\t ", model.NormalizeIndentation(" \t "));
        Assert.Equal("\t", model.NormalizeIndentation(" \t"));

        Assert.Equal("\ta", model.NormalizeIndentation("\ta"));
        Assert.Equal("\ta", model.NormalizeIndentation("    a"));
        Assert.Equal("   a", model.NormalizeIndentation("   a"));
        Assert.Equal("  a", model.NormalizeIndentation("  a"));
        Assert.Equal(" a", model.NormalizeIndentation(" a"));
        Assert.Equal("a", model.NormalizeIndentation("a"));
        Assert.Equal("\t\ta", model.NormalizeIndentation(" \t    a"));
        Assert.Equal("\t   a", model.NormalizeIndentation(" \t   a"));
        Assert.Equal("\t  a", model.NormalizeIndentation(" \t  a"));
        Assert.Equal("\t a", model.NormalizeIndentation(" \t a"));
        Assert.Equal("\ta", model.NormalizeIndentation(" \ta"));
    }

    // Monaco: 'normalizeIndentation 2'
    [Fact]
    public void NormalizeIndentation2()
    {
        var model = new TextModel(string.Empty);

        Assert.Equal("    a", model.NormalizeIndentation("\ta"));
        Assert.Equal("    a", model.NormalizeIndentation("    a"));
        Assert.Equal("   a", model.NormalizeIndentation("   a"));
        Assert.Equal("  a", model.NormalizeIndentation("  a"));
        Assert.Equal(" a", model.NormalizeIndentation(" a"));
        Assert.Equal("a", model.NormalizeIndentation("a"));
        Assert.Equal("        a", model.NormalizeIndentation(" \t    a"));
        Assert.Equal("       a", model.NormalizeIndentation(" \t   a"));
        Assert.Equal("      a", model.NormalizeIndentation(" \t  a"));
        Assert.Equal("     a", model.NormalizeIndentation(" \t a"));
        Assert.Equal("    a", model.NormalizeIndentation(" \ta"));
    }

    [Fact]
    public void DetectIndentation_RaisesOptionsChanged()
    {
        var model = new TextModel("x\n\ty\n\tz", options: new TextModelOptions(tabSize: 4, insertSpaces: true));
        var events = new List<ModelOptionsChangedEventArgs>();
        model.OptionsChanged += (_, e) => events.Add(e);

        model.DetectIndentation(defaultInsertSpaces: true, defaultTabSize: 4);

        ModelOptionsChangedEventArgs e = Assert.Single(events);
        Assert.True(e.InsertSpaces);
        Assert.False(e.TabSize);
        Assert.False(model.Options.InsertSpaces);
        Assert.False(model.Options.IndentSizeIsTabSize);
        Assert.Equal(4, model.Options.IndentSize);
    }

    [Fact]
    public void GuessIndentation_EmptyText_KeepsTheDefaults()
    {
        GuessedIndentation guess = IndentationGuesser.GuessIndentation(TestTextBuffers.Create([string.Empty]), 3, false);

        Assert.Equal(new GuessedIndentation(InsertSpaces: false, TabSize: 3), guess);
    }
}
