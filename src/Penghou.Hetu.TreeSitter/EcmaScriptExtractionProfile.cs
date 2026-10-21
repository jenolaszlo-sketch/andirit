using System.Security.Cryptography;
using System.Text;

namespace Penghou.Hetu;

/// <summary>
/// Maps TypeScript and JavaScript syntax (as exposed by the pinned
/// <c>tree-sitter-typescript</c>/<c>tree-sitter-javascript</c> grammars) onto
/// Hetu graph facts. The two grammars share a common ancestry, so one profile
/// serves both: declarations, containment, imports, and structural invocation
/// names are observed from syntax and never resolved.
///
/// Everything emitted is structural evidence. The profile never emits
/// <c>calls</c> or <c>references</c>: invocations record the invoked name
/// through <c>invokes-name</c>, and imported modules become plain
/// declaration-less nodes. Declared symbols carry the language property so
/// discovery filters apply uniformly.
/// </summary>
internal sealed class EcmaScriptExtractionProfile : ITreeSitterExtractionProfile
{
    /// <summary>
    /// Extraction-logic revision, folded into the plugin version so profile
    /// fixes invalidate previous indexes even when the grammar pin is
    /// unchanged. Bump on any change to emitted facts.
    /// Revision history:
    /// 1: initial structural extraction for TypeScript and JavaScript.
    /// </summary>
    internal const int ProfileRevision = 1;

    // Kinds both grammars can emit. `implements` exists only in TypeScript, so
    // JavaScript reports it as not-produced rather than claiming an empty set.
    private static readonly CodeEdgeKind[] SharedRelationshipKinds =
    [
        CodeEdgeKinds.Contains,
        CodeEdgeKinds.Declares,
        CodeEdgeKinds.Imports,
        CodeEdgeKinds.InvokesName,
        CodeEdgeKinds.Inherits
    ];

    /// <summary>Relationship kinds the TypeScript grammar can emit.</summary>
    internal static readonly IReadOnlyList<CodeEdgeKind> TypeScriptRelationshipKinds =
        [.. SharedRelationshipKinds, CodeEdgeKinds.Implements];

    /// <summary>Relationship kinds the JavaScript grammar can emit.</summary>
    internal static readonly IReadOnlyList<CodeEdgeKind> JavaScriptRelationshipKinds =
        [.. SharedRelationshipKinds];

    private readonly string _path;
    private readonly string _language;
    private readonly string _providerId;
    private readonly string _providerVersion;
    private readonly TreeSitterExtractionResult _result;

    internal EcmaScriptExtractionProfile(
        string path,
        string languageKey,
        CodePluginId providerId,
        string providerVersion)
    {
        _path = path;
        _language = languageKey;
        _providerId = providerId.Value;
        _providerVersion = providerVersion;
        _result = new TreeSitterExtractionResult();
    }

    public string LanguageKey => _language;

    public int Revision => ProfileRevision;

    public TreeSitterExtractionResult Extract(
        TreeSitterSyntaxNode root,
        CodeNodeId fileNodeId,
        string path)
    {
        Walk(root, new Scope(null, fileNodeId, fileNodeId));
        return _result;
    }

