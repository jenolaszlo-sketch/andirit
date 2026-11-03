using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace SemanticCorpusOracle;

/// <summary>
/// Independent re-implementation of the accepted R2 identity tuple algebra
/// (compctx1, compctx2, asmctx3, usage4) and the S2.1 verified dependency
/// provenance shape. It is written from the frozen contract documents, not by
/// calling Andirit production helpers. The frozen R2 anchor vectors are checked
/// at startup (see <see cref="SelfTest"/>).
/// </summary>
public static class R2Identity
{
    public const string Compctx1 = "compctx1";
    public const string Compctx2 = "compctx2";
    public const string UsageSemantics = "usage4-b2c-exp-r2-20261101";
    public const string TargetDomain = "asmctx3";
    public const string TargetPrefix = "asmctx3:sha256:";
    public const string InvocationCategory = "method-invocation";

    /// <summary>Big-endian 32-bit length-prefixed SHA-256 over the domain and parts.</summary>
    public static string Hash(string domain, IEnumerable<string> parts) =>
        Convert.ToHexStringLower(SHA256.HashData(EncodedTuple(domain, parts)));

    /// <summary>
    /// Exact preimage bytes of the accepted tuple encoding: the domain followed by
    /// each part, every value prefixed with its big-endian 32-bit UTF-8 byte length.
    /// Exposed so cross-view checks can compare encoded bytes rather than only
    /// twenty-hex-character summaries of them.
    /// </summary>
    public static byte[] EncodedTuple(string domain, IEnumerable<string> parts)
    {
        using var stream = new MemoryStream();
        void Append(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            Span<byte> length = stackalloc byte[4];
            BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
            stream.Write(length);
            stream.Write(bytes);
        }

        Append(domain);
        foreach (var part in parts)
            Append(part);
        return stream.ToArray();
    }

