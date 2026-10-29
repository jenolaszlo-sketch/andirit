using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using static Andirit.InterfaceImpactResultV1;

namespace Andirit;

/// <summary>Deterministic joins over one published inventory; no source reads or stored binding truth.</summary>
internal sealed class InterfaceImpactQueryV1
{
    private sealed record Fact(CodeGraphEdge Edge, CodeFactOrigin Origin, JsonElement Data)
    {
        internal string? Text(string key) => Data.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        internal bool Flag(string key) => Data.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.True;
        internal string[] Names(string key) => Data.TryGetProperty(key, out var value) ? value.EnumerateArray().Select(x => x.GetString()!).ToArray() : [];
    }
    private sealed class LimitReached : Exception { }
    private readonly CodeGraphPublication _publication;
    private readonly InterfaceImpactOptionsV1 _limits;
    private readonly CancellationToken _cancellation;
    private readonly Dictionary<string, CodeGraphNode> _nodes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (CodeGraphDeclaration Value, CodeFactOrigin Origin)> _declarations = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Fact> _edges = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<Fact>> _byKind = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CodeGraphNode> _files = new(StringComparer.Ordinal);
    private readonly List<Implementation> _confirmed = [];
    private readonly List<Consumer> _consumers = [];
    private readonly List<Unknown> _unknown = [];
    private readonly Dictionary<string, Fact> _resolutions = new(StringComparer.Ordinal);
    private Fact[] _imports = [], _contexts = [], _exports = [], _attributions = [], _declarationContexts = [];
    private readonly HashSet<string> _consumed = new(StringComparer.Ordinal);
    private readonly HashSet<string> _associatedNames = new(StringComparer.Ordinal);
    private readonly int[] _charged = new int[5];
    private string? _stage;
    private int _omitted;
    private int _evaluatedImports, _heritageEvaluated, _unavailable, _external, _unresolved, _exportUnresolved, _exportUnsupported;
    private string? _targetFile;
    private CodeDeclarationId? _targetRecord;
    private readonly CodeGraphNode _target;

    private InterfaceImpactQueryV1(CodeGraphPublication publication, CodeGraphNode target, InterfaceImpactOptionsV1 limits, CancellationToken cancellation)
    { _publication = publication; _target = target; _limits = limits; _cancellation = cancellation; }

    internal static InterfaceImpactResultV1 Execute(CodeGraphPublication publication, CodeGraphNode target,
        IReadOnlyList<CodeIndexUnitReplacement> units, InterfaceImpactOptionsV1 limits, CancellationToken cancellation)
    {
        var query = new InterfaceImpactQueryV1(publication, target, limits, cancellation);
        try { query.Run(units); }
        catch (LimitReached) { }
        cancellation.ThrowIfCancellationRequested();
        return query.Result();
    }

    private void Charge(int kind, string stage, int amount = 1)
    {
        _cancellation.ThrowIfCancellationRequested();
        var maximum = kind switch { 0 => _limits.MaxCandidateScans, 1 => _limits.MaxHeritageScans, 2 => _limits.MaxResolverJoins, 3 => _limits.MaxAttributionLookups, _ => _limits.MaxWitnessSteps };
        if (amount > maximum - _charged[kind])
        { _stage ??= stage; _omitted += Math.Max(1, amount); throw new LimitReached(); }
        _charged[kind] += amount;
    }

