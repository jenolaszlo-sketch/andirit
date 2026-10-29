using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Andirit;

/// <summary>Single-file EC-6D syntax facts. Never publishes cross-file bindings.</summary>
internal sealed class EcmaScriptInterfaceFacts
{
    private const string Prefix = "penghou.hetu.tree-sitter.ecmascript-";
    private readonly string _path;
    private readonly CodeNodeId _file;
    private readonly TreeSitterExtractionResult _facts;
    private readonly bool _module;
    private readonly Dictionary<string, DeclarationContext> _declarations = new(StringComparer.Ordinal);

    private sealed record DeclarationContext(string DeclarationRecordId, string Name, bool ModuleScope, bool Ambient, bool Recovered, bool Eligible);

    private EcmaScriptInterfaceFacts(TreeSitterSyntaxNode root, string path, CodeNodeId file, TreeSitterExtractionResult facts)
    {
        _path = path;
        _file = file;
        _facts = facts;
        _module = root.NamedChildren.Any(n => n.Type is "import_statement" or "export_statement");
    }

    internal static void Publish(TreeSitterSyntaxNode root, string path, CodeNodeId file, string pluginVersion, TreeSitterExtractionResult facts)
    {
        var publisher = new EcmaScriptInterfaceFacts(root, path, file, facts);
        // EC6D-IMPL-01 owner correction: ambient exports gain an additive slot
        // only so their declaration ineligibility can be reported explicitly.
        var ambientExports = new TreeSitterExtractionResult();
        foreach (var statement in root.NamedChildren.Where(n => n.Type == "export_statement" && !n.Children.Any(c => c.Type == "default")))
        {
            var ambient = statement.NamedChildren.FirstOrDefault(n => n.Type == "ambient_declaration");
            var declaration = ambient?.NamedChildren.FirstOrDefault(n => n.Type == "interface_declaration");
            var name = declaration?.ChildByFieldName("name");
            if (name is null || name.HasError || statement.HasError) continue;
            ambientExports.ExportObservations.Add(new(path, "typescript", name.Text, name.Text,
                EcmaScriptExportKind.Named, EcmaScriptExportForm.DirectDeclaration, false,
                EcmaScriptNamespaceProjection.Type, publisher.Location(name), publisher.Location(statement)));
        }
        EcmaScriptExportGraphPublisher.Publish(path, "typescript", file, pluginVersion, ambientExports);
        foreach (var entry in ambientExports.Nodes) facts.Nodes.TryAdd(entry.Key, entry.Value);
        foreach (var occurrence in ambientExports.Edges) facts.Edges.Add(occurrence.Key, occurrence.Value);
        publisher.Walk(root, [], path.EndsWith(".d.ts", StringComparison.Ordinal) || path.EndsWith(".d.mts", StringComparison.Ordinal) || path.EndsWith(".d.cts", StringComparison.Ordinal));
        publisher.Attributions();
        publisher.Add("interface-profile", file, file, publisher.Location(root), "profile", "imports1.local-v1", new
        {
            providers = new[] { "imports1.local-v1", "heritage1.local-v1", "module1.local-v1", "exports2.local-v1" },
            fileModuleContext = publisher._module ? "module" : "script"
        });
    }

