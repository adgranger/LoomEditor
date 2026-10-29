using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Snippets;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tests.Snippets.TestHelpers;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Snippets;

// Monaco: suite 'Snippet Variables Resolver' (snippetVariables.test.ts). Tests that needed a label service or a workspace service
// use ISnippetVariableHost doubles; the month and day names are English.
public class SnippetVariablesTests
{
    private const string Text = "this is line one\nthis is line two\n    this is line three";

    private static readonly char Sep = Path.DirectorySeparatorChar;

    private sealed class TestHost(
        string? filePath = null,
        string? relativeFilePath = null,
        string? workspaceName = null,
        string? workspaceFolder = null,
        string? clipboard = null) : ISnippetVariableHost
    {
        public string? FilePath => filePath;

        public string? RelativeFilePath => relativeFilePath;

        public string? WorkspaceName => workspaceName;

        public string? WorkspaceFolder => workspaceFolder;

        public string? ReadClipboardText() => clipboard;
    }

    private sealed class FuncResolver(Func<Variable, string?> resolve) : IVariableResolver
    {
        public string? Resolve(Variable variable) => resolve(variable);
    }

    private static CompositeVariableResolver CreateResolver(TextModel model) =>
        new CompositeVariableResolver(
            new ModelBasedVariableResolver(new TestHost("/foo/files/text.txt")),
            new SelectionBasedVariableResolver(model, new Selection(1, 1, 1, 1), 0));

    private static void AssertVariableResolve(IVariableResolver resolver, string varName, string? expected)
    {
        TextmateSnippet snippet = new SnippetParser().Parse("$" + varName);
        var variable = (Variable)snippet.Children[0];

        variable.Resolve(resolver);

        if (variable.Children.Count == 0)
        {
            Assert.Null(expected);
        }
        else
        {
            Assert.Equal(expected, variable.ToString());
        }
    }

    private static void AssertVariableResolve2(string input, string expected, string? varValue = null)
    {
        TextmateSnippet snippet = new SnippetParser().Parse(input)
            .ResolveVariables(new FuncResolver(variable => string.IsNullOrEmpty(varValue) ? variable.Name : varValue));

        Assert.Equal(expected, snippet.ToString());
    }

    private static string F(IFormattable value, string? format = null) => value.ToString(format, System.Globalization.CultureInfo.InvariantCulture);

    private static string Native(string path) => path.Replace('/', Sep);

    // Monaco: 'editor variables, basics'
    [Fact]
    public void EditorVariables_Basics()
    {
        IVariableResolver resolver = CreateResolver(new TextModel(Text));

        AssertVariableResolve(resolver, "TM_FILENAME", "text.txt");
        AssertVariableResolve(resolver, "something", null);
    }

    // Monaco: 'editor variables, file/dir'
    [Fact]
    public void EditorVariables_FileDir()
    {
        IVariableResolver resolver = CreateResolver(new TextModel(Text));
        AssertVariableResolve(resolver, "TM_FILENAME", "text.txt");
        AssertVariableResolve(resolver, "TM_DIRECTORY", Native("/foo/files"));
        AssertVariableResolve(resolver, "TM_DIRECTORY_BASE", "files");
        AssertVariableResolve(resolver, "TM_FILEPATH", "/foo/files/text.txt");

        resolver = new ModelBasedVariableResolver(new TestHost("/abc/def/ghi"));
        AssertVariableResolve(resolver, "TM_FILENAME", "ghi");
        AssertVariableResolve(resolver, "TM_DIRECTORY", Native("/abc/def"));
        AssertVariableResolve(resolver, "TM_DIRECTORY_BASE", "def");
        AssertVariableResolve(resolver, "TM_FILEPATH", "/abc/def/ghi");

        resolver = new ModelBasedVariableResolver(new TestHost("fff.ts"));
        AssertVariableResolve(resolver, "TM_DIRECTORY", string.Empty);
        AssertVariableResolve(resolver, "TM_DIRECTORY_BASE", string.Empty);
        AssertVariableResolve(resolver, "TM_FILEPATH", "fff.ts");
    }

