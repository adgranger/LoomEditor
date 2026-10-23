// Ported from microsoft/vscode: src/vs/platform/markers/common/markerService.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Markers;

/// <summary>
/// Holds the markers (problems) each owner currently reports for the editor's single document, and tells when they change.
/// Differences from Monaco: one resource only (keyed by owner: no <c>changeAll</c>, <c>installResourceFilter</c>, resource read options),
/// no related information or <c>origin</c>, and <see cref="MarkersChanged"/> is synchronous (no debounce).
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
public sealed class MarkerService
{
    private readonly Dictionary<string, Marker[]> _byOwner = new(StringComparer.Ordinal);
    private readonly List<string> _owners = [];

    /// <summary>Raised after <see cref="Change"/> or <see cref="Remove"/> altered the markers.</summary>
    public event EventHandler? MarkersChanged;

    /// <summary>
    /// Replaces the markers of <paramref name="owner"/>. Null or empty removes them (raising the event only if there were some);
    /// otherwise the event is raised even when the new list equals the old one (Monaco does the same).
    /// Items without a message are dropped and positions are sanitized.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="owner"/> is null.</exception>
    public void Change(string owner, IReadOnlyList<MarkerData>? markers)
    {
        ArgumentNullException.ThrowIfNull(owner);

        var accepted = new List<Marker>(markers?.Count ?? 0);
        foreach (var data in markers ?? [])
        {
            if (ToMarker(owner, data) is { } marker)
            {
                accepted.Add(marker);
            }
        }

        if (accepted.Count == 0)
        {
            if (_byOwner.Remove(owner))
            {
                _owners.Remove(owner);
                MarkersChanged?.Invoke(this, EventArgs.Empty);
            }

            return;
        }

        if (!_byOwner.ContainsKey(owner))
        {
            _owners.Add(owner);
        }

        _byOwner[owner] = [.. accepted];
        MarkersChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Removes the markers of <paramref name="owner"/>; same as <c>Change(owner, null)</c>.</summary>
    public void Remove(string owner) => Change(owner, null);

    /// <summary>
    /// The markers of <paramref name="owner"/> (every owner when null, in the order the owners first called <see cref="Change"/>).
    /// </summary>
    /// <param name="owner">The owner to read, or null for all.</param>
    /// <param name="severities">A mask of <see cref="MarkerSeverity"/> bits to keep, or null to keep all.</param>
    /// <param name="take">Maximum number of markers; zero or negative means unlimited.</param>
    public IReadOnlyList<Marker> Read(string? owner = null, int? severities = null, int take = -1)
    {
        var result = new List<Marker>();
        IEnumerable<string> owners = owner is null ? _owners : [owner];
        foreach (var name in owners)
        {
            if (!_byOwner.TryGetValue(name, out var markers))
            {
                continue;
            }

            foreach (var marker in markers)
            {
                if (severities is { } mask && (mask & (int)marker.Severity) != (int)marker.Severity)
                {
                    continue;
                }

                result.Add(marker);
                if (take > 0 && result.Count >= take)
                {
                    return result;
                }
            }
        }

        return result;
    }

    /// <summary>Counts the current markers per severity.</summary>
    public MarkerStatistics GetStatistics()
    {
        int errors = 0, warnings = 0, infos = 0, unknowns = 0;
        foreach (var markers in _byOwner.Values)
        {
            foreach (var marker in markers)
            {
                switch (marker.Severity)
                {
                    case MarkerSeverity.Error:
                        errors++;
                        break;
                    case MarkerSeverity.Warning:
                        warnings++;
                        break;
                    case MarkerSeverity.Info:
                        infos++;
                        break;
                    default:
                        unknowns++;
                        break;
                }
            }
        }

        return new MarkerStatistics(errors, warnings, infos, unknowns);
    }

    // Monaco _toMarker
    private static Marker? ToMarker(string owner, MarkerData data)
    {
        if (string.IsNullOrEmpty(data.Message))
        {
            return null;
        }

        var startLine = data.StartLineNumber > 0 ? data.StartLineNumber : 1;
        var startColumn = data.StartColumn > 0 ? data.StartColumn : 1;
        var endLine = data.EndLineNumber >= startLine ? data.EndLineNumber : startLine;
        var endColumn = data.EndColumn > 0 ? data.EndColumn : startColumn;
        return new Marker(owner, data.Severity, data.Message, startLine, startColumn, endLine, endColumn, data.Code, data.Source, data.ModelVersionId, data.Tags);
    }
}
