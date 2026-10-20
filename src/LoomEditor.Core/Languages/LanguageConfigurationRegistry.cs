// Ported from microsoft/vscode: src/vs/editor/common/languages/languageConfigurationRegistry.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Languages;

/// <summary>
/// The language configurations registered by language id, with priorities, and the cache of their resolved
/// configurations (Monaco: <c>LanguageConfigurationService</c> and <c>LanguageConfigurationRegistry</c> in one class).
/// </summary>
/// <remarks>
/// <para>
/// Differences from Monaco: an <b>instance</b>, one per editor (ADR 0009, decision 5), not a service singleton; the
/// configuration service part (<c>customizedLanguageConfigKeys</c>, <c>getCustomizedLanguageConfig</c>,
/// <c>validateBracketPairs</c>: the settings <c>editor.language.brackets</c> and
/// <c>editor.language.colorizedBracketPairs</c>) and the language service check of <c>computeConfig</c> are not ported.
/// </para>
/// <para>Threading: not thread-safe; use it from the UI thread.</para>
/// </remarks>
internal sealed class LanguageConfigurationRegistry
{
    private readonly Dictionary<string, ComposedLanguageConfiguration> _entries = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ResolvedLanguageConfiguration> _configurations = new(StringComparer.Ordinal);

    /// <summary>Registers the <c>plaintext</c> defaults (priority 0).</summary>
    public LanguageConfigurationRegistry()
    {
        Register(LanguageIdCodec.PlainTextLanguageId, new LanguageConfiguration
        {
            Brackets =
            [
                new("(", ")"),
                new("[", "]"),
                new("{", "}"),
            ],
            SurroundingPairs =
            [
                new("{", "}"),
                new("[", "]"),
                new("(", ")"),
                new("<", ">"),
                new("\"", "\""),
                new("'", "'"),
                new("`", "`"),
            ],
            ColorizedBracketPairs = [],
            Folding = new FoldingRules(OffSide: true),
        }, 0);
    }

    /// <summary><c>onDidChange</c>: raised after the cached configuration of a language is dropped.</summary>
    public event EventHandler<LanguageConfigurationChangedEventArgs>? Changed;

    /// <summary>
    /// <c>getIndentationAtPosition</c>: the leading whitespace of a line, cut at <paramref name="column"/>.
    /// </summary>
    public static string GetIndentationAtPosition(TextModel model, int lineNumber, int column)
    {
        ArgumentNullException.ThrowIfNull(model);
        string lineText = model.GetLineContent(lineNumber);
        string indentation = Strings.GetLeadingWhitespace(lineText);
        if (indentation.Length > column - 1)
        {
            indentation = indentation[..(column - 1)];
        }

        return indentation;
    }

    /// <summary><c>register</c>: registers a configuration; dispose the result to unregister it.</summary>
    /// <param name="languageId">The language.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="priority">Use a higher number for higher priority.</param>
    public IDisposable Register(string languageId, LanguageConfiguration configuration, int priority = 0)
    {
        ArgumentNullException.ThrowIfNull(languageId);
        ArgumentNullException.ThrowIfNull(configuration);
        if (!_entries.TryGetValue(languageId, out ComposedLanguageConfiguration? entries))
        {
            entries = new ComposedLanguageConfiguration();
            _entries[languageId] = entries;
        }

        Action unregister = entries.Register(configuration, priority);
        OnDidChange(languageId);

        return new Registration(() =>
        {
            unregister();
            OnDidChange(languageId);
        });
    }

    /// <summary>
    /// <c>getLanguageConfiguration</c>: the resolved configuration of a language (empty when nothing is registered),
    /// cached until the next change of that language.
    /// </summary>
    public ResolvedLanguageConfiguration GetLanguageConfiguration(string languageId)
    {
        ArgumentNullException.ThrowIfNull(languageId);
        if (!_configurations.TryGetValue(languageId, out ResolvedLanguageConfiguration? result))
        {
            result = ComputeConfig(languageId);
            _configurations[languageId] = result;
        }

        return result;
    }