    // Monaco: 'Path delimiters in code snippet variables aren't specific to remote OS #76840'
    [Fact]
    public void PathDelimitersAreNotSpecificToRemoteOs_76840()
    {
        var resolver = new CompositeVariableResolver(new ModelBasedVariableResolver(new TestHost("|foo|files|text.txt")));

        AssertVariableResolve(resolver, "TM_FILEPATH", "|foo|files|text.txt");
    }

    // Monaco: 'editor variables, selection'
    [Fact]
    public void EditorVariables_Selection()
    {
        var model = new TextModel(Text);

        IVariableResolver resolver = new SelectionBasedVariableResolver(model, new Selection(1, 2, 2, 3), 0);
        AssertVariableResolve(resolver, "TM_SELECTED_TEXT", "his is line one\nth");
        AssertVariableResolve(resolver, "TM_CURRENT_LINE", "this is line two");
        AssertVariableResolve(resolver, "TM_LINE_INDEX", "1");
        AssertVariableResolve(resolver, "TM_LINE_NUMBER", "2");
        AssertVariableResolve(resolver, "CURSOR_INDEX", "0");
        AssertVariableResolve(resolver, "CURSOR_NUMBER", "1");

        resolver = new SelectionBasedVariableResolver(model, new Selection(1, 2, 2, 3), 4);
        AssertVariableResolve(resolver, "CURSOR_INDEX", "4");
        AssertVariableResolve(resolver, "CURSOR_NUMBER", "5");

        resolver = new SelectionBasedVariableResolver(model, new Selection(2, 3, 1, 2), 0);
        AssertVariableResolve(resolver, "TM_SELECTED_TEXT", "his is line one\nth");
        AssertVariableResolve(resolver, "TM_CURRENT_LINE", "this is line one");
        AssertVariableResolve(resolver, "TM_LINE_INDEX", "0");
        AssertVariableResolve(resolver, "TM_LINE_NUMBER", "1");

        resolver = new SelectionBasedVariableResolver(model, new Selection(1, 2, 1, 2), 0);
        AssertVariableResolve(resolver, "TM_SELECTED_TEXT", null);
        AssertVariableResolve(resolver, "TM_CURRENT_WORD", "this");

        resolver = new SelectionBasedVariableResolver(model, new Selection(3, 1, 3, 1), 0);
        AssertVariableResolve(resolver, "TM_CURRENT_WORD", null);
    }

    // Monaco: 'TextmateSnippet, resolve variable'
    [Fact]
    public void TextmateSnippet_ResolveVariable()
    {
        TextmateSnippet snippet = new SnippetParser().Parse("\"$TM_CURRENT_WORD\"", true);
        Assert.Equal("\"\"", snippet.ToString());

        snippet.ResolveVariables(CreateResolver(new TextModel(Text)));

        Assert.Equal("\"this\"", snippet.ToString());
    }

    // Monaco: 'TextmateSnippet, resolve variable with default'
    [Fact]
    public void TextmateSnippet_ResolveVariableWithDefault()
    {
        TextmateSnippet snippet = new SnippetParser().Parse("\"${TM_CURRENT_WORD:foo}\"", true);
        Assert.Equal("\"foo\"", snippet.ToString());

        snippet.ResolveVariables(CreateResolver(new TextModel(Text)));

        Assert.Equal("\"this\"", snippet.ToString());
    }

    // Monaco: 'More useful environment variables for snippets, #32737'
    [Fact]
    public void MoreUsefulEnvironmentVariables_32737()
    {
        AssertVariableResolve(CreateResolver(new TextModel(Text)), "TM_FILENAME_BASE", "text");
        AssertVariableResolve(new ModelBasedVariableResolver(new TestHost("/abc/def/ghi")), "TM_FILENAME_BASE", "ghi");
        AssertVariableResolve(new ModelBasedVariableResolver(new TestHost(".git")), "TM_FILENAME_BASE", ".git");
        AssertVariableResolve(new ModelBasedVariableResolver(new TestHost("foo.")), "TM_FILENAME_BASE", "foo");
    }

