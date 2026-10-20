using System.Text.RegularExpressions;
using LoomEditor.Core.Languages;
using LoomEditor.TextMate.Tokenization;

namespace LoomEditor.TextMate.Tests.Tokenization;

public class LanguageConfigurationReaderTests
{
    private readonly List<string> _warnings = [];

    [Fact]
    public void Read_AllSections_AreRead()
    {
        LanguageConfiguration configuration = Read(
            """
            {
              "comments": { "lineComment": "//", "blockComment": ["/*", "*/"] },
              "brackets": [["{", "}"], ["[", "]"], ["(", ")"]],
              "autoClosingPairs": [
                { "open": "{", "close": "}" },
                { "open": "'", "close": "'", "notIn": ["string", "comment"] }
              ],
              "surroundingPairs": [["{", "}"], ["'", "'"]],
              "colorizedBracketPairs": [["(", ")"]],
              "autoCloseBefore": ";:.,=}])> \n\t",
              "wordPattern": "(-?\\d*\\.\\d\\w*)|([^\\s]+)",
              "indentationRules": {
                "increaseIndentPattern": "^.*\\{[^}\"']*$",
                "decreaseIndentPattern": "^\\s*\\}",
                "unIndentedLinePattern": "^\\s*\\*"
              },
              "onEnterRules": [
                {
                  "beforeText": "^\\s*/\\*\\*(?!/)([^\\*]|\\*(?!/))*$",
                  "afterText": "^\\s*\\*/$",
                  "action": { "indent": "indentOutdent", "appendText": " * " }
                }
              ],
              "folding": { "markers": { "start": "^\\s*//\\s*#?region\\b", "end": "^\\s*//\\s*#?endregion\\b" } }
            }
            """);

        Assert.Empty(_warnings);
        Assert.Equal(new LineCommentConfig("//"), configuration.Comments!.LineComment);
        Assert.Equal(new CharacterPair("/*", "*/"), configuration.Comments.BlockComment);
        Assert.Equal([new CharacterPair("{", "}"), new CharacterPair("[", "]"), new CharacterPair("(", ")")], configuration.Brackets!);
        Assert.Equal(2, configuration.AutoClosingPairs!.Count);
        Assert.Equal(["string", "comment"], configuration.AutoClosingPairs[1].NotIn!);
        Assert.Equal([new AutoClosingPair("{", "}"), new AutoClosingPair("'", "'")], configuration.SurroundingPairs!);
        Assert.Equal([new CharacterPair("(", ")")], configuration.ColorizedBracketPairs!);
        Assert.Equal(";:.,=}])> \n\t", configuration.AutoCloseBefore);
        Assert.Matches(configuration.WordPattern!, "-1.5e");
        Assert.Matches(configuration.IndentationRules!.IncreaseIndentPattern, "if (x) {");
        Assert.Matches(configuration.IndentationRules.DecreaseIndentPattern, "  }");
        Assert.Null(configuration.IndentationRules.IndentNextLinePattern);
        Assert.NotNull(configuration.IndentationRules.UnIndentedLinePattern);
        OnEnterRule rule = Assert.Single(configuration.OnEnterRules!);
        Assert.Matches(rule.BeforeText, "  /** doc");
        Assert.Matches(rule.AfterText!, " */");
        Assert.Null(rule.PreviousLineText);
        Assert.Equal(new EnterAction(IndentAction.IndentOutdent, " * "), rule.Action);
        Assert.False(configuration.Folding!.OffSide);
        Assert.Matches(configuration.Folding.Markers!.Start, "// #region a");
        Assert.Matches(configuration.Folding.Markers.End, "// #endregion");
        Assert.Null(configuration.DocComment);
    }

    [Fact]
    public void Read_CommentsAndTrailingCommas_AreAccepted()
    {
        LanguageConfiguration configuration = Read(
            """
            // A comment.
            {
              /* block */ "brackets": [["{", "}"],],
              "autoCloseBefore": ";",
            }
            """);

        Assert.Empty(_warnings);
        Assert.Equal([new CharacterPair("{", "}")], configuration.Brackets!);
        Assert.Equal(";", configuration.AutoCloseBefore);
    }

    [Fact]
    public void Read_PairsAsArraysOrObjects_AreRead()
    {
        LanguageConfiguration configuration = Read(
            """
            {
              "autoClosingPairs": [["(", ")"], { "open": "[", "close": "]" }],
              "surroundingPairs": [["(", ")"], { "open": "<", "close": ">" }]
            }
            """);

        Assert.Empty(_warnings);
        Assert.Equal([new AutoClosingPairConditional("(", ")"), new AutoClosingPairConditional("[", "]")], configuration.AutoClosingPairs!);
        Assert.Equal([new AutoClosingPair("(", ")"), new AutoClosingPair("<", ">")], configuration.SurroundingPairs!);
    }