    private void Walk(TreeSitterSyntaxNode node, Scope scope)
    {
        // Error and missing nodes are recovery artifacts, not understood
        // structure: skip the subtree but keep visiting valid siblings. The
        // caller marks the file partial from the root error flag.
        if (node.IsError || node.IsMissing)
            return;

        switch (node.Type)
        {
            case "export_statement":
                HandleExport(node, scope);
                return;
            case "import_statement":
                EmitImport(node, scope);
                return;
            case "class_declaration":
            case "abstract_class_declaration":
                if (TryEmitNamed(node, node.ChildByFieldName("name"), scope, CodeNodeKinds.Type, "class", out var classScope))
                {
                    EmitHeritage(node, classScope.OwnerNode);
                    WalkChildren(node, classScope);
                }
                else
                {
                    WalkChildren(node, scope);
                }

                return;
            case "interface_declaration":
                if (TryEmitNamed(node, node.ChildByFieldName("name"), scope, CodeNodeKinds.Interface, "interface", out var interfaceScope))
                    EmitHeritage(node, interfaceScope.OwnerNode);
                return;
            case "type_alias_declaration":
                TryEmitNamed(node, node.ChildByFieldName("name"), scope, CodeNodeKinds.Type, "type", out _);
                return;
            case "enum_declaration":
                TryEmitNamed(node, node.ChildByFieldName("name"), scope, CodeNodeKinds.Type, "enum", out var enumScope);
                WalkChildren(node, enumScope);
                return;
            case "internal_module":
            case "module":
                if (TryEmitNamed(node, node.ChildByFieldName("name"), scope, CodeNodeKinds.Namespace, "namespace", out var namespaceScope))
                    WalkChildren(node, namespaceScope);
                else
                    WalkChildren(node, scope);
                return;
            case "function_declaration":
            case "generator_function_declaration":
                if (TryEmitNamed(node, node.ChildByFieldName("name"), scope, CodeNodeKinds.Callable, "callable", out var functionScope))
                    WalkChildren(node, functionScope);
                else
                    WalkChildren(node, scope);
                return;
            case "method_definition":
                if (TryEmitNamed(node, node.ChildByFieldName("name"), scope, CodeNodeKinds.Callable, "callable", out var methodScope))
                    WalkChildren(node, methodScope);
                else
                    WalkChildren(node, scope);
                return;
            case "variable_declarator":
                HandleDeclarator(node, scope);
                return;
            case "call_expression":
                TryEmitPrototypeCall(node, scope);
                EmitCall(node, scope);
                WalkChildren(node, scope);
                return;
            case "new_expression":
                EmitNew(node, scope);
                WalkChildren(node, scope);
                return;
            case "assignment_expression":
                TryEmitPrototypeAssignment(node, scope);
                WalkChildren(node, scope);
                return;
            default:
                WalkChildren(node, scope);
                return;
        }
    }

    private void HandleExport(TreeSitterSyntaxNode node, Scope scope)
    {
        var declaration = node.ChildByFieldName("declaration");
        if (declaration is not null)
        {
            Walk(declaration, scope);
            return;
        }

        var source = node.ChildByFieldName("source");
        if (source is not null)
        {
            if (StringLiteral(source) is { } target)
                EmitImportTarget(target, node, scope);
            return;
        }

        WalkChildren(node, scope);
    }

    private void HandleDeclarator(TreeSitterSyntaxNode node, Scope scope)
    {
        var value = node.ChildByFieldName("value");
        if (value is not null && IsFunctionValue(value) &&
            TryEmitNamed(node, node.ChildByFieldName("name"), scope, CodeNodeKinds.Callable, "callable", out var callableScope))
        {
            WalkChildren(value, callableScope);
            return;
        }

        WalkChildren(node, scope);
    }

    private static bool IsFunctionValue(TreeSitterSyntaxNode node) =>
        node.Type is "arrow_function" or "function_expression" or "function" or "generator_function";

    private void EmitImport(TreeSitterSyntaxNode node, Scope scope)
    {
        if (node.ChildByFieldName("source") is { } source &&
            StringLiteral(source) is { } target)
        {
            EmitImportTarget(target, node, scope);
        }
    }

    private void EmitImportTarget(string target, TreeSitterSyntaxNode locationNode, Scope scope)
    {
        var nodeId = new CodeNodeId($"{_language}:import:{Hash(target)}");
        AddNode(new CodeGraphNode(nodeId, CodeNodeKinds.Type, ShortName(target), target));
        AddEdge(CodeEdgeKinds.Imports, scope.OwnerNode, nodeId, Location(locationNode), $"imports:{nodeId.Value}");
    }

    private void EmitCall(TreeSitterSyntaxNode node, Scope scope)
    {
        var target = CalleeName(node.ChildByFieldName("function"));
        if (target is null)
            return;
        if (target == "require")
        {
            TryEmitRequire(node, scope);
            return;
        }

        EmitInvocation(target, node, scope);
    }

    private void EmitNew(TreeSitterSyntaxNode node, Scope scope)
    {
        if (CalleeName(node.ChildByFieldName("constructor")) is { } target)
            EmitInvocation(target, node, scope);
    }