    // Monaco: 'Variable Snippet Transform'
    [Fact]
    public void VariableSnippetTransform()
    {
        TextmateSnippet snippet = new SnippetParser().Parse("name=${TM_FILENAME/(.*)\\..+$/$1/}", true);
        snippet.ResolveVariables(CreateResolver(new TextModel(Text)));
        Assert.Equal("name=text", snippet.ToString());

        AssertVariableResolve2("${ThisIsAVar/([A-Z]).*(Var)/$2/}", "Var");
        AssertVariableResolve2("${ThisIsAVar/([A-Z]).*(Var)/$2-${1:/downcase}/}", "Var-t");
        AssertVariableResolve2("${Foo/(.*)/${1:+Bar}/img}", "Bar");

        // https://github.com/microsoft/vscode/issues/33162
        AssertVariableResolve2("export default class ${TM_FILENAME/(\\w+)\\.js/$1/g}", "export default class FooFile", "FooFile.js");

        AssertVariableResolve2("${foobarfoobar/(foo)/${1:+FAR}/g}", "FARbarFARbar"); // global
        AssertVariableResolve2("${foobarfoobar/(foo)/${1:+FAR}/}", "FARbarfoobar"); // first match
        AssertVariableResolve2("${foobarfoobar/(bazz)/${1:+FAR}/g}", "foobarfoobar"); // no match, no else
        AssertVariableResolve2("${foobarfoobar/(foo)/${2:+FAR}/g}", "barbar"); // bad group reference
    }

    // Monaco: 'Snippet transforms do not handle regex with alternatives or optional matches, #36089'
    [Fact]
    public void SnippetTransformsWithAlternativesOrOptionalMatches_36089()
    {
        const string Transform = "${TM_FILENAME/^(.)|(?:-(.))|(\\.js)/${1:/upcase}${2:/upcase}/g}";
        AssertVariableResolve2(Transform, "MyClass", "my-class.js");

        // no hyphens
        AssertVariableResolve2(Transform, "Myclass", "myclass.js");

        // none matching suffix
        AssertVariableResolve2(Transform, "Myclass.foo", "myclass.foo");

        // more than one hyphen
        AssertVariableResolve2(Transform, "ThisIsAFile", "this-is-a-file.js");

        // KEBAB CASE
        const string Kebab = "${TM_FILENAME_BASE/([A-Z][a-z]+)([A-Z][a-z]+$)?/${1:/downcase}-${2:/downcase}/g}";
        AssertVariableResolve2(Kebab, "capital-case", "CapitalCase");
        AssertVariableResolve2(Kebab, "capital-case-more", "CapitalCaseMore");
    }

    // Monaco: 'Add variable to insert value from clipboard to a snippet #40153'
    [Fact]
    public void AddVariableToInsertValueFromClipboard_40153()
    {
        AssertVariableResolve(new ClipboardBasedVariableResolver(() => null, 1, 0, true), "CLIPBOARD", null);
        AssertVariableResolve(new ClipboardBasedVariableResolver(() => string.Empty, 1, 0, true), "CLIPBOARD", null);
        AssertVariableResolve(new ClipboardBasedVariableResolver(() => "foo", 1, 0, true), "CLIPBOARD", "foo");
        AssertVariableResolve(new ClipboardBasedVariableResolver(() => "foo", 1, 0, true), "foo", null);
        AssertVariableResolve(new ClipboardBasedVariableResolver(() => "foo", 1, 0, true), "cLIPBOARD", null);
    }

    // Monaco: 'Add variable to insert value from clipboard to a snippet #40153, 2'
    [Fact]
    public void AddVariableToInsertValueFromClipboard_40153_2()
    {
        AssertVariableResolve(new ClipboardBasedVariableResolver(() => "line1", 1, 2, true), "CLIPBOARD", "line1");
        AssertVariableResolve(new ClipboardBasedVariableResolver(() => "line1\nline2\nline3", 1, 2, true), "CLIPBOARD", "line1\nline2\nline3");
        AssertVariableResolve(new ClipboardBasedVariableResolver(() => "line1\nline2", 1, 2, true), "CLIPBOARD", "line2");
        AssertVariableResolve(new ClipboardBasedVariableResolver(() => "line1\nline2", 0, 2, true), "CLIPBOARD", "line1");
        AssertVariableResolve(new ClipboardBasedVariableResolver(() => "line1\nline2", 0, 2, false), "CLIPBOARD", "line1\nline2");
    }

