using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Andirit;

internal sealed record CompilationSourceIdentity(
    string Path, string ContentHash, string LanguageVersion, string PreprocessorSymbols)
{
    internal string CanonicalForm => string.Join("|", Path, ContentHash, LanguageVersion, PreprocessorSymbols);
}

// Recomputable data is deliberately separate from a privately constructed live proof.
internal sealed record CompilationProvenanceDescriptor(
    string ContractVersion, string RepositoryId, string ProjectUnitId, string AssemblyName,
    string MetadataIdentity, string ModuleName, string OptionsDigest,
    ImmutableArray<CompilationSourceIdentity> Sources, ImmutableArray<string> References,
    string DescriptorDigest)
{
    internal string ComputeDigest()
    {
        if (ContractVersion is not ("compctx1" or "compctx2"))
            throw new InvalidDataException("UnsupportedContextVersion");
        return ContractVersion + ":sha256:" + ProvenanceHash.Hash(ContractVersion + "-context",
            [ContractVersion, RepositoryId, ProjectUnitId, AssemblyName, MetadataIdentity,
             ModuleName, OptionsDigest, .. Sources.Select(s => s.CanonicalForm), .. References]);
    }
    internal CompilationProvenanceDescriptor Verified()
    {
        if (ComputeDigest() != DescriptorDigest)
            throw new InvalidDataException("Compilation descriptor digest does not recompute.");
        return this;
    }
}

internal static class ProvenanceHash
{
    internal static string Hash(string domain, IEnumerable<string> parts)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        void Append(string value)
        {
            byte[] data = Encoding.UTF8.GetBytes(value);
            Span<byte> length = stackalloc byte[4];
            BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
            hash.AppendData(length); hash.AppendData(data);
        }
        Append(domain);
        foreach (string part in parts) Append(part);
        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }
    internal static string Metadata(AssemblyIdentity identity) => string.Join("|", identity.Name,
        identity.Version.ToString(), identity.CultureName ?? string.Empty,
        identity.PublicKeyToken.IsDefaultOrEmpty ? string.Empty : Convert.ToHexStringLower(identity.PublicKeyToken.AsSpan()));
    internal static string Options(CSharpCompilationOptions options) => Hash("compctx1-options",
        [options.OutputKind.ToString(), options.NullableContextOptions.ToString(), options.AllowUnsafe.ToString(),
         options.OptimizationLevel.ToString(), options.Platform.ToString(), options.CheckOverflow.ToString(),
         options.Deterministic.ToString(), options.WarningLevel.ToString(CultureInfo.InvariantCulture),
         options.GeneralDiagnosticOption.ToString(), string.Join(",", options.SpecificDiagnosticOptions
            .OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => p.Key + "=" + p.Value))]);
}