    private void TryEmitRequire(TreeSitterSyntaxNode node, Scope scope)
    {
        var first = node.ChildByFieldName("arguments")?.NamedChildren.FirstOrDefault();
        if (first is { Type: "string" } && StringLiteral(first) is { } target)
            EmitImportTarget(target, node, scope);
    }

    private void EmitInvocation(string target, TreeSitterSyntaxNode locationNode, Scope scope)
    {
        var nodeId = new CodeNodeId($"{_language}:invoked:{Hash(target)}");
        AddNode(new CodeGraphNode(nodeId, CodeNodeKinds.Callable, target));
        AddEdge(CodeEdgeKinds.InvokesName, scope.OwnerNode, nodeId, Location(locationNode), $"invokes:{nodeId.Value}");
    }

    /// <summary>
    /// Emits structural <c>inherits</c>/<c>implements</c> from class or
    /// interface heritage. TypeScript wraps heritage in explicit clauses;
    /// JavaScript places the base type directly under <c>class_heritage</c>.
    /// Expression bases (mixins) carry no stable name and are skipped rather
    /// than guessed.
    /// </summary>
    private void EmitHeritage(TreeSitterSyntaxNode definition, CodeNodeId owner)
    {
        var heritage = FindChild(definition, "class_heritage")
            ?? FindChild(definition, "extends_type_clause");
        if (heritage is null)
            return;

        foreach (var clause in heritage.NamedChildren)
        {
            switch (clause.Type)
            {
                case "extends_clause":
                    if (clause.ChildByFieldName("value") is { } value && HeritageName(value) is { } baseName)
                        EmitHeritageEdge(CodeEdgeKinds.Inherits, owner, baseName, value, EvidenceQuality.Structural);
                    break;
                case "extends_type_clause":
                    foreach (var type in clause.NamedChildren)
                    {
                        if (HeritageName(type) is { } interfaceName)
                            EmitHeritageEdge(CodeEdgeKinds.Inherits, owner, interfaceName, type, EvidenceQuality.Structural);
                    }

                    break;
                case "implements_clause":
                    foreach (var type in clause.NamedChildren)
                    {
                        if (HeritageName(type) is { } implementedName)
                            EmitHeritageEdge(CodeEdgeKinds.Implements, owner, implementedName, type, EvidenceQuality.Structural);
                    }

                    break;
                default:
                    // JavaScript: the base type sits directly under class_heritage.
                    if (HeritageName(clause) is { } directBase)
                        EmitHeritageEdge(CodeEdgeKinds.Inherits, owner, directBase, clause, EvidenceQuality.Structural);
                    break;
            }
        }
    }

    /// <summary>
    /// Recognizes the well-known prototype-inheritance idioms. These are not
    /// grammar-level inheritance, so they are emitted as <c>Derived</c>
    /// evidence: Hetu computed them deterministically from an exact syntax
    /// pattern, never as resolved or structural fact.
    /// </summary>
    private void TryEmitPrototypeAssignment(TreeSitterSyntaxNode node, Scope scope)
    {
        var left = node.ChildByFieldName("left");
        var right = node.ChildByFieldName("right");
        if (PrototypeOwner(left) is { } child && PrototypeBase(right) is { } parent)
            EmitHeritageEdge(CodeEdgeKinds.Inherits, scope.OwnerNode, parent, node, EvidenceQuality.Derived, child);
    }

    private void TryEmitPrototypeCall(TreeSitterSyntaxNode node, Scope scope)
    {
        var target = CalleeName(node.ChildByFieldName("function"));
        var arguments = node.ChildByFieldName("arguments")?.NamedChildren;
        if (arguments is not { Count: >= 2 })
            return;

        if (target == "util.inherits")
        {
            if (HeritageName(arguments[0]) is { } child && HeritageName(arguments[1]) is { } parent)
                EmitHeritageEdge(CodeEdgeKinds.Inherits, scope.OwnerNode, parent, node, EvidenceQuality.Derived, child);
        }
        else if (target == "Object.setPrototypeOf")
        {
            if (PrototypeOwner(arguments[0]) is { } child && PrototypeOwner(arguments[1]) is { } parent)
                EmitHeritageEdge(CodeEdgeKinds.Inherits, scope.OwnerNode, parent, node, EvidenceQuality.Derived, child);
        }
    }

