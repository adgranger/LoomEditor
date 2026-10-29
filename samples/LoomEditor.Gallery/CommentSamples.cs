using LoomEditor.Core.Comments;

namespace LoomEditor.Gallery;

/// <summary>The "Comment threads" sample of phase 11: a small C# file and three review threads built in code (no WinUI types).</summary>
/// <remarks>Threading: every member is pure except <see cref="AddThreads"/>, which runs on the editor's UI thread.</remarks>
internal static class CommentSamples
{
    /// <summary>The name of the sample in the Features picker.</summary>
    public const string Name = "Comment threads";

    /// <summary>The line of <c>int max = 10;</c>, the target of the suggestion.</summary>
    public const int MaxLine = 10;

    /// <summary>The first line of the range thread: the <c>for</c> loop of <c>Sum</c>.</summary>
    public const int RangeStart = 12;

    /// <summary>The last line of the range thread (the closing brace of the loop).</summary>
    public const int RangeEnd = 15;

    /// <summary>The line of the exact-text thread: <c>int best = int.MinValue;</c> of <c>Max</c>, which no other sample thread covers.</summary>
    public const int TextLine = 29;

    /// <summary>The identifier the exact-text thread is anchored to (on <see cref="TextLine"/>).</summary>
    public const string TextWord = "best";

    /// <summary>The author of the comments the user posts.</summary>
    public static CommentAuthor You { get; } = new("You");

    /// <summary>The picker entry.</summary>
    public static FeatureSample Sample { get; } = new(
        Name, "csharp", Text, MaxLine, 9,
        "Click a comment's Reply, the gutter + (switch on), Apply suggestion; Ctrl+Alt+M adds a thread at the caret");

    /// <summary>The modified version of <see cref="Text"/> for the diff window: two changed lines and one added line.</summary>
    public static string ModifiedText { get; } = Text
        .Replace("int max = 10;", "const int Max = 10;", StringComparison.Ordinal)
        .Replace("i < max", "i < Max", StringComparison.Ordinal)
        .Replace("        return total;", "        Console.WriteLine(total);\n        return total;", StringComparison.Ordinal);

    /// <summary>
    /// Adds the three threads: a Markdown comment by Ada Lovelace; a comment, a reply by Grace Hopper and a suggestion
    /// (<c>int max = 10;</c> → <c>const int Max = 10;</c>); a resolved thread with two comments.
    /// </summary>
    /// <param name="addThread">Adds a thread on a line with a title (the editor's or one side of the diff editor's <c>AddCommentThread</c>).</param>
    /// <param name="addRangeThread">Adds a thread on a range of lines with a title; used for the 4-line thread with a multi-line suggestion.</param>
    /// <param name="addTextThread">Adds a thread on the exact text of a range with a title; used for the thread on <see cref="TextWord"/>.</param>
    public static void AddThreads(
        Func<int, string?, CommentThread> addThread,
        Func<int, int, string?, CommentThread> addRangeThread,
        Func<Range, string?, CommentThread> addTextThread)
    {
        ArgumentNullException.ThrowIfNull(addThread);
        ArgumentNullException.ThrowIfNull(addRangeThread);
        ArgumentNullException.ThrowIfNull(addTextThread);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var ada = new CommentAuthor("Ada Lovelace");
        var grace = new CommentAuthor("Grace Hopper");
        var linus = new CommentAuthor("Linus");

        addThread(8, null).AddComment(Comment.Create(
            ada, "The **sum** should not depend on `Console`:\n\n- return the value\n- let the caller print it", now.AddHours(-2)));

        CommentThread suggestion = addThread(MaxLine, "Magic number");
        suggestion.AddComment(Comment.Create(linus, "Why is the limit a local?", now.AddMinutes(-50)));
        suggestion.AddComment(Comment.Create(
            grace, "Make it a constant:\n\n```suggestion\nconst int Max = 10;\n```", now.AddMinutes(-20)));

        // Four lines in, four lines out: the diff shows three context rows around the changed one.
        addRangeThread(RangeStart, RangeEnd, null).AddComment(Comment.Create(
            grace,
            "Guard against overflow:\n\n```suggestion\n        for (int i = 0; i < max && i < values.Count; i++)\n        {\n            total = checked(total + values[i]);\n        }\n```",
            now.AddMinutes(-10)));

        // One identifier of a line: only the word is tinted; the suggestion still rewrites the whole line.
        string line = Text.Split('\n')[TextLine - 1];
        int start = line.IndexOf(TextWord, StringComparison.Ordinal) + 1;
        addTextThread(new Range(TextLine, start, TextLine, start + TextWord.Length), null).AddComment(Comment.Create(
            grace, "A clearer name:\n\n```suggestion\n        int largest = int.MinValue;\n```", now.AddMinutes(-5)));

        CommentThread resolved = addThread(24, null);
        resolved.AddComment(Comment.Create(grace, "This line is too long to read in a review.", now.AddDays(-1)));
        resolved.AddComment(Comment.Create(ada, "Wrapped it in the next change.", now.AddHours(-20)));
        resolved.State = CommentThreadState.Resolved;
    }

    // About 60 lines: a class with a method whose body has `int max = 10;` (line 10), a loop and a long line (24).
    private const string Text = """
        using System;
        using System.Collections.Generic;

        namespace Demo;

        public static class Totals
        {
            public static int Sum(IReadOnlyList<int> values)
            {
                int max = 10;
                int total = 0;
                for (int i = 0; i < max && i < values.Count; i++)
                {
                    total += values[i];
                }

                Console.WriteLine(total);
                return total;
            }

            public static string Describe(IReadOnlyList<int> values)
            {
                int total = Sum(values);
                return $"The sum of the first values of the list is {total}, computed over {values.Count} values with a limit, and this sentence is long on purpose so that it scrolls.";
            }

            public static int Max(IReadOnlyList<int> values)
            {
                int best = int.MinValue;
                foreach (int value in values)
                {
                    if (value > best)
                    {
                        best = value;
                    }
                }

                return best;
            }

            public static int Min(IReadOnlyList<int> values)
            {
                int best = int.MaxValue;
                foreach (int value in values)
                {
                    if (value < best)
                    {
                        best = value;
                    }
                }

                return best;
            }

            public static double Average(IReadOnlyList<int> values) =>
                values.Count == 0 ? 0 : (double)Sum(values) / values.Count;

            public static IEnumerable<int> Evens(IEnumerable<int> values)
            {
                foreach (int value in values)
                {
                    if (value % 2 == 0)
                    {
                        yield return value;
                    }
                }
            }
        }
        """;
}