    // Monaco: 'Add time variables for snippets #41631, #43140'
    [Theory]
    [InlineData("CURRENT_YEAR")]
    [InlineData("CURRENT_YEAR_SHORT")]
    [InlineData("CURRENT_MONTH")]
    [InlineData("CURRENT_DATE")]
    [InlineData("CURRENT_HOUR")]
    [InlineData("CURRENT_MINUTE")]
    [InlineData("CURRENT_SECOND")]
    [InlineData("CURRENT_MILLISECOND")]
    [InlineData("CURRENT_DAY_NAME")]
    [InlineData("CURRENT_DAY_NAME_SHORT")]
    [InlineData("CURRENT_MONTH_NAME")]
    [InlineData("CURRENT_MONTH_NAME_SHORT")]
    [InlineData("CURRENT_SECONDS_UNIX")]
    [InlineData("CURRENT_MILLISECONDS_UNIX")]
    [InlineData("CURRENT_TIMEZONE_OFFSET")]
    [InlineData("CURRENT_TIMEZONE_NAME")]
    public void AddTimeVariables_41631(string name)
    {
        var resolver = new TimeBasedVariableResolver(TimeProvider.System);
        var variable = (Variable)new SnippetParser().Parse("$" + name).Children[0];

        Assert.True(variable.Resolve(resolver), $"{name} failed to resolve");
    }

    // Monaco: 'Time-based snippet variables have deterministic millisecond and unix values'
    [Fact]
    public void TimeBasedVariablesHaveDeterministicMillisecondAndUnixValues()
    {
        var now = new DateTimeOffset(2024, 4, 15, 12, 34, 56, 7, TimeSpan.Zero);
        var resolver = new TimeBasedVariableResolver(new FixedTimeProvider(now));
        DateTimeOffset local = TimeZoneInfo.ConvertTime(now, TimeZoneInfo.Local);

        AssertVariableResolve(resolver, "CURRENT_YEAR", F(local.Year));
        AssertVariableResolve(resolver, "CURRENT_YEAR_SHORT", F(local.Year)[^2..]);
        AssertVariableResolve(resolver, "CURRENT_MONTH", F(local.Month, "D2"));
        AssertVariableResolve(resolver, "CURRENT_DATE", F(local.Day, "D2"));
        AssertVariableResolve(resolver, "CURRENT_HOUR", F(local.Hour, "D2"));
        AssertVariableResolve(resolver, "CURRENT_MINUTE", F(local.Minute, "D2"));
        AssertVariableResolve(resolver, "CURRENT_SECOND", F(local.Second, "D2"));
        AssertVariableResolve(resolver, "CURRENT_MILLISECOND", "007");
        AssertVariableResolve(resolver, "CURRENT_SECONDS_UNIX", F(now.ToUnixTimeSeconds()));
        AssertVariableResolve(resolver, "CURRENT_MILLISECONDS_UNIX", F(now.ToUnixTimeMilliseconds()));
    }

    // Monaco: 'Time-based snippet variables resolve to the same values even as time progresses'
    [Fact]
    public void TimeBasedVariablesResolveToTheSameValuesAsTimeProgresses()
    {
        const string SnippetText = "$CURRENT_YEAR $CURRENT_YEAR_SHORT $CURRENT_MONTH $CURRENT_DATE $CURRENT_HOUR $CURRENT_MINUTE $CURRENT_SECOND "
            + "$CURRENT_MILLISECOND $CURRENT_DAY_NAME $CURRENT_DAY_NAME_SHORT $CURRENT_MONTH_NAME $CURRENT_MONTH_NAME_SHORT "
            + "$CURRENT_SECONDS_UNIX $CURRENT_MILLISECONDS_UNIX $CURRENT_TIMEZONE_OFFSET $CURRENT_TIMEZONE_NAME";
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var resolver = new TimeBasedVariableResolver(clock);

        TextmateSnippet first = new SnippetParser().Parse(SnippetText).ResolveVariables(resolver);
        clock.Advance(TimeSpan.FromMilliseconds((365 * 24 * 3600 * 1000L) + (24 * 3600 * 1000L) + (3661 * 1000L)));
        TextmateSnippet second = new SnippetParser().Parse(SnippetText).ResolveVariables(resolver);

        Assert.Equal(first.ToString(), second.ToString());
    }