    private void Walk(TreeSitterSyntaxNode node, IReadOnlyList<TreeSitterSyntaxNode> ancestors, bool ambient)
    {
        ambient |= node.Type == "ambient_declaration" || node.Children.Any(c => c.Type == "declare");
        var declarations = _facts.Declarations.Values.Where(d => d.Location == Location(node)).ToArray();
        foreach (var declaration in declarations)
        {
            if (_declarations.ContainsKey(declaration.Id.Value)) continue;
            var symbol = _facts.Nodes[declaration.SymbolNodeId.Value];
            // Physical containment is determined from syntax, rather than a merged node's edges.
            var moduleScope = ancestors.All(a => a.Type is "program" or "export_statement" or "ambient_declaration" or "lexical_declaration" or "variable_declaration");
            var eligible = moduleScope && !ambient && !node.HasError && !node.IsMissing;
            var context = new DeclarationContext(declaration.Id.Value, symbol.Name, moduleScope, ambient, node.HasError || node.IsMissing, eligible);
            _declarations.Add(declaration.Id.Value, context);
            Add("declaration-context", declaration.SymbolNodeId, _file, declaration.Location, "heritage-context", "heritage1.local-v1", context);
            if (node.Type is "class_declaration" or "abstract_class_declaration" or "interface_declaration")
                Heritage(node, declaration, context);
        }

        if (node.Type is "import_statement" or "import_alias") Imports(node);
        if (node.Type == "export_statement") ExportReference(node);
        if (node.Type == "import_type")
            Import(node, Descendants(node).FirstOrDefault(n => n.Type == "string"), null, null, null, "unsupported", true, "import-type");
        if (node.Type == "call_expression" && node.ChildByFieldName("function")?.Text == "import")
        {
            var token = node.ChildByFieldName("arguments")?.NamedChildren.FirstOrDefault(n => n.Type == "string");
            Import(node, token, null, null, null, "dynamic", false, "import");
        }
        foreach (var child in node.NamedChildren)
            Walk(child, [.. ancestors, node], ambient);
    }

    private void Imports(TreeSitterSyntaxNode node)
    {
        var token = node.ChildByFieldName("source");
        var equals = Descendants(node).FirstOrDefault(n => n.Type == "import_require_clause");
        if (equals is not null || node.Children.Any(n => n.Type == "="))
        {
            token ??= Descendants(node).FirstOrDefault(n => n.Type == "string");
            var local = Descendants(node).FirstOrDefault(n => n.Type == "identifier");
            Import(node, token, local, null, local?.Text, "import-equals", false, "import");
            return;
        }
        var wholeType = node.Children.Any(c => c.Type == "type");
        var reference = wholeType ? "import-type" : "import";
        var count = 0;
        foreach (var specifier in Descendants(node).Where(n => n.Type == "import_specifier"))
        {
            var name = specifier.ChildByFieldName("name");
            var alias = specifier.ChildByFieldName("alias");
            Import(node, token, alias ?? name, name?.Text, (alias ?? name)?.Text,
                alias is null ? "named" : "named-aliased", wholeType || specifier.Children.Any(c => c.Type == "type"), reference);
            count++;
        }
        var clause = node.NamedChildren.FirstOrDefault(n => n.Type == "import_clause");
        foreach (var local in clause?.NamedChildren.Where(n => n.Type == "identifier") ?? [])
        {
            Import(node, token, local, null, local.Text, "default", wholeType, reference);
            count++;
        }
        foreach (var ns in Descendants(node).Where(n => n.Type == "namespace_import"))
        {
            var local = ns.NamedChildren.LastOrDefault();
            Import(node, token, local, null, local?.Text, "namespace", wholeType, reference);
            count++;
        }
        if (count == 0) Import(node, token, null, null, null, clause is null && token is not null ? "side-effect" : "unsupported", wholeType, reference);
    }

    private void Import(TreeSitterSyntaxNode statement, TreeSitterSyntaxNode? token, TreeSitterSyntaxNode? binding,
        string? importedName, string? localName, string form, bool typeOnly, string reference)
    {
        var specifier = Raw(token);
        if (!_facts.ModuleReferences.Any(r => r.Location == Location(statement)))
            _facts.ModuleReferences.Add(new(null, reference, typeOnly, Location(statement)));
        var module = specifier is null ? _file : new CodeNodeId($"typescript:import:{Hash(specifier)}");
        if (module != _file && !_facts.Nodes.ContainsKey(module.Value))
            _facts.Nodes.Add(module.Value, new CodeGraphNode(module, CodeNodeKinds.Type, specifier!, specifier));
        Add("imports-name", _file, module, Location(statement), "import-occurrences", "imports1.local-v1", new
        {
            sourceFile = _file.Value,
            statementSpan = Location(statement),
            moduleSpecifierSpan = token is null ? null : Location(token),
            bindingSpan = binding is null ? null : Location(binding),
            moduleSpecifier = specifier,
            importedName,
            localName,
            isTypeOnly = typeOnly,
            form,
            referenceForm = reference,
            // A resolver-status counterpart is emitted by the inventory pass.
            resolverKey = Key(_file, Location(statement), specifier, reference)
        });
    }

