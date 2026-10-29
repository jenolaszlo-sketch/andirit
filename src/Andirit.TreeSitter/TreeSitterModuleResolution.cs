using System.Security.Cryptography;
using System.Text;

namespace Andirit;

/// <summary>A literal module-reference occurrence captured by an ECMAScript profile.</summary>
internal sealed record TreeSitterModuleReference(
    string? Specifier,
    string ModuleReference,
    bool TypeOnly,
    CodeLocation Location);

/// <summary>One decodable source file and its captured module references.</summary>
internal sealed record TreeSitterModuleFile(
    string Path,
    CodeNodeId FileNodeId,
    IReadOnlyList<TreeSitterModuleReference> References, IReadOnlyList<CodeGraphEdge>? InterfaceOccurrences = null);

/// <summary>Outcome counters and facts from one plugin-local module pass.</summary>
internal sealed record TreeSitterModuleResolutionResult(
    IReadOnlyList<CodeGraphEdge> Edges,
    int Candidates,
    int InternalTargets,
    int CrossProjectTargets,
    int UnresolvedTargets,
    int ExternalTargets,
    int AmbiguousTargets,
    int UnsupportedTargets, IReadOnlyList<CodeGraphEdge>? OccurrenceEdges = null);

/// <summary>Internal seam for a language plugin's module-resolution pass.</summary>
internal interface ITreeSitterModuleResolver
{
    string Language { get; }
    string Provider { get; }
    string ProviderVersion { get; }

    TreeSitterModuleResolutionResult Resolve(
        IReadOnlyList<TreeSitterModuleFile> inventory,
        CancellationToken cancellationToken);
}

/// <summary>
/// Resolves ECMAScript module references using the owner-accepted relative-v1
/// policy and only the decodable sources in one Tree-sitter plugin inventory.
/// </summary>
internal sealed class EcmaScriptModuleResolver : ITreeSitterModuleResolver
{
    internal const string Policy = "relative-v1";
    internal const string ResolverProvider = "penghou.hetu.tree-sitter.ecmascript-module-resolver";
    internal const int ResolverRevision = 2;

    private static readonly string[] TypeScriptSuffixes = [".ts", ".tsx", ".d.ts", ".mts", ".cts"];
    private static readonly string[] JavaScriptSuffixes = [".js", ".jsx", ".mjs", ".cjs"];
    private static readonly HashSet<string> EcmaScriptExtensions = new(StringComparer.Ordinal)
    {
        ".ts", ".tsx", ".mts", ".cts", ".js", ".jsx", ".mjs", ".cjs"
    };

    internal EcmaScriptModuleResolver(string language, string packageVersion)
    {
        if (language is not ("typescript" or "javascript"))
            throw new ArgumentOutOfRangeException(nameof(language));
        ArgumentException.ThrowIfNullOrWhiteSpace(packageVersion);
        Language = language;
        ProviderVersion = $"{packageVersion}+resolver{ResolverRevision}.{Policy}";
    }

    public string Language { get; }
    public string Provider => ResolverProvider;
    public string ProviderVersion { get; }

    public TreeSitterModuleResolutionResult Resolve(
        IReadOnlyList<TreeSitterModuleFile> inventory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(inventory);

        var files = new SortedDictionary<string, TreeSitterModuleFile>(StringComparer.Ordinal);
        foreach (var file in inventory)
        {
            if (!files.TryAdd(file.Path, file))
                throw new InvalidOperationException($"Duplicate module inventory path '{file.Path}'.");
        }

        var edges = new Dictionary<string, CodeGraphEdge>(StringComparer.Ordinal);
        var candidates = 0;
        var internalTargets = 0;
        var crossProjectTargets = 0;
        var unresolvedTargets = 0;
        var externalTargets = 0;
        var unsupportedTargets = 0;

        foreach (var file in files.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var reference in file.References
                         .OrderBy(value => value.Location.StartLine)
                         .ThenBy(value => value.Location.StartColumn)
                         .ThenBy(value => value.Location.EndLine)
                         .ThenBy(value => value.Location.EndColumn))
            {
                candidates++;
                var outcome = ResolveTarget(file.Path, reference.Specifier, files);
                switch (outcome.Kind)
                {
                    case OutcomeKind.External:
                        externalTargets++;
                        continue;
                    case OutcomeKind.Unsupported:
                        unsupportedTargets++;
                        continue;
                    case OutcomeKind.Unresolved:
                        unresolvedTargets++;
                        continue;
                    case OutcomeKind.Resolved:
                        break;
                    default:
                        throw new InvalidOperationException("Unknown module-resolution outcome.");
                }

                var target = outcome.Target ?? throw new InvalidOperationException(
                    "A resolved module outcome must name an indexed target.");
                var rule = outcome.Rule ?? throw new InvalidOperationException(
                    "A resolved module outcome must include its resolution rule.");
                var edge = CreateEdge(file, target, reference, rule);
                if (!edges.TryAdd(edge.Id.Value, edge))
                    throw new InvalidOperationException(
                        $"Duplicate resolves-to identity '{edge.Id.Value}' was produced.");

                if (file.FileNodeId == target.FileNodeId)
                    internalTargets++;
                else
                    crossProjectTargets++;
            }
        }

