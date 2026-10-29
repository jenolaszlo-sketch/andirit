using System.Security.Cryptography;
using System.Text;

namespace Penghou.Andirit;

/// <summary>
/// Maps Elixir syntax (as exposed by the pinned Tree-sitter grammar) onto
/// Andirit graph facts. The elixir-lang grammar mirrors the Elixir AST, so
/// modules, definitions, aliases, and invocations all surface as
/// <c>call</c> nodes distinguished by their target identifier; this profile
/// is a small adapter over those shapes, not a parser.
///
/// Everything emitted is structural evidence. The profile never emits
/// <c>calls</c> or <c>references</c>: an invocation records only the invoked
/// name through <c>invokes-name</c>, and imported modules become plain
/// declaration-less nodes that later converge with real declarations only
/// when identifier schemes match exactly. Declared symbols carry the
/// language property so discovery filters apply uniformly; unresolved name
/// nodes deliberately do not, since the language of an unresolved name is
/// unknowable.
/// </summary>
internal sealed class ElixirExtractionProfile : ITreeSitterExtractionProfile
{
    /// <summary>
    /// Extraction-logic revision, folded into the plugin version so profile
    /// fixes invalidate previous indexes even when the grammar pin is
    /// unchanged. Bump on any change to emitted facts.
    /// Revision history:
    /// 1: initial structural extraction.
    /// 2: declarations cover whole definitions (enclosing-symbol support).
    /// </summary>
    internal const int ProfileRevision = 2;

    /// <summary>Relationship kinds the Elixir profile actually emits.</summary>
    internal static readonly IReadOnlyList<CodeEdgeKind> ProducedRelationshipKinds =
    [
        CodeEdgeKinds.Contains,
        CodeEdgeKinds.Declares,
        CodeEdgeKinds.Imports,
        CodeEdgeKinds.InvokesName
    ];

    public string LanguageKey => "elixir";

    public int Revision => ProfileRevision;

    private static readonly HashSet<string> ModuleTargets = new(StringComparer.Ordinal)
    {
        "defmodule", "defprotocol"
    };

    private static readonly HashSet<string> FunctionTargets = new(StringComparer.Ordinal)
    {
        "def", "defp", "defmacro", "defmacrop", "defguard", "defguardp", "defdelegate"
    };

    private static readonly HashSet<string> ImportTargets = new(StringComparer.Ordinal)
    {
        "alias", "import", "require", "use"
    };

    /// <summary>
    /// Call targets that are language control forms rather than invocations.
    /// Emitting them as <c>invokes-name</c> would fabricate callables that no
    /// Elixir program defines.
    /// </summary>
    private static readonly HashSet<string> NonInvocationTargets = new(StringComparer.Ordinal)
    {
        "if", "unless", "case", "cond", "for", "with", "try", "receive",
        "after", "rescue", "catch", "else", "quote", "unquote", "unquote_splicing",
        "fn", "do", "when"
    };

    private readonly string _path;
    private readonly TreeSitterExtractionResult _result;

    internal ElixirExtractionProfile(string path)
    {
        _path = path;
        _result = new TreeSitterExtractionResult();
    }

    public TreeSitterExtractionResult Extract(
        TreeSitterSyntaxNode root,
        CodeNodeId fileNodeId,
        string path)
    {
        Walk(root, new ElixirScope(null, fileNodeId, fileNodeId));
        return _result;
    }

    private void Walk(TreeSitterSyntaxNode node, ElixirScope scope)
    {
        // Error and missing nodes are recovery artifacts, not understood
        // structure: skip the subtree but keep visiting valid siblings. The
        // caller marks the file partial from the root error flag.
        if (node.IsError || node.IsMissing)
            return;

        if (node is { IsNamed: true, Type: "call" } &&
            CallTarget(node) is { } target)
        {
            if (ModuleTargets.Contains(target))
            {
                if (TryEmitModule(node, scope, out var moduleScope))
                    scope = moduleScope;
                WalkChildren(node, scope);
                return;
            }

            if (FunctionTargets.Contains(target))
            {
                WalkHeadless(node, TryEmitFunction(node, scope) ?? scope);
                return;
            }

            if (ImportTargets.Contains(target))
            {
                TryEmitImport(node, scope);
                WalkChildren(node, scope);
                return;
            }

            if (!NonInvocationTargets.Contains(target))
                TryEmitInvocation(node, target, scope);
        }
        else if (node.IsNamed && node.Type == "unary_operator" && IsAttribute(node))
        {
            // Module attributes (@moduledoc, @spec, @behaviour, ...) describe
            // their owner; their contents are not invocations.
            return;
        }

        WalkChildren(node, scope);
    }

