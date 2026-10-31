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
    private sealed class GraphBuilder(
        CodeGraphPluginContext context,
        CSharpCodeGraphPlugin plugin,
        CSharpProjectModel project,
        CSharpCompilation compilation,
        IReadOnlyDictionary<string, CSharpCompilation> availableCompilations,
        RunSymbols runSymbols)
    {
        private readonly SortedDictionary<string, CodeGraphNode> _nodes =
            new(StringComparer.Ordinal);
        private readonly SortedDictionary<string, CodeGraphDeclaration> _declarations =
            new(StringComparer.Ordinal);
        private readonly SortedDictionary<string, CodeGraphEdge> _edges =
            new(StringComparer.Ordinal);
        private readonly HashSet<string> _contributingSources = new(StringComparer.Ordinal);
        private readonly Dictionary<string, CodeNodeId> _fileNodes = new(StringComparer.Ordinal);
        private readonly List<(string Path, SyntaxNode Root, SemanticModel Model)> _syntax = [];
        private readonly Dictionary<string, RelationshipCounters> _counters =
            new(StringComparer.Ordinal);
        private readonly SortedDictionary<string, SortedDictionary<string, SortedSet<string>>> _documentation =
            new(StringComparer.Ordinal);

        private const int MaxMergedDocumentationLength = 2048;
        private static readonly string[] MergedDocumentationKeys =
            [CodePropertyKeys.DocSummary, CodePropertyKeys.DocRemarks];

        private static readonly HashSet<string> TestPackageReferences = new(
            [
                "Microsoft.NET.Test.Sdk",
                "Microsoft.Testing.Platform",
                "xunit",
                "xunit.v3",
                "xunit.runner.visualstudio",
                "NUnit",
                "NUnit3TestAdapter",
                "MSTest.TestFramework",
                "MSTest.TestAdapter",
                "TUnit"
            ],
            StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, RelationshipCounters> Counters => _counters;

        public IReadOnlyCollection<string> ContributingSourcePaths => _contributingSources;

        public void AddProject()
        {
            var id = ProjectNodeId(project.Path);
            var properties = new Dictionary<string, CodePropertyValue>
            {
                [CodePropertyKeys.Language] = new CodeTextProperty("csharp"),
                [CodePropertyKeys.AssemblyName] = new CodeTextProperty(project.AssemblyName),
                [CodePropertyKeys.TargetFramework] = new CodeTextProperty(project.TargetFramework ?? string.Empty),
                [CodePropertyKeys.Nullable] = new CodeTextProperty(project.Nullable ?? string.Empty),
                [CodePropertyKeys.ImplicitUsings] = new CodeBooleanProperty(project.ImplicitUsings),
                [CodePropertyKeys.DefineConstants] = new CodeTextListProperty(project.DefineConstants)
            };
            // Test projects are an exact package-reference allowlist. A name
            // convention or near-miss package is never treated as a test
            // project, mirroring the test-method attribute allowlist.
            if (project.PackageReferences.Any(reference =>
                    TestPackageReferences.Contains(reference.Name)))
            {
                properties[CodePropertyKeys.TestProject] = new CodeBooleanProperty(true);
            }
            _nodes.Add(
                id.Value,
                new CodeGraphNode(
                    id,
                    CodeNodeKinds.Project,
                    project.Name,
                    project.Path,
                    properties: properties));
            foreach (var reference in project.ProjectReferences.Where(
                         availableCompilations.ContainsKey))
            {
                AddEdge(
                    CodeEdgeKinds.DependsOn,
                    id,
                    ProjectNodeId(reference),
                    ProjectLocation());
            }
            // Package references are syntax facts from the project file: no
            // MSBuild evaluation, no transitive closure, no version guessing.
            foreach (var package in project.PackageReferences)
            {
                var packageId = NodeId("package", $"nuget:{package.Name}\n{package.Version}");
                _nodes.TryAdd(
                    packageId.Value,
                    new CodeGraphNode(
                        packageId,
                        CodeNodeKinds.Package,
                        package.Name,
                        $"nuget:{package.Name}",
                        properties: new Dictionary<string, CodePropertyValue>
                        {
                            [CodePropertyKeys.PackageVersion] = new CodeTextProperty(package.Version ?? string.Empty),
                            [CodePropertyKeys.PackageCondition] = new CodeTextProperty(package.Condition ?? string.Empty)
                        }));
                AddEdge(
                    CodeEdgeKinds.DependsOn,
                    id,
                    packageId,
                    ProjectLocation(),
                    discriminator: $"package:{package.Name}",
                    evidenceQuality: EvidenceQuality.Structural);
            }
        }

        public void AddFile(CodeGraphSource source)
        {
            var id = NodeId("file", source.Path);
            _fileNodes.Add(source.Path, id);
            _nodes.Add(
                id.Value,
                new CodeGraphNode(
                    id,
                    CodeNodeKinds.File,
                    System.IO.Path.GetFileName(source.Path),
                    source.Path,
                    properties: new Dictionary<string, CodePropertyValue>
                    {
                        [CodePropertyKeys.Language] = new CodeTextProperty("csharp"),
                        [CodePropertyKeys.ContentHash] = new CodeTextProperty(source.ContentHash)
                    }));
            AddEdge(
                CodeEdgeKinds.Contains,
                ProjectNodeId(project.Path),
                id,
                new CodeLocation(source.Path, 1, 1, 1, 1),
                evidenceQuality: EvidenceQuality.Structural);
        }

        public void AddDeclarations(
            string path,
            SyntaxNode root,
            SemanticModel model,
            CancellationToken cancellationToken)
        {
            _syntax.Add((path, root, model));
            foreach (var syntax in root.DescendantNodes(descendIntoTrivia: false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var symbol = GetSupportedSymbol(syntax, model, cancellationToken);
                if (symbol is null)
                    continue;
                var kind = GetNodeKind(symbol);
                if (kind is null)
                    continue;
                AddSymbol(path, syntax, symbol, kind);
                _contributingSources.Add(path);
            }
        }

        /// <summary>
        /// Second pass over the completed declaration set: resolves semantic
        /// relationships now that every node of the project exists. Targets
        /// resolve against this project first, then any project processed
        /// earlier in dependency order; externally-owned targets are counted
        /// as external rather than emitted or guessed, and genuinely
        /// unresolved targets are counted per relationship kind instead of
        /// producing guessed edges.
        /// </summary>
        public void AddRelationships(CancellationToken cancellationToken)
        {
            foreach (var (path, root, model) in _syntax)
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var syntax in root.DescendantNodes(descendIntoTrivia: false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    switch (syntax)
                    {
                        case TypeDeclarationSyntax typeDeclaration:
                            HandleBaseTypes(path, typeDeclaration, model);
                            break;
                        case InvocationExpressionSyntax invocation:
                            HandleInvocation(path, invocation, model);
                            break;
                        case ObjectCreationExpressionSyntax creation:
                            HandleCreation(path, creation, model);
                            break;
                        case ImplicitObjectCreationExpressionSyntax creation:
                            HandleImplicitCreation(path, creation, model);
                            break;
                        case ConstructorInitializerSyntax initializer:
                            HandleConstructorInitializer(path, initializer, model);
                            break;
                        case UsingDirectiveSyntax usingDirective:
                            HandleUsingDirective(path, usingDirective, model);
                            break;
                        case AttributeSyntax attribute:
                            HandleAttributeReference(path, attribute, model);
                            break;
                        case SimpleNameSyntax name when
                            name.FirstAncestorOrSelf<UsingDirectiveSyntax>() is null:
                            HandleReference(path, name, model);
                            break;
                        case PredefinedTypeSyntax predefined:
                            HandleReference(path, predefined, model);
                            break;
                    }
                }
            }

            AddExercisedByRelationships();
        }

        /// <summary>
        /// Derives test-to-production mapping from already-resolved semantic
        /// calls: every production target called by an allowlisted test method
        /// gains one bounded <c>exercised-by</c> edge back to that test. Only
        /// resolved calls are mapped; external or ambiguous callees never
        /// produce an edge.
        /// </summary>
        private void AddExercisedByRelationships()
        {
            var exercised = _edges.Values
                .Where(edge =>
                    edge.Kind == CodeEdgeKinds.Calls &&
                    IsTestMethodNode(edge.SourceId))
                .Select(edge => (
                    Production: edge.TargetId,
                    Test: edge.SourceId,
                    Location: edge.Evidence.Location))
                .ToArray();
            foreach (var (production, test, location) in exercised)
            {
                AddEdge(
                    CodeEdgeKinds.ExercisedBy,
                    production,
                    test,
                    location ?? ProjectLocation(),
                    evidenceQuality: EvidenceQuality.Derived);
                CountEmitted(CodeEdgeKinds.ExercisedBy.Value, test);
            }
        }

        private bool IsTestMethodNode(CodeNodeId id) =>
            _nodes.TryGetValue(id.Value, out var node) &&
            node.Properties.TryGetValue(CodePropertyKeys.TestMethod, out var value) &&
            value is CodeBooleanProperty { Value: true };

        public async ValueTask WriteAsync(
            ICodeGraphSink sink,
            CancellationToken cancellationToken)
        {
            ApplyMergedDocumentation();
            var origin = new CodeFactOrigin(
                context.RepositoryId,
                plugin.Id,
                plugin.Version,
                context.IndexRunId,
                new CodeIndexUnitId(CSharpProjectDiscovery.IndexUnitId(project.Path)));
            await WriteChunksAsync(
                _nodes.Values,
                sink.Limits.MaxNodes,
                values => new CodeGraphBatch(origin, nodes: values),
                sink,
                cancellationToken).ConfigureAwait(false);
            await WriteChunksAsync(
                _declarations.Values,
                sink.Limits.MaxDeclarations,
                values => new CodeGraphBatch(origin, declarations: values),
                sink,
                cancellationToken).ConfigureAwait(false);
            await WriteChunksAsync(
                _edges.Values,
                sink.Limits.MaxEdges,
                values => new CodeGraphBatch(origin, edges: values),
                sink,
                cancellationToken).ConfigureAwait(false);
            await sink.WriteBatchAsync(
                new CodeGraphBatch(origin, completesIndexUnit: true),
                cancellationToken).ConfigureAwait(false);
        }

        private void AddSymbol(
            string path,
            SyntaxNode syntax,
            ISymbol symbol,
            CodeNodeKind kind)
        {
            var canonical = ScopedSymbolKey(symbol);
            var symbolId = new CodeSymbolId($"csharp:{Hash(canonical)}");
            var nodeId = NodeId("symbol", canonical);
            var qualifiedName = QualifiedName(symbol);
            var location = Location(path, syntax);

            // Register in the run-wide map before constructing properties so
            // later projects can resolve this symbol as a relationship target.
            // The canonical key is a lookup aid; the registration also carries
            // this project's provenance, definition fingerprint and declaration
            // paths so a target can never be confirmed from the key alone.
            runSymbols.Register(
                CanonicalSymbolKey(symbol),
                project.Path,
                nodeId,
                DefinitionFingerprint(symbol),
                path);

            var properties = CSharpSymbolPropertyFactory.BuildProperties(symbol, syntax);
            var declarationDocumentation = ExtractDeclarationDocumentation(properties);
            var node = new CodeGraphNode(
                nodeId,
                kind,
                DisplayName(symbol),
                qualifiedName,
                symbolId,
                properties);
            if (_nodes.TryGetValue(nodeId.Value, out var existing) && !NodesEquivalent(existing, node))
            {
                var differingKeys = existing.Properties.Keys
                    .Union(node.Properties.Keys)
                    .Where(key => !Equals(
                        existing.Properties.TryGetValue(key, out var firstValue) ? firstValue : null,
                        node.Properties.TryGetValue(key, out var secondValue) ? secondValue : null))
                    .Order(StringComparer.Ordinal)
                    .ToArray();
                var differingValues = string.Join(", ", differingKeys.Select(key =>
                    $"{key}=[existing:{FormatProperty(existing.Properties, key)}|new:{FormatProperty(node.Properties, key)}]"));
                throw new InvalidOperationException(
                    $"Roslyn produced inconsistent facts for symbol '{canonical}'; " +
                    $"differing: [{differingValues}].");
            }
            _nodes[nodeId.Value] = node;
            AccumulateDocumentation(nodeId, declarationDocumentation);

            var declarationId = new CodeDeclarationId(
                $"csharp:{Hash($"{canonical}\n{path}\n{location.StartLine}:{location.StartColumn}:{location.EndLine}:{location.EndColumn}")}");
            _declarations[declarationId.Value] = new(
                declarationId,
                symbolId,
                nodeId,
                location);
            AddEdge(
                CodeEdgeKinds.Declares,
                _fileNodes[path],
                nodeId,
                location,
                declarationId.Value,
                EvidenceQuality.Structural);

            var containing = symbol.ContainingSymbol;
            if (containing is not null &&
                containing is not IAssemblySymbol and not IModuleSymbol &&
                containing is not INamespaceSymbol { IsGlobalNamespace: true })
            {
                var containingId = NodeId("symbol", ScopedSymbolKey(containing));
                if (_nodes.ContainsKey(containingId.Value))
                    AddEdge(
                        CodeEdgeKinds.Contains,
                        containingId,
                        nodeId,
                        location,
                        evidenceQuality: EvidenceQuality.Structural);
            }

        }


        private static Dictionary<string, string> ExtractDeclarationDocumentation(
            Dictionary<string, CodePropertyValue> properties)
        {
            var documentation = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var key in MergedDocumentationKeys)
            {
                if (!properties.TryGetValue(key, out var value))
                    continue;
                if (value is not CodeTextProperty text)
                    continue;

                documentation[key] = text.Value;
                properties.Remove(key);
            }

            return documentation;
        }

        /// <summary>
        /// Documentation is declaration-specific. Each physical partial part
        /// contributes its value to a set; the logical node receives the
        /// ordinally sorted distinct values joined by a blank line and bounded
        /// to <see cref="MaxMergedDocumentationLength"/> characters. This is
        /// independent of source enumeration order and never drops one part in
        /// favour of whichever declaration happened to be visited last.
        /// </summary>
        private void AccumulateDocumentation(
            CodeNodeId nodeId,
            IReadOnlyDictionary<string, string> documentation)
        {
            if (documentation.Count == 0)
                return;

            if (!_documentation.TryGetValue(nodeId.Value, out var byKey))
            {
                byKey = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
                _documentation[nodeId.Value] = byKey;
            }

            foreach (var (key, value) in documentation)
            {
                if (!byKey.TryGetValue(key, out var values))
                {
                    values = new SortedSet<string>(StringComparer.Ordinal);
                    byKey[key] = values;
                }

                values.Add(value);
            }
        }

        private void ApplyMergedDocumentation()
        {
            foreach (var (nodeKey, byKey) in _documentation)
            {
                if (!_nodes.TryGetValue(nodeKey, out var node))
                    continue;

                var properties = new SortedDictionary<string, CodePropertyValue>(
                    StringComparer.Ordinal);
                foreach (var (key, value) in node.Properties)
                    properties[key] = value;
                foreach (var (key, values) in byKey)
                {
                    var merged = string.Join("\n\n", values);
                    if (merged.Length > MaxMergedDocumentationLength)
                        merged = merged[..MaxMergedDocumentationLength];
                    properties[key] = new CodeTextProperty(merged);
                }

                _nodes[nodeKey] = new CodeGraphNode(
                    node.Id,
                    node.Kind,
                    node.Name,
                    node.QualifiedName,
                    node.SymbolId,
                    properties);
            }
        }

        private static string FormatProperty(
            IReadOnlyDictionary<string, CodePropertyValue> properties,
            string key)
        {
            if (!properties.TryGetValue(key, out var value))
                return "none";

            return value switch
            {
                CodeTextProperty text => text.Value,
                CodeTextListProperty list => string.Join("/", list.Values),
                CodeIntegerProperty integer => integer.Value.ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
                CodeNumberProperty number => number.Value.ToString(
                    "R",
                    System.Globalization.CultureInfo.InvariantCulture),
                CodeBooleanProperty boolean => boolean.Value ? "true" : "false",
                _ => value.GetType().Name
            };
        }

        private string ScopedSymbolKey(ISymbol symbol) =>
            $"{project.Path}\n{CanonicalSymbolKey(symbol)}";

        private CodeLocation ProjectLocation() => new(
            project.Path == "@loose/csharp" ? "@loose/csharp" : project.Path,
            1,
            1,
            1,
            1);

        /// <summary>
        /// Compares logical-symbol invariants. Declaration-specific
        /// documentation is removed before this comparison and merged by
        /// <see cref="AccumulateDocumentation"/>; every remaining property must
        /// agree, so a genuine invariant conflict still fails extraction.
        /// </summary>
        private static bool NodesEquivalent(CodeGraphNode first, CodeGraphNode second) =>
            first.Id == second.Id &&
            first.Kind == second.Kind &&
            first.Name == second.Name &&
            first.QualifiedName == second.QualifiedName &&
            first.SymbolId == second.SymbolId &&
            first.Properties.Count == second.Properties.Count &&
            first.Properties.All(pair =>
                second.Properties.TryGetValue(pair.Key, out var value) && value == pair.Value);

        private void AddEdge(
            CodeEdgeKind kind,
            CodeNodeId source,
            CodeNodeId target,
            CodeLocation location,
            string? discriminator = null,
            EvidenceQuality evidenceQuality = EvidenceQuality.Resolved,
            IReadOnlyDictionary<string, CodePropertyValue>? properties = null)
        {
            var id = new CodeEdgeId(
                $"csharp:{Hash($"{kind.Value}\n{source.Value}\n{target.Value}\n{discriminator}")}");
            _edges.TryAdd(id.Value, new(
                id,
                source,
                target,
                kind,
                new CodeEvidence(
                    evidenceQuality,
                    plugin.Id.Value,
                    plugin.Version,
                    location),
                properties));
        }

        private void CountEmitted(string relationshipKind, CodeNodeId targetId)
        {
            var counters = GetCounters(relationshipKind);
            counters.Candidates++;
            counters.EdgesEmitted++;
            if (_nodes.ContainsKey(targetId.Value))
                counters.InternalTargets++;
            else
                counters.CrossProjectTargets++;
        }

        private void CountUnresolved(string relationshipKind)
        {
            var counters = GetCounters(relationshipKind);
            counters.Candidates++;
            counters.UnresolvedTargets++;
        }

        private void CountAmbiguous(string relationshipKind)
        {
            var counters = GetCounters(relationshipKind);
            counters.Candidates++;
            counters.AmbiguousTargets++;
            counters.UnresolvedTargets++;
        }

        private void CountExternal(string relationshipKind)
        {
            var counters = GetCounters(relationshipKind);
            counters.Candidates++;
            counters.ExternalTargets++;
        }

        private void CountUnsupported(string relationshipKind)
        {
            var counters = GetCounters(relationshipKind);
            counters.Candidates++;
            counters.UnsupportedTargets++;
        }

        private RelationshipCounters GetCounters(string relationshipKind)
        {
            if (!_counters.TryGetValue(
                    relationshipKind,
                    out var counters))
            {
                counters = new RelationshipCounters();
                _counters[relationshipKind] = counters;
            }

            return counters;
        }

        private enum TargetMatch
        {
            Found,
            External,
            Ambiguous
        }

        /// <summary>
        /// Confirms a bound Roslyn target only when its defining compilation and
        /// definition correspond to an indexed declaration. The run-wide
        /// canonical map is a lookup aid; canonical documentation IDs and
        /// assembly names never establish identity on their own. A target that
        /// cannot be proven to belong to the current project or an eligible
        /// dependency is external, and a target with competing eligible
        /// declarations is ambiguous.
        /// </summary>
        private TargetMatch TryResolveTarget(
            ISymbol target,
            out CodeNodeId nodeId)
        {
            nodeId = null!;

            var canonical = CanonicalSymbolKey(target);
            var fingerprint = DefinitionFingerprint(target);
            var scopedId = NodeId("symbol", ScopedSymbolKey(target));

            var belongsToCurrentCompilation =
                target.ContainingAssembly is not null &&
                SymbolEqualityComparer.Default.Equals(
                    target.ContainingAssembly,
                    compilation.Assembly);
            var isUnownedNamespace = target is INamespaceSymbol
            {
                ContainingAssembly: null
            };

            // Same compilation: the project-scoped node is the exact declaration.
            // A merged namespace can have no containing assembly even when the
            // current project contributes a declaration, so keep that local case.
            if ((belongsToCurrentCompilation || isUnownedNamespace) &&
                _nodes.ContainsKey(scopedId.Value))
            {
                nodeId = scopedId;
                return TargetMatch.Found;
            }

            if (belongsToCurrentCompilation)
            {
                // Declared in this compilation but not an indexed declaration
                // (for example an implicit constructor). Never fall through to
                // an unrelated project's canonical match.
                return TargetMatch.External;
            }

            var exactCompilations = availableCompilations
                .Where(pair =>
                    target.ContainingAssembly is not null &&
                    SymbolEqualityComparer.Default.Equals(
                        target.ContainingAssembly,
                        pair.Value.Assembly))
                .Select(pair => pair.Key)
                .Order(StringComparer.Ordinal)
                .ToArray();
            if (exactCompilations.Length == 1)
            {
                return ResolveFromProject(
                    exactCompilations[0],
                    canonical,
                    fingerprint,
                    out nodeId);
            }

            if (exactCompilations.Length > 1)
                return TargetMatch.Ambiguous;

            // A namespace can merge declarations from several assemblies, in
            // which case Roslyn exposes no owning assembly. If any indexed
            // project contributes the name, selecting one would be a guess.
            if (isUnownedNamespace)
            {
                return runSymbols.CountDistinctNodes(canonical) == 0
                    ? TargetMatch.External
                    : TargetMatch.Ambiguous;
            }

            // Fallback for metadata-symbol targets: require full assembly
            // identity, the definition fingerprint and - when the target has
            // source locations - matching declaration provenance. This keeps a
            // same-named unrelated project from being accepted by name equality.
            var identityCandidates = availableCompilations
                .Where(pair =>
                    target.ContainingAssembly is not null &&
                    target.ContainingAssembly.Identity.Equals(
                        pair.Value.Assembly.Identity))
                .Select(pair => pair.Key)
                .Order(StringComparer.Ordinal)
                .ToArray();
            if (identityCandidates.Length == 0)
                return TargetMatch.External;

            var definitionCandidates = identityCandidates
                .Where(projectPath =>
                    runSymbols.HasDefinition(canonical, projectPath, fingerprint))
                .ToArray();
            if (definitionCandidates.Length == 0)
                return TargetMatch.External;

            if (definitionCandidates.Length == 1)
            {
                return ResolveFromProject(
                    definitionCandidates[0],
                    canonical,
                    fingerprint,
                    out nodeId);
            }

            var targetPaths = target.Locations
                .Where(location => location.IsInSource)
                .Select(location => location.SourceTree!.FilePath)
                .Distinct(CSharpProjectDiscovery.PathComparer)
                .ToArray();
            if (targetPaths.Length > 0)
            {
                var provenanceCandidates = definitionCandidates
                    .Where(projectPath => runSymbols.DeclaredIn(
                        canonical,
                        projectPath,
                        targetPaths,
                        fingerprint))
                    .ToArray();
                if (provenanceCandidates.Length == 1)
                {
                    return ResolveFromProject(
                        provenanceCandidates[0],
                        canonical,
                        fingerprint,
                        out nodeId);
                }

                return provenanceCandidates.Length > 1
                    ? TargetMatch.Ambiguous
                    : TargetMatch.External;
            }

            if (definitionCandidates.Length == 1)
            {
                return ResolveFromProject(
                    definitionCandidates[0],
                    canonical,
                    fingerprint,
                    out nodeId);
            }

            return TargetMatch.Ambiguous;
        }

        private TargetMatch ResolveFromProject(
            string projectPath,
            string canonical,
            string fingerprint,
            out CodeNodeId nodeId)
        {
            nodeId = null!;
            if (runSymbols.TryGet(canonical, projectPath, fingerprint, out var registration))
            {
                nodeId = registration.NodeId;
                return TargetMatch.Found;
            }

            return runSymbols.HasConflict(canonical, projectPath)
                ? TargetMatch.Ambiguous
                : TargetMatch.External;
        }

        private void TryAddRelationshipEdge(
            CodeEdgeKind kind,
            CodeNodeId sourceId,
            ISymbol target,
            CodeLocation location)
        {
            if (!TryResolveEmissionTarget(target, kind.Value, out var targetId))
                return;

            AddEdge(kind, sourceId, targetId, location);
            CountEmitted(kind.Value, targetId);
        }

        /// <summary>
        /// Resolves an emission target, counting external and ambiguous
        /// outcomes so coverage stays honest. Returns false when no edge
        /// may be emitted.
        /// </summary>
        private bool TryResolveEmissionTarget(
            ISymbol target,
            string relationshipKind,
            out CodeNodeId targetId)
        {
            var match = TryResolveTarget(Normalize(target), out targetId);
            switch (match)
            {
                case TargetMatch.Found:
                    return true;
                case TargetMatch.Ambiguous:
                    CountAmbiguous(relationshipKind);
                    return false;
                default:
                    CountExternal(relationshipKind);
                    return false;
            }
        }

        private static ISymbol? EnclosingCallable(SyntaxNode node, SemanticModel model)
        {
            foreach (var ancestor in node.Ancestors())
            {
                switch (ancestor)
                {
                    case LocalFunctionStatementSyntax localFunction:
                        return model.GetDeclaredSymbol(localFunction);
                    case BaseMethodDeclarationSyntax method:
                        return model.GetDeclaredSymbol(method);
                    case PropertyDeclarationSyntax property:
                        return model.GetDeclaredSymbol(property);
                    case BaseTypeDeclarationSyntax:
                    case NamespaceDeclarationSyntax:
                        // Callables cannot span outside their declaring type.
                        return null;
                }
            }

            return null;
        }

        private CodeNodeId? SourceNodeFor(ISymbol? callable)
        {
            if (callable is null)
                return null;

            var normalized = Normalize(callable);
            var match = TryResolveTarget(normalized, out var nodeId);
            return match == TargetMatch.Found ? nodeId : null;
        }

        private void HandleBaseTypes(
            string path,
            TypeDeclarationSyntax declaration,
            SemanticModel model)
        {
            if (declaration.BaseList is null ||
                model.GetDeclaredSymbol(declaration) is not INamedTypeSymbol sourceType)
            {
                return;
            }

            var sourceNode = SourceNodeFor(sourceType);
            if (sourceNode is null)
                return;

            foreach (var baseType in declaration.BaseList.Types)
            {
                if (model.GetTypeInfo(baseType.Type).Type is not INamedTypeSymbol targetType)
                    continue;

                var kind = sourceType.TypeKind == TypeKind.Interface
                    ? CodeEdgeKinds.Inherits
                    : targetType.TypeKind == TypeKind.Interface
                        ? CodeEdgeKinds.Implements
                        : CodeEdgeKinds.Inherits;
                TryAddRelationshipEdge(
                    kind,
                    sourceNode,
                    Normalize(targetType),
                    Location(path, baseType));
            }
        }

        private void HandleInvocation(
            string path,
            InvocationExpressionSyntax invocation,
            SemanticModel model)
        {
            var info = model.GetSymbolInfo(invocation);

            if (info.Symbol is null &&
                info.CandidateSymbols.Length == 0)
            {
                if (SourceNodeFor(EnclosingCallable(invocation, model)) is not null)
                    CountUnresolved(CodeEdgeKinds.Calls.Value);
                return;
            }

            if (info.Symbol is not IMethodSymbol method)
                return;

            if (!TryResolveEmissionTarget(
                    method.ReducedFrom ?? method,
                    CodeEdgeKinds.Calls.Value,
                    out var targetNode))
                return; // external or ambiguous callee, already counted

            var sourceNode =
                SourceNodeFor(EnclosingCallable(invocation, model));
            if (sourceNode is null)
                return; // e.g. constructor initializers, field initializers

            AddEdge(
                CodeEdgeKinds.Calls,
                sourceNode,
                targetNode,
                Location(path, invocation));
            CountEmitted(CodeEdgeKinds.Calls.Value, targetNode);
        }

        private void HandleCreation(
            string path,
            ObjectCreationExpressionSyntax creation,
            SemanticModel model)
        {
            var info = model.GetSymbolInfo(creation);
            if (info.Symbol is IMethodSymbol constructor)
            {
                var sourceNode =
                    SourceNodeFor(EnclosingCallable(creation, model));
                if (sourceNode is null)
                    return;
                if (!TryResolveEmissionTarget(
                        constructor,
                        CodeEdgeKinds.Calls.Value,
                        out var constructorTarget))
                    return; // external or ambiguous callee, already counted
                AddEdge(
                    CodeEdgeKinds.Calls,
                    sourceNode,
                    constructorTarget,
                    Location(path, creation));
                CountEmitted(CodeEdgeKinds.Calls.Value, constructorTarget);
            }
            else if (info.Symbol is null && info.CandidateSymbols.Length == 0 &&
                     SourceNodeFor(EnclosingCallable(creation, model)) is not null)
            {
                CountUnresolved(CodeEdgeKinds.Calls.Value);
            }
        }

        private void HandleImplicitCreation(
            string path,
            ImplicitObjectCreationExpressionSyntax creation,
            SemanticModel model) =>
            HandleConstructorCall(path, creation, model);

        private void HandleConstructorInitializer(
            string path,
            ConstructorInitializerSyntax initializer,
            SemanticModel model) =>
            HandleConstructorCall(path, initializer, model);

        private void HandleConstructorCall(
            string path,
            SyntaxNode syntax,
            SemanticModel model)
        {
            var info = model.GetSymbolInfo(syntax);
            var sourceNode = SourceNodeFor(EnclosingCallable(syntax, model));
            if (sourceNode is null)
                return;

            if (info.Symbol is IMethodSymbol constructor)
            {
                if (!TryResolveEmissionTarget(
                        constructor,
                        CodeEdgeKinds.Calls.Value,
                        out var targetNode))
                    return; // external or ambiguous callee, already counted

                AddEdge(
                    CodeEdgeKinds.Calls,
                    sourceNode,
                    targetNode,
                    Location(path, syntax));
                CountEmitted(CodeEdgeKinds.Calls.Value, targetNode);
            }
            else if (info.Symbol is null && info.CandidateSymbols.Length == 0)
            {
                CountUnresolved(CodeEdgeKinds.Calls.Value);
            }
        }

        private void HandleReference(
            string path,
            SyntaxNode syntax,
            SemanticModel model)
        {
            var info = model.GetSymbolInfo(syntax);
            if (syntax is IdentifierNameSyntax { Identifier.ValueText: "var" } &&
                info.Symbol is null)
            {
                // `var` without a resolved symbol is deliberately omitted:
                // there is no target to attribute.
                CountUnsupported(CodeEdgeKinds.References.Value);
                return;
            }

            var sourceNode = SourceNodeFor(EnclosingReferenceOwner(syntax, model)) ??
                (_fileNodes.TryGetValue(path, out var fileNode) ? fileNode : null);
            if (sourceNode is null)
                return;

            var symbol = info.Symbol is IAliasSymbol alias
                ? alias.Target
                : info.Symbol;
            if (symbol is not null && symbol is not INamespaceSymbol)
            {
                TryAddRelationshipEdge(
                    CodeEdgeKinds.References,
                    sourceNode,
                    Normalize(symbol),
                    Location(path, syntax));
            }
            else if (symbol is null && info.CandidateSymbols.Length == 0)
            {
                CountUnresolved(CodeEdgeKinds.References.Value);
            }
        }

        private void HandleAttributeReference(
            string path,
            AttributeSyntax attribute,
            SemanticModel model)
        {
            var sourceNode = SourceNodeFor(EnclosingReferenceOwner(attribute, model)) ??
                (_fileNodes.TryGetValue(path, out var fileNode) ? fileNode : null);
            if (sourceNode is null)
                return;

            var info = model.GetSymbolInfo(attribute);
            if (info.Symbol is IMethodSymbol constructor)
            {
                TryAddRelationshipEdge(
                    CodeEdgeKinds.References,
                    sourceNode,
                    Normalize(constructor.ContainingType),
                    Location(path, attribute));
            }
            else if (info.Symbol is null && info.CandidateSymbols.Length == 0)
            {
                CountUnresolved(CodeEdgeKinds.References.Value);
            }
        }

        private static ISymbol? EnclosingReferenceOwner(
            SyntaxNode node,
            SemanticModel model)
        {
            foreach (var ancestor in node.Ancestors())
            {
                switch (ancestor)
                {
                    case LocalFunctionStatementSyntax localFunction:
                        return model.GetDeclaredSymbol(localFunction);
                    case BaseMethodDeclarationSyntax method:
                        return model.GetDeclaredSymbol(method);
                    case BasePropertyDeclarationSyntax property:
                        return model.GetDeclaredSymbol(property);
                    case FieldDeclarationSyntax field when
                        field.Declaration.Variables.FirstOrDefault() is { } variable:
                        return model.GetDeclaredSymbol(variable);
                    case VariableDeclaratorSyntax variable when
                        variable.Parent?.Parent is FieldDeclarationSyntax:
                        return model.GetDeclaredSymbol(variable);
                    case BaseTypeDeclarationSyntax type:
                        return model.GetDeclaredSymbol(type);
                }
            }

            return null;
        }

        private void HandleUsingDirective(
            string path,
            UsingDirectiveSyntax usingDirective,
            SemanticModel model)
        {
            if (usingDirective.Name is null)
                return;

            var info = model.GetSymbolInfo(usingDirective.Name);
            var target = info.Symbol is IAliasSymbol alias
                ? alias.Target
                : info.Symbol;
            if (target is not INamespaceSymbol and not INamedTypeSymbol)
            {
                // Aliases to non-namespace/non-type members are deliberately
                // omitted: imports model namespace/type scopes only.
                CountUnsupported(CodeEdgeKinds.Imports.Value);
                return;
            }

            var isGlobal = usingDirective.GlobalKeyword != default;
            var containingNamespace = usingDirective.Ancestors()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .FirstOrDefault();
            var scope = isGlobal
                ? "project"
                : containingNamespace is null ? "file" : "namespace";
            CodeNodeId? sourceNode = isGlobal
                ? ProjectNodeId(project.Path)
                : containingNamespace is not null
                    ? SourceNodeFor(model.GetDeclaredSymbol(containingNamespace))
                    : _fileNodes.TryGetValue(path, out var fileNode) ? fileNode : null;
            if (sourceNode is null)
                return;

            var match = TryResolveTarget(Normalize(target), out var targetNode);
            if (match == TargetMatch.Found)
            {
                var aliasName = usingDirective.Alias?.Name.Identifier.ValueText ?? string.Empty;
                var isStatic = usingDirective.StaticKeyword != default;
                AddEdge(
                    CodeEdgeKinds.Imports,
                    sourceNode,
                    targetNode,
                    Location(path, usingDirective),
                    discriminator: $"{scope}:{aliasName}:{isStatic}:{isGlobal}",
                    properties: new Dictionary<string, CodePropertyValue>
                    {
                        [CodePropertyKeys.ImportAlias] = new CodeTextProperty(aliasName),
                        [CodePropertyKeys.ImportStatic] = new CodeBooleanProperty(isStatic),
                        [CodePropertyKeys.ImportGlobal] = new CodeBooleanProperty(isGlobal),
                        [CodePropertyKeys.ImportScope] = new CodeTextProperty(scope)
                    });
                CountEmitted(CodeEdgeKinds.Imports.Value, targetNode);
            }
            else if (match == TargetMatch.Ambiguous)
            {
                CountAmbiguous(CodeEdgeKinds.Imports.Value);
            }
            else
            {
                CountExternal(CodeEdgeKinds.Imports.Value);
            }
        }

        private static async ValueTask WriteChunksAsync<T>(
            IEnumerable<T> values,
            int chunkSize,
            Func<IReadOnlyList<T>, CodeGraphBatch> createBatch,
            ICodeGraphSink sink,
            CancellationToken cancellationToken)
        {
            foreach (var chunk in values.Chunk(chunkSize))
            {
                await sink.WriteBatchAsync(createBatch(chunk), cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}
