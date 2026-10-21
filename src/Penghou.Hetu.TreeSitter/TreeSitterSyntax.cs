namespace Penghou.Hetu;

using TreeSitterApi = global::TreeSitter;

/// <summary>
/// Package-internal view of one Tree-sitter syntax node. This is the only
/// shape language extraction profiles may observe: the third-party node type
/// never crosses this boundary, so no <c>TSNode</c>, <c>TSTree</c>, or
/// binding type can reach Hetu abstractions or consumers.
/// </summary>
internal sealed class TreeSitterSyntaxNode
{
    private readonly TreeSitterApi.Node _node;
    private IReadOnlyList<TreeSitterSyntaxNode>? _children;
    private IReadOnlyList<TreeSitterSyntaxNode>? _namedChildren;

    internal TreeSitterSyntaxNode(TreeSitterApi.Node node)
    {
        _node = node;
    }

    /// <summary>Gets the grammar node type (for example <c>call</c>).</summary>
    internal string Type => _node.Type;

    internal bool IsNamed => _node.IsNamed;

    /// <summary>Gets whether this node itself is a syntax error.</summary>
    internal bool IsError => _node.IsError;

    /// <summary>Gets whether the parser inserted this node during error recovery.</summary>
    internal bool IsMissing => _node.IsMissing;

    /// <summary>Gets whether this node or any descendant carries an error.</summary>
    internal bool HasError => _node.HasError;

    /// <summary>Gets the zero-based start row.</summary>
    internal int StartRow => _node.StartPosition.Row;

    /// <summary>
    /// Gets the zero-based start column in source bytes, as reported by the
    /// parser. Hetu locations add one; non-ASCII columns stay byte-oriented
    /// rather than character-oriented.
    /// </summary>
    internal int StartColumn => _node.StartPosition.Column;

    /// <summary>Gets the zero-based end row.</summary>
    internal int EndRow => _node.EndPosition.Row;

    /// <summary>Gets the zero-based end column in source bytes.</summary>
    internal int EndColumn => _node.EndPosition.Column;

    /// <summary>Gets the source slice covered by this node.</summary>
    internal string Text => _node.Text;

    internal IReadOnlyList<TreeSitterSyntaxNode> Children =>
        _children ??= Wrap(_node.Children);

    internal IReadOnlyList<TreeSitterSyntaxNode> NamedChildren =>
        _namedChildren ??= Wrap(_node.NamedChildren);

    /// <summary>Gets the named child bound to a grammar field, if present.</summary>
    internal TreeSitterSyntaxNode? ChildByFieldName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _node.GetChildForField(name) is { } child
            ? new TreeSitterSyntaxNode(child)
            : null;
    }

    private static IReadOnlyList<TreeSitterSyntaxNode> Wrap(IReadOnlyList<TreeSitterApi.Node> nodes)
    {
        var wrapped = new TreeSitterSyntaxNode[nodes.Count];
        for (var index = 0; index < nodes.Count; index++)
            wrapped[index] = new TreeSitterSyntaxNode(nodes[index]);
        return wrapped;
    }
}