    private void WalkChildren(TreeSitterSyntaxNode node, ElixirScope scope)
    {
        foreach (var child in node.NamedChildren)
            Walk(child, scope);
    }

    /// <summary>
    /// Walks a definition body without treating the definition head as an
    /// invocation: the head names the function, it does not call it. Guards
    /// and other head siblings are still visited; only the head itself is
    /// skipped.
    /// </summary>
    private void WalkHeadless(TreeSitterSyntaxNode definition, ElixirScope body)
    {
        var head = DefinitionHead(definition);
        foreach (var child in definition.NamedChildren)
        {
            if (child.Type != "arguments")
            {
                Walk(child, body);
                continue;
            }

            foreach (var argument in child.NamedChildren)
            {
                if (head is null || !ReferenceEquals(argument, head))
                    Walk(argument, body);
            }
        }
    }

    /// <summary>Gets the target identifier of a call node, if it has one.</summary>
    private static string? CallTarget(TreeSitterSyntaxNode call)
    {
        var target = call.NamedChildren.Count > 0 ? call.NamedChildren[0] : null;
        if (target is null)
            return null;
        if (target.Type == "identifier")
            return BoundedText(target);
        if (target.Type == "dot")
            return BoundedText(target);
        return null;
    }

    /// <summary>Gets the definition head: the first named argument of a definition call.</summary>
    private static TreeSitterSyntaxNode? DefinitionHead(TreeSitterSyntaxNode definition)
    {
        foreach (var child in definition.NamedChildren)
        {
            if (child.Type != "arguments")
                continue;
            return child.NamedChildren.Count > 0 ? child.NamedChildren[0] : null;
        }

        return null;
    }

    private bool TryEmitModule(TreeSitterSyntaxNode node, ElixirScope scope, out ElixirScope moduleScope)
    {
        moduleScope = scope;
        if (ModuleName(node) is not { } name)
        {
            // A dynamically named module contributes no stable identity, but
            // its body may still hold declarations worth visiting.
            return false;
        }

        var qualified = name.Text;
        var shortName = qualified.Contains('.', StringComparison.Ordinal)
            ? qualified[(qualified.LastIndexOf('.') + 1)..]
            : qualified;
        var symbolId = new CodeSymbolId($"elixir:{Hash($"module\n{qualified}")}");
        var nodeId = new CodeNodeId($"elixir:symbol:{Hash($"module\n{qualified}")}");
        var location = Location(name.Node);
        AddNode(new CodeGraphNode(
            nodeId,
            CodeNodeKinds.Type,
            shortName,
            qualified,
            symbolId,
            new Dictionary<string, CodePropertyValue>
            {
                [CodePropertyKeys.Language] = new CodeTextProperty("elixir")
            }));
        // The declaration covers the whole definition so enclosing-symbol
        // queries resolve body positions; edges keep the precise name span.
        AddDeclaration(symbolId, nodeId, Location(node));
        AddEdge(CodeEdgeKinds.Declares, scope.FileNode, nodeId, location, $"declares:{nodeId.Value}");
        if (scope.ModuleNode is not null)
            AddEdge(CodeEdgeKinds.Contains, scope.ModuleNode, nodeId, location, $"contains:{nodeId.Value}");
        else
            AddEdge(CodeEdgeKinds.Contains, scope.FileNode, nodeId, location, $"contains:{nodeId.Value}");
        moduleScope = new ElixirScope(qualified, nodeId, scope.FileNode);
        return true;
    }

