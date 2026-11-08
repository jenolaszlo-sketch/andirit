using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Andirit;

/// <summary>
/// Opaque, versioned, stateless occurrence-continuation token codec (S2.4 D1).
/// A token is base64url of a canonical JSON envelope carrying the query binding,
/// the publication binding and the last-emitted total-order cursor. Tokens are
/// never persisted and require no server-side session; the reader re-validates
/// every token against trusted state (the current publication and the target
/// index) rather than trusting caller-supplied token fields. The <c>sum</c>
/// field is an unkeyed integrity checksum (tamper-evident against accidental
/// corruption), not cryptographic authentication.
/// </summary>
internal static class CodeOccurrenceContinuation
{
    internal const int Version = 1;
    internal const string TargetMode = "target";
    internal const string Ascending = "asc";

    /// <summary>Maximum accepted token length in characters (bounded decode).</summary>
    internal const int MaxTokenChars = 1024;

    /// <summary>Maximum accepted decoded envelope size in bytes.</summary>
    internal const int MaxEnvelopeBytes = 768;

    private static readonly string[] Fields =
    [
        "v", "repo", "mode", "target", "pubRun", "pubId", "dir",
        "afterPath", "afterStartLine", "afterStartColumn", "afterEndLine", "afterEndColumn", "afterId", "size"
    ];

    /// <summary>The decoded, validated cursor carried by a continuation token.</summary>
    internal readonly record struct Envelope(
        string RepositoryId,
        string Mode,
        string TargetId,
        string PublicationRun,
        string PublicationIdentity,
        string Direction,
        string AfterPath,
        int AfterStartLine,
        int AfterStartColumn,
        int AfterEndLine,
        int AfterEndColumn,
        string AfterId,
        int Size);

    internal static string Encode(in Envelope envelope)
    {
        var json = new JsonObject
        {
            ["v"] = Version,
            ["repo"] = envelope.RepositoryId,
            ["mode"] = envelope.Mode,
            ["target"] = envelope.TargetId,
            ["pubRun"] = envelope.PublicationRun,
            ["pubId"] = envelope.PublicationIdentity,
            ["dir"] = envelope.Direction,
            ["afterPath"] = envelope.AfterPath,
            ["afterStartLine"] = envelope.AfterStartLine,
            ["afterStartColumn"] = envelope.AfterStartColumn,
            ["afterEndLine"] = envelope.AfterEndLine,
            ["afterEndColumn"] = envelope.AfterEndColumn,
            ["afterId"] = envelope.AfterId,
            ["size"] = envelope.Size,
        };
        json["sum"] = Checksum(envelope);
        var bytes = Encoding.UTF8.GetBytes(json.ToJsonString());
        return Base64UrlEncode(bytes);
    }

    /// <summary>
    /// Strictly decodes and validates a token. Returns false for any malformed,
    /// altered, oversized, non-canonical or version-incompatible token. Binding to
    /// the caller's repository/target/publication/page size is checked by the
    /// reader against trusted state, not here.
    /// </summary>
    internal static bool TryDecode(string? token, out Envelope envelope)
    {
        envelope = default;
        if (string.IsNullOrEmpty(token) || token.Length > MaxTokenChars)
            return false;
        if (!Base64UrlTryDecode(token, out var bytes) || bytes.Length is 0 or > MaxEnvelopeBytes)
            return false;
        // D1: a non-canonical encoding (padding, standard base64 alphabet, or any
        // variant that is not the exact canonical base64url form) is invalid.
        if (!string.Equals(Base64UrlEncode(bytes), token, StringComparison.Ordinal))
            return false;

        JsonObject json;
        try
        {
            // Duplicate envelope keys and malformed JSON are rejected here. Note that
            // JsonObject detects duplicate property names lazily, on Count/enumeration,
            // so materialization and membership checks must stay inside this boundary:
            // a duplicate key would otherwise surface as an unhandled ArgumentException.
            if (JsonNode.Parse(bytes) is not JsonObject parsed || parsed.Count != Fields.Length + 1)
                return false;
            foreach (var field in Fields)
            {
                if (!parsed.ContainsKey(field))
                    return false;
            }
            if (!parsed.ContainsKey("sum"))
                return false;
            json = parsed;
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException)
        {
            // Malformed or duplicated caller-controlled envelope.
            return false;
        }

        if (!TryInt(json, "v", out var version) || version != Version)
            return false;
        if (!TryString(json, "repo", out var repositoryId) ||
            !TryString(json, "mode", out var mode) || mode != TargetMode ||
            !TryString(json, "target", out var targetId) ||
            !TryString(json, "pubRun", out var publicationRun) ||
            !TryString(json, "pubId", out var publicationIdentity) ||
            !TryString(json, "dir", out var direction) || direction != Ascending ||
            !TryString(json, "afterPath", out var afterPath) ||
            !TryInt(json, "afterStartLine", out var afterStartLine) ||
            !TryInt(json, "afterStartColumn", out var afterStartColumn) ||
            !TryInt(json, "afterEndLine", out var afterEndLine) ||
            !TryInt(json, "afterEndColumn", out var afterEndColumn) ||
            !TryString(json, "afterId", out var afterId) ||
            !TryInt(json, "size", out var size) ||
            !TryString(json, "sum", out var sum))
            return false;

        var candidate = new Envelope(
            repositoryId, mode, targetId, publicationRun, publicationIdentity, direction,
            afterPath, afterStartLine, afterStartColumn, afterEndLine, afterEndColumn, afterId, size);
        if (!string.Equals(sum, Checksum(candidate), StringComparison.Ordinal))
            return false;

        envelope = candidate;
        return true;
    }