        // Additive occurrence-exact resolver outcomes include non-resolved references.
        // The original resolves-to facts above retain their exact identities and bytes.
        foreach (var file in files.Values)
        {
            foreach (var occurrence in file.InterfaceOccurrences ?? [])
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var data = System.Text.Json.JsonDocument.Parse(EcmaScriptInterfaceFacts.Text(occurrence, "ec6d.data")!);
                var root = data.RootElement;
                var key = root.GetProperty("resolverKey").GetString()!;
                var span = occurrence.Evidence.Location!;
                var counterparts = file.References.Where(r => r.Location == span).ToArray();
                if (counterparts.Length != 1)
                    throw new InvalidOperationException("An EC-6D occurrence requires exactly one resolver reference.");
                var reference = counterparts[0];
                var outcome = ResolveTarget(file.Path, reference.Specifier, files);
                var payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    // Owner-authorized relevance only; this never becomes binding resolution.
                    relevanceTarget = occurrence.Kind.Value == "imports-name" &&
                        root.GetProperty("form").GetString() is "default" or "namespace" or "import-equals" or "dynamic"
                        ? ResolveTarget(file.Path, root.GetProperty("moduleSpecifier").GetString(), files).Target?.FileNodeId.Value : null,
                    relevancePolicy = "relative-v1.relevance-only",
                    resolverKey = key,
                    sourceFile = file.FileNodeId.Value,
                    statementSpan = span,
                    moduleSpecifier = root.GetProperty("moduleSpecifier").GetString(),
                    referenceForm = root.GetProperty("referenceForm").GetString(),
                    resolutionStatus = outcome.Kind.ToString().ToLowerInvariant(),
                    resolvedTarget = outcome.Target?.FileNodeId.Value,
                    resolutionRule = outcome.Rule,
                    policy = Policy,
                    resolverEdgeId = outcome.Kind == OutcomeKind.Resolved
                        ? CreateEdge(file, outcome.Target!, reference, outcome.Rule!).Id.Value : null
                });
                var id = new CodeEdgeId($"{Language}:resolver-status:{key}");
                var status = new CodeGraphEdge(id, file.FileNodeId, outcome.Target?.FileNodeId ?? file.FileNodeId,
                    new CodeEdgeKind("resolver-status"),
                    new CodeEvidence(EvidenceQuality.Structural, Provider, ProviderVersion, span),
                    new Dictionary<string, CodePropertyValue> { ["ec6d.data"] = new CodeTextProperty(payload) });
                if (edges.TryGetValue(id.Value, out var previous))
                {
                    if (EcmaScriptInterfaceFacts.Text(previous, "ec6d.data") != payload)
                        throw new InvalidOperationException("Conflicting resolver-status occurrence identity.");
                }
                else edges.Add(id.Value, status);
            }
        }
        var orderedEdges = edges.Values.Where(e => e.Kind == CodeEdgeKinds.ResolvesTo)
            .OrderBy(edge => edge.Kind.Value, StringComparer.Ordinal)
            .ThenBy(edge => edge.SourceId.Value, StringComparer.Ordinal)
            .ThenBy(edge => edge.TargetId.Value, StringComparer.Ordinal)
            .ThenBy(edge => edge.Id.Value, StringComparer.Ordinal)
            .ToArray();
        return new(
            orderedEdges,
            candidates,
            internalTargets,
            crossProjectTargets,
            unresolvedTargets,
            externalTargets,
            0,
            unsupportedTargets, edges.Values.Where(e => e.Kind.Value == "resolver-status").OrderBy(e => e.Id.Value, StringComparer.Ordinal).ToArray());
    }

    private ResolutionOutcome ResolveTarget(
        string importerPath,
        string? specifier,
        IReadOnlyDictionary<string, TreeSitterModuleFile> inventory)
    {
        if (specifier is null)
            return ResolutionOutcome.Unsupported;
        if (specifier.Length > 256)
            return ResolutionOutcome.Unsupported;

        var isRelative = specifier.StartsWith("./", StringComparison.Ordinal) ||
            specifier.StartsWith("../", StringComparison.Ordinal);
        if (specifier.Contains('\\') || specifier.StartsWith("/", StringComparison.Ordinal))
            return ResolutionOutcome.Unsupported;
        if (!isRelative)
            return ResolutionOutcome.External;
        if (specifier.Contains('?') || specifier.Contains('#'))
            return ResolutionOutcome.Unsupported;

        var joined = NormalizeJoin(importerPath, specifier);
        if (joined is null)
            return ResolutionOutcome.Unsupported;
        if (inventory.TryGetValue(joined, out var exact))
            return ResolutionOutcome.Resolved(exact, "exact");

        var finalSegment = joined[(joined.LastIndexOf('/') + 1)..];
        if (finalSegment.Contains(".", StringComparison.Ordinal))
        {
            var extensionStart = finalSegment.LastIndexOf('.');
            var extension = finalSegment[extensionStart..];
            return EcmaScriptExtensions.Contains(extension)
                ? ResolutionOutcome.Unresolved
                : ResolutionOutcome.Unsupported;
        }

        var suffixes = Language == "typescript" ? TypeScriptSuffixes : JavaScriptSuffixes;
        foreach (var suffix in suffixes)
        {
            if (inventory.TryGetValue(joined + suffix, out var target))
                return ResolutionOutcome.Resolved(target, "extension-substitution");
        }

        foreach (var suffix in suffixes)
        {
            if (inventory.TryGetValue(joined + "/index" + suffix, out var target))
                return ResolutionOutcome.Resolved(target, "directory-index");
        }

        return ResolutionOutcome.Unresolved;
    }

    private static string? NormalizeJoin(string importerPath, string specifier)
    {
        var segments = importerPath.Split('/').ToList();
        if (segments.Count == 0)
            return null;
        segments.RemoveAt(segments.Count - 1);

        foreach (var segment in specifier.Split('/', StringSplitOptions.None))
        {
            if (segment.Length == 0)
                return null;
            if (segment == ".")
                continue;
            if (segment == "..")
            {
                if (segments.Count == 0)
                    return null;
                segments.RemoveAt(segments.Count - 1);
                continue;
            }

            segments.Add(segment);
        }

        return segments.Count == 0 ? null : string.Join("/", segments);
    }

    private CodeGraphEdge CreateEdge(
        TreeSitterModuleFile source,
        TreeSitterModuleFile target,
        TreeSitterModuleReference reference,
        string resolutionRule)
    {
        var specifier = reference.Specifier ?? throw new InvalidOperationException(
            "A resolved module reference must retain its written specifier.");
        var discriminator = $"resolves-to:{reference.ModuleReference}:{specifier}";
        var location = reference.Location;
        var span = $"{location.StartLine}:{location.StartColumn}:{location.EndLine}:{location.EndColumn}";
        var id = new CodeEdgeId(
            $"{Language}:{Hash($"{CodeEdgeKinds.ResolvesTo.Value}\n{source.FileNodeId.Value}\n{target.FileNodeId.Value}\n{discriminator}\n{span}")}");
        var properties = new Dictionary<string, CodePropertyValue>(StringComparer.Ordinal)
        {
            [CodePropertyKeys.Specifier] = new CodeTextProperty(specifier),
            [CodePropertyKeys.ModuleReference] = new CodeTextProperty(reference.ModuleReference),
            [CodePropertyKeys.ResolutionPolicy] = new CodeTextProperty(Policy),
            [CodePropertyKeys.ResolutionRule] = new CodeTextProperty(resolutionRule)
        };
        if (reference.TypeOnly)
            properties.Add(CodePropertyKeys.TypeOnly, new CodeBooleanProperty(true));

        return new(
            id,
            source.FileNodeId,
            target.FileNodeId,
            CodeEdgeKinds.ResolvesTo,
            new CodeEvidence(EvidenceQuality.Resolved, Provider, ProviderVersion, location),
            properties);
    }

    private static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private enum OutcomeKind
    {
        Resolved,
        Unresolved,
        External,
        Unsupported
    }

    private sealed record ResolutionOutcome(
        OutcomeKind Kind,
        TreeSitterModuleFile? Target = null,
        string? Rule = null)
    {
        internal static ResolutionOutcome Unresolved { get; } = new(OutcomeKind.Unresolved);
        internal static ResolutionOutcome External { get; } = new(OutcomeKind.External);
        internal static ResolutionOutcome Unsupported { get; } = new(OutcomeKind.Unsupported);
        internal static ResolutionOutcome Resolved(TreeSitterModuleFile target, string rule) =>
            new(OutcomeKind.Resolved, target, rule);
    }
}