    private void EmitHeritageEdge(
        CodeEdgeKind kind,
        CodeNodeId defaultOwner,
        string targetName,
        TreeSitterSyntaxNode locationNode,
        EvidenceQuality quality,
        string? sourceName = null)
    {
        var source = sourceName is null ? defaultOwner : StructuralTypeNode(sourceName);
        var target = StructuralTypeNode(targetName);
        AddEdge(kind, source, target, Location(locationNode), $"{kind.Value}:{source.Value}:{target.Value}", quality);
    }

    /// <summary>
    /// Resolves a heritage type name to a declaration-less type node whose
    /// identity matches a same-named class declared in the same scope, so the
    /// edge converges when the base type is indexed here.
    /// </summary>
    private CodeNodeId StructuralTypeNode(string name)
    {
        var nodeId = new CodeNodeId($"{_language}:symbol:{Hash($"class\n{name}")}");
        AddNode(new CodeGraphNode(nodeId, CodeNodeKinds.Type, ShortName(name), name));
        return nodeId;
    }

    private static string? HeritageName(TreeSitterSyntaxNode? node) =>
        node?.Type switch
        {
            "identifier" or "type_identifier" => BoundedText(node),
            "member_expression" => MemberName(node),
            _ => null
        };

    private static string? PrototypeOwner(TreeSitterSyntaxNode? node)
    {
        if (node is null || node.Type != "member_expression")
            return null;
        var property = node.ChildByFieldName("property");
        if (property is null || BoundedText(property) != "prototype")
            return null;
        return HeritageName(node.ChildByFieldName("object"));
    }

    private static string? PrototypeBase(TreeSitterSyntaxNode? node)
    {
        if (node is null)
            return null;
        if (node.Type == "new_expression")
            return HeritageName(node.ChildByFieldName("constructor"));
        if (node.Type == "call_expression" && CalleeName(node.ChildByFieldName("function")) == "Object.create")
        {
            var first = node.ChildByFieldName("arguments")?.NamedChildren.FirstOrDefault();
            return PrototypeOwner(first) ?? HeritageName(first);
        }

        return null;
    }

    private static TreeSitterSyntaxNode? FindChild(TreeSitterSyntaxNode node, string type)
    {
        foreach (var child in node.NamedChildren)
        {
            if (child.Type == type)
                return child;
        }

        return null;
    }

    private bool TryEmitNamed(
        TreeSitterSyntaxNode definition,
        TreeSitterSyntaxNode? nameNode,
        Scope scope,
        CodeNodeKind kind,
        string discriminator,
        out Scope childScope)
    {
        childScope = scope;
        if (nameNode is null || BoundedText(nameNode) is not { } name)
            return false;

        var qualified = Qualify(scope.Container, name);
        var symbolId = new CodeSymbolId($"{_language}:{Hash($"{discriminator}\n{qualified}")}");
        var nodeId = new CodeNodeId($"{_language}:symbol:{Hash($"{discriminator}\n{qualified}")}");
        var location = Location(nameNode);
        AddNode(new CodeGraphNode(
            nodeId,
            kind,
            name,
            qualified,
            symbolId,
            LanguageProperties()));
        // The declaration covers the whole definition so enclosing-symbol
        // queries resolve body positions; edges keep the precise name span.
        AddDeclaration(symbolId, nodeId, Location(definition));
        AddEdge(CodeEdgeKinds.Declares, scope.FileNode, nodeId, location, $"declares:{nodeId.Value}");
        if (scope.OwnerNode != nodeId)
            AddEdge(CodeEdgeKinds.Contains, scope.OwnerNode, nodeId, location, $"contains:{nodeId.Value}");
        childScope = new Scope(qualified, nodeId, scope.FileNode);
        return true;
    }

