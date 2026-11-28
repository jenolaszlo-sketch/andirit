using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Andirit;
using Andirit.CSharp;
var cases = new[] { "net10.0", "net8.0", "netstandard2.0" };
var results = new List<object>();
foreach (var tfm in cases)
{
    var project = $"<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>{tfm}</TargetFramework><AssemblyName>Probe</AssemblyName><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Compile Include=\"Probe.cs\" /></ItemGroup></Project>";
    const string source = "namespace Probe; public static class P { public static System.Type T = typeof(object); public static string S = string.Empty; public static System.Half H = (System.Half)1.0; public static System.Range R = 0..1; }";
    var sources = new[]
    {
        new CodeGraphSource("Probe/Probe.csproj", "sha256:" + Hex(Encoding.UTF8.GetBytes(project)), _ => ValueTask.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(project), false))),
        new CodeGraphSource("Probe/Probe.cs", "sha256:" + Hex(Encoding.UTF8.GetBytes(source)), _ => ValueTask.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(source), false))),
    };
    var context = new CodeGraphPluginContext(new CodeRepositoryId("repo:framework-probe"), null, new CodeIndexRunId("run:" + tfm), sources);
    var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
    var verified = analysis.Projects.Single().Verified!;
    results.Add(new
    {
        targetFramework = tfm,
        complete = analysis.Universe.IsComplete,
        descriptor = verified.Descriptor.DescriptorDigest,
        options = verified.Descriptor.OptionsDigest,
        referenceCount = verified.Descriptor.References.Length,
        referenceNames = verified.Descriptor.References.Select(r => r.Split('|')[1]).ToArray(),
        runtimeDirectory = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(),
        frameworkDescription = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
        compilationReferenceCount = verified.Compilation.References.Count(),
    });
}
var json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
File.WriteAllText(args[0], json + "\n");
Console.WriteLine(json);
static string Hex(byte[] b) => Convert.ToHexStringLower(SHA256.HashData(b));




