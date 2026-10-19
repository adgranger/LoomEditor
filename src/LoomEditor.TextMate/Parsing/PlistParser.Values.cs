// Ported from microsoft/vscode-textmate: src/plist.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace LoomEditor.TextMate.Parsing;

// The state machine of _parse: pushState, popState, enterDict, leaveDict, enterArray, leaveArray, acceptKey, the
// accept* functions (one AcceptValue: they only differ by the value they store), and escapeVal.
internal sealed partial class PlistParser
{
    private void PushState(State newState, JsonNode? newCur)
    {
        _stateStack.Push(_state);
        _objStack.Push(_cur);
        _state = newState;
        _cur = newCur;
    }

    private void PopState()
    {
        if (_stateStack.Count == 0)
        {
            throw Fail("illegal state stack");
        }

        _state = _stateStack.Pop();
        _cur = _objStack.Pop();
    }

    private void EnterDict()
    {
        if (_state == State.DictState)
        {
            // dictState.enterDict
            JsonObject newDict = [];
            StoreInDict(newDict);
            PushState(State.DictState, newDict);
        }
        else if (_state == State.ArrState)
        {
            // arrState.enterDict
            JsonObject newDict = [];
            ((JsonArray)_cur!).Add((JsonNode)newDict);
            PushState(State.DictState, newDict);
        }
        else
        {
            // ROOT_STATE
            _cur = new JsonObject();
            PushState(State.DictState, _cur);
        }
    }

    private void LeaveDict()
    {
        if (_state == State.DictState)
        {
            PopState();
        }
        else
        {
            throw Fail("unexpected </dict>");
        }
    }

    private void EnterArray()
    {
        if (_state == State.DictState)
        {
            // dictState.enterArray
            JsonArray newArr = [];
            StoreInDict(newArr);
            PushState(State.ArrState, newArr);
        }
        else if (_state == State.ArrState)
        {
            // arrState.enterArray
            JsonArray newArr = [];
            ((JsonArray)_cur!).Add((JsonNode)newArr);
            PushState(State.ArrState, newArr);
        }
        else
        {
            // ROOT_STATE
            _cur = new JsonArray();
            PushState(State.ArrState, _cur);
        }
    }

    private void LeaveArray()
    {
        if (_state == State.ArrState)
        {
            PopState();
        }
        else
        {
            throw Fail("unexpected </array>");
        }
    }

    private void AcceptKey(string val)
    {
        if (_state == State.DictState)
        {
            if (_curKey is not null)
            {
                throw Fail("too many <key>");
            }

            _curKey = val;
        }
        else
        {
            throw Fail("unexpected <key>");
        }
    }

    // acceptString, acceptReal, acceptInteger, acceptDate, acceptData, acceptBool.
    private void AcceptValue(JsonNode val)
    {
        if (_state == State.DictState)
        {
            StoreInDict(val);
        }
        else if (_state == State.ArrState)
        {
            ((JsonArray)_cur!).Add(val);
        }
        else
        {
            // ROOT_STATE
            _cur = val;
        }
    }

    // cur[curKey] = val; curKey = null (an existing key keeps its place, as in a JavaScript object).
    private void StoreInDict(JsonNode val)
    {
        if (_curKey is null)
        {
            throw Fail("missing <key>");
        }

        ((JsonObject)_cur!)[_curKey] = val;
        _curKey = null;
    }

    private string EscapeVal(string str)
    {
        string result = DecimalReference().Replace(str, match => FromCodePoint(match.Groups[1].Value, NumberStyles.None));
        result = HexReference().Replace(result, match => FromCodePoint(match.Groups[1].Value, NumberStyles.AllowHexSpecifier));
        return NamedEntity().Replace(result, match => match.Value switch
        {
            "&amp;" => "&",
            "&lt;" => "<",
            "&gt;" => ">",
            "&quot;" => "\"",
            "&apos;" => "'",
            _ => match.Value,
        });
    }

    // String.fromCodePoint(parseInt(m0, radix)): a code point above U+10FFFF is a RangeError; a surrogate code point
    // gives a lone surrogate.
    private string FromCodePoint(string digits, NumberStyles style)
    {
        if (!long.TryParse(digits, style, CultureInfo.InvariantCulture, out long codePoint) || codePoint > 0x10FFFF)
        {
            throw Fail("invalid code point " + digits);
        }

        return codePoint <= char.MaxValue ? ((char)codePoint).ToString() : char.ConvertFromUtf32((int)codePoint);
    }

    [GeneratedRegex("&#([0-9]+);", RegexOptions.CultureInvariant)]
    private static partial Regex DecimalReference();

    [GeneratedRegex("&#x([0-9a-f]+);", RegexOptions.CultureInvariant)]
    private static partial Regex HexReference();

    [GeneratedRegex("&amp;|&lt;|&gt;|&quot;|&apos;", RegexOptions.CultureInvariant)]
    private static partial Regex NamedEntity();
}
