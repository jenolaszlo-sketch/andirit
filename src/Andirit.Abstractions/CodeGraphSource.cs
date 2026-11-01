namespace Andirit;

/// <summary>Provides repeatable access to one selected repository source.</summary>
public sealed class CodeGraphSource
{
    private readonly Func<CancellationToken, ValueTask<Stream>> _openReadAsync;

    public CodeGraphSource(
        string path,
        string contentHash,
        Func<CancellationToken, ValueTask<Stream>> openReadAsync)
    {
        Path = ContractValue.RelativePath(path, nameof(path));
        ContentHash = ContractValue.Identifier(
            contentHash,
            nameof(contentHash));
        _openReadAsync = openReadAsync ??
            throw new ArgumentNullException(nameof(openReadAsync));
    }

    // This handoff is available only to the lifecycle and C# implementation.
    // The lifecycle retains ownership and never exposes a writable stream/array publicly.
    internal ReadOnlyMemory<byte>? MaterializedBytes { get; private init; }
    internal string? MaterializedByteHash { get; private init; }
    internal static CodeGraphSource FromMaterialized(string path, string contentHash,
        byte[] buffer, int length, string byteHash) => new(path, contentHash, cancellationToken =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new ValueTask<Stream>(new MemoryStream(buffer, 0, length, writable: false, publiclyVisible: false));
        }) { MaterializedBytes = buffer.AsMemory(0, length), MaterializedByteHash = byteHash };

    public string Path { get; }
    public string ContentHash { get; }

    public async ValueTask<Stream> OpenReadAsync(
        CancellationToken cancellationToken = default)
    {
        var stream = await _openReadAsync(cancellationToken)
            .ConfigureAwait(false);
        if (stream is null)
        {
            throw new InvalidOperationException(
                $"The source reader for '{Path}' returned null.");
        }

        if (!stream.CanRead)
        {
            await stream.DisposeAsync().ConfigureAwait(false);
            throw new InvalidOperationException(
                $"The source reader for '{Path}' returned an unreadable stream.");
        }

        return stream;
    }
}