    // Monaco: 'creating snippet - format-condition doesn't work #53617'
    [Fact]
    public void FormatConditionDoesNotWork_53617()
    {
        TextmateSnippet snippet = new SnippetParser().Parse("${TM_LINE_NUMBER/(10)/${1:?It is:It is not}/} line 10", true);

        snippet.ResolveVariables(new FuncResolver(_ => "10"));
        Assert.Equal("It is line 10", snippet.ToString());

        snippet.ResolveVariables(new FuncResolver(_ => "11"));
        Assert.Equal("It is not line 10", snippet.ToString());
    }

    // Monaco: 'Add workspace name and folder variables for snippets #68261'
    [Fact]
    public void AddWorkspaceNameAndFolderVariables_68261()
    {
        // empty workspace
        var resolver = new WorkspaceBasedVariableResolver(new TestHost());
        AssertVariableResolve(resolver, "WORKSPACE_NAME", null);
        AssertVariableResolve(resolver, "WORKSPACE_FOLDER", null);

        // single folder workspace
        resolver = new WorkspaceBasedVariableResolver(new TestHost(workspaceName: "folderName", workspaceFolder: "/folderName"));
        AssertVariableResolve(resolver, "WORKSPACE_NAME", "folderName");
        AssertVariableResolve(resolver, "WORKSPACE_FOLDER", "/folderName");

        // workspace with config
        resolver = new WorkspaceBasedVariableResolver(new TestHost(workspaceName: "testWorkspace", workspaceFolder: "/"));
        AssertVariableResolve(resolver, "WORKSPACE_NAME", "testWorkspace");
        AssertVariableResolve(resolver, "WORKSPACE_FOLDER", "/");
    }

    // Monaco: 'Add RELATIVE_FILEPATH snippet variable #114208'
    [Fact]
    public void AddRelativeFilepathVariable_114208()
    {
        AssertVariableResolve(
            new ModelBasedVariableResolver(new TestHost("/foo/files/text.txt", "/foo/files/text.txt")), "RELATIVE_FILEPATH", "/foo/files/text.txt");
        AssertVariableResolve(
            new ModelBasedVariableResolver(new TestHost("/foo/files/text.txt", "files/text.txt")), "RELATIVE_FILEPATH", "files/text.txt");
    }

    [Fact]
    public void Time_Values_PaddedTwoOrThreeAndNamed()
    {
        var now = new DateTimeOffset(2024, 1, 5, 3, 4, 5, 6, TimeSpan.FromHours(2));
        var resolver = new TimeBasedVariableResolver(new FixedTimeProvider(now, TimeZoneInfo.CreateCustomTimeZone("t", TimeSpan.FromHours(2), "t", "t")));

        AssertVariableResolve(resolver, "CURRENT_YEAR", "2024");
        AssertVariableResolve(resolver, "CURRENT_YEAR_SHORT", "24");
        AssertVariableResolve(resolver, "CURRENT_MONTH", "01");
        AssertVariableResolve(resolver, "CURRENT_DATE", "05");
        AssertVariableResolve(resolver, "CURRENT_HOUR", "03");
        AssertVariableResolve(resolver, "CURRENT_MINUTE", "04");
        AssertVariableResolve(resolver, "CURRENT_SECOND", "05");
        AssertVariableResolve(resolver, "CURRENT_MILLISECOND", "006");
        AssertVariableResolve(resolver, "CURRENT_DAY_NAME", "Friday");
        AssertVariableResolve(resolver, "CURRENT_DAY_NAME_SHORT", "Fri");
        AssertVariableResolve(resolver, "CURRENT_MONTH_NAME", "January");
        AssertVariableResolve(resolver, "CURRENT_MONTH_NAME_SHORT", "Jan");
        AssertVariableResolve(resolver, "CURRENT_TIMEZONE_NAME", "t");
    }