    [Fact]
    public void Read_InvalidEntries_AreSkippedWithAWarning()
    {
        LanguageConfiguration configuration = Read(
            """
            {
              "comments": { "lineComment": 3, "blockComment": ["/*"] },
              "brackets": [["{", "}"], ["(", ")", "x"], 5],
              "autoClosingPairs": [{ "open": "(" }, { "open": "[", "close": "]", "notIn": [1] }, "x", ["{", "}"]],
              "surroundingPairs": { "open": "(" },
              "colorizedBracketPairs": [[1, 2]],
              "onEnterRules": [3, { "beforeText": "a" }, { "beforeText": "a", "action": { "indent": "up" } }]
            }
            """);

        Assert.Null(configuration.Comments);
        Assert.Equal([new CharacterPair("{", "}")], configuration.Brackets!);
        Assert.Equal([new AutoClosingPairConditional("{", "}")], configuration.AutoClosingPairs!);
        Assert.Null(configuration.SurroundingPairs);
        Assert.Empty(configuration.ColorizedBracketPairs!);
        Assert.Null(configuration.OnEnterRules);
        Assert.Equal(
            [
                "[ts]: language configuration: expected `comments.lineComment` to be a string or an object with comment property.",
                "[ts]: language configuration: expected `comments.blockComment` to be an array of two strings.",
                "[ts]: language configuration: expected `brackets[1]` to be an array of two strings.",
                "[ts]: language configuration: expected `brackets[2]` to be an array of two strings.",
                "[ts]: language configuration: expected `autoClosingPairs[0].close` to be a string.",
                "[ts]: language configuration: expected `autoClosingPairs[1].notIn` to be a string array.",
                "[ts]: language configuration: expected `autoClosingPairs[2]` to be an array of two strings or an object.",
                "[ts]: language configuration: expected `surroundingPairs` to be an array.",
                "[ts]: language configuration: expected `colorizedBracketPairs[0]` to be an array of two strings.",
                "[ts]: language configuration: expected `onEnterRules[0]` to be an object.",
                "[ts]: language configuration: expected `onEnterRules[1].action` to be an object.",
                "[ts]: language configuration: expected `onEnterRules[2].action.indent` to be 'none', 'indent', 'indentOutdent' or 'outdent'.",
            ],
            _warnings);
    }

    [Fact]
    public void Read_LineCommentObject_KeepsNoIndent()
    {
        LanguageConfiguration withNoIndent = Read("""{ "comments": { "lineComment": { "comment": "#", "noIndent": true } } }""");
        LanguageConfiguration withoutNoIndent = Read("""{ "comments": { "lineComment": { "comment": "#" } } }""");
        LanguageConfiguration noComment = Read("""{ "comments": { "lineComment": { "noIndent": true } } }""");

        Assert.Equal(new LineCommentConfig("#", true), withNoIndent.Comments!.LineComment);
        Assert.Equal(new LineCommentConfig("#", null), withoutNoIndent.Comments!.LineComment);
        Assert.Null(noComment.Comments);
        Assert.Equal(["[ts]: language configuration: expected `comments.lineComment.comment` to be a string."], _warnings);
    }

    [Fact]
    public void Read_RegexObjectWithFlags_MapsIgnoreCase()
    {
        LanguageConfiguration configuration = Read("""{ "wordPattern": { "pattern": "^abc$", "flags": "gimu" } }""");

        Regex wordPattern = configuration.WordPattern!;
        Assert.Empty(_warnings);
        Assert.Equal(RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant, wordPattern.Options);
        Assert.Matches(wordPattern, "x\nABC");
    }

    [Fact]
    public void Read_UnknownRegexFlag_IsAWarning()
    {
        LanguageConfiguration configuration = Read("""{ "wordPattern": { "pattern": "a", "flags": "ix" } }""");

        Assert.Null(configuration.WordPattern);
        Assert.Equal(["[ts]: Invalid regular expression in `wordPattern`: invalid flags 'ix'"], _warnings);
    }

    [Fact]
    public void Read_InvalidRegex_IsAWarning()
    {
        LanguageConfiguration configuration = Read("""{ "wordPattern": "(a", "folding": { "markers": { "start": 3, "end": "b" } } }""");

        Assert.Null(configuration.WordPattern);
        Assert.Null(configuration.Folding!.Markers);
        Assert.Equal(2, _warnings.Count);
        Assert.StartsWith("[ts]: Invalid regular expression in `wordPattern`: ", _warnings[0], StringComparison.Ordinal);
        Assert.Equal("[ts]: language configuration: expected `folding.markers.start` to be a string or an object.", _warnings[1]);
    }