    private ElixirScope? TryEmitFunction(TreeSitterSyntaxNode node, ElixirScope scope)
    {
        var head = DefinitionHead(node);
        var (name, arity) = FunctionName(head);
        if (name is null)
            return null;

        var qualified = scope.Module is not null
            ? $"{scope.Module}.{name.Text}/{arity}"
            : $"{name.Text}/{arity}";
        var symbolId = new CodeSymbolId($"elixir:{Hash($"function\n{qualified}")}");
        var nodeId = new CodeNodeId($"elixir:symbol:{Hash($"function\n{qualified}")}");
        var location = Location(name.Node);
        AddNode(new CodeGraphNode(
            nodeId,
            CodeNodeKinds.Callable,
            name.Text,
            qualified,
            symbolId,
            new Dictionary<string, CodePropertyValue>
            {
                [CodePropertyKeys.Language] = new CodeTextProperty("elixir")
            }));
        // The declaration covers the whole definition so enclosing-symbol
        // queries resolve body positions; edges keep the precise name span.
        AddDeclaration(symbolId, nodeId, Location(node));
        AddEdge(CodeEdgeKinds.Declares, scope.FileNode, nodeId, location, $"declares:{nodeId.Value}");
        var owner = scope.ModuleNode ?? scope.FileNode;
        if (owner != nodeId)
            AddEdge(CodeEdgeKinds.Contains, owner, nodeId, location, $"contains:{nodeId.Value}");
        return new ElixirScope(scope.Module, nodeId, scope.FileNode, nodeId);
    }

    private void TryEmitImport(TreeSitterSyntaxNode node, ElixirScope scope)
    {
        var target = ImportTarget(node);
        if (target is null)
            return;

        var shortName = target.Contains('.', StringComparison.Ordinal)
            ? target[(target.LastIndexOf('.') + 1)..]
            : target;
        var nodeId = new CodeNodeId($"elixir:symbol:{Hash($"module\n{target}")}");
        AddNode(new CodeGraphNode(nodeId, CodeNodeKinds.Type, shortName, target));
        var owner = scope.CallableNode ?? scope.ModuleNode ?? scope.FileNode;
        AddEdge(CodeEdgeKinds.Imports, owner, nodeId, Location(node), $"imports:{nodeId.Value}");
    }

    private void TryEmitInvocation(TreeSitterSyntaxNode node, string target, ElixirScope scope)
    {
        var nodeId = new CodeNodeId($"elixir:invoked:{Hash(target)}");
        AddNode(new CodeGraphNode(nodeId, CodeNodeKinds.Callable, target));
        var owner = scope.CallableNode ?? scope.ModuleNode ?? scope.FileNode;
        AddEdge(CodeEdgeKinds.InvokesName, owner, nodeId, Location(node), $"invokes:{nodeId.Value}");
    }

    /// <summary>Gets the module name of a definition call, if statically known.</summary>
    private static (string Text, TreeSitterSyntaxNode Node)? ModuleName(TreeSitterSyntaxNode definition)
    {
        foreach (var child in definition.NamedChildren)
        {
            if (child.Type != "arguments" || child.NamedChildren.Count == 0)
                continue;
            var first = child.NamedChildren[0];
            if (first.Type == "alias" && BoundedText(first) is { } text)
                return (text, first);
            return null;
        }

        return null;
    }

    /// <summary>Gets the imported module base of an alias/import/require/use call.</summary>
    private static string? ImportTarget(TreeSitterSyntaxNode node)
    {
        foreach (var child in node.NamedChildren)
        {
            if (child.Type != "arguments" || child.NamedChildren.Count == 0)
                continue;
            var first = child.NamedChildren[0];
            if (first.Type == "alias")
                return BoundedText(first);
            if (first.Type == "dot")
                return FirstAliasText(first);
            return null;
        }

        return null;
    }