    private void Run(IReadOnlyList<CodeIndexUnitReplacement> units)
    {
        foreach (var unit in units.OrderBy(u => u.Origin.PluginId.Value, StringComparer.Ordinal).ThenBy(u => u.Origin.IndexUnitId.Value, StringComparer.Ordinal))
        {
            var origin = unit.Origin;
            if (origin.RepositoryId != _publication.RepositoryId) Fail("Unit belongs to another repository.");
            foreach (var node in unit.Nodes.OrderBy(n => n.Id.Value, StringComparer.Ordinal))
            {
                Charge(0, "discovery");
                if (_nodes.TryGetValue(node.Id.Value, out var existing) && Canonical(existing) != Canonical(node)) Fail("Conflicting node identity.");
                _nodes[node.Id.Value] = node;
                if (node.Kind == CodeNodeKinds.File && node.QualifiedName is not null) _files[node.QualifiedName] = node;
            }
            foreach (var declaration in unit.Declarations.OrderBy(d => d.Id.Value, StringComparer.Ordinal))
            {
                Charge(0, "discovery");
                if (_declarations.TryGetValue(declaration.Id.Value, out var existing) && Canonical(existing.Value) != Canonical(declaration)) Fail("Conflicting physical declaration identity.");
                _declarations[declaration.Id.Value] = (declaration, origin);
            }
            foreach (var edge in unit.Edges.OrderBy(e => e.Id.Value, StringComparer.Ordinal))
            {
                Charge(0, "discovery");
                if (_edges.TryGetValue(edge.Id.Value, out var existing) && Canonical(existing.Edge) != Canonical(edge)) Fail("Conflicting edge identity.");
                var data = edge.Properties.TryGetValue("ec6d.data", out var p) && p is CodeTextProperty text
                    ? JsonDocument.Parse(text.Value).RootElement.Clone() : default;
                if (!_edges.ContainsKey(edge.Id.Value))
                {
                    var fact = new Fact(edge, origin, data);
                    _edges.Add(edge.Id.Value, fact);
                    if (!_byKind.TryGetValue(edge.Kind.Value, out var kind)) _byKind[edge.Kind.Value] = kind = [];
                    kind.Add(fact);
                }
            }
        }
        _imports = Kind("imports-name"); _contexts = Kind("heritage-context"); _exports = Kind("export-reference");
        _attributions = Kind("export-attribution"); _declarationContexts = Kind("declaration-context");
        Charge(0, "discovery", _declarations.Count);
        var targetDeclarations = _declarations.Values.Where(d => d.Value.SymbolNodeId == _target.Id).ToArray();
        if (targetDeclarations.Length == 0) throw new ArgumentException("Target is not a physical TypeScript interface declaration.");
        var paths = targetDeclarations.Select(d => d.Value.Location.Path).Distinct(StringComparer.Ordinal).ToArray();
        if (paths.Length != 1) Fail("Interface declaration crosses file identities.");
        _targetFile = _files.GetValueOrDefault(paths[0])?.Id.Value;
        if (_targetFile is null) Fail("Target file is absent.");
        if (!Kind("interface-profile").Any(f => f.Edge.SourceId.Value == _targetFile &&
                f.Names("providers").Order(StringComparer.Ordinal).SequenceEqual(new[] { "exports2.local-v1", "heritage1.local-v1", "imports1.local-v1", "module1.local-v1" })))
            throw new ArgumentException("Publication lacks the EC-6D provider set.");
        if (targetDeclarations.Length == 1) _targetRecord = targetDeclarations[0].Value.Id;

        foreach (var fact in _edges.Values)
        {
            Charge(0, "discovery");
            if (!_nodes.ContainsKey(fact.Edge.SourceId.Value) || !_nodes.ContainsKey(fact.Edge.TargetId.Value)) Fail("Edge endpoint is missing.");
            if (fact.Data.ValueKind == JsonValueKind.Undefined) continue;
            if (fact.Origin.PluginId.Value != "penghou.hetu.tree-sitter.typescript" || !fact.Origin.PluginVersion.EndsWith(".p10", StringComparison.Ordinal))
                Fail("EC-6D fact has mismatched unit provider/version.");
            if (fact.Edge.Kind.Value != "resolver-status")
            {
                if (fact.Origin.SourcePath != fact.Edge.Evidence.Location?.Path || string.IsNullOrWhiteSpace(fact.Origin.SourceHash)) Fail("Source fact has invalid provenance.");
                var file = _files.GetValueOrDefault(fact.Origin.SourcePath!);
                if (file is null || Property(file.Properties, CodePropertyKeys.ContentHash) != fact.Origin.SourceHash) Fail("Superseded source hash in fact provenance.");
            }
            ValidateFact(fact);
        }
        foreach (var declaration in _declarations.Values.Where(d => d.Origin.PluginId.Value == "penghou.hetu.tree-sitter.typescript"))
        {
            Charge(0, "discovery", _declarationContexts.Length);
            if (_declarationContexts.Count(c => c.Text("DeclarationRecordId") == declaration.Value.Id.Value) != 1)
                Fail("A physical declaration lacks a unique declaration-context record.");
        }
        foreach (var resolver in Kind("resolver-status"))
        {
            Charge(0, "discovery");
            var key = resolver.Text("resolverKey") ?? throw new InvalidOperationException("Resolver key missing.");
            if (!_resolutions.TryAdd(key, resolver)) Fail("Duplicate resolver key.");
        }
        var names = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal) { [_target.Name] = ["target:" + _target.Id.Value] };
        foreach (var attribution in _attributions)
        {
            Charge(3, "discovery");
            if (_targetRecord is not null && attribution.Text("status") == "attributed" && attribution.Text("declarationRecordId") == _targetRecord.Value)
                Associate(names, attribution.Text("exportedName")!, attribution.Edge.Id.Value);
        }
        var a1 = names.Keys.ToHashSet(StringComparer.Ordinal);
        var starBarrels = new HashSet<string>(StringComparer.Ordinal);
        foreach (var export in _exports)
        {
            Charge(0, "discovery");
            var resolution = Resolve(export);
            if (resolution.Text("resolutionStatus") == "unresolved") _exportUnresolved++;
            if (resolution.Text("resolutionStatus") is "unsupported" or "external") _exportUnsupported++;
            if (export.Text("form") == "export-star" && resolution.Text("resolvedTarget") == _targetFile) starBarrels.Add(export.Edge.SourceId.Value);
            if (export.Text("form") is not ("export-from" or "export-type-from")) continue;
            foreach (var clause in export.Data.GetProperty("clauses").EnumerateArray())
            {
                Charge(0, "discovery");
                if (clause.GetProperty("importedName").GetString() is { } imported && a1.Contains(imported) && clause.GetProperty("exportedName").GetString() is { } exported)
                    Associate(names, exported, export.Edge.Id.Value + ":" + Canonical(clause));
            }
        }
        var aliases = new Dictionary<string, Dictionary<string, HashSet<string>>>(StringComparer.Ordinal);
        _associatedNames.UnionWith(names.Keys);
        foreach (var import in _imports)
        {
            Charge(0, "discovery");
            var resolved = Resolve(import);
            _evaluatedImports++;
            if (resolved.Text("resolutionStatus") == "unresolved") _unresolved++;
            if (resolved.Text("resolutionStatus") == "external") _external++;
            if ((Named(import) && names.ContainsKey(import.Text("importedName") ?? "") || !Named(import) && resolved.Text("relevanceTarget") == _targetFile) && import.Text("localName") is { } local)
            {
                if (!aliases.TryGetValue(import.Edge.SourceId.Value, out var locals)) aliases[import.Edge.SourceId.Value] = locals = new(StringComparer.Ordinal);
                Associate(locals, local, import.Edge.Id.Value);
            }
            if (Named(import) && import.Edge.SourceId.Value != _targetFile && resolved.Text("resolvedTarget") == _targetFile && resolved.Text("resolutionStatus") == "resolved")
            {
                if (_consumers.Sum(c => c.Occurrences.Count) == _limits.MaxOccurrences) { _stage ??= "occurrence-output"; _omitted++; continue; }
                _consumers.Add(new(import.Edge.SourceId, PathOf(import.Edge.SourceId), [OccurrenceView(import, resolved)]));
            }
        }
        var candidates = new List<(Fact Context, string[] Origins)>();
        foreach (var context in _contexts)
        {
            Charge(1, "context-gates");
            var origins = new HashSet<string>(StringComparer.Ordinal);
            var name = context.Text("writtenForm") == "simple" ? context.Text("writtenName") : context.Text("terminalIdentifier");
            if (name is not null && names.TryGetValue(name, out var associated)) origins.UnionWith(associated.Select(s => "associated-name:" + name + ":" + s));
            if (context.Text("writtenForm") == "simple" && name is not null && aliases.TryGetValue(context.Edge.TargetId.Value, out var locals) && locals.TryGetValue(name, out var imports))
                origins.UnionWith(imports.Select(id => "import-local-name:" + name + ":" + id));
            if (origins.Count > 0) candidates.Add((context, origins.Order(StringComparer.Ordinal).ToArray()));
        }
        foreach (var candidate in candidates)
        {
            _heritageEvaluated++;
            Bind(candidate.Context, candidate.Origins);
        }
        // Legacy heritage with no context remains observable, with no name fallback for confirmation.
        foreach (var heritage in Kind("implements").Concat(Kind("inherits")).OrderBy(f => f.Edge.Id.Value, StringComparer.Ordinal))
        {
            Charge(1, "context-gates");
            Charge(0, "context-gates", _contexts.Length);
            if (_contexts.Any(c => c.Edge.SourceId == heritage.Edge.SourceId && c.Edge.Evidence.Location == heritage.Edge.Evidence.Location)) continue;
            var writtenName = _nodes[heritage.Edge.TargetId.Value].Name;
            var sourceFile = _files[heritage.Edge.Evidence.Location!.Path].Id.Value;
            if (!names.ContainsKey(writtenName) && (!aliases.TryGetValue(sourceFile, out var localNames) || !localNames.ContainsKey(writtenName))) continue;
            _unavailable++; _heritageEvaluated++;
            UnknownView(heritage, "heritage", "binding-context-unavailable", [heritage], [], ["legacy-heritage:" + heritage.Edge.Id.Value]);
        }
        foreach (var import in _imports)
        {
            Charge(0, "binding");
            if (_consumed.Contains(import.Edge.Id.Value)) continue;
            var resolution = Resolve(import);
            if (!names.ContainsKey(import.Text("importedName") ?? "") && !starBarrels.Contains(resolution.Text("resolvedTarget") ?? "")) continue;
            if (!Named(import)) continue;
            var reason = ImportRoute(import, resolution, out var proof);
            if (reason is not null) UnknownView(import, "occurrence", reason, [import, resolution, .. proof], Barriers(resolution), ["associated-import:" + import.Edge.Id.Value]);
        }
    }

    private void Bind(Fact context, string[] origins)
    {
        var name = context.Text("writtenName")!;
        var file = context.Edge.TargetId;
        Charge(0, "binding", _imports.Length);
        var imports = _imports.Where(i => i.Edge.SourceId == file && i.Text("localName") == name).ToArray();
        foreach (var import in imports) _consumed.Add(import.Edge.Id.Value);
        Charge(0, "binding", _declarationContexts.Length);
        var locals = _declarationContexts.Where(d => d.Edge.TargetId == file && d.Text("Name") == name && d.Flag("ModuleScope")).ToArray();
        var eligibleImports = imports.Where(Named).ToArray();
        var reason = context.Text("clauseSyntaxStatus") == "recovered" ? "recovered-heritage-syntax"
            : context.Flag("ownerAmbient") || context.Text("fileModuleContext") != "module" ? "ambient-or-global-context"
            : context.Text("ownerScope") == "nested" ? "nested-scope-heritage"
            : context.Text("heritageKind") != "class-implements" && _associatedNames.Contains(name) ? "heritage-kind-not-supported"
            : context.Text("writtenForm") == "qualified" ? "qualified-heritage-unresolved"
            : context.Text("writtenForm") == "expression" ? "unsupported-heritage-form"
            : context.Names("ownerTypeParameterNames").Contains(name, StringComparer.Ordinal) ? "type-parameter-shadowing"
            : imports.Any(i => i.Text("form") is "default" or "namespace" or "import-equals" or "dynamic") ? "unsupported-import-form"
            : eligibleImports.Length > 1 ? "duplicate-local-name"
            : eligibleImports.Length == 1 && locals.Length > 0 ? "conflicting-local-binding" : null;
        var proof = new List<Fact> { context };
        Fact[] barriers = [];
        string route;
        string? recordId = null;
        if (reason is null && eligibleImports.Length == 1)
        {
            route = "import";
            var import = eligibleImports[0];
            var resolution = Resolve(import);
            reason = ImportRoute(import, resolution, out var chain);
            proof.Add(import); proof.Add(resolution); proof.AddRange(chain);
            barriers = Barriers(resolution);
            if (reason is null) recordId = chain.Last().Text("declarationRecordId");
        }
        else
        {
            route = "same-file";
            if (reason is null)
            {
                if (locals.Length == 0) reason = "unresolved-heritage-name";
                else if (locals.Length > 1) reason = "ambiguous-local-declaration";
                else if (!locals[0].Flag("Eligible")) reason = "ineligible-declaration";
                else recordId = locals[0].Text("DeclarationRecordId");
            }
            if (reason is not null) proof.AddRange(locals);
        }
        if (reason is not null)
        {
            proof.AddRange(imports.Where(i => proof.All(p => p.Edge.Id != i.Edge.Id)));
            UnknownView(context, "heritage", reason, proof, barriers, origins);
            return;
        }
        if (recordId is null || !_declarations.TryGetValue(recordId, out var declaration)) Fail("Binding references a missing declaration.");
        declaration = _declarations[recordId!];
        if (declaration.Value.SymbolNodeId != _target.Id || _targetRecord?.Value != recordId) return;
        Charge(0, "binding", Kind("implements").Length);
        var heritage = Kind("implements").Where(f => f.Edge.SourceId == context.Edge.SourceId && f.Edge.Evidence.Location == context.Edge.Evidence.Location).ToArray();
        if (heritage.Length != 1) Fail("Completed binding lacks its unique stored heritage fact.");
        var steps = new List<Step> { StepView(heritage[0], "heritage"), StepView(context, "context") };
        if (route == "import")
        {
            var import = proof.Single(f => f.Edge.Kind.Value == "imports-name");
            var resolution = proof.Single(f => f.Edge.Kind.Value == "resolver-status");
            var resolverId = resolution.Text("resolverEdgeId")!;
            if (!_edges.TryGetValue(resolverId, out var resolverEdge)) Fail("Resolver witness edge is missing.");
            var attribution = proof.Last();
            var entry = _nodes[attribution.Text("entryNode")!];
            steps.Add(StepView(import, "import")); steps.Add(StepView(resolverEdge!, "resolution"));
            steps.Add(new("export-entry", entry.Id.Value, entry.Id, entry.Id, null, attribution.Origin));
            foreach (var occurrence in attribution.Data.GetProperty("exportSpans").EnumerateArray())
            {
                if (!_edges.TryGetValue(occurrence.GetProperty("occurrenceId").GetString()!, out var exportOccurrence)) Fail("Export occurrence witness is missing.");
                steps.Add(StepView(exportOccurrence!, "export-occurrence"));
            }
            steps.Add(StepView(attribution, "attribution"));
        }
        steps.Add(new("declaration", declaration.Value.Id.Value, declaration.Value.SymbolNodeId, _target.Id, null, declaration.Origin, declaration.Value.Properties, declaration.Value.Location));
        Charge(4, "binding", steps.Count);
        var tuple = steps.Select(s => s.FactId).Append(_target.Id.Value).Append("binding1.local-v1").ToArray();
        var witness = new Witness("binding1.local-v1:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(tuple)))), steps.ToArray());
        if (_confirmed.Select(c => (c.ClassNodeId, c.TargetDeclarationRecordId)).Distinct().Count() == _limits.MaxConfirmed && !_confirmed.Any(c => c.ClassNodeId == context.Edge.SourceId))
        { _stage ??= "confirmed-output"; _omitted++; return; }
        var owner = _nodes[context.Edge.SourceId.Value];
        _confirmed.Add(new(owner.Id, owner.Name, context.Edge.Evidence.Location!.Path, context.Edge.Evidence.Location, route,
            declaration.Value.Id, _target.Id, [witness], origins));
    }

    private string? ImportRoute(Fact import, Fact resolution, out Fact[] proof)
    {
        proof = [];
        var status = resolution.Text("resolutionStatus");
        if (status != "resolved") return status switch { "unresolved" => "unresolved-module", "external" => "external-module", "unsupported" => "unsupported-import-form", _ => throw new InvalidOperationException("Invalid resolution status.") };
        var targetFile = resolution.Text("resolvedTarget")!;
        var exportFacts = Kind("exports");
        Charge(0, "binding", exportFacts.Length);
        var exports = exportFacts.Where(f => f.Edge.SourceId.Value == targetFile && _nodes[f.Edge.TargetId.Value].Name == import.Text("importedName")).ToArray();
        if (exports.Length == 0) return Barriers(resolution).Length == 0 ? "missing-export" : "re-export-barrier";
        var entries = exports.Select(e => e.Edge.TargetId).Distinct().ToArray();
        if (entries.Length != 1) Fail("Export slot has contradictory identities.");
        Charge(3, "binding");
        Charge(0, "binding", _attributions.Length);
        var attributions = _attributions.Where(a => a.Edge.SourceId == entries[0]).ToArray();
        if (attributions.Length == 0) return "unattributed-export";
        if (attributions.Length != 1) Fail("Export attribution is contradictory.");
        var attribution = attributions[0];
        proof = [attribution];
        return attribution.Text("status") switch
        {
            "missing-declaration" => "unattributed-export",
            "ambiguous-declarations" => "ambiguous-export",
            "ineligible-declaration-context" => "ineligible-declaration",
            "attributed" => null,
            _ => throw new InvalidOperationException("Invalid attribution status.")
        };
    }

    private Fact Resolve(Fact occurrence)
    {
        Charge(2, "binding");
        if (!_resolutions.TryGetValue(occurrence.Text("resolverKey")!, out var resolution)) throw new InvalidOperationException("Required resolver counterpart is missing.");
        if (resolution.Edge.SourceId != occurrence.Edge.SourceId || resolution.Edge.Evidence.Location != occurrence.Edge.Evidence.Location ||
            resolution.Text("moduleSpecifier") != occurrence.Text("moduleSpecifier") || resolution.Text("referenceForm") != occurrence.Text("referenceForm")) Fail("Contradictory resolver counterpart.");
        if (resolution.Text("resolutionStatus") == "resolved")
        {
            if (resolution.Text("resolvedTarget") != resolution.Edge.TargetId.Value || !_nodes.TryGetValue(resolution.Edge.TargetId.Value, out var file) || file.Kind != CodeNodeKinds.File) Fail("Invalid resolved module endpoint.");
            if (!_edges.TryGetValue(resolution.Text("resolverEdgeId")!, out var edge) || edge.Edge.Kind != CodeEdgeKinds.ResolvesTo || edge.Edge.SourceId != occurrence.Edge.SourceId || edge.Edge.TargetId != resolution.Edge.TargetId || edge.Edge.Evidence.Location != occurrence.Edge.Evidence.Location)
                Fail("Missing or contradictory resolver fact.");
        }
        else if (resolution.Text("resolvedTarget") is not null || resolution.Edge.SourceId != resolution.Edge.TargetId) Fail("Non-resolved occurrence claims a target.");
        return resolution;
    }

    private void ValidateFact(Fact fact)
    {
        var edge = fact.Edge;
        var payload = ((CodeTextProperty)edge.Properties["ec6d.data"]).Value;
        if (edge.Kind.Value != "resolver-status")
        {
            var material = $"{edge.Kind.Value}\n{edge.SourceId.Value}\n{edge.TargetId.Value}\n{payload}";
            var expectedId = "typescript:ec6d:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
            if (edge.Id.Value != expectedId) Fail("Occurrence identity does not match its stored content.");
        }
        else if (edge.Id.Value != "typescript:resolver-status:" + fact.Text("resolverKey")) Fail("Resolver occurrence identity is contradictory.");
        var provider = "penghou.hetu.tree-sitter.ecmascript-";
        var expected = edge.Kind.Value switch
        {
            "imports-name" => (provider + "import-occurrences", "imports1.local-v1"),
            "heritage-context" or "declaration-context" => (provider + "heritage-context", "heritage1.local-v1"),
            "export-reference" => (provider + "module-occurrences", "module1.local-v1"),
            "export-attribution" => (provider + "export-attribution", "exports2.local-v1"),
            "interface-profile" => (provider + "profile", "imports1.local-v1"),
            "resolver-status" => (provider + "module-resolver", edge.Evidence.ProviderVersion!),
            _ => throw new InvalidOperationException("Unrecognized EC-6D fact vocabulary.")
        };
        if (edge.Evidence.Provider != expected.Item1 || edge.Evidence.ProviderVersion != expected.Item2) Fail("Mismatched fact provider/version.");
        if (edge.Kind.Value is "imports-name" or "export-reference" or "resolver-status")
        {
            if (fact.Text("sourceFile") != edge.SourceId.Value || _nodes[edge.SourceId.Value].Kind != CodeNodeKinds.File) Fail("Occurrence source identity is contradictory.");
        }
        if (edge.Kind.Value == "heritage-context" && (fact.Text("ownerNode") != edge.SourceId.Value || fact.Text("sourceFile") != edge.TargetId.Value))
            Fail("Heritage context endpoints are contradictory.");
        if (edge.Kind.Value == "declaration-context")
        {
            if (!_declarations.TryGetValue(fact.Text("DeclarationRecordId")!, out var declaration) || declaration.Value.SymbolNodeId != edge.SourceId ||
                declaration.Value.Location != edge.Evidence.Location || declaration.Origin.SourceHash != fact.Origin.SourceHash ||
                fact.Text("Name") != _nodes[edge.SourceId.Value].Name || fact.Flag("Eligible") != (fact.Flag("ModuleScope") && !fact.Flag("Ambient") && !fact.Flag("Recovered")))
                Fail("Declaration context does not prove its claimed physical record.");
        }
        if (edge.Kind.Value != "export-attribution") return;
        if (fact.Text("entryNode") != edge.SourceId.Value || _nodes[edge.SourceId.Value].Kind != CodeNodeKinds.Export || fact.Text("exportedName") != _nodes[edge.SourceId.Value].Name)
            Fail("Attribution export entry is contradictory.");
        var candidates = fact.Names("candidateDeclarationRecordIds");
        var ineligible = fact.Names("ineligibleDeclarationRecordIds");
        if (candidates.Distinct(StringComparer.Ordinal).Count() != candidates.Length || ineligible.Intersect(candidates, StringComparer.Ordinal).Any() ||
            fact.Data.GetProperty("eligiblePhysicalDeclarationCount").GetInt32() != candidates.Length)
            Fail("Attribution physical cardinality is contradictory.");
        foreach (var id in candidates.Concat(ineligible))
        {
            Charge(0, "discovery", _declarationContexts.Length);
            var contexts = _declarationContexts.Where(c => c.Text("DeclarationRecordId") == id && c.Edge.TargetId == edge.TargetId && c.Flag("ModuleScope")).ToArray();
            if (contexts.Length != 1 || contexts[0].Flag("Eligible") != candidates.Contains(id, StringComparer.Ordinal)) Fail("Attribution candidate context is missing or contradictory.");
        }
        var status = fact.Text("status");
        if (status == "attributed")
        {
            if (candidates.Length != 1 || ineligible.Length != 0 || fact.Text("declarationRecordId") != candidates[0] ||
                !_declarations.TryGetValue(candidates[0], out var declaration) || declaration.Value.SymbolNodeId.Value != fact.Text("declarationNodeId") ||
                declaration.Value.SymbolId.Value != fact.Text("symbolId") || _nodes[declaration.Value.SymbolNodeId.Value].Name != fact.Text("localName"))
                Fail("Unique attribution is not supported by its physical record.");
        }
        else if (status is not ("missing-declaration" or "ambiguous-declarations" or "ineligible-declaration-context") ||
            fact.Text("declarationRecordId") is not null || fact.Text("declarationNodeId") is not null || fact.Text("symbolId") is not null)
            Fail("Non-attributed export claims a declaration identity.");
        Charge(0, "discovery", Kind("exports").Length);
        var actual = Kind("exports").Where(e => e.Edge.TargetId == edge.SourceId && e.Edge.SourceId == edge.TargetId).Select(e => e.Edge.Id.Value).Order(StringComparer.Ordinal).ToArray();
        var claimed = fact.Data.GetProperty("exportSpans").EnumerateArray().Select(s => s.GetProperty("occurrenceId").GetString()!).Order(StringComparer.Ordinal).ToArray();
        if (!actual.SequenceEqual(claimed, StringComparer.Ordinal)) Fail("Attribution export occurrence evidence is missing or contradictory.");
    }

    private Fact[] Barriers(Fact resolution)
    {
        Charge(0, "binding", _exports.Length);
        return _exports.Where(e => e.Edge.SourceId.Value == resolution.Text("resolvedTarget") && e.Text("form") is "export-from" or "export-type-from" or "export-star" or "export-namespace-from").ToArray();
    }
    private void UnknownView(Fact subject, string kind, string reason, IEnumerable<Fact> evidence, Fact[] barriers, string[] origins)
    {
        if (_unknown.Count == _limits.MaxUnknowns) { _stage ??= "unknown-output"; _omitted++; return; }
        var facts = evidence.DistinctBy(f => f.Edge.Id).ToArray();
        Charge(4, "binding", facts.Length + barriers.Length);
        var sourceFile = kind == "heritage" ? _files[subject.Edge.Evidence.Location!.Path].Id : subject.Edge.SourceId;
        _unknown.Add(new(kind, sourceFile,
            kind == "heritage" ? subject.Edge.SourceId : null, subject.Edge.Id.Value, subject.Edge.Evidence.Location!, reason,
            facts.Select(f => StepView(f, f.Edge.Kind.Value)).ToArray(), barriers.Select(f => StepView(f, "barrier")).ToArray(), origins));
    }

    private InterfaceImpactResultV1 Result()
    {
        var implementations = _confirmed.GroupBy(c => (c.ClassNodeId, c.TargetDeclarationRecordId)).Select(g => g.First() with
        {
            Witnesses = g.SelectMany(c => c.Witnesses).DistinctBy(w => w.WitnessId).OrderBy(w => w.WitnessId, StringComparer.Ordinal).ToArray(),
            DiscoveryOrigins = g.SelectMany(c => c.DiscoveryOrigins).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray()
        }).OrderBy(c => c.Path, StringComparer.Ordinal).ThenBy(c => c.ClassName, StringComparer.Ordinal).ThenBy(c => c.ClassNodeId.Value, StringComparer.Ordinal).ThenBy(c => c.TargetDeclarationRecordId.Value, StringComparer.Ordinal).ToArray();
        var consumers = _consumers.GroupBy(c => c.FileNodeId).Select(g => new Consumer(g.Key, g.First().Path, g.SelectMany(c => c.Occurrences)
            .DistinctBy(o => o.OccurrenceId).OrderBy(o => o.StatementSpan.StartLine).ThenBy(o => o.StatementSpan.StartColumn)
            .ThenBy(o => o.BindingSpan?.StartLine ?? int.MaxValue).ThenBy(o => o.BindingSpan?.StartColumn ?? int.MaxValue)
            .ThenBy(o => o.Form, StringComparer.Ordinal).ThenBy(o => o.LocalName, StringComparer.Ordinal).ThenBy(o => o.OccurrenceId, StringComparer.Ordinal).ToArray()))
            .OrderBy(c => c.Path, StringComparer.Ordinal).ThenBy(c => c.FileNodeId.Value, StringComparer.Ordinal).ToArray();
        var unknowns = _unknown.DistinctBy(u => (u.SubjectId, u.Reason)).OrderBy(u => u.SubjectKind == "heritage" ? 0 : 1)
            .ThenBy(u => u.Span.Path, StringComparer.Ordinal).ThenBy(u => u.ClassNodeId?.Value ?? u.SubjectId, StringComparer.Ordinal)
            .ThenBy(u => u.Span.StartLine).ThenBy(u => u.Span.StartColumn).ThenBy(u => u.Reason, StringComparer.Ordinal).ThenBy(u => u.SubjectId, StringComparer.Ordinal).ToArray();
        var targetDeclaration = _targetRecord is not null && _declarations.TryGetValue(_targetRecord.Value, out var d) ? d.Value : null;
        var qualification = _publication.IsConsistentSnapshot && !string.IsNullOrWhiteSpace(_publication.SnapshotIdentity) ? "consistent" : "qualified";
        var coverage = new CoverageInfo("ec6d.typescript.local-v1", qualification, _stage is not null, _evaluatedImports,
            _imports.Count(i => !Named(i)), _external, _unresolved,
            _contexts.Length + _unavailable, _heritageEvaluated,
            _contexts.Count(c => c.Text("ownerScope") == "nested"), _contexts.Count(c => c.Text("writtenForm") == "qualified"),
            _contexts.Count(c => c.Text("writtenForm") == "expression"), _contexts.Count(c => c.Text("heritageKind") != "class-implements"), _unavailable,
            _exports.Length, _exportUnresolved, _exportUnsupported, implementations.Length, consumers.Length,
            new SortedDictionary<string, int>(unknowns.GroupBy(u => u.Reason).ToDictionary(g => g.Key, g => g.Count()), StringComparer.Ordinal),
            ["renamed/multi-hop re-export paths beyond one hop", "CommonJS/require", "bare packages", "ordinary type-use references and call sites", "implicit structural implementations", "transitive impact", "default/namespace binding forms", "other languages including .astro and cross-plugin symbols"]);
        return new(new(_publication.RepositoryId, _publication.IndexRunId, _publication.IndexIdentity, _publication.SnapshotIdentity, _publication.IsConsistentSnapshot), new(_target.Id, _targetRecord, _target.Name, _target.QualifiedName,
            targetDeclaration?.Location.Path ?? (_targetFile is null ? null : PathOf(new CodeNodeId(_targetFile))), targetDeclaration?.Location),
            implementations, consumers, unknowns, coverage,
            new(_stage is null ? "complete" : "truncated", _stage, new(_charged[0], _charged[1], _charged[2], _charged[3], _charged[4]), _limits, _omitted, _stage is not null));
    }

    private Occurrence OccurrenceView(Fact import, Fact resolution) => new(import.Edge.Id.Value, import.Text("moduleSpecifier"), import.Text("importedName"), import.Text("localName"),
        import.Flag("isTypeOnly"), import.Text("form")!, import.Edge.Evidence.Location!, Span(import, "moduleSpecifierSpan"), Span(import, "bindingSpan"), resolution.Text("resolutionRule")!, import.Origin);
    private static CodeLocation? Span(Fact fact, string key) => fact.Data.TryGetProperty(key, out var value) && value.ValueKind != JsonValueKind.Null ? value.Deserialize<CodeLocation>() : null;
    private Fact[] Kind(string kind) => (_byKind.GetValueOrDefault(kind) ?? []).OrderBy(f => f.Edge.Evidence.Location?.Path, StringComparer.Ordinal)
        .ThenBy(f => f.Edge.Evidence.Location?.StartLine).ThenBy(f => f.Edge.Evidence.Location?.StartColumn).ThenBy(f => f.Edge.Id.Value, StringComparer.Ordinal).ToArray();
    private static bool Named(Fact import) => import.Text("form") is "named" or "named-aliased";
    private string PathOf(CodeNodeId file) => _nodes.TryGetValue(file.Value, out var node) ? node.QualifiedName ?? node.Name : throw new InvalidOperationException("Missing source file.");
    private static string? Property(IReadOnlyDictionary<string, CodePropertyValue> p, string key) => p.TryGetValue(key, out var value) && value is CodeTextProperty text ? text.Value : null;
    private static Step StepView(Fact fact, string kind) => new(kind, fact.Edge.Id.Value, fact.Edge.SourceId, fact.Edge.TargetId, fact.Edge.Evidence, fact.Origin, fact.Edge.Properties, fact.Edge.Evidence.Location);
    private static string Canonical<T>(T value) => JsonSerializer.Serialize(value, CodeJsonDefaults.Options);
    private static void Associate(Dictionary<string, HashSet<string>> names, string name, string origin)
    { if (!names.TryGetValue(name, out var origins)) names[name] = origins = new(StringComparer.Ordinal); origins.Add(origin); }
    private static void Fail(string message) => throw new InvalidOperationException("EC-6D integrity failure: " + message);
}
