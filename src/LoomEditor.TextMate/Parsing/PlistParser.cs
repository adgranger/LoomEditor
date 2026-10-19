// Ported from microsoft/vscode-textmate: src/plist.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.Json.Nodes;

namespace LoomEditor.TextMate.Parsing;

/// <summary>
/// <c>parsePLIST</c>: "a very fast plist parser", the hand-written parser of vscode-textmate. It builds a
/// <see cref="JsonNode"/> tree, as upstream builds a JavaScript object: <c>&lt;dict&gt;</c> → <see cref="JsonObject"/>
/// (document order), <c>&lt;array&gt;</c> → <see cref="JsonArray"/>, <c>&lt;string&gt;</c>, <c>&lt;date&gt;</c> and
/// <c>&lt;data&gt;</c> → string, <c>&lt;integer&gt;</c> and <c>&lt;real&gt;</c> → number, <c>&lt;true/&gt;</c> and
/// <c>&lt;false/&gt;</c> → boolean.
/// </summary>
/// <remarks>
/// <para>
/// Not an XML parser, on purpose (same entity handling and errors as upstream): processing instructions, comments and
/// <c>&lt;!DOCTYPE&gt;</c> are skipped, values are decoded for <c>&amp;#N;</c>, <c>&amp;#xN;</c> (lowercase hex digits)
/// and the five named entities, and a CDATA section inside a value is kept as is. <c>parseWithLocation</c> is not
/// ported (no location metadata, ADR 0008 decision 3). A <c>&lt;date&gt;</c> is kept as its text.
/// </para>
/// <para>Threading: stateless; safe on any thread.</para>
/// </remarks>
internal sealed partial class PlistParser
{
    // ChCode
    private const char Bom = '﻿';
    private const char Slash = '/';
    private const char LessThan = '<';
    private const char QuestionMark = '?';
    private const char ExclamationMark = '!';

    private readonly string _content;
    private readonly int _len;
    private readonly Stack<State> _stateStack = new();
    private readonly Stack<JsonNode?> _objStack = new();
    private int _pos;
    private State _state = State.RootState;
    private JsonNode? _cur;
    private string? _curKey;

    private PlistParser(string content)
    {
        _content = content;
        _len = content.Length;
    }

    private enum State
    {
        RootState = 0,
        DictState = 1,
        ArrState = 2,
    }

    /// <summary><c>parsePLIST</c>: the root value, or <see langword="null"/> for an empty document.</summary>
    /// <exception cref="FormatException">The plist is malformed (the message is vscode-textmate's).</exception>
    public static JsonNode? Parse(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new PlistParser(content).ParseDocument();
    }

    private JsonNode? ParseDocument()
    {
        // Skip UTF8 BOM
        if (_len > 0 && _content[0] == Bom)
        {
            _pos = 1;
        }

        while (_pos < _len)
        {
            SkipWhitespace();
            if (_pos >= _len)
            {
                break;
            }

            char chCode = _content[_pos];
            _pos++;
            if (chCode != LessThan)
            {
                throw Fail("expected <");
            }

            if (_pos >= _len)
            {
                throw Fail("unexpected end of input");
            }

            char peekChCode = _content[_pos];

            if (peekChCode == QuestionMark)
            {
                _pos++;
                AdvanceUntil("?>");
                continue;
            }

            if (peekChCode == ExclamationMark)
            {
                _pos++;

                if (AdvanceIfStartsWith("--"))
                {
                    AdvanceUntil("-->");
                    continue;
                }

                AdvanceUntil(">");
                continue;
            }

            if (peekChCode == Slash)
            {
                _pos++;
                SkipWhitespace();

                if (AdvanceIfStartsWith("plist"))
                {
                    AdvanceUntil(">");
                    continue;
                }

                if (AdvanceIfStartsWith("dict"))
                {
                    AdvanceUntil(">");
                    LeaveDict();
                    continue;
                }

                if (AdvanceIfStartsWith("array"))
                {
                    AdvanceUntil(">");
                    LeaveArray();
                    continue;
                }

                throw Fail("unexpected closed tag");
            }

            (string name, bool isClosed) = ParseOpenTag();

            switch (name)
            {
                case "dict":
                    EnterDict();
                    if (isClosed)
                    {
                        LeaveDict();
                    }

                    continue;

                case "array":
                    EnterArray();
                    if (isClosed)
                    {
                        LeaveArray();
                    }

                    continue;

                case "key":
                    AcceptKey(ParseTagValue(isClosed));
                    continue;

                case "string":
                case "date":
                case "data":
                    AcceptValue(JsonValue.Create(ParseTagValue(isClosed)));
                    continue;

                case "real":
                    AcceptValue(JsonValue.Create(Number(JsNumbers.ParseFloat(ParseTagValue(isClosed)), "cannot parse float")));
                    continue;

                case "integer":
                    AcceptValue(JsonValue.Create(Number(JsNumbers.ParseInt(ParseTagValue(isClosed)), "cannot parse integer")));
                    continue;

                case "true":
                    ParseTagValue(isClosed);
                    AcceptValue(JsonValue.Create(true));
                    continue;

                case "false":
                    ParseTagValue(isClosed);
                    AcceptValue(JsonValue.Create(false));
                    continue;
            }

            if (name.StartsWith("plist", StringComparison.Ordinal))
            {
                continue;
            }

            throw Fail("unexpected opened tag " + name);
        }

        return _cur;
    }

    private void SkipWhitespace()
    {
        while (_pos < _len)
        {
            char chCode = _content[_pos];
            if (chCode is not ' ' and not '\t' and not '\r' and not '\n')
            {
                break;
            }

            _pos++;
        }
    }

    private bool AdvanceIfStartsWith(string str)
    {
        if (_pos + str.Length <= _len && string.CompareOrdinal(_content, _pos, str, 0, str.Length) == 0)
        {
            _pos += str.Length;
            return true;
        }

        return false;
    }

    private void AdvanceUntil(string str)
    {
        int nextOccurence = _content.IndexOf(str, _pos, StringComparison.Ordinal);
        // EOF when not found
        _pos = nextOccurence != -1 ? nextOccurence + str.Length : _len;
    }

    private string CaptureUntil(string str)
    {
        int nextOccurence = _content.IndexOf(str, _pos, StringComparison.Ordinal);
        if (nextOccurence != -1)
        {
            string r = _content[_pos..nextOccurence];
            _pos = nextOccurence + str.Length;
            return r;
        }

        // EOF
        string rest = _content[_pos..];
        _pos = _len;
        return rest;
    }

    private (string Name, bool IsClosed) ParseOpenTag()
    {
        string r = CaptureUntil(">");
        bool isClosed = false;
        if (r.Length > 0 && r[^1] == Slash)
        {
            isClosed = true;
            r = r[..^1];
        }

        return (r.Trim(), isClosed);
    }

    private string ParseTagValue(bool isClosed)
    {
        if (isClosed)
        {
            return string.Empty;
        }

        string val = CaptureUntil("</");
        AdvanceUntil(">");
        return EscapeVal(val);
    }

    private double Number(double value, string message) => double.IsNaN(value) ? throw Fail(message) : value;

    private FormatException Fail(string msg)
    {
        int start = Math.Min(_pos, _len);
        string near = _content.Substring(start, Math.Min(50, _len - start));
        return new FormatException("Near offset " + _pos + ": " + msg + " ~~~" + near + "~~~");
    }
}