    private static string? FirstAliasText(TreeSitterSyntaxNode node)
    {
        foreach (var child in node.NamedChildren)
        {
            if (child.Type == "alias")
                return BoundedText(child);
            if (child.Type == "dot" && FirstAliasText(child) is { } nested)
                return nested;
        }

        return null;
    }

    /// <summary>Resolves a definition head to its function name and written arity.</summary>
    private static (ElixirName? Name, int Arity) FunctionName(TreeSitterSyntaxNode? head)
    {
        var current = head;
        while (current is not null)
        {
            if (current.Type == "identifier" && BoundedText(current) is { } name)
                return (new ElixirName(name, current), 0);
            if (current.Type == "call")
            {
                var target = current.NamedChildren.Count > 0 ? current.NamedChildren[0] : null;
                if (target?.Type == "identifier" && BoundedText(target) is { } callName)
                    return (new ElixirName(callName, target), ArityOf(current));
                return (null, 0);
            }
            if (current.Type == "binary_operator" && current.NamedChildren.Count > 0)
            {
                current = current.NamedChildren[0];
                continue;
            }
            return (null, 0);
        }

        return (null, 0);
    }

    private static int ArityOf(TreeSitterSyntaxNode call)
    {
        foreach (var child in call.NamedChildren)
        {
            if (child.Type == "arguments")
                return child.NamedChildren.Count;
        }

        return 0;
    }

    private static bool IsAttribute(TreeSitterSyntaxNode unary)
    {
        foreach (var child in unary.Children)
        {
            if (!child.IsNamed && child.Text == "@")
                return true;
        }

        return false;
    }

    /// <summary>Gets source text bounded for identifiers: pathological names never become facts.</summary>
    private static string? BoundedText(TreeSitterSyntaxNode node)
    {
        var text = node.Text;
        return text.Length is > 0 and <= 256 ? text : null;
    }

    private CodeLocation Location(TreeSitterSyntaxNode node) =>
        new(
            _path,
            node.StartRow + 1,
            node.StartColumn + 1,
            node.EndRow + 1,
            node.EndColumn + 1);

    private void AddNode(CodeGraphNode node)
    {
        // Import targets arrive without symbol identity and may later converge
        // with a real declaration carrying the same stable identity. The
        // symbol-bearing fact always wins; identical facts collapse.
        if (_result.Nodes.TryGetValue(node.Id.Value, out var existing))
        {
            if (existing.SymbolId is not null || node.SymbolId is null)
                return;
        }

        _result.Nodes[node.Id.Value] = node;
    }

    private void AddDeclaration(CodeSymbolId symbolId, CodeNodeId nodeId, CodeLocation location)
    {
        var id = new CodeDeclarationId($"elixir:{Hash($"{symbolId.Value}\n{_path}\n{location.StartLine}:{location.StartColumn}:{location.EndLine}:{location.EndColumn}")}");
        _result.Declarations[id.Value] = new CodeGraphDeclaration(id, symbolId, nodeId, location);
    }

    private void AddEdge(CodeEdgeKind kind, CodeNodeId source, CodeNodeId target, CodeLocation location, string discriminator)
    {
        var id = new CodeEdgeId($"elixir:{Hash($"{kind.Value}\n{source.Value}\n{target.Value}\n{discriminator}\n{location.StartLine}:{location.StartColumn}:{location.EndLine}:{location.EndColumn}")}");
        if (_result.Edges.TryAdd(id.Value, new CodeGraphEdge(
                id,
                source,
                target,
                kind,
                new CodeEvidence(
                    EvidenceQuality.Structural,
                    TreeSitterElixirPlugin.PluginId.Value,
                    TreeSitterElixirPlugin.PluginVersion,
                    location))))
            _result.EdgeCounts[kind] = _result.EdgeCounts.GetValueOrDefault(kind) + 1;
    }

    private static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private sealed record ElixirName(string Text, TreeSitterSyntaxNode Node);

    /// <summary>Lexical scope while walking one file: module, callable, and file owners.</summary>
    private sealed record ElixirScope(string? Module, CodeNodeId ModuleNode, CodeNodeId FileNode, CodeNodeId? CallableNode = null);
}