    [Fact]
    public void Time_TimezoneOffset_Positive_And_Negative()
    {
        var positive = new TimeBasedVariableResolver(new FixedTimeProvider(new DateTimeOffset(2024, 1, 1, 0, 0, 0, new TimeSpan(5, 30, 0))));
        var negative = new TimeBasedVariableResolver(new FixedTimeProvider(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.FromHours(-8))));
        var zero = new TimeBasedVariableResolver(new FixedTimeProvider(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)));

        // The provider converts to its local zone: compare with what that zone says.
        TimeSpan localPositive = TimeZoneInfo.Local.GetUtcOffset(new DateTimeOffset(2024, 1, 1, 0, 0, 0, new TimeSpan(5, 30, 0)));
        string Expected(TimeSpan offset) =>
            $"{(offset < TimeSpan.Zero ? '-' : '+')}{(int)Math.Abs(offset.TotalMinutes) / 60:D2}:{(int)Math.Abs(offset.TotalMinutes) % 60:D2}";

        AssertVariableResolve(positive, "CURRENT_TIMEZONE_OFFSET", Expected(localPositive));
        Assert.Matches(@"^[+-]\d{2}:\d{2}$", Resolve(negative, "CURRENT_TIMEZONE_OFFSET"));
        Assert.Matches(@"^[+-]\d{2}:\d{2}$", Resolve(zero, "CURRENT_TIMEZONE_OFFSET"));
    }

    [Fact]
    public void Time_TimezoneOffset_FormatsSignAndMinutes()
    {
        TimeZoneInfo plus = TimeZoneInfo.CreateCustomTimeZone("p", new TimeSpan(5, 30, 0), "p", "p");
        TimeZoneInfo minus = TimeZoneInfo.CreateCustomTimeZone("m", TimeSpan.FromHours(-8), "m", "m");

        Assert.Equal("+05:30", Resolve(new TimeBasedVariableResolver(new FixedTimeProvider(DateTimeOffset.UnixEpoch, plus)), "CURRENT_TIMEZONE_OFFSET"));
        Assert.Equal("-08:00", Resolve(new TimeBasedVariableResolver(new FixedTimeProvider(DateTimeOffset.UnixEpoch, minus)), "CURRENT_TIMEZONE_OFFSET"));
    }

    [Fact]
    public void Time_Unix_Seconds_And_Millis()
    {
        var resolver = new TimeBasedVariableResolver(new FixedTimeProvider(DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_123_456)));

        AssertVariableResolve(resolver, "CURRENT_SECONDS_UNIX", "1700000123");
        AssertVariableResolve(resolver, "CURRENT_MILLISECONDS_UNIX", "1700000123456");
    }

    [Fact]
    public void Random_Is6Digits()
    {
        for (int seed = 0; seed < 20; seed++)
        {
            string value = Resolve(new RandomBasedVariableResolver(new Random(seed)), "RANDOM");

            Assert.Matches(@"^\d{6}$", value);
        }
    }

    [Fact]
    public void RandomHex_Is6HexDigits()
    {
        for (int seed = 0; seed < 20; seed++)
        {
            string value = Resolve(new RandomBasedVariableResolver(new Random(seed)), "RANDOM_HEX");

            Assert.True(System.Text.RegularExpressions.Regex.IsMatch(value, "^[0-9a-f]{6}$"), $"seed {seed}: {value}");
        }
    }

    [Fact]
    public void Uuid_IsAGuid()
    {
        string value = Resolve(new RandomBasedVariableResolver(new Random(1)), "UUID");

        Assert.True(Guid.TryParse(value, out _));
        Assert.Equal(value.ToLowerInvariant(), value);
        Assert.Equal("00000001-0000-0000-0000-000000000000", Resolve(new RandomBasedVariableResolver(new Random(1), () => new Guid(1, 0, 0, new byte[8])), "UUID"));
    }

    [Fact]
    public void Composite_FirstNonNullWins()
    {
        var composite = new CompositeVariableResolver(new FuncResolver(_ => null), new FuncResolver(_ => "second"), new FuncResolver(_ => "third"));

        Assert.Equal("second", composite.Resolve(new Variable("X")));
    }

    [Fact]
    public void Composite_EmptyStringWins()
    {
        var composite = new CompositeVariableResolver(new FuncResolver(_ => string.Empty), new FuncResolver(_ => "second"));

        Assert.Equal(string.Empty, composite.Resolve(new Variable("X")));
        Assert.Null(new CompositeVariableResolver().Resolve(new Variable("X")));
    }

    [Fact]
    public void Model_NoHost_AllNull()
    {
        var resolver = new ModelBasedVariableResolver(ISnippetVariableHost.None);

        foreach (string name in new[] { "TM_FILENAME", "TM_FILENAME_BASE", "TM_DIRECTORY", "TM_DIRECTORY_BASE", "TM_FILEPATH", "RELATIVE_FILEPATH" })
        {
            Assert.Null(resolver.Resolve(new Variable(name)));
        }

        Assert.Null(new WorkspaceBasedVariableResolver(ISnippetVariableHost.None).Resolve(new Variable("WORKSPACE_NAME")));
        Assert.Null(ISnippetVariableHost.None.ReadClipboardText());
    }

    [Fact]
    public void Selection_Multiline_ReindentsAgainstVariableIndent()
    {
        var model = new TextModel("a\n  b\n  c\nrest");
        TextmateSnippet snippet = new SnippetParser().Parse("if (x) {\n    $TM_SELECTED_TEXT\n}");
        var resolver = new SelectionBasedVariableResolver(model, new Selection(2, 3, 3, 4), 0);

        snippet.ResolveVariables(resolver);

        // The line has the indentation "  " before the selection; the variable has "    ": two extra spaces after each line break.
        Assert.Equal("if (x) {\n    b\n    c\n}", snippet.ToString());
    }

    [Fact]
    public void Selection_Empty_ReturnsNull()
    {
        var resolver = new SelectionBasedVariableResolver(new TextModel("abc"), new Selection(1, 2, 1, 2), 0);

        Assert.Null(resolver.Resolve(new Variable("TM_SELECTED_TEXT")));
        Assert.Null(resolver.Resolve(new Variable("SELECTION")));
    }

    [Fact]
    public void Comment_NoCommentRules_Null()
    {
        var model = new TextModel("abc");
        var resolver = new CommentBasedVariableResolver(model, new Position(1, 1));

        Assert.Null(resolver.Resolve(new Variable("LINE_COMMENT")));
        Assert.Null(resolver.Resolve(new Variable("BLOCK_COMMENT_START")));
        Assert.Null(resolver.Resolve(new Variable("BLOCK_COMMENT_END")));
    }

    [Fact]
    public void Comment_WithRules_ReadsThemFromTheLanguageConfiguration()
    {
        var model = new TextModel("abc");
        model.LanguageConfigurationRegistry.Register(
            LanguageIdCodec.PlainTextLanguageId,
            new LanguageConfiguration { Comments = new CommentRule(new LineCommentConfig("//"), new CharacterPair("/*", "*/")) });
        var resolver = new CommentBasedVariableResolver(model, new Position(1, 1));

        Assert.Equal("//", resolver.Resolve(new Variable("LINE_COMMENT")));
        Assert.Equal("/*", resolver.Resolve(new Variable("BLOCK_COMMENT_START")));
        Assert.Equal("*/", resolver.Resolve(new Variable("BLOCK_COMMENT_END")));
        Assert.Null(resolver.Resolve(new Variable("OTHER")));
    }

    [Fact]
    public void KnownNames_Has39Names() => Assert.Equal(39, SnippetVariables.KnownNames.Count);

    private static string Resolve(IVariableResolver resolver, string name) =>
        resolver.Resolve(new Variable(name)) ?? throw new InvalidOperationException(name + " did not resolve");
}