    // computeConfig without the configuration and language services: the composed configuration, or an empty one.
    private ResolvedLanguageConfiguration ComputeConfig(string languageId)
    {
        LanguageConfiguration? config = _entries.GetValueOrDefault(languageId)?.Resolve();
        return new ResolvedLanguageConfiguration(languageId, config ?? new LanguageConfiguration());
    }

    private void OnDidChange(string languageId)
    {
        _configurations.Remove(languageId);
        Changed?.Invoke(this, new LanguageConfigurationChangedEventArgs(languageId));
    }

    /// <summary>
    /// <c>combineLanguageConfigurations</c>: later entries win, property by property. <c>||</c> is <c>??</c>, except for
    /// <see cref="LanguageConfiguration.AutoCloseBefore"/> where an empty string is falsy.
    /// </summary>
    internal static LanguageConfiguration CombineLanguageConfigurations(IEnumerable<LanguageConfiguration> configs)
    {
        var result = new LanguageConfiguration();
        foreach (LanguageConfiguration entry in configs)
        {
            result = new LanguageConfiguration
            {
                Comments = entry.Comments ?? result.Comments,
                Brackets = entry.Brackets ?? result.Brackets,
                WordPattern = entry.WordPattern ?? result.WordPattern,
                IndentationRules = entry.IndentationRules ?? result.IndentationRules,
                OnEnterRules = entry.OnEnterRules ?? result.OnEnterRules,
                AutoClosingPairs = entry.AutoClosingPairs ?? result.AutoClosingPairs,
                SurroundingPairs = entry.SurroundingPairs ?? result.SurroundingPairs,
                AutoCloseBefore = string.IsNullOrEmpty(entry.AutoCloseBefore) ? result.AutoCloseBefore : entry.AutoCloseBefore,
                Folding = entry.Folding ?? result.Folding,
                ColorizedBracketPairs = entry.ColorizedBracketPairs ?? result.ColorizedBracketPairs,
                DocComment = entry.DocComment ?? result.DocComment,
            };
        }

        return result;
    }

    /// <summary>
    /// The entries of one language (Monaco: <c>ComposedLanguageConfiguration</c>; its resolved cache is the registry's
    /// cache, dropped on the same changes).
    /// </summary>
    private sealed class ComposedLanguageConfiguration
    {
        private readonly List<LanguageConfigurationContribution> _entries = [];
        private int _order;

        public Action Register(LanguageConfiguration configuration, int priority)
        {
            var entry = new LanguageConfigurationContribution(configuration, priority, ++_order);
            _entries.Add(entry);
            return () => _entries.Remove(entry);
        }

        public LanguageConfiguration? Resolve()
        {
            if (_entries.Count == 0)
            {
                return null;
            }

            // The order is total (priority, then registration order), so the unstable List.Sort is safe.
            _entries.Sort(LanguageConfigurationContribution.Cmp);
            return CombineLanguageConfigurations(_entries.Select(e => e.Configuration));
        }
    }

    /// <summary>Monaco: <c>LanguageConfigurationContribution</c>.</summary>
    private sealed record LanguageConfigurationContribution(LanguageConfiguration Configuration, int Priority, int Order)
    {
        public static int Cmp(LanguageConfigurationContribution a, LanguageConfigurationContribution b)
        {
            if (a.Priority == b.Priority)
            {
                // higher order last
                return a.Order - b.Order;
            }

            // higher priority last
            return a.Priority.CompareTo(b.Priority);
        }
    }

    /// <summary>Runs its action once (Monaco's <c>toDisposable</c>).</summary>
    private sealed class Registration(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;

        public void Dispose()
        {
            Action? action = _dispose;
            _dispose = null;
            action?.Invoke();
        }
    }
}
