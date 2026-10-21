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
                    WalkChildren(node, classScope);
                else
                    WalkChildren(node, scope);
                return;
            case "interface_declaration":
                TryEmitNamed(node, node.ChildByFieldName("name"), scope, CodeNodeKinds.Interface, "interface", out _);
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
                EmitCall(node, scope);
                WalkChildren(node, scope);
                return;
            case "new_expression":
                EmitNew(node, scope);
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

    private void AddEdge(CodeEdgeKind kind, CodeNodeId source, CodeNodeId target, CodeLocation location, string discriminator)
    {
        var id = new CodeEdgeId(
            $"{_language}:{Hash($"{kind.Value}\n{source.Value}\n{target.Value}\n{discriminator}\n{location.StartLine}:{location.StartColumn}:{location.EndLine}:{location.EndColumn}")}");
        if (_result.Edges.TryAdd(id.Value, new CodeGraphEdge(
                id,
                source,
                target,
                kind,
                new CodeEvidence(
                    EvidenceQuality.Structural,
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