    private void ExportReference(TreeSitterSyntaxNode statement)
    {
        var token = statement.ChildByFieldName("source");
        var exportEquals = statement.Children.Any(n => n.Type == "=");
        if (token is null && !exportEquals) return;
        var reference = _facts.ModuleReferences.SingleOrDefault(r => r.Location == Location(statement));
        var form = exportEquals ? "export-equals" : reference?.ModuleReference ?? "unsupported";
        var clauses = Descendants(statement).Where(n => n.Type is "export_specifier" or "namespace_export").Select(n => new
        {
            importedName = n.ChildByFieldName("name")?.Text,
            exportedName = (n.ChildByFieldName("alias") ?? n.ChildByFieldName("name") ?? (n.Type == "namespace_export" ? n.NamedChildren.LastOrDefault() : null))?.Text,
            clauseSpan = Location(n)
        }).ToArray();
        var specifier = Raw(token);
        Add("export-reference", _file, _file, Location(statement), "module-occurrences", "module1.local-v1", new
        {
            sourceFile = _file.Value,
            statementSpan = Location(statement),
            moduleSpecifierSpan = token is null ? null : Location(token),
            moduleSpecifier = specifier,
            form,
            clauses,
            referenceForm = reference?.ModuleReference ?? "export-from",
            resolverKey = Key(_file, Location(statement), specifier, reference?.ModuleReference ?? "export-from")
        });
    }

    private void Heritage(TreeSitterSyntaxNode owner, CodeGraphDeclaration declaration, DeclarationContext context)
    {
        var isInterface = owner.Type == "interface_declaration";
        var container = owner.NamedChildren.FirstOrDefault(n => n.Type is "class_heritage" or "extends_type_clause");
        if (container is null) return;
        var clauses = container.Type == "extends_type_clause" ? new[] { container } : container.NamedChildren;
        foreach (var clause in clauses)
        {
            var kind = isInterface ? "interface-extends" : clause.Type == "implements_clause" ? "class-implements" : "class-extends";
            var expressions = clause.Type is "implements_clause" or "extends_type_clause"
                ? clause.NamedChildren : clause.ChildByFieldName("value") is { } value ? new[] { value } : new[] { clause };
            foreach (var expression in expressions)
            {
                var name = expression.Type == "generic_type" ? expression.ChildByFieldName("name") ?? expression.NamedChildren[0] : expression;
                var form = expression.Type == "generic_type" ? "expression" : name.Type is "identifier" or "type_identifier" ? "simple"
                    : name.Type is "nested_type_identifier" or "member_expression" ? "qualified" : "expression";
                var terminal = form == "simple" ? name.Text : Descendants(name).LastOrDefault(n => n.Type is "identifier" or "type_identifier" or "property_identifier")?.Text;
                var parameters = owner.ChildByFieldName("type_parameters");
                var names = parameters is null ? Array.Empty<string>() : parameters.NamedChildren.Select(p => p.ChildByFieldName("name")?.Text ?? p.NamedChildren.FirstOrDefault()?.Text).OfType<string>().ToArray();
                Add("heritage-context", declaration.SymbolNodeId, _file, Location(expression), "heritage-context", "heritage1.local-v1", new
                {
                    sourceFile = _file.Value,
                    ownerNode = declaration.SymbolNodeId.Value,
                    heritageSpan = Location(expression),
                    heritageKind = kind,
                    ownerKind = isInterface ? "interface" : "class",
                    writtenForm = form,
                    writtenText = expression.Text,
                    writtenName = name.Text,
                    terminalIdentifier = terminal,
                    ownerScope = context.ModuleScope ? "module" : "nested",
                    ownerTypeParameterNames = names,
                    ownerAmbient = context.Ambient,
                    fileModuleContext = _module ? "module" : "script",
                    clauseSyntaxStatus = owner.HasError || clause.HasError ? "recovered" : "well-formed"
                });
            }
        }
    }