    private static string? CalleeName(TreeSitterSyntaxNode? node)
    {
        if (node is null)
            return null;

        return node.Type switch
        {
            "identifier" => BoundedText(node),
            "property_identifier" => BoundedText(node),
            "private_property_identifier" => BoundedText(node),
            "this" => "this",
            "super" => "super",
            "member_expression" => MemberName(node),
            _ => null
        };
    }

    private static string? MemberName(TreeSitterSyntaxNode member)
    {
        var target = CalleeName(member.ChildByFieldName("object"));
        var property = CalleeName(member.ChildByFieldName("property"));
        return target is not null && property is not null
            ? $"{target}.{property}"
            : null;
    }

    private static string? StringLiteral(TreeSitterSyntaxNode node)
    {
        foreach (var child in node.NamedChildren)
        {
            if (child.Type == "string_fragment")
                return BoundedText(child);
        }

        var text = node.Text;
        if (text.Length >= 2 && (text[0] == '"' || text[0] == '\'') && text[^1] == text[0])
            text = text[1..^1];
        return text.Length is > 0 and <= 256 ? text : null;
    }

    private static string ShortName(string target)
    {
        var separator = Math.Max(target.LastIndexOf('/'), target.LastIndexOf(':'));
        return separator >= 0 && separator < target.Length - 1
            ? target[(separator + 1)..]
            : target;
    }

    private static string Qualify(string? container, string name) =>
        container is null ? name : $"{container}.{name}";

    /// <summary>Gets source text bounded for identifiers: pathological names never become facts.</summary>
    private static string? BoundedText(TreeSitterSyntaxNode node)
    {
        var text = node.Text;
        return text.Length is > 0 and <= 256 ? text : null;
    }

    private Dictionary<string, CodePropertyValue> LanguageProperties() =>
        new()
        {
            [CodePropertyKeys.Language] = new CodeTextProperty(_language)
        };

    private CodeLocation Location(TreeSitterSyntaxNode node) =>
        new(
            _path,
            node.StartRow + 1,
            node.StartColumn + 1,
            node.EndRow + 1,
            node.EndColumn + 1);

    private void WalkChildren(TreeSitterSyntaxNode node, Scope scope)
    {
        foreach (var child in node.NamedChildren)
            Walk(child, scope);
    }

    private void AddNode(CodeGraphNode node)
    {
        // Import and invocation targets arrive without symbol identity and may
        // later converge with a real declaration carrying the same stable
        // identity. The symbol-bearing fact always wins; identical facts
        // collapse.
        if (_result.Nodes.TryGetValue(node.Id.Value, out var existing))
        {
            if (existing.SymbolId is not null || node.SymbolId is null)
                return;
        }

        _result.Nodes[node.Id.Value] = node;
    }

    private void AddDeclaration(CodeSymbolId symbolId, CodeNodeId nodeId, CodeLocation location)
    {
        var id = new CodeDeclarationId(
            $"{_language}:{Hash($"{symbolId.Value}\n{_path}\n{location.StartLine}:{location.StartColumn}:{location.EndLine}:{location.EndColumn}")}");
        _result.Declarations[id.Value] = new CodeGraphDeclaration(id, symbolId, nodeId, location);
    }

    private void AddEdge(
        CodeEdgeKind kind,
        CodeNodeId source,
        CodeNodeId target,
        CodeLocation location,
        string discriminator,
        EvidenceQuality quality = EvidenceQuality.Structural)
    {
        var id = new CodeEdgeId(
            $"{_language}:{Hash($"{kind.Value}\n{source.Value}\n{target.Value}\n{discriminator}\n{location.StartLine}:{location.StartColumn}:{location.EndLine}:{location.EndColumn}")}");
        if (_result.Edges.TryAdd(id.Value, new CodeGraphEdge(
                id,
                source,
                target,
                kind,
                new CodeEvidence(
                    quality,
                    _providerId,
                    _providerVersion,
                    location))))
            _result.EdgeCounts[kind] = _result.EdgeCounts.GetValueOrDefault(kind) + 1;
    }

    private static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    /// <summary>Lexical scope while walking one file: container, owner, and file nodes.</summary>
    private sealed record Scope(string? Container, CodeNodeId OwnerNode, CodeNodeId FileNode);
}