    [Fact]
    public void Read_IndentationRulesWithoutDecreasePattern_AreDropped()
    {
        LanguageConfiguration configuration = Read("""{ "indentationRules": { "increaseIndentPattern": "\\{$" } }""");

        Assert.Null(configuration.IndentationRules);
        Assert.Equal(["[ts]: language configuration: expected `indentationRules.decreaseIndentPattern` to be a string or an object."], _warnings);
    }

    [Fact]
    public void Read_FoldingMarkers_NeedStartAndEnd()
    {
        LanguageConfiguration onlyStart = Read("""{ "folding": { "offSide": true, "markers": { "start": "^#region" } } }""");
        LanguageConfiguration offSideNotTrue = Read("""{ "folding": { "offSide": 1 } }""");

        Assert.True(onlyStart.Folding!.OffSide);
        Assert.Null(onlyStart.Folding.Markers);
        Assert.False(offSideNotTrue.Folding!.OffSide);
        Assert.Empty(_warnings);
    }

    [Fact]
    public void Read_OnEnterAction_IndentNamesAndRemoveText()
    {
        LanguageConfiguration configuration = Read(
            """
            {
              "onEnterRules": [
                { "beforeText": "a", "action": { "indent": "none", "removeText": 1 } },
                { "beforeText": "a", "action": { "indent": "indent", "appendText": "" } },
                { "beforeText": "a", "action": { "indent": "indentOutdent" } },
                { "beforeText": { "pattern": "a" }, "previousLineText": "b", "action": { "indent": "outdent", "removeText": "x", "appendText": 2 } }
              ]
            }
            """);

        Assert.Equal(
            [
                new EnterAction(IndentAction.None, null, 1),
                new EnterAction(IndentAction.Indent),
                new EnterAction(IndentAction.IndentOutdent),
                new EnterAction(IndentAction.Outdent),
            ],
            configuration.OnEnterRules!.Select(rule => rule.Action));
        Assert.Matches(configuration.OnEnterRules![3].PreviousLineText!, "b");
        Assert.Equal(
            [
                "[ts]: language configuration: expected `onEnterRules[3].action.appendText` to be undefined or a string.",
                "[ts]: language configuration: expected `onEnterRules[3].action.removeText` to be undefined or a number.",
            ],
            _warnings);
    }

    [Fact]
    public void Read_NotAnObject_GivesAnEmptyConfiguration()
    {
        LanguageConfiguration array = Read("[1, 2]");
        LanguageConfiguration invalid = Read("{ \"brackets\": ");

        Assert.Equal(new LanguageConfiguration(), array);
        Assert.Equal(new LanguageConfiguration(), invalid);
        Assert.Equal(2, _warnings.Count);
        Assert.Equal("[ts]: Invalid format, JSON object expected.", _warnings[0]);
        Assert.StartsWith("[ts]: Errors parsing language configuration: ", _warnings[1], StringComparison.Ordinal);
    }

    [Fact]
    public void TranslateJavaScriptPattern_UnicodePropertyNames_InsideAndOutsideClasses()
    {
        Assert.Equal(@"\p{N}[\p{Mn}]", LanguageConfigurationReader.TranslateJavaScriptPattern(@"\p{Number}[\p{Nonspacing_Mark}]"));
        Assert.Equal(@"[\p{L}\p{Nl}\p{Mn}\p{Mc}]+", LanguageConfigurationReader.TranslateJavaScriptPattern(@"\p{Alphabetic}+"));
        Assert.Equal(@"[_\p{L}\p{Nl}\p{Mn}\p{Mc}]", LanguageConfigurationReader.TranslateJavaScriptPattern(@"[_\p{Alphabetic}]"));
        Assert.Equal(@"\[[\p{L}\p{Nl}\p{Mn}\p{Mc}]]",LanguageConfigurationReader.TranslateJavaScriptPattern(@"\[\p{Alphabetic}]"));
        Assert.Equal(@"\\p{Number}\p{L}", LanguageConfigurationReader.TranslateJavaScriptPattern(@"\\p{Number}\p{L}"));
        Assert.Matches(new Regex(LanguageConfigurationReader.TranslateJavaScriptPattern(@"^[\p{Alphabetic}\p{Number}_]+$")), "été_42");
    }

    private LanguageConfiguration Read(string json) => LanguageConfigurationReader.Read(json, "ts", _warnings.Add);
}