    private void Attributions()
    {
        foreach (var group in _facts.Edges.Values.Where(e => e.Kind == CodeEdgeKinds.Exports && Text(e, CodePropertyKeys.ExportKind) == "named").GroupBy(e => e.TargetId).ToArray())
        {
            var occurrences = group.OrderBy(e => e.Evidence.Location!.StartLine).ThenBy(e => e.Evidence.Location!.StartColumn).ThenBy(e => e.Id.Value, StringComparer.Ordinal).ToArray();
            var localNames = occurrences.Select(e => Text(e, CodePropertyKeys.LocalName)).Distinct(StringComparer.Ordinal).ToArray();
            var contexts = _declarations.Values.Where(d => localNames.Contains(d.Name, StringComparer.Ordinal) && d.ModuleScope).ToArray();
            var eligible = contexts.Where(d => d.Eligible).OrderBy(d => d.DeclarationRecordId, StringComparer.Ordinal).ToArray();
            var ineligible = contexts.Where(d => !d.Eligible).OrderBy(d => d.DeclarationRecordId, StringComparer.Ordinal).ToArray();
            var status = localNames.Length != 1 || eligible.Length > 1 ? "ambiguous-declarations"
                : eligible.Length == 1 && ineligible.Length == 0 ? "attributed"
                : ineligible.Length > 0 ? "ineligible-declaration-context" : "missing-declaration";
            var record = status == "attributed" ? _facts.Declarations[eligible[0].DeclarationRecordId] : null;
            Add("export-attribution", group.Key, _file, occurrences[0].Evidence.Location!, "export-attribution", "exports2.local-v1", new
            {
                entryNode = group.Key.Value,
                exportedName = _facts.Nodes[group.Key.Value].Name,
                status,
                localName = localNames.Length == 1 ? localNames[0] : null,
                eligiblePhysicalDeclarationCount = eligible.Length,
                candidateDeclarationRecordIds = eligible.Select(c => c.DeclarationRecordId).ToArray(),
                ineligibleDeclarationRecordIds = ineligible.Select(c => c.DeclarationRecordId).ToArray(),
                declarationRecordId = record?.Id.Value,
                declarationNodeId = record?.SymbolNodeId.Value,
                symbolId = record?.SymbolId.Value,
                exportSpans = occurrences.Select(e => new { occurrenceId = e.Id.Value, span = e.Evidence.Location }).ToArray()
            });
        }
    }

    private void Add(string kind, CodeNodeId from, CodeNodeId to, CodeLocation location, string provider, string version, object data)
    {
        var json = JsonSerializer.Serialize(data);
        var id = new CodeEdgeId($"typescript:ec6d:{Hash($"{kind}\n{from.Value}\n{to.Value}\n{json}")}");
        _facts.Edges.Add(id.Value, new CodeGraphEdge(id, from, to, new CodeEdgeKind(kind),
            new CodeEvidence(EvidenceQuality.Structural, Prefix + provider, version, location),
            new Dictionary<string, CodePropertyValue> { ["ec6d.data"] = new CodeTextProperty(json) }));
    }

    internal static string Key(CodeNodeId file, CodeLocation span, string? specifier, string form) =>
        Hash(JsonSerializer.Serialize(new { file = file.Value, span, specifier, form }));
    internal static string? Text(CodeGraphEdge edge, string key) => edge.Properties.TryGetValue(key, out var value) && value is CodeTextProperty t ? t.Value : null;
    internal static string? Raw(TreeSitterSyntaxNode? token) => token is { Type: "string" } && token.Text.Length >= 2 ? token.Text[1..^1] : null;
    private CodeLocation Location(TreeSitterSyntaxNode node) => new(_path, node.StartRow + 1, node.StartColumn + 1, node.EndRow + 1, node.EndColumn + 1);
    private static IEnumerable<TreeSitterSyntaxNode> Descendants(TreeSitterSyntaxNode node)
    {
        foreach (var child in node.NamedChildren)
        {
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
    internal static string Hash(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
