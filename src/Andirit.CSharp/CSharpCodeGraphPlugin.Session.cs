using System.Security.Cryptography;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Andirit;

public sealed partial class CSharpCodeGraphPlugin
{
    private sealed class Session(
        CodeGraphPluginContext context,
        CSharpCodeGraphPlugin plugin) : ICodeGraphExtractionSession, IVerifiedCSharpSession
    {
        public VerifiedCSharpAnalysis? Provenance { get; private set; }
        public async ValueTask<CodeGraphExtractionResult> ExtractAsync(
            ICodeGraphSink sink,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(sink);
            Provenance = null;
            var analysis = await PrepareAnalysisAsync(context, cancellationToken).ConfigureAwait(false);
            var sourcesByPath = context.Sources.ToDictionary(source => source.Path, StringComparer.Ordinal);
            var projects = analysis.Projects.Select(p => p.Project).ToArray();
            var allDiagnostics = new List<Diagnostic>();
            var warningCodes = new HashSet<string>(StringComparer.Ordinal);
            warningCodes.UnionWith(analysis.DiscoveryWarnings);
            if (!analysis.Universe.IsComplete) warningCodes.Add("csharp.provenance.incomplete");
            var contributingSources = new HashSet<string>(StringComparer.Ordinal);
            var runSymbols = new RunSymbols();
            var relationshipTotals = new Dictionary<string, RelationshipCounters>(
                StringComparer.Ordinal);
            var indexUnitCoverage = new List<CodeIndexUnitCoverage>();
            foreach (var prepared in analysis.Projects)
            {
                var project = prepared.Project;
                var compilation = prepared.Compilation;
                var trees = compilation.SyntaxTrees.ToArray();
                var availableCompilations = prepared.AvailableCompilations;
                var builder = new GraphBuilder(
                    context,
                    plugin,
                    project,
                    compilation,
                    availableCompilations,
                    runSymbols);
                builder.AddProject();
                foreach (var tree in trees)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                    var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);
                    var source = sourcesByPath[tree.FilePath];
                    builder.AddFile(source);
                    builder.AddDeclarations(source.Path, root, model, cancellationToken);
                }
                builder.AddRelationships(cancellationToken);
                // WP-S23-3A: publish this project's real occurrence contribution
                // before the completing graph batch, so one replacement stages the
                // graph, facts, coverage and authority together.
                if (prepared.Verified is not null)
                {
                    await PublishOccurrencesAsync(
                        sink,
                        context.RepositoryId,
                        plugin.Id,
                        plugin.Version,
                        context.IndexRunId,
                        CSharpProjectDiscovery.IndexUnitId(project.Path),
                        analysis.Universe,
                        prepared.Verified,
                        cancellationToken).ConfigureAwait(false);
                }

                await builder.WriteAsync(sink, cancellationToken).ConfigureAwait(false);
                contributingSources.UnionWith(builder.ContributingSourcePaths);
                allDiagnostics.AddRange(compilation.GetDiagnostics(cancellationToken));
                warningCodes.UnionWith(project.WarningCodes);
                foreach (var (kind, counters) in builder.Counters)
                {
                    var total = relationshipTotals.TryGetValue(
                        kind,
                        out var existing)
                        ? existing
                        : new RelationshipCounters();
                    total.EdgesEmitted += counters.EdgesEmitted;
                    total.UnresolvedTargets += counters.UnresolvedTargets;
                    total.Candidates += counters.Candidates;
                    total.InternalTargets += counters.InternalTargets;
                    total.CrossProjectTargets += counters.CrossProjectTargets;
                    total.ExternalTargets += counters.ExternalTargets;
                    total.AmbiguousTargets += counters.AmbiguousTargets;
                    total.UnsupportedTargets += counters.UnsupportedTargets;
                    relationshipTotals[kind] = total;
                }

                indexUnitCoverage.Add(new CodeIndexUnitCoverage(
                    new CodeIndexUnitId(CSharpProjectDiscovery.IndexUnitId(project.Path)),
                    BuildCoverage(
                        builder.Counters,
                        hasSources: trees.Length > 0)));
            }