    // D1: a cursor that is not a well-formed sort key (or an out-of-range page
    // size) is invalid continuation. Checked by the reader against trusted
    // publication state (after publication binding) so a foreign-publication token
    // still reports publication staleness rather than a structural refusal.
    internal static bool IsWellFormedCursor(in Envelope envelope)
    {
        if (string.IsNullOrEmpty(envelope.AfterPath))
            return false;
        if (!CodeOccurrenceIdentity.IsCanonicalOccurrence(envelope.AfterId))
            return false;
        if (envelope.AfterStartLine < 1 || envelope.AfterStartColumn < 1 ||
            envelope.AfterEndLine < envelope.AfterStartLine || envelope.AfterEndColumn < 1)
            return false;
        if (envelope.AfterEndLine == envelope.AfterStartLine && envelope.AfterEndColumn < envelope.AfterStartColumn)
            return false;
        return envelope.Size >= 1 && envelope.Size <= CodeOccurrenceQuery.AbsoluteMaxPageSize;
    }

    private static bool TryString(JsonObject json, string name, out string value)
    {
        value = string.Empty;
        if (json[name] is not JsonValue node || !node.TryGetValue(out string? text) || text is null)
            return false;
        value = text;
        return true;
    }

    private static bool TryInt(JsonObject json, string name, out int value)
    {
        value = 0;
        return json[name] is JsonValue node && node.TryGetValue(out value);
    }

    // Length-prefixed SHA-256 over the canonical field order; first 16 hex chars.
    private static string Checksum(in Envelope envelope)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> length = stackalloc byte[4];
        static void Append(IncrementalHash hash, Span<byte> length, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
            hash.AppendData(length);
            hash.AppendData(bytes);
        }

        Append(hash, length, "andirit-occurrence-continuation");
        Append(hash, length, Version.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Append(hash, length, envelope.RepositoryId);
        Append(hash, length, envelope.Mode);
        Append(hash, length, envelope.TargetId);
        Append(hash, length, envelope.PublicationRun);
        Append(hash, length, envelope.PublicationIdentity);
        Append(hash, length, envelope.Direction);
        Append(hash, length, envelope.AfterPath);
        Append(hash, length, envelope.AfterStartLine.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Append(hash, length, envelope.AfterStartColumn.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Append(hash, length, envelope.AfterEndLine.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Append(hash, length, envelope.AfterEndColumn.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Append(hash, length, envelope.AfterId);
        Append(hash, length, envelope.Size.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return Convert.ToHexStringLower(hash.GetHashAndReset())[..16];
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static bool Base64UrlTryDecode(string token, out byte[] bytes)
    {
        bytes = [];
        var padded = token.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 0: break;
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
            default: return false;
        }

        try
        {
            bytes = Convert.FromBase64String(padded);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
