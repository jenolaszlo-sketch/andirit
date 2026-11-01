using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Andirit;

/// <summary>An owned snapshot; never reloads the source after acquisition.</summary>
internal sealed class VerifiedCSharpSource
{
    private readonly ReadOnlyMemory<byte> bytes;
    private VerifiedCSharpSource(string path, ReadOnlyMemory<byte> bytes, string hash,
        SourceText text, string encoding, string? refusal)
    {
        Path = path; this.bytes = bytes; ContentHash = hash; Text = text;
        EncodingName = encoding; Refusal = refusal;
    }
    internal string Path { get; }
    internal string ContentHash { get; }
    internal SourceText Text { get; }
    internal string EncodingName { get; }
    internal string? Refusal { get; }
    internal int ByteLength => bytes.Length;

    internal static async ValueTask<VerifiedCSharpSource> ReadAsync(
        CodeGraphSource source, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReadOnlyMemory<byte> content;
        string hash;
        if (source.MaterializedBytes is { } retained)
        {
            // Only lifecycle-owned buffers can enter this internal handoff.
            content = retained;
            hash = source.MaterializedByteHash!;
        }
        else
        {
            await using var input = await source.OpenReadAsync(cancellationToken).ConfigureAwait(false);
            using var output = new MemoryStream();
            var buffer = new byte[81920];
            while (true)
            {
                int count = await input.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (count == 0) break;
                if (output.Length + count > 16 * 1024 * 1024)
                    throw new InvalidDataException("Standalone C# source exceeds the 16 MiB acquisition bound.");
                output.Write(buffer, 0, count);
            }
            content = output.ToArray();
            hash = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(content.Span));
        }
        string? refusal = null;
        if (!source.ContentHash.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
            refusal = "UnsupportedSourceHash:" + source.Path;
        else if (!string.Equals(hash, source.ContentHash, StringComparison.OrdinalIgnoreCase))
            refusal = "SourceHashMismatch:" + source.Path;

        Encoding encoding = new UTF8Encoding(false, true);
        int skip = 0;
        // UTF-32 must be checked before its UTF-16 prefix.
        if (content.Span.StartsWith(new byte[] { 0xff, 0xfe, 0, 0 }))
        { encoding = new UTF32Encoding(false, true, true); skip = 4; }
        else if (content.Span.StartsWith(new byte[] { 0, 0, 0xfe, 0xff }))
        { encoding = new UTF32Encoding(true, true, true); skip = 4; }
        else if (content.Span.StartsWith(new byte[] { 0xff, 0xfe }))
        { encoding = new UnicodeEncoding(false, true, true); skip = 2; }
        else if (content.Span.StartsWith(new byte[] { 0xfe, 0xff }))
        { encoding = new UnicodeEncoding(true, true, true); skip = 2; }
        else if (content.Span.StartsWith(new byte[] { 0xef, 0xbb, 0xbf })) skip = 3;
        string text;
        try { text = encoding.GetString(content.Span[skip..]); }
        catch (DecoderFallbackException)
        {
            refusal ??= "InvalidSourceEncoding:" + source.Path;
            // Preserve historical graph decoding, but never verify this input.
            using var fallback = new StreamReader(new MemoryStream(content.ToArray()), Encoding.UTF8, true);
            text = fallback.ReadToEnd();
        }
        cancellationToken.ThrowIfCancellationRequested();
        return new(source.Path, content, hash, SourceText.From(text, encoding),
            encoding.WebName + (skip == 0 ? ":no-bom" : ":bom"), refusal);
    }
}