            var diagnostics = allDiagnostics
                .Where(diagnostic => diagnostic.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error)
                .ToArray();
            warningCodes.UnionWith(diagnostics
                .Select(diagnostic => $"csharp.roslyn.{diagnostic.Id.ToLowerInvariant()}")
                .Take(100));
            var currentUnits = projects
                .Select(project => new CodeIndexUnitId(CSharpProjectDiscovery.IndexUnitId(project.Path)))
                .ToHashSet();
            var obsoleteUnits = context.Changes
                .Where(change =>
                    change.Kind == CodeGraphSourceChangeKind.Deleted &&
                    change.Path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                .Select(change => new CodeIndexUnitId(
                    CSharpProjectDiscovery.IndexUnitId(change.Path)))
                .Concat(context.PreviousIndexUnits.Where(unit => !currentUnits.Contains(unit)))
                .Distinct()
                .OrderBy(unit => unit.Value, StringComparer.Ordinal)
                .ToArray();
            cancellationToken.ThrowIfCancellationRequested();
            Provenance = analysis;
            return new CodeGraphExtractionResult(
                obsoleteUnits,
                sourcesExamined: context.Sources.Count,
                sourcesContributingFacts: contributingSources.Count,
                unresolvedRelationships: diagnostics.Count(diagnostic =>
                    diagnostic.Severity == DiagnosticSeverity.Error &&
                    UnresolvedDiagnosticIds.Contains(diagnostic.Id)),
                warningCodes: warningCodes.Order(StringComparer.Ordinal).Take(100).ToArray(),
                relationshipCoverage: BuildCoverage(relationshipTotals, hasSources: projects.Length > 0),
                indexUnitCoverage: indexUnitCoverage);
        }

        private static IReadOnlyCollection<CodeRelationshipCoverage> BuildCoverage(
            IReadOnlyDictionary<string, RelationshipCounters> totals,
            bool hasSources)
        {
            var coverage = new List<CodeRelationshipCoverage>();
            foreach (var kind in new[]
                     {
                         CodeEdgeKinds.Inherits,
                         CodeEdgeKinds.Implements,
                         CodeEdgeKinds.Calls,
                         CodeEdgeKinds.References,
                         CodeEdgeKinds.Imports,
                         CodeEdgeKinds.ExercisedBy
                     })
            {
                var counters = totals.TryGetValue(
                    kind.Value,
                    out var value)
                    ? value
                    : new RelationshipCounters();
                coverage.Add(ToCoverage(kind.Value, counters, hasSources));
            }

            // Return/parameter typing is deliberately not produced yet: the
            // cross-language meaning is not precise enough to publish without
            // guessing. The not-produced entries keep that decision visible.
            coverage.Add(new(
                CodeEdgeKinds.Returns.Value,
                CodeRelationshipCoverageState.NotProduced,
                0,
                0));
            coverage.Add(new(
                CodeEdgeKinds.Accepts.Value,
                CodeRelationshipCoverageState.NotProduced,
                0,
                0));
            return coverage;
        }

        private static CodeRelationshipCoverage ToCoverage(
            string kind,
            RelationshipCounters counters,
            bool hasSources)
        {
            // A scan that ran and found nothing is a complete empty result,
            // not a missing one. Only a unit without sources cannot claim
            // anything about its relationships.
            if (counters.Candidates == 0)
            {
                var empty = hasSources
                    ? CodeRelationshipCoverageState.Produced
                    : CodeRelationshipCoverageState.Unavailable;
                return new(kind, empty, 0, 0);
            }

            var state = counters.UnresolvedTargets > 0
                ? CodeRelationshipCoverageState.Partial
                : CodeRelationshipCoverageState.Produced;
            return new(
                kind,
                state,
                counters.EdgesEmitted,
                counters.UnresolvedTargets,
                counters.Candidates,
                counters.InternalTargets,
                counters.CrossProjectTargets,
                counters.ExternalTargets,
                counters.AmbiguousTargets,
                counters.UnsupportedTargets);
        }

