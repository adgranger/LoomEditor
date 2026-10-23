using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Automation.Text;
using Windows.Foundation;
using LoomEditorControl = LoomEditor.WinUI.Controls.LoomEditor;

namespace LoomEditor.Gallery;

/// <summary>
/// Exercises the editor's UI Automation providers from inside the process (no UIA client), on the normal and the Native AOT build.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal static class UiaSelfCheck
{
    private const string Document = "one two\nthree four\nfive";

    /// <summary>Runs every check; never throws (an exception becomes a failed check). Restores the text and the selections.</summary>
    public static IReadOnlyList<(string Name, bool Passed, string Detail)> Run(LoomEditorControl editor)
    {
        var results = new List<(string, bool, string)>();
        string previousText = editor.Text;
        var previousSelections = editor.Selections.ToArray();
        try
        {
            editor.Text = Document;
            editor.SetSelections(new[] { new Selection(1, 1, 1, 4) });
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(editor);
            double[]? firstRectangle = null;

            void Check(string name, Func<string?> check)
            {
                try
                {
                    string? failure = check();
                    results.Add((name, failure is null, failure ?? "ok"));
                }
                catch (Exception ex)
                {
                    results.Add((name, false, $"{ex.GetType().Name}: {ex.Message}"));
                }
            }

            string? Expect(object? actual, object? expected) =>
                Equals(actual, expected) ? null : $"expected {Format(expected)}, got {Format(actual)}";

            ITextProvider Text() => (ITextProvider)peer.GetPattern(PatternInterface.Text);
            ITextRangeProvider Doc() => Text().DocumentRange;
            ITextRangeProvider Selected() => Text().GetSelection()[0];

            Check("Peer_IsEditControl", () => Expect(peer.GetAutomationControlType(), AutomationControlType.Edit));
            Check("Patterns_Text_Value_Text2", () =>
                peer.GetPattern(PatternInterface.Text) is not ITextProvider ? "no Text pattern"
                : peer.GetPattern(PatternInterface.Value) is not IValueProvider ? "no Value pattern"
                : peer.GetPattern(PatternInterface.Text2) is not ITextProvider2 ? "no Text2 pattern" : null);
            Check("Document_Text", () => Expect(Doc().GetText(-1), Document));
            Check("Selection_IsOne", () =>
            {
                var selection = Text().GetSelection();
                return selection.Length != 1 ? $"{selection.Length} ranges" : Expect(selection[0].GetText(-1), "one");
            });
            Check("Expand_Word", () =>
            {
                var range = Selected().Clone();
                range.ExpandToEnclosingUnit(TextUnit.Word);
                return Expect(range.GetText(-1), "one ");
            });
            Check("Move_Word", () =>
            {
                var range = Selected().Clone();
                int moved = range.Move(TextUnit.Word, 1);
                return Expect(moved, 1) ?? Expect(range.GetText(-1), "two\n");
            });
            Check("Expand_Line_Paragraph", () =>
            {
                var range = Doc().Clone();
                range.ExpandToEnclosingUnit(TextUnit.Line);
                string? failure = Expect(range.GetText(-1), "one two\n");
                int moved = range.Move(TextUnit.Paragraph, 1);
                return failure ?? Expect(moved, 1) ?? Expect(range.GetText(-1), "three four\n");
            });
            Check("MoveEndpointByUnit", () =>
            {
                var range = Selected().Clone();
                int moved = range.MoveEndpointByUnit(TextPatternRangeEndpoint.End, TextUnit.Word, 1);
                return Expect(moved, 1) ?? Expect(range.GetText(-1), "one ");
            });
            Check("Compare", () =>
            {
                var original = Selected();
                var clone = original.Clone();
                return !clone.Compare(original) ? "clone differs"
                    : Expect(clone.CompareEndpoints(TextPatternRangeEndpoint.Start, original, TextPatternRangeEndpoint.Start), 0);
            });
            Check("FindText", () =>
            {
                string? found = Doc().FindText("four", false, true)?.GetText(-1);
                string? missing = Doc().FindText("zzz", false, true)?.GetText(-1);
                return Expect(found, "four") ?? Expect(missing, null);
            });
            Check("Select_MovesTheCaret", () =>
            {
                Doc().FindText("four", false, true)!.Select();
                return Expect(editor.Selections[0], new Selection(2, 7, 2, 11));
            });
            Check("AddToSelection", () =>
            {
                Doc().FindText("five", false, true)!.AddToSelection();
                return Expect(editor.Selections.Count, 2);
            });
            Check("RemoveFromSelection", () =>
            {
                Doc().FindText("five", false, true)!.RemoveFromSelection();
                return Expect(editor.Selections.Count, 1);
            });
            Check("GetVisibleRanges", () => ((ITextProvider)Text()).GetVisibleRanges().Length == 0 ? "empty" : null);
            Check("Bounding_Rectangles", () =>
            {
                if (editor.XamlRoot is null || editor.ActualWidth == 0)
                {
                    return "skipped: not in a window";
                }

                Doc().GetBoundingRectangles(out firstRectangle);
                return firstRectangle.Length == 0 || firstRectangle.Length % 4 != 0 ? $"length {firstRectangle.Length}" : null;
            });
            Check("RangeFromPoint", () =>
            {
                if (firstRectangle is not { Length: >= 4 })
                {
                    return "skipped: no rectangle";
                }

                return Text().RangeFromPoint(new Point(firstRectangle[0] + 1, firstRectangle[1] + 1)) is null ? "null" : null;
            });
            Check("Value_Pattern", () =>
            {
                var value = (IValueProvider)peer.GetPattern(PatternInterface.Value);
                return Expect(value.Value, editor.Model.GetValue(EndOfLinePreference.LF)) ?? Expect(value.IsReadOnly, editor.IsReadOnly);
            });
            Check("Caret_Range", () =>
            {
                var range = ((ITextProvider2)peer.GetPattern(PatternInterface.Text2)).GetCaretRange(out _);
                return Expect(range.GetText(-1), string.Empty);
            });
            Check("ScrollIntoView_DoesNotThrow", () =>
            {
                Selected().ScrollIntoView(true);
                return null;
            });
        }
        catch (Exception ex)
        {
            results.Add(("Setup", false, $"{ex.GetType().Name}: {ex.Message}"));
        }
        finally
        {
            try
            {
                editor.Text = previousText;
                editor.SetSelections(previousSelections);
            }
            catch (Exception ex)
            {
                results.Add(("Restore", false, $"{ex.GetType().Name}: {ex.Message}"));
            }
        }

        return results;
    }

    private static string Format(object? value) => value switch
    {
        null => "null",
        string s => $"\"{s.Replace("\n", "\\n")}\"",
        _ => value.ToString() ?? "null",
    };
}
