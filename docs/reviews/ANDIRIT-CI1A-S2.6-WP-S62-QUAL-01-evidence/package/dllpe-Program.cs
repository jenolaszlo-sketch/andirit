// Reviewer PE check: locate every byte region where the two package DLLs differ and
// attribute it to the PE structures a deterministic build derives from the source hash.
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;

if (args.Length != 2)
{
    Console.Error.WriteLine("usage: dllpe <base.dll> <impl.dll>");
    return 2;
}

static byte[] Read(string p) => File.ReadAllBytes(p);

var b = Read(args[0]);
var i = Read(args[1]);

Console.WriteLine($"baseline       : bytes={b.Length} sha256={Convert.ToHexString(SHA256.HashData(b))}");
Console.WriteLine($"implementation : bytes={i.Length} sha256={Convert.ToHexString(SHA256.HashData(i))}");

// contiguous differing regions
var regions = new List<(int Start, int End)>();
int? start = null;
for (var n = 0; n < Math.Min(b.Length, i.Length); n++)
{
    if (b[n] != i[n])
    {
        if (start is null) start = n;
    }
    else if (start is not null)
    {
        regions.Add((start.Value, n));
        start = null;
    }
}
if (start is not null) regions.Add((start.Value, Math.Min(b.Length, i.Length)));
Console.WriteLine($"differing regions: {regions.Count}");

foreach (var pe in new[] { args[0], args[1] })
{
    using var stream = File.OpenRead(pe);
    using var reader = new PEReader(stream);
    var header = reader.PEHeaders.PEHeader;
    var debugStart = header.DebugTableDirectory.RelativeVirtualAddress;
    var debugSize = header.DebugTableDirectory.Size;
    // map RVA->file offset using the section table
    string Where(int offset)
    {
        foreach (var s in reader.PEHeaders.SectionHeaders)
        {
            if (offset >= s.PointerToRawData && offset < s.PointerToRawData + s.SizeOfRawData)
            {
                return $"{s.Name}+0x{offset - s.PointerToRawData:X}";
            }
        }
        return $"file+0x{offset:X}";
    }
    Console.WriteLine($"-- {Path.GetFileName(Path.GetDirectoryName(pe)) ?? pe}");
    foreach (var (s, e) in regions)
    {
        Console.WriteLine($"   0x{s:X5}-0x{e:X5} ({e - s} bytes) in {Where(s)}");
    }
}
return 0;