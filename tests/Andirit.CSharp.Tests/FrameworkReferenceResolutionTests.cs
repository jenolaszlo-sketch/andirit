using System.Security.Cryptography;
using System.Text;
using Andirit;
using Microsoft.CodeAnalysis;

namespace Andirit.CSharp.Tests;

/// <summary>
/// Target-framework reference-resolution controls for the bounded R2 correction.
/// </summary>
public sealed class FrameworkReferenceResolutionTests
{
    private static readonly CodeRepositoryId Repository = new("repo:framework-resolution");

    [Fact]
    public void Resolver_SelectsDistinctInstalledReferencePacks()
    {
        var frameworks = new[] { "net8.0", "net9.0", "net10.0", "netstandard2.1" };
        var sets = frameworks.Select(FrameworkReferenceResolver.Resolve).ToArray();
        Assert.All(sets, set =>
        {
            Assert.NotEmpty(set.PackFamily);
            Assert.NotEmpty(set.PackVersion);
            Assert.NotEmpty(set.References);
            Assert.All(set.References, reference =>
            {
                Assert.StartsWith("refctx1|", reference.Identity, StringComparison.Ordinal);
                Assert.DoesNotContain("\\", reference.Identity, StringComparison.Ordinal);
            });
        });
        Assert.Equal(sets.Length, sets.Select(set => set.ReferenceSetHash).Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(sets, set => set.PackFamily == "Microsoft.NETCore.App.Ref" && set.TargetFramework == "net8.0");
        Assert.Contains(sets, set => set.PackFamily == "Microsoft.NETCore.App.Ref" && set.TargetFramework == "net10.0");
        Assert.Contains(sets, set => set.PackFamily == "NETStandard.Library.Ref" && set.TargetFramework == "netstandard2.1");
    }

    [Theory]
    [InlineData("net8.0")]
    [InlineData("net9.0")]
    [InlineData("net10.0")]
    [InlineData("netstandard2.1")]
    public async Task SupportedFramework_CompilesAgainstItsOwnClosure(string targetFramework)
    {
        var context = Context(targetFramework, "namespace Probe; public static class P { public static object O = new(); }");
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
        Assert.True(analysis.Universe.IsComplete, string.Join(";", analysis.Universe.Refusals));
        var verified = analysis.Projects.Single().Verified!;
        var resolved = FrameworkReferenceResolver.Resolve(targetFramework);
        Assert.Equal(resolved.References.Select(reference => reference.Identity).ToArray(),
            verified.Descriptor.References.ToArray());
        Assert.Equal(resolved.References.Length, verified.Compilation.References.Count());
        Assert.Equal(targetFramework, analysis.Projects.Single().Project.TargetFramework);
    }

    [Fact]
    public async Task SupportedFrameworks_ProduceDistinctDescriptors()
    {
        const string source = "namespace Probe; public static class P { public static object O = new(); }";
        var net10 = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context("net10.0", source));
        var net8 = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context("net8.0", source));
        var netstandard = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context("netstandard2.1", source));
        Assert.True(net10.Universe.IsComplete);
        Assert.True(net8.Universe.IsComplete);
        Assert.True(netstandard.Universe.IsComplete);
        var descriptors = new[]
        {
            net10.Projects.Single().Verified!.Descriptor.DescriptorDigest,
            net8.Projects.Single().Verified!.Descriptor.DescriptorDigest,
            netstandard.Projects.Single().Verified!.Descriptor.DescriptorDigest,
        };
        Assert.Equal(3, descriptors.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task FrameworkApiAvailability_IsEnforcedByTheDeclaredTarget()
    {
        const string halfAndRange =
            "namespace Probe; public static class P { public static System.Half H = (System.Half)1.0; public static System.Range R = 0..1; }";
        var net8 = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context("net8.0", halfAndRange));
        Assert.True(net8.Universe.IsComplete, string.Join(";", net8.Universe.Refusals));

        var netstandard21 = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context("netstandard2.1", halfAndRange));
        Assert.False(netstandard21.Universe.IsComplete);
        Assert.Contains(netstandard21.Universe.Refusals,
            refusal => refusal.StartsWith("FrameworkReferenceErrors:", StringComparison.Ordinal));
        Assert.Null(netstandard21.Projects.Single().Verified);

        var netstandard20 = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context("netstandard2.0", halfAndRange));
        Assert.False(netstandard20.Universe.IsComplete);
        Assert.Contains(netstandard20.Universe.Refusals,
            refusal => refusal.StartsWith("UnsupportedTargetFramework:netstandard2.0", StringComparison.Ordinal));
        Assert.Null(netstandard20.Projects.Single().Verified);
    }

    [Theory]
    [InlineData("net6.0", "UnsupportedTargetFramework:net6.0")]
    [InlineData("net10.0-windows", "UnsupportedTargetFramework:net10.0-windows")]
    public async Task UnsupportedFramework_IsRefusedActionably(string targetFramework, string expected)
    {
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(
            Context(targetFramework, "namespace Probe; public static class P { }"));
        Assert.False(analysis.Universe.IsComplete);
        Assert.Contains(analysis.Universe.Refusals,
            refusal => refusal.StartsWith(expected, StringComparison.Ordinal));
        Assert.Null(analysis.Projects.Single().Verified);
    }

    [Fact]
    public async Task LooseSourceWithoutConfiguredTargetFramework_IsRefused()
    {
        var context = new CodeGraphPluginContext(Repository, null, new CodeIndexRunId("run:loose"),
            [new CodeGraphSource("Loose.cs",
                "sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("public class L { }"))),
                _ => ValueTask.FromResult<Stream>(new MemoryStream(
                    Encoding.UTF8.GetBytes("public class L { }"), writable: false)))]);
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
        Assert.False(analysis.Universe.IsComplete);
        Assert.Contains(analysis.Universe.Refusals, refusal => refusal == "MissingTargetFramework");
        Assert.Null(analysis.Projects.Single().Verified);
    }

    [Fact]
    public async Task ConfiguredLooseTargetFramework_IsSupported()
    {
        const string source = "public class L { public int V() => 1; }";
        var context = new CodeGraphPluginContext(Repository, null, new CodeIndexRunId("run:loose-configured"),
            [new CodeGraphSource("Loose.cs",
                "sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(source))),
                _ => ValueTask.FromResult<Stream>(new MemoryStream(
                    Encoding.UTF8.GetBytes(source), writable: false)))],
            settings: FrameworkTestSettings.Net10);
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
        Assert.True(analysis.Universe.IsComplete, string.Join(";", analysis.Universe.Refusals));
        Assert.NotNull(analysis.Projects.Single().Verified);
    }

    [Fact]
    public void EquivalentReferenceContentAtDifferentPaths_HasEqualIdentity()
    {
        var resolved = FrameworkReferenceResolver.Resolve("net10.0");
        var source = resolved.References.First(reference => reference.Identity.Contains("|System.Runtime|", StringComparison.Ordinal));
        var directory = Directory.CreateTempSubdirectory("andirit-pack-copy-");
        try
        {
            var first = Path.Combine(directory.FullName, "a.dll");
            var second = Path.Combine(directory.FullName, "b.dll");
            var path = resolved.References.First(reference => reference.Identity.Contains("|System.Runtime|", StringComparison.Ordinal));
            var originalPath = typeof(object).Assembly.Location;
            // Capture the same bytes from the selected pack through a copied path.
            var packFile = Path.Combine(resolved.PackDirectory, "ref", "net10.0", "System.Runtime.dll");
            File.Copy(packFile, first);
            File.Copy(packFile, second);
            var firstCapture = VerifiedMetadataReference.Capture(first);
            var secondCapture = VerifiedMetadataReference.Capture(second);
            Assert.Equal(firstCapture.Identity, secondCapture.Identity);
            Assert.DoesNotContain(first, firstCapture.Identity, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(second, secondCapture.Identity, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static CodeGraphPluginContext Context(string targetFramework, string source)
    {
        var project = $"<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>{targetFramework}</TargetFramework><AssemblyName>Probe</AssemblyName><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Compile Include=\"Probe.cs\" /></ItemGroup></Project>";
        return new(Repository, null, new CodeIndexRunId("run:" + targetFramework),
        [
            new CodeGraphSource("Probe/Probe.csproj",
                "sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(project))),
                _ => ValueTask.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(project), writable: false))),
            new CodeGraphSource("Probe/Probe.cs",
                "sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(source))),
                _ => ValueTask.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(source), writable: false))),
        ]);
    }
}