        public ValueTask DisposeAsync()
        {
            Provenance = null;
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Symbols shared across every project in one extraction run. Registrations
    /// are keyed by canonical documentation ID for lookup, but every entry keeps
    /// its project provenance, definition fingerprint and declaration paths.
    /// Canonical IDs never establish target identity on their own.
    /// </summary>
    private sealed class RunSymbols
    {
        private readonly SortedDictionary<string, SortedDictionary<string, Registration>> _registrations =
            new(StringComparer.Ordinal);

        public void Register(
            string canonicalKey,
            string projectPath,
            CodeNodeId nodeId,
            string definitionFingerprint,
            string declarationPath)
        {
            if (!_registrations.TryGetValue(canonicalKey, out var byProject))
            {
                byProject = new(StringComparer.Ordinal);
                _registrations[canonicalKey] = byProject;
            }

            if (!byProject.TryGetValue(projectPath, out var registration))
            {
                registration = new Registration(nodeId, definitionFingerprint);
                byProject[projectPath] = registration;
            }
            else if (registration.NodeId != nodeId ||
                     !string.Equals(
                         registration.DefinitionFingerprint,
                         definitionFingerprint,
                         StringComparison.Ordinal))
            {
                registration.Conflicting = true;
            }

            registration.DeclarationPaths.Add(declarationPath);
        }

        public bool TryGet(
            string canonicalKey,
            string projectPath,
            string definitionFingerprint,
            out Registration registration)
        {
            registration = null!;
            return _registrations.TryGetValue(canonicalKey, out var byProject) &&
                byProject.TryGetValue(projectPath, out registration!) &&
                !registration.Conflicting &&
                string.Equals(
                    registration.DefinitionFingerprint,
                    definitionFingerprint,
                    StringComparison.Ordinal);
        }

        public bool HasDefinition(
            string canonicalKey,
            string projectPath,
            string definitionFingerprint) =>
            TryGet(canonicalKey, projectPath, definitionFingerprint, out _);

        public bool HasConflict(string canonicalKey, string projectPath) =>
            _registrations.TryGetValue(canonicalKey, out var byProject) &&
            byProject.TryGetValue(projectPath, out var registration) &&
            registration.Conflicting;

        public bool DeclaredIn(
            string canonicalKey,
            string projectPath,
            IReadOnlyCollection<string> paths,
            string definitionFingerprint)
        {
            if (!TryGet(canonicalKey, projectPath, definitionFingerprint, out var registration))
                return false;

            return paths.Any(path => registration.DeclarationPaths.Contains(path));
        }

        public int CountDistinctNodes(string canonicalKey) =>
            _registrations.TryGetValue(canonicalKey, out var byProject)
                ? byProject.Values
                    .Where(registration => !registration.Conflicting)
                    .Select(registration => registration.NodeId)
                    .Distinct()
                    .Count()
                : 0;

        public sealed class Registration(
            CodeNodeId nodeId,
            string definitionFingerprint)
        {
            public CodeNodeId NodeId { get; } = nodeId;
            public string DefinitionFingerprint { get; } = definitionFingerprint;
            public SortedSet<string> DeclarationPaths { get; } =
                new(CSharpProjectDiscovery.PathComparer);
            public bool Conflicting { get; set; }
        }
    }

    private sealed class RelationshipCounters
    {
        public int EdgesEmitted;
        public int UnresolvedTargets;
        public int Candidates;
        public int InternalTargets;
        public int CrossProjectTargets;
        public int ExternalTargets;
        public int AmbiguousTargets;
        public int UnsupportedTargets;
    }

    private static IReadOnlyList<CSharpProjectModel> OrderProjects(
        IReadOnlyList<CSharpProjectModel> projects,
        IReadOnlyDictionary<string, CSharpProjectModel> projectByPath,
        ISet<string> warningCodes)
    {
        var ordered = new List<CSharpProjectModel>();
        var visited = new HashSet<string>(CSharpProjectDiscovery.PathComparer);
        var visiting = new HashSet<string>(CSharpProjectDiscovery.PathComparer);

        void Visit(CSharpProjectModel project)
        {
            if (visited.Contains(project.Path))
                return;
            if (!visiting.Add(project.Path))
            {
                warningCodes.Add("csharp.project.reference-cycle");
                return;
            }
            foreach (var reference in project.ProjectReferences)
            {
                if (projectByPath.TryGetValue(reference, out var dependency))
                    Visit(dependency);
            }
            visiting.Remove(project.Path);
            visited.Add(project.Path);
            ordered.Add(project);
        }

        foreach (var project in projects.OrderBy(project => project.Path, StringComparer.Ordinal))
            Visit(project);
        return ordered;
    }

    // WP-S23-3A real-fact occurrence publication. The S2.2 verified extractor
    // already produces authority-gated `usage4` facts and the accepted authority
    // envelope; this wires that output into the existing batch/sink publication
    // path together with the internal authority handoff, without changing the
    // public graph-batch contract, the persisted schema, or the Option A identity
    // separation.
    //
    // The identifier/declaration/shape component digests remain caller-supplied
    // under the accepted S-01 qualification: the forms below are this plugin's
    // deterministic caller convention, not an independent canonicalization claim.
    private static string OccurrenceIdentifierDigest(IMethodSymbol method) =>
        CodeOccurrenceIdentity.ComputeIdentifierDigest([method.Name]);

    private static string OccurrenceDeclarationDigest(IMethodSymbol method) =>
        CodeOccurrenceIdentity.ComputeDeclarationDigest([CanonicalSymbolKey(Normalize(method))]);

    private static string OccurrenceShapeDigest(IMethodSymbol method)
    {
        var normalized = (IMethodSymbol)Normalize(method);
        return CodeOccurrenceIdentity.ComputeShapeDigest("method|" + normalized.Arity + "|" +
            string.Join(",", normalized.Parameters.Select(parameter => TypeIdentity(parameter.Type))));
    }

    private static string OccurrenceTargetDocumentId(IMethodSymbol method) =>
        method.GetDocumentationCommentId() ?? QualifiedName(method);

    // Publishes this source unit's real occurrence contribution, if any, through
    // the accepted sink. It writes an occurrence batch and registers the matching
    // authority envelope for the same origin; the completing graph batch then
    // stages one replacement carrying both. A sink that cannot carry authority
    // receives graph-only output, so occurrence facts are never published without
    // the association required to serve them.
    private static async ValueTask PublishOccurrencesAsync(
        ICodeGraphSink sink,
        CodeRepositoryId repositoryId,
        CodePluginId pluginId,
        string pluginVersion,
        CodeIndexRunId runId,
        string unitId,
        VerifiedCSharpUniverse universe,
        VerifiedCSharpCompilation source,
        CancellationToken cancellationToken)
    {
        if (sink is not ICodeOccurrenceAuthoritySink authoritySink)
            return;

        var extraction = VerifiedCSharpOccurrenceExtractor.Extract(universe,
            new VerifiedOccurrenceExtractionRequest(repositoryId, pluginId, source,
                OccurrenceIdentifierDigest, OccurrenceDeclarationDigest, OccurrenceShapeDigest,
                OccurrenceTargetDocumentId, pluginVersion));
        if (extraction.Authority is null)
            return;

        var origin = new CodeFactOrigin(repositoryId, pluginId, pluginVersion, runId,
            new CodeIndexUnitId(unitId));
        await authoritySink.SetOccurrenceAuthorityAsync(origin, extraction.Authority, cancellationToken)
            .ConfigureAwait(false);
        await sink.WriteBatchAsync(new CodeGraphBatch(origin,
                occurrences: extraction.Facts, occurrenceCoverage: extraction.Coverage),
            cancellationToken).ConfigureAwait(false);
    }
}
