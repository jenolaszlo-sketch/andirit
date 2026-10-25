namespace Penghou.Hetu;

/// <summary>An extensible normalized node classification.</summary>
public sealed record CodeNodeKind
{
    public CodeNodeKind(string value) =>
        Value = ContractValue.Identifier(value, nameof(value));

    public string Value { get; }

    public override string ToString() => Value;
}

/// <summary>Well-known normalized node kinds.</summary>
public static class CodeNodeKinds
{
    public static readonly CodeNodeKind Repository = new("repository");
    public static readonly CodeNodeKind Project = new("project");
    public static readonly CodeNodeKind Package = new("package");
    public static readonly CodeNodeKind File = new("file");
    public static readonly CodeNodeKind Namespace = new("namespace");
    public static readonly CodeNodeKind Type = new("type");
    public static readonly CodeNodeKind Interface = new("interface");
    public static readonly CodeNodeKind Callable = new("callable");
    public static readonly CodeNodeKind Property = new("property");
    public static readonly CodeNodeKind Field = new("field");
    public static readonly CodeNodeKind Parameter = new("parameter");
    /// <summary>One written exported name in an owning module.</summary>
    public static readonly CodeNodeKind Export = new("export");
}

/// <summary>An extensible normalized edge classification.</summary>
public sealed record CodeEdgeKind
{
    public CodeEdgeKind(string value) =>
        Value = ContractValue.Identifier(value, nameof(value));

    public string Value { get; }

    public override string ToString() => Value;
}

/// <summary>Well-known normalized edge kinds.</summary>
public static class CodeEdgeKinds
{
    public static readonly CodeEdgeKind Contains = new("contains");
    public static readonly CodeEdgeKind Declares = new("declares");
    public static readonly CodeEdgeKind References = new("references");
    public static readonly CodeEdgeKind Calls = new("calls");
    /// <summary>
    /// Structural invocation evidence: the source invokes something with this
    /// name. Unlike <see cref="Calls"/>, it does not imply a resolved target
    /// and must carry structural evidence.
    /// </summary>
    public static readonly CodeEdgeKind InvokesName = new("invokes-name");
    public static readonly CodeEdgeKind Implements = new("implements");
    public static readonly CodeEdgeKind Inherits = new("inherits");
    public static readonly CodeEdgeKind Imports = new("imports");
    /// <summary>A verified module-file target under a declared resolution policy.</summary>
    public static readonly CodeEdgeKind ResolvesTo = new("resolves-to");
    public static readonly CodeEdgeKind DependsOn = new("depends-on");
    public static readonly CodeEdgeKind Returns = new("returns");
    public static readonly CodeEdgeKind Accepts = new("accepts");
    public static readonly CodeEdgeKind ExercisedBy = new("exercised-by");
    /// <summary>A module file exposes a written exported name.</summary>
    public static readonly CodeEdgeKind Exports = new("exports");
}
