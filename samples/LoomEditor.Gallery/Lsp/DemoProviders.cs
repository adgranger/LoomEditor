using System.Text.RegularExpressions;
using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.Gallery.Lsp;

/// <summary>Tiny in-process providers so every widget can be tried without a language server.</summary>
/// <remarks>Threading: UI thread only; the providers read the model synchronously and return completed tasks.</remarks>
internal static partial class DemoProviders
{
    internal static readonly string[] Keywords = ["if", "else", "for", "while", "return", "class", "static", "void", "int", "string"];

    /// <summary>Registers the three providers for <paramref name="languageId"/>; disposing unregisters them.</summary>
    internal static IDisposable Register(LanguageFeaturesService features, string languageId, string documentUri)
    {
        IDisposable[] registrations =
        [
            features.CompletionProviders.Register(languageId, new DemoCompletionProvider()),
            features.HoverProviders.Register(languageId, new DemoHoverProvider()),
            features.DefinitionProviders.Register(languageId, new DemoDefinitionProvider(documentUri)),
        ];
        return new Registrations(registrations);
    }

    [GeneratedRegex(@"[A-Za-z_][A-Za-z0-9_]*")]
    internal static partial Regex WordRegex();

    /// <summary>The identifier at (or ending at) the position, with its range.</summary>
    internal static (string Word, Range Range)? WordAt(TextModel model, Position position)
    {
        string line = model.GetLineContent(position.LineNumber);
        foreach (Match match in WordRegex().Matches(line))
        {
            if (position.Column - 1 >= match.Index && position.Column - 1 <= match.Index + match.Length)
            {
                return (match.Value, new Range(position.LineNumber, match.Index + 1, position.LineNumber, match.Index + match.Length + 1));
            }
        }

        return null;
    }

    private sealed class Registrations(IDisposable[] items) : IDisposable
    {
        public void Dispose()
        {
            foreach (IDisposable item in items)
            {
                item.Dispose();
            }
        }
    }
}

/// <summary>Completes the words of the document and a few keywords.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class DemoCompletionProvider : ICompletionItemProvider
{
    public IReadOnlyList<string>? TriggerCharacters { get; } = new[] { "." };

    public Task<CompletionList?> ProvideCompletionItemsAsync(TextModel model, Position position, CompletionContext context, CancellationToken token)
    {
        SortedSet<string> words = new(StringComparer.Ordinal);
        foreach (Match match in DemoProviders.WordRegex().Matches(model.GetValue(EndOfLinePreference.LF)))
        {
            if (match.Length > 1)
            {
                words.Add(match.Value);
            }
        }

        List<CompletionItem> items = [.. words.Select(word => new CompletionItem(word, word, CompletionItemKind.Text))];
        items.AddRange(DemoProviders.Keywords.Select(keyword => new CompletionItem(keyword, keyword, CompletionItemKind.Keyword)));
        return Task.FromResult<CompletionList?>(new CompletionList(items));
    }
}

/// <summary>Hovers the word under the pointer.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class DemoHoverProvider : IHoverProvider
{
    public Task<Hover?> ProvideHoverAsync(TextModel model, Position position, CancellationToken token)
    {
        if (DemoProviders.WordAt(model, position) is not var (word, range))
        {
            return Task.FromResult<Hover?>(null);
        }

        return Task.FromResult<Hover?>(new Hover(new[] { new MarkdownString($"**{word}**\n\nDemo hover from an in-process provider.") }, range));
    }
}

/// <summary>Finds the first declaration of the word under the caret in the same document.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class DemoDefinitionProvider(string documentUri) : IDefinitionProvider
{
    public Task<IReadOnlyList<LocationLink>?> ProvideDefinitionAsync(TextModel model, Position position, CancellationToken token)
    {
        IReadOnlyList<LocationLink> none = Array.Empty<LocationLink>();
        if (DemoProviders.WordAt(model, position) is not var (word, _))
        {
            return Task.FromResult<IReadOnlyList<LocationLink>?>(none);
        }

        Regex declaration = new($@"\b(?:class|struct|void|int)\s+({Regex.Escape(word)})\b");
        for (int line = 1; line <= model.LineCount; line++)
        {
            Match match = declaration.Match(model.GetLineContent(line));
            if (match.Success)
            {
                Group group = match.Groups[1];
                return Task.FromResult<IReadOnlyList<LocationLink>?>(
                    new[] { new LocationLink(documentUri, new Range(line, group.Index + 1, line, group.Index + group.Length + 1)) });
            }
        }

        return Task.FromResult<IReadOnlyList<LocationLink>?>(none);
    }
}