    public static string Sha256Hex(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    public static string NormalizePath(string path) => path.Replace('\\', '/');

    // ---- compctx1 / compctx2 -------------------------------------------------

    public sealed record SourceIdentity(string Path, string ContentHash, string LanguageVersion, string PreprocessorSymbols)
    {
        public string CanonicalForm => string.Join("|", Path, ContentHash, LanguageVersion, PreprocessorSymbols);
    }

    public sealed record CompilationDescriptor(
        string ContractVersion,
        string RepositoryId,
        string ProjectUnitId,
        string AssemblyName,
        string MetadataIdentity,
        string ModuleName,
        string OptionsDigest,
        IReadOnlyList<SourceIdentity> Sources,
        IReadOnlyList<string> References)
    {
        public string ComputeDigest() => ContractVersion + ":sha256:" + Hash(ContractVersion + "-context",
        [
            ContractVersion,
            RepositoryId,
            ProjectUnitId,
            AssemblyName,
            MetadataIdentity,
            ModuleName,
            OptionsDigest,
            .. Sources.Select(s => s.CanonicalForm),
            .. References,
        ]);
    }

    /// <summary>The R2 project-reference item (text/tree-only, frozen legacy form).</summary>
    public static string R2ProjectReferenceItem(string assemblyName, string metadataIdentity, IEnumerable<(string Path, string TextHash)> trees)
    {
        var treePart = string.Join(",", trees.OrderBy(t => t.Path, StringComparer.Ordinal)
            .Select(t => NormalizePath(t.Path) + "=" + t.TextHash));
        return "compilation|" + assemblyName + "|" + metadataIdentity + "|" + treePart;
    }

    /// <summary>The S2.1 verified project-reference item (strong dependency context).</summary>
    public static string VerifiedProjectReferenceItem(string dependencyCompctx2) =>
        "compilation-context|" + dependencyCompctx2;

    public static string MetadataReferenceItem(string path, string sha256Hex) =>
        "file|" + NormalizePath(path) + "|" + sha256Hex;

    // ---- asmctx3 -------------------------------------------------------------

    /// <summary>
    /// assembles asmctx3 from the frozen R2 domain and tuple. The component
    /// digests are fixture-supplied canonical inputs; this method does not
    /// define how they are canonicalized from source (an S2.2 obligation).
    /// </summary>
    public static string TargetId(string compctx1, string metadataIdentity, string moduleName,
        string identifierDigest, string declarationDigest, string shapeDigest) =>
        TargetPrefix + Hash(TargetDomain + "-context",
        [
            Compctx1,
            compctx1,
            metadataIdentity,
            moduleName,
            identifierDigest,
            declarationDigest,
            shapeDigest,
        ]);

    public static string IdentifierDigest(IEnumerable<string> forms) => Hash(TargetDomain + "-identifier", forms);
    public static string DeclarationDigest(IEnumerable<string> forms) => Hash(TargetDomain + "-declarations", forms);
    public static string ShapeDigest(string shape) => Hash(TargetDomain + "-target", [shape]);

    // ---- usage4 --------------------------------------------------------------

    /// <summary>
    /// The complete accepted R2 <c>usage4</c> tuple in canonical field order.
    /// Both the identity view and the lifecycle projection build their occurrence
    /// identity from this single ordered part list.
    /// </summary>
    public static string[] Usage4Parts(
        string repository, string plugin, string semanticUnit, string path, string sourceHash,
        int startLine, int startColumn, int endLine, int endColumn,
        string targetDocumentId, string asmctx3) =>
    [
        repository,
        plugin,
        UsageSemantics,
        semanticUnit,
        path,
        sourceHash,
        startLine.ToString(System.Globalization.CultureInfo.InvariantCulture),
        startColumn.ToString(System.Globalization.CultureInfo.InvariantCulture),
        endLine.ToString(System.Globalization.CultureInfo.InvariantCulture),
        endColumn.ToString(System.Globalization.CultureInfo.InvariantCulture),
        targetDocumentId,
        asmctx3,
        InvocationCategory,
    ];

    public static string OccurrenceId(
        string repository, string plugin, string semanticUnit, string path, string sourceHash,
        int startLine, int startColumn, int endLine, int endColumn,
        string targetDocumentId, string asmctx3) =>
        Hash("usage4", Usage4Parts(repository, plugin, semanticUnit, path, sourceHash,
            startLine, startColumn, endLine, endColumn, targetDocumentId, asmctx3));

    // ---- options digest ------------------------------------------------------

    public static string OptionsDigest(Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions options) =>
        Hash("compctx1-options",
        [
            options.OutputKind.ToString(),
            options.NullableContextOptions.ToString(),
            options.AllowUnsafe.ToString(),
            options.OptimizationLevel.ToString(),
            options.Platform.ToString(),
            options.CheckOverflow.ToString(),
            options.Deterministic.ToString(),
            options.WarningLevel.ToString(System.Globalization.CultureInfo.InvariantCulture),
            options.GeneralDiagnosticOption.ToString(),
            string.Join(",", options.SpecificDiagnosticOptions.OrderBy(p => p.Key, StringComparer.Ordinal)
                .Select(p => p.Key + "=" + p.Value)),
        ]);

    /// <summary>
    /// Validates this implementation against frozen accepted anchor vectors
    /// using fixed inputs. Throws if an anchor does not reproduce.
    /// </summary>
    public static void SelfTest()
    {
        var source = new SourceIdentity("src/App.cs", "sha256:" + new string('0', 64), "Latest", "FEATURE,TRACE");
        var compctx1 = new CompilationDescriptor(Compctx1, "repo:test", "src/App.csproj", "App",
            "App|1.0.0.0||", "App.dll", "sha256:" + new string('a', 64), [source],
            ["file|/ref/System.Runtime.dll|sha256:" + new string('1', 64)]).ComputeDigest();
        const string expectedCompctx1 = "compctx1:sha256:71d89657b28013dc7e6fcf6f755980a4412451802437a85aea8e67a3fe4f1333";
        if (compctx1 != expectedCompctx1)
            throw new InvalidOperationException($"compctx1 anchor mismatch: {compctx1}");

        var compctx2 = new CompilationDescriptor(Compctx2, "repo:test", "src/App.csproj", "App",
            "App|1.0.0.0||", "App.dll", "sha256:" + new string('a', 64), [source],
            [VerifiedProjectReferenceItem("compctx2:sha256:" + new string('b', 64))]).ComputeDigest();
        const string expectedCompctx2 = "compctx2:sha256:9e55d2dbb920ed6a03935e115cf65d48fbd61f9185a7cd3030e03cd6fa6dd924";
        if (compctx2 != expectedCompctx2)
            throw new InvalidOperationException($"compctx2 anchor mismatch: {compctx2}");

        var usage4 = OccurrenceId(
            "experiment:andirit-31c33a9", "experiment.csharp.occurrences", "caller-unit", "Caller.cs",
            "sha256:2f7803daa5910bf2f5fb5927b2e9f999ebd1b7074340096f620df37278df484a",
            1, 72, 1, 77, "M:N.C.M",
            "asmctx3:sha256:632e7953365d1dbadbb55414b7cbcb991c9bc2653ec6cafb6f6d9d95a9c2032d");
        const string expectedUsage4 = "bd0487b749795a3e6b61028c7c9cf2d663530cae901ac6d9f35bc311536dc3af";
        if (usage4 != expectedUsage4)
            throw new InvalidOperationException($"usage4 anchor mismatch: {usage4}");
    }
}
