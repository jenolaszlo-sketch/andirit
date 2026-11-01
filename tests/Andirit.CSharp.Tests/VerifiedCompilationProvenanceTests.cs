using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Andirit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Andirit.CSharp.Tests;

/// <summary>Focused construction tests; no occurrence extraction or storage publication.</summary>
public sealed class VerifiedCompilationProvenanceTests
{
    private const string RepositoryId = "fixture:s2.1";
    private static readonly CSharpCompilationOptions LibraryOptions = new(
        OutputKind.DynamicallyLinkedLibrary,
        optimizationLevel: OptimizationLevel.Release,
        nullableContextOptions: NullableContextOptions.Disable,
        deterministic: true,
        concurrentBuild: false);

    [Fact]
    public async Task SourceRead_BindsExactUtf8BytesToTextAndHash()
    {
        const string text = "namespace N; public sealed class A { public string Name => \"東京\"; }";
        var bytes = Encoding.UTF8.GetBytes(text);
        var source = await ReadSourceAsync("src/A.cs", bytes);

        Assert.Null(source.Refusal);
        Assert.Equal("src/A.cs", source.Path);
        Assert.Equal("sha256:" + Sha256(bytes), source.ContentHash);
        Assert.Equal(text, source.Text.ToString());
        Assert.NotNull(source.Text.Encoding);
    }

    [Fact]
    public async Task SourceRead_DecodesBomAndPreservesUnicodeEmptyAndLineEndings()
    {
        var cases = new[]
        {
            ("src/utf8.cs", "class A { }", Encoding.UTF8.GetBytes("class A { }")),
            ("src/utf8-bom.cs", "class B { }", new UTF8Encoding(true, true).GetPreamble().Concat(Encoding.UTF8.GetBytes("class B { }")).ToArray()),
            ("src/utf16-bom.cs", "class C { }", Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("class C { }")).ToArray()),
            ("src/unicode.cs", "class 東京 { string S = \"é🚀\"; }", Encoding.UTF8.GetBytes("class 東京 { string S = \"é🚀\"; }")),
            ("src/empty.cs", "", Array.Empty<byte>()),
            ("src/crlf.cs", "class A {\r\n}\r\n", Encoding.UTF8.GetBytes("class A {\r\n}\r\n")),
            ("src/lf.cs", "class A {\n}\n", Encoding.UTF8.GetBytes("class A {\n}\n"))
        };

        foreach (var (path, text, bytes) in cases)
        {
            var source = await ReadSourceAsync(path, bytes);
            Assert.Null(source.Refusal);
            Assert.Equal(text, source.Text.ToString());
            Assert.Equal("sha256:" + Sha256(bytes), source.ContentHash);
            Assert.False(string.IsNullOrWhiteSpace(source.EncodingName));
        }

        var crlf = await ReadSourceAsync(cases[5].Item1, cases[5].Item3);
        var lf = await ReadSourceAsync(cases[6].Item1, cases[6].Item3);
        Assert.NotEqual(crlf.ContentHash, lf.ContentHash);
        Assert.Contains("\r\n", crlf.Text.ToString());
        Assert.DoesNotContain("\r", lf.Text.ToString());
    }

    [Fact]
    public async Task SourceRead_RefusesInvalidUtf8NonShaAndMismatchedHash()
    {
        var invalidUtf8 = await ReadSourceAsync("src/invalid.cs", [0xC3, 0x28]);
        var bytes = Encoding.UTF8.GetBytes("class A {}");
        var nonSha = await ReadSourceAsync("src/A.cs", bytes, "md5:" + new string('a', 32));
        var mismatch = await ReadSourceAsync("src/A.cs", bytes, "sha256:" + new string('0', 64));

        Assert.False(string.IsNullOrWhiteSpace(invalidUtf8.Refusal));
        Assert.False(string.IsNullOrWhiteSpace(nonSha.Refusal));
        Assert.False(string.IsNullOrWhiteSpace(mismatch.Refusal));
    }

    [Fact]
    public async Task SourceRead_RetainsImmutableSnapshotIfBackingStreamChanges()
    {
        var original = Encoding.UTF8.GetBytes("class A { public int Value => 1; }");
        var current = original;
        var opens = 0;
        var planned = new CodeGraphSource(
            "src/A.cs",
            "sha256:" + Sha256(original),
            _ =>
            {
                Interlocked.Increment(ref opens);
                return ValueTask.FromResult<Stream>(new MemoryStream(current, writable: false));
            });

        var captured = await VerifiedCSharpSource.ReadAsync(planned, CancellationToken.None);
        current = Encoding.UTF8.GetBytes("class A { public int Value => 2; }");

        Assert.Null(captured.Refusal);
        Assert.Equal("class A { public int Value => 1; }", captured.Text.ToString());
        Assert.Equal("sha256:" + Sha256(original), captured.ContentHash);
        Assert.Equal(1, opens);
    }

    [Fact]
    public async Task SourceRead_CancellationThrows()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await VerifiedCSharpSource.ReadAsync(
                PlannedSource("src/A.cs", Encoding.UTF8.GetBytes("class A {}")),
                cancellation.Token));
    }

    [Fact]
    public async Task MetadataCapture_CapturedPeIdentityTracksBytesAtSamePath()
    {
        var directory = NewTemporaryDirectory();
        try
        {
            var path = Path.Combine(directory, "Same.dll");
            await File.WriteAllBytesAsync(path, EmitLibrary("public static int Value() => 1;"));
            var first = VerifiedMetadataReference.Capture(path, CancellationToken.None);
            var firstIdentity = first.Identity;

            await File.WriteAllBytesAsync(path, EmitLibrary("public static string Value() => \"changed\";"));
            var second = VerifiedMetadataReference.Capture(path, CancellationToken.None);

            Assert.StartsWith("file|", first.Identity);
            Assert.NotEqual(firstIdentity, second.Identity);
            Assert.Equal(firstIdentity, first.Identity);
            Assert.Equal(SpecialType.System_Int32, ReferencedValueType(ConsumerFor(first.Reference)).SpecialType);
            Assert.Equal(SpecialType.System_String, ReferencedValueType(ConsumerFor(second.Reference)).SpecialType);

            var platform = await CapturePlatformAsync();
            var callerSource = await ReadTextSourceAsync(
                "Consumer/Consumer.cs", "public sealed class Consumer { public object Read() => RefLib.Api.Value(); }");
            var firstConsumer = CreateVerified("Consumer/Consumer.csproj",
                Compile("Consumer", [Tree(callerSource)], [platform.Reference, first.Reference], LibraryOptions),
                [callerSource], [platform, first], []);
            var secondConsumer = CreateVerified("Consumer/Consumer.csproj",
                Compile("Consumer", [Tree(callerSource)], [platform.Reference, second.Reference], LibraryOptions),
                [callerSource], [platform, second], []);
            Assert.NotEqual(firstConsumer.Descriptor.DescriptorDigest, secondConsumer.Descriptor.DescriptorDigest);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task MetadataCapture_MissingImageAndCancellationThrow()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.dll");
        Assert.ThrowsAny<Exception>(() => VerifiedMetadataReference.Capture(missing, CancellationToken.None));

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() => VerifiedMetadataReference.Capture(typeof(object).Assembly.Location, cancellation.Token));
    }

    [Fact]
    public async Task VerifiedCompilation_RejectsTreeTextMismatchMissingOrExtraSnapshots()
    {
        var a = await ReadTextSourceAsync("src/A.cs", "public sealed class A {}");
        var b = await ReadTextSourceAsync("src/B.cs", "public sealed class B {}");
        var platform = await CapturePlatformAsync();

        var mismatched = Compile("A", [Tree("src/A.cs", "public sealed class A { int Changed; }")], [platform.Reference], LibraryOptions);
        var missing = Compile("AB", [Tree(a), Tree(b)], [platform.Reference], LibraryOptions);
        var extra = Compile("A", [Tree(a)], [platform.Reference], LibraryOptions);

        Assert.Throws<InvalidDataException>(() => CreateVerified("src/A.csproj", mismatched, [a], [platform], []));
        Assert.Throws<InvalidDataException>(() => CreateVerified("src/A.csproj", missing, [a], [platform], []));
        Assert.Throws<InvalidDataException>(() => CreateVerified("src/A.csproj", extra, [a, b], [platform], []));
    }

    [Fact]
    public async Task VerifiedCompilation_RejectsRefusedSource()
    {
        var refused = await ReadSourceAsync("src/A.cs", [0xC3, 0x28]);
        var platform = await CapturePlatformAsync();
        var compilation = Compile("A", [], [platform.Reference], LibraryOptions);

        Assert.False(string.IsNullOrWhiteSpace(refused.Refusal));
        Assert.Throws<InvalidDataException>(() => CreateVerified("src/A.csproj", compilation, [refused], [platform], []));
    }

    [Fact]
    public async Task VerifiedCompilation_SourceOptionsAndParseChangesChangeContext()
    {
        const string text = "#if FEATURE\npublic sealed class A { public int Value; }\n#else\npublic sealed class A { public string Value = \"\"; }\n#endif";
        var source = await ReadTextSourceAsync("src/A.cs", text);
        var platform = await CapturePlatformAsync();

        var baseline = CreateVerified("src/A.csproj",
            Compile("A", [Tree(source)], [platform.Reference], LibraryOptions), [source], [platform], []);
        var nullable = CreateVerified("src/A.csproj",
            Compile("A", [Tree(source)], [platform.Reference],
                LibraryOptions.WithNullableContextOptions(NullableContextOptions.Enable)),
            [source], [platform], []);
        var feature = CreateVerified("src/A.csproj",
            Compile("A", [Tree(source, ParseOptions("FEATURE"))], [platform.Reference], LibraryOptions),
            [source], [platform], []);

        Assert.Equal("compctx2", baseline.Descriptor.ContractVersion);
        Assert.NotEqual(baseline.Descriptor.OptionsDigest, nullable.Descriptor.OptionsDigest);
        Assert.NotEqual(baseline.Descriptor.DescriptorDigest, nullable.Descriptor.DescriptorDigest);
        Assert.Equal(source.Text.ToString(), feature.Compilation.SyntaxTrees.Single().GetText().ToString());
        Assert.NotEqual(baseline.Descriptor.DescriptorDigest, feature.Descriptor.DescriptorDigest);
    }

    [Fact]
    public async Task VerifiedCompilation_EachR2MaterialOptionChangesContext()
    {
        var source = await ReadTextSourceAsync("src/A.cs", "public sealed class A { public int Value; }");
        var platform = await CapturePlatformAsync();
        var baseline = CreateVerified("src/A.csproj",
            Compile("A", [Tree(source)], [platform.Reference], LibraryOptions), [source], [platform], []);
        var optionVariants = new (string Name, CSharpCompilationOptions Options)[]
        {
            ("OutputKind", LibraryOptions.WithOutputKind(OutputKind.ConsoleApplication)),
            ("NullableContextOptions", LibraryOptions.WithNullableContextOptions(NullableContextOptions.Enable)),
            ("AllowUnsafe", LibraryOptions.WithAllowUnsafe(true)),
            ("OptimizationLevel", LibraryOptions.WithOptimizationLevel(OptimizationLevel.Debug)),
            ("Platform", LibraryOptions.WithPlatform(Platform.X64)),
            ("CheckOverflow", LibraryOptions.WithOverflowChecks(true)),
            ("Deterministic", LibraryOptions.WithDeterministic(false)),
            ("WarningLevel", LibraryOptions.WithWarningLevel(1)),
            ("GeneralDiagnosticOption", LibraryOptions.WithGeneralDiagnosticOption(ReportDiagnostic.Error)),
            ("SpecificDiagnosticOptions", LibraryOptions.WithSpecificDiagnosticOptions(
                ImmutableDictionary<string, ReportDiagnostic>.Empty.Add("CS0168", ReportDiagnostic.Error)))
        };

        foreach (var (name, options) in optionVariants)
        {
            var variant = CreateVerified("src/A.csproj",
                Compile("A", [Tree(source)], [platform.Reference], options), [source], [platform], []);
            Assert.True(baseline.Descriptor.OptionsDigest != variant.Descriptor.OptionsDigest,
                $"R2 material option {name} did not change the options digest.");
            Assert.True(baseline.Descriptor.DescriptorDigest != variant.Descriptor.DescriptorDigest,
                $"R2 material option {name} did not change the context digest.");
        }
    }

    [Fact]
    public async Task VerifiedCompilation_LanguageVersionChangesContextButConcurrentBuildDoesNot()
    {
        var source = await ReadTextSourceAsync("src/A.cs", "public sealed class A { public int Value; }");
        var platform = await CapturePlatformAsync();
        var baseline = CreateVerified("src/A.csproj",
            Compile("A", [Tree(source)], [platform.Reference], LibraryOptions), [source], [platform], []);
        var olderLanguage = CreateVerified("src/A.csproj",
            Compile("A", [Tree(source, new CSharpParseOptions(
                LanguageVersion.CSharp11, DocumentationMode.Diagnose, SourceCodeKind.Regular))],
                [platform.Reference], LibraryOptions), [source], [platform], []);
        var concurrentBuild = CreateVerified("src/A.csproj",
            Compile("A", [Tree(source)], [platform.Reference], LibraryOptions.WithConcurrentBuild(true)),
            [source], [platform], []);

        Assert.NotEqual(baseline.Descriptor.DescriptorDigest, olderLanguage.Descriptor.DescriptorDigest);
        Assert.Equal(baseline.Descriptor.OptionsDigest, concurrentBuild.Descriptor.OptionsDigest);
        Assert.Equal(baseline.Descriptor.DescriptorDigest, concurrentBuild.Descriptor.DescriptorDigest);
    }

    [Fact]
    public async Task VerifiedCompilation_RejectsUnsupportedDocumentationScriptAndExtraOptions()
    {
        var source = await ReadTextSourceAsync("src/A.cs", "public sealed class A { public int Value; }");
        var platform = await CapturePlatformAsync();
        var documentationMode = Compile("A",
            [Tree(source, new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse, SourceCodeKind.Regular))],
            [platform.Reference], LibraryOptions);
        var scriptKind = Compile("A",
            [Tree(source, new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Diagnose, SourceCodeKind.Script))],
            [platform.Reference], LibraryOptions);
        var extraCompilationOption = Compile("A", [Tree(source)], [platform.Reference],
            LibraryOptions.WithMetadataImportOptions(MetadataImportOptions.All));

        Assert.Throws<InvalidDataException>(() => CreateVerified(
            "src/A.csproj", documentationMode, [source], [platform], []));
        Assert.Throws<InvalidDataException>(() => CreateVerified(
            "src/A.csproj", scriptKind, [source], [platform], []));
        Assert.Throws<InvalidDataException>(() => CreateVerified(
            "src/A.csproj", extraCompilationOption, [source], [platform], []));
    }

    [Fact]
    public async Task VerifiedCompilation_SourceBytesAndProjectUnitChangeContext()
    {
        var one = await ReadTextSourceAsync("src/A.cs", "public sealed class A { public int Value => 1; }");
        var two = await ReadTextSourceAsync("src/A.cs", "public sealed class A { public int Value => 2; }");
        var platform = await CapturePlatformAsync();

        var first = CreateVerified("src/One.csproj",
            Compile("A", [Tree(one)], [platform.Reference], LibraryOptions), [one], [platform], []);
        var changed = CreateVerified("src/One.csproj",
            Compile("A", [Tree(two)], [platform.Reference], LibraryOptions), [two], [platform], []);
        var otherUnit = CreateVerified("src/Two.csproj",
            Compile("A", [Tree(one)], [platform.Reference], LibraryOptions), [one], [platform], []);

        Assert.NotEqual(one.ContentHash, two.ContentHash);
        Assert.NotEqual(first.Descriptor.DescriptorDigest, changed.Descriptor.DescriptorDigest);
        Assert.NotEqual(first.Descriptor.DescriptorDigest, otherUnit.Descriptor.DescriptorDigest);
    }

    [Fact]
    public async Task VerifiedCompilation_ProjectSourceParseAndOptionChangesPropagateToCaller()
    {
        const string dependencyText = "#if FEATURE\nnamespace N; public static class Lib { public static int Get() => 1; }\n#else\nnamespace N; public static class Lib { public static string Get() => \"\"; }\n#endif";
        var source = await ReadTextSourceAsync("Lib/Lib.cs", dependencyText);
        var platform = await CapturePlatformAsync();
        var baseline = CreateVerified("Lib/Lib.csproj",
            Compile("Lib", [Tree(source)], [platform.Reference], LibraryOptions), [source], [platform], []);
        var symbols = CreateVerified("Lib/Lib.csproj",
            Compile("Lib", [Tree(source, ParseOptions("FEATURE"))], [platform.Reference], LibraryOptions),
            [source], [platform], []);
        var options = CreateVerified("Lib/Lib.csproj",
            Compile("Lib", [Tree(source)], [platform.Reference],
                LibraryOptions.WithNullableContextOptions(NullableContextOptions.Enable)),
            [source], [platform], []);

        var callerSource = await ReadTextSourceAsync(
            "App/App.cs", "namespace N; public static class App { public static object Read() => Lib.Get(); }");
        var callerA = CreateCaller(callerSource, platform, baseline);
        var callerB = CreateCaller(callerSource, platform, symbols);
        var callerC = CreateCaller(callerSource, platform, options);

        Assert.NotEqual(baseline.Descriptor.DescriptorDigest, symbols.Descriptor.DescriptorDigest);
        Assert.NotEqual(baseline.Descriptor.DescriptorDigest, options.Descriptor.DescriptorDigest);
        Assert.NotEqual(callerA.Descriptor.DescriptorDigest, callerB.Descriptor.DescriptorDigest);
        Assert.NotEqual(callerA.Descriptor.DescriptorDigest, callerC.Descriptor.DescriptorDigest);
        Assert.Contains("compilation-context|" + symbols.Descriptor.DescriptorDigest, callerB.Descriptor.References);
        Assert.Equal(SpecialType.System_Int32, GetReferencedMethod(callerB.Compilation, "N.Lib", "Get").ReturnType.SpecialType);
        Assert.Equal(SpecialType.System_String, GetReferencedMethod(callerA.Compilation, "N.Lib", "Get").ReturnType.SpecialType);
    }

    [Fact]
    public async Task VerifiedCompilation_TransitiveDependencyChangesPropagateToCaller()
    {
        var platform = await CapturePlatformAsync();
        var leaf1 = await ReadTextSourceAsync("Leaf/Leaf.cs", "namespace N; public static class Leaf { public static int Value() => 1; }");
        var leaf2 = await ReadTextSourceAsync("Leaf/Leaf.cs", "namespace N; public static class Leaf { public static int Value() => 2; }");
        var verifiedLeaf1 = CreateVerified("Leaf/Leaf.csproj",
            Compile("Leaf", [Tree(leaf1)], [platform.Reference], LibraryOptions), [leaf1], [platform], []);
        var verifiedLeaf2 = CreateVerified("Leaf/Leaf.csproj",
            Compile("Leaf", [Tree(leaf2)], [platform.Reference], LibraryOptions), [leaf2], [platform], []);

        var middle = await ReadTextSourceAsync(
            "Middle/Middle.cs", "namespace N; public static class Middle { public static int Read() => Leaf.Value(); }");
        var verifiedMiddle1 = CreateVerified("Middle/Middle.csproj",
            Compile("Middle", [Tree(middle)],
                [platform.Reference, verifiedLeaf1.Compilation.ToMetadataReference()], LibraryOptions),
            [middle], [platform], [verifiedLeaf1]);
        var verifiedMiddle2 = CreateVerified("Middle/Middle.csproj",
            Compile("Middle", [Tree(middle)],
                [platform.Reference, verifiedLeaf2.Compilation.ToMetadataReference()], LibraryOptions),
            [middle], [platform], [verifiedLeaf2]);

        var root = await ReadTextSourceAsync("Root/Root.cs", "namespace N; public static class Root { public static int Read() => Middle.Read(); }");
        var caller1 = CreateCaller(root, platform, verifiedMiddle1, "Root/Root.csproj");
        var caller2 = CreateCaller(root, platform, verifiedMiddle2, "Root/Root.csproj");

        Assert.NotEqual(verifiedLeaf1.Descriptor.DescriptorDigest, verifiedLeaf2.Descriptor.DescriptorDigest);
        Assert.NotEqual(verifiedMiddle1.Descriptor.DescriptorDigest, verifiedMiddle2.Descriptor.DescriptorDigest);
        Assert.NotEqual(caller1.Descriptor.DescriptorDigest, caller2.Descriptor.DescriptorDigest);
    }

    [Fact]
    public async Task VerifiedCompilation_BindsDependencyToExactLiveReference()
    {
        var platform = await CapturePlatformAsync();
        var dependencySource = await ReadTextSourceAsync(
            "Lib/Lib.cs", "namespace N; public static class Lib { public static int Get() => 1; }");
        var dependency = CreateVerified("Lib/Lib.csproj",
            Compile("Lib", [Tree(dependencySource)], [platform.Reference], LibraryOptions),
            [dependencySource], [platform], []);
        var callerSource = await ReadTextSourceAsync(
            "App/App.cs", "namespace N; public static class App { public static int Run() => Lib.Get(); }");
        var caller = CreateCaller(callerSource, platform, dependency);
        var liveReference = caller.Compilation.References.OfType<CompilationReference>().Single();

        Assert.Same(dependency.Compilation, liveReference.Compilation);
        Assert.Contains("compilation-context|" + dependency.Descriptor.DescriptorDigest, caller.Descriptor.References);
    }

    [Fact]
    public async Task VerifiedCompilation_RejectsSubstitutedDependencyInstanceEvenWhenDigestMatches()
    {
        var platform = await CapturePlatformAsync();
        var source = await ReadTextSourceAsync(
            "Lib/Lib.cs", "namespace N; public static class Lib { public static int Get() => 1; }");
        var dependencyA = CreateVerified("Lib/Lib.csproj",
            Compile("Lib", [Tree(source)], [platform.Reference], LibraryOptions), [source], [platform], []);
        var dependencyB = CreateVerified("Lib/Lib.csproj",
            Compile("Lib", [Tree(source)], [platform.Reference], LibraryOptions), [source], [platform], []);
        Assert.Equal(dependencyA.Descriptor.DescriptorDigest, dependencyB.Descriptor.DescriptorDigest);
        Assert.NotSame(dependencyA.Compilation, dependencyB.Compilation);

        var callerSource = await ReadTextSourceAsync(
            "App/App.cs", "namespace N; public static class App { public static int Run() => Lib.Get(); }");
        var callerCompilation = Compile(
            "App", [Tree(callerSource)],
            [platform.Reference, dependencyB.Compilation.ToMetadataReference()],
            LibraryOptions);

        Assert.Throws<InvalidDataException>(() => CreateVerified(
            "App/App.csproj", callerCompilation, [callerSource], [platform], [dependencyA]));
    }

    [Fact]
    public async Task VerifiedCompilation_RejectsMissingDependencyWithoutTextOnlyFallback()
    {
        var platform = await CapturePlatformAsync();
        var source = await ReadTextSourceAsync(
            "Lib/Lib.cs", "namespace N; public static class Lib { public static int Get() => 1; }");
        var dependency = CreateVerified("Lib/Lib.csproj",
            Compile("Lib", [Tree(source)], [platform.Reference], LibraryOptions), [source], [platform], []);
        var callerSource = await ReadTextSourceAsync(
            "App/App.cs", "namespace N; public static class App { public static int Run() => Lib.Get(); }");
        var callerCompilation = Compile(
            "App", [Tree(callerSource)],
            [platform.Reference, dependency.Compilation.ToMetadataReference()],
            LibraryOptions);

        Assert.Throws<InvalidDataException>(() => CreateVerified(
            "App/App.csproj", callerCompilation, [callerSource], [platform], []));
    }

    [Fact]
    public async Task VerifiedCompilation_RejectsNonDefaultProjectReferenceProperties()
    {
        var platform = await CapturePlatformAsync();
        var source = await ReadTextSourceAsync(
            "Lib/Lib.cs", "namespace N; public static class Lib { public static int Get() => 1; }");
        var dependency = CreateVerified("Lib/Lib.csproj",
            Compile("Lib", [Tree(source)], [platform.Reference], LibraryOptions), [source], [platform], []);
        var callerSource = await ReadTextSourceAsync(
            "App/App.cs", "namespace N; public static class App { public static int Run() => Lib.Get(); }");
        var aliased = dependency.Compilation.ToMetadataReference(aliases: ImmutableArray.Create("LibAlias"));
        var callerCompilation = Compile(
            "App", [Tree(callerSource)], [platform.Reference, aliased], LibraryOptions);

        Assert.Throws<InvalidDataException>(() => CreateVerified(
            "App/App.csproj", callerCompilation, [callerSource], [platform], [dependency]));
    }

    [Fact]
    public async Task VerifiedCompilation_ReferenceInventoryChangeChangesContext()
    {
        var source = await ReadTextSourceAsync("src/A.cs", "public sealed class A {}");
        var core = await CapturePlatformAsync();
        var console = VerifiedMetadataReference.Capture(typeof(Console).Assembly.Location, CancellationToken.None);
        var first = CreateVerified("src/A.csproj",
            Compile("A", [Tree(source)], [core.Reference], LibraryOptions), [source], [core], []);
        var second = CreateVerified("src/A.csproj",
            Compile("A", [Tree(source)], [core.Reference, console.Reference], LibraryOptions),
            [source], [core, console], []);

        Assert.NotEqual(first.Descriptor.References, second.Descriptor.References);
        Assert.NotEqual(first.Descriptor.DescriptorDigest, second.Descriptor.DescriptorDigest);
    }

    [Fact]
    public async Task VerifiedCompilation_SourceAndReferenceEnumerationOrderIsDeterministic()
    {
        var a = await ReadTextSourceAsync("src/A.cs", "public sealed class A {}");
        var b = await ReadTextSourceAsync("src/B.cs", "public sealed class B {}");
        var core = await CapturePlatformAsync();
        var console = VerifiedMetadataReference.Capture(typeof(Console).Assembly.Location, CancellationToken.None);

        var first = CreateVerified("src/App.csproj",
            Compile("App", [Tree(a), Tree(b)], [core.Reference, console.Reference], LibraryOptions),
            [a, b], [core, console], []);
        var second = CreateVerified("src/App.csproj",
            Compile("App", [Tree(b), Tree(a)], [console.Reference, core.Reference], LibraryOptions),
            [b, a], [console, core], []);

        Assert.Equal(first.Descriptor.Sources.ToArray(), second.Descriptor.Sources.ToArray());
        Assert.Equal(first.Descriptor.References.ToArray(), second.Descriptor.References.ToArray());
        Assert.Equal(first.Descriptor.DescriptorDigest, second.Descriptor.DescriptorDigest);
    }

    [Fact]
    public void Descriptor_Compctx1SyntheticR2TupleVectorMatchesIndependentOracle()
    {
        var descriptor = FixtureDescriptor(
            "compctx1",
            "file|/ref/System.Runtime.dll|sha256:" + new string('1', 64));
        const string expected = "compctx1:sha256:71d89657b28013dc7e6fcf6f755980a4412451802437a85aea8e67a3fe4f1333";

        Assert.Equal(expected, descriptor.ComputeDigest());
        Assert.Equal(expected, IndependentlyComputeDigest(
            "compctx1",
            "repo:test", "src/App.csproj", "App", "App|1.0.0.0||", "App.dll",
            "sha256:" + new string('a', 64),
            "src/App.cs|sha256:" + new string('0', 64) + "|Latest|FEATURE,TRACE",
            "file|/ref/System.Runtime.dll|sha256:" + new string('1', 64)));
        Assert.Equal(expected, (descriptor with { DescriptorDigest = expected }).Verified().DescriptorDigest);
    }

    [Fact]
    public void Descriptor_Compctx2UsesVersionedDomainAndCanonicalProjectReferenceItem()
    {
        var descriptor = FixtureDescriptor(
            "compctx2",
            "compilation-context|compctx2:sha256:" + new string('b', 64));
        const string expected = "compctx2:sha256:9e55d2dbb920ed6a03935e115cf65d48fbd61f9185a7cd3030e03cd6fa6dd924";

        Assert.Equal(expected, descriptor.ComputeDigest());
        Assert.Equal(expected, IndependentlyComputeDigest(
            "compctx2",
            "repo:test", "src/App.csproj", "App", "App|1.0.0.0||", "App.dll",
            "sha256:" + new string('a', 64),
            "src/App.cs|sha256:" + new string('0', 64) + "|Latest|FEATURE,TRACE",
            "compilation-context|compctx2:sha256:" + new string('b', 64)));
        Assert.Equal(expected, (descriptor with { DescriptorDigest = expected }).Verified().DescriptorDigest);
    }

    [Fact]
    public void Descriptor_RejectsForgedDigestAndUnsupportedVersion()
    {
        var descriptor = FixtureDescriptor(
            "compctx2",
            "compilation-context|compctx2:sha256:" + new string('b', 64));
        var forged = descriptor with { DescriptorDigest = "compctx2:sha256:" + new string('0', 64) };
        var unsupported = descriptor with { ContractVersion = "compctx3" };

        Assert.Throws<InvalidDataException>(() => forged.Verified());
        Assert.Throws<InvalidDataException>(() => unsupported.Verified());
    }

    [Fact]
    public async Task VerifiedCompilation_CancellationThrows()
    {
        var source = await ReadTextSourceAsync("src/A.cs", "public sealed class A {}");
        var platform = await CapturePlatformAsync();
        var compilation = Compile("A", [Tree(source)], [platform.Reference], LibraryOptions);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(() => VerifiedCSharpCompilation.Create(
            RepositoryId, "src/A.csproj", compilation, [source], [platform], [], cancellation.Token));
    }

    [Fact]
    public async Task VerifiedCompilation_LegacyDescriptorExistsOnlyWithoutProjectReferences()
    {
        var platform = await CapturePlatformAsync();
        var source = await ReadTextSourceAsync("src/A.cs", "public sealed class A {}");
        var standalone = CreateVerified("src/A.csproj",
            Compile("A", [Tree(source)], [platform.Reference], LibraryOptions), [source], [platform], []);
        var dependencySource = await ReadTextSourceAsync("Lib/Lib.cs", "namespace Lib; public static class Api { public static int M() => 1; }");
        var dependency = CreateVerified("Lib/Lib.csproj",
            Compile("Lib", [Tree(dependencySource)], [platform.Reference], LibraryOptions), [dependencySource], [platform], []);
        var callerSource = await ReadTextSourceAsync("App/App.cs", "public static class App { public static int M() => Lib.Api.M(); }");
        var callerCompilation = Compile(
            "App", [Tree(callerSource)],
            [platform.Reference, dependency.Compilation.ToMetadataReference()], LibraryOptions);
        var caller = CreateVerified("App/App.csproj", callerCompilation, [callerSource], [platform], [dependency]);

        Assert.Equal("compctx2", standalone.Descriptor.ContractVersion);
        Assert.NotNull(standalone.LegacyDescriptor);
        Assert.Equal("compctx1", standalone.LegacyDescriptor!.ContractVersion);
        Assert.Null(caller.LegacyDescriptor);
        Assert.Equal("compctx2", caller.Descriptor.ContractVersion);
    }

    [Fact]
    public async Task VerifiedCompilation_RejectsSelfDependencyCycle()
    {
        var platform = await CapturePlatformAsync();
        var source = await ReadTextSourceAsync("src/A.cs", "public sealed class A {}");
        var compilation = Compile("A", [Tree(source)], [platform.Reference], LibraryOptions);
        var verified = CreateVerified("src/A.csproj", compilation, [source], [platform], []);
        var selfReference = compilation.ToMetadataReference();
        var cyclic = compilation.WithReferences(compilation.References.Append(selfReference));

        Assert.Throws<InvalidDataException>(() => CreateVerified(
            "src/A.csproj", cyclic, [source], [platform], [verified]));
    }

    [Fact]
    public async Task VerifiedCompilation_ValidatesExpectedDescriptorAgainstConstructedInputs()
    {
        var platform = await CapturePlatformAsync();
        var source = await ReadTextSourceAsync("src/A.cs", "public sealed class A {}");
        var compilation = Compile("A", [Tree(source)], [platform.Reference], LibraryOptions);
        var verified = CreateVerified("src/A.csproj", compilation, [source], [platform], []);
        var otherUnit = CreateVerified("other/A.csproj", compilation, [source], [platform], []);

        verified.ValidateExpectedDescriptor(verified.Descriptor);
        Assert.Throws<InvalidDataException>(() => verified.ValidateExpectedDescriptor(otherUnit.Descriptor));
    }
    [Fact]
    public async Task VerifiedCompilation_RejectsSemanticCyclesAndConflictingDependencyRevisions()
    {
        var platform = await CapturePlatformAsync();
        var a = await ReadTextSourceAsync("A/A.cs", "public class A {}");
        var a0 = CreateVerified("A/A.csproj", Compile("A", [Tree(a)], [platform.Reference], LibraryOptions), [a], [platform], []);
        var b = await ReadTextSourceAsync("B/B.cs", "public class B {}");
        var b0 = CreateVerified("B/B.csproj", Compile("B", [Tree(b)], [platform.Reference, a0.Compilation.ToMetadataReference()], LibraryOptions),
            [b], [platform], [a0]);
        Assert.Throws<InvalidDataException>(() => CreateVerified("A/A.csproj",
            Compile("A", [Tree(a)], [platform.Reference, b0.Compilation.ToMetadataReference()], LibraryOptions), [a], [platform], [b0]));
        Assert.Throws<InvalidDataException>(() => CreateVerified(@"A\A.csproj",
            Compile("A", [Tree(a)], [platform.Reference, a0.Compilation.ToMetadataReference()], LibraryOptions), [a], [platform], [a0]));
        var aChanged = await ReadTextSourceAsync("A/A.cs", "public class A { int Changed; }");
        var a1 = CreateVerified("A/A.csproj", Compile("A", [Tree(aChanged)], [platform.Reference], LibraryOptions), [aChanged], [platform], []);
        var c = await ReadTextSourceAsync("C/C.cs", "public class C {}");
        var c0 = CreateVerified("C/C.csproj", Compile("C", [Tree(c)], [platform.Reference, a1.Compilation.ToMetadataReference()], LibraryOptions),
            [c], [platform], [a1]);
        var root = await ReadTextSourceAsync("Root/Root.cs", "public class Root {}");
        Assert.Throws<InvalidDataException>(() => CreateVerified("Root/Root.csproj",
            Compile("Root", [Tree(root)], [platform.Reference, b0.Compilation.ToMetadataReference(), c0.Compilation.ToMetadataReference()], LibraryOptions),
            [root], [platform], [b0, c0]));
    }

    [Fact]
    public async Task UniverseRejectsReusedLiveCompilationAcrossDifferentUnits()
    {
        var platform = await CapturePlatformAsync();
        var source = await ReadTextSourceAsync("Shared.cs", "public class Shared {}");
        var compilation = Compile("Shared", [Tree(source)], [platform.Reference], LibraryOptions);
        var first = CreateVerified("A.csproj", compilation, [source], [platform], []);
        var second = CreateVerified("B.csproj", compilation, [source], [platform], []);
        var scope = VerifiedCSharpScope.Declare(RepositoryId, [new("A.csproj", "A.csproj", ["Shared.cs"]), new("B.csproj", "B.csproj", ["Shared.cs"])]);
        Assert.Throws<InvalidDataException>(() => VerifiedCSharpUniverse.Create(RepositoryId, scope, [first, second]));
    }

    [Fact]
    public async Task MetadataProofRejectsUncapturedReferenceInstance()
    {
        var platform = await CapturePlatformAsync();
        var source = await ReadTextSourceAsync("A.cs", "public class A {}");
        var unrelated = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        var compilation = Compile("A", [Tree(source)], [unrelated], LibraryOptions);
        Assert.Throws<InvalidDataException>(() => CreateVerified("A.csproj", compilation, [source], [platform], []));
    }

    [Fact]
    public async Task SourceReadSupportsStrictBigEndianUtf16AndBothUtf32Boms()
    {
        const string text = "class Unicode { string Value = \"東京🚀\"; }";
        foreach (var encoding in new Encoding[] { new UnicodeEncoding(true, true, true),
                     new UTF32Encoding(false, true, true), new UTF32Encoding(true, true, true) })
        {
            var bytes = encoding.GetPreamble().Concat(encoding.GetBytes(text)).ToArray();
            var source = await ReadSourceAsync("Unicode.cs", bytes);
            Assert.Null(source.Refusal);
            Assert.Equal(text, source.Text.ToString());
            Assert.Equal("sha256:" + Sha256(bytes), source.ContentHash);
        }
        var invalid = await ReadSourceAsync("Invalid.cs", [0xff, 0xfe, 0x00, 0xd8]);
        Assert.NotNull(invalid.Refusal);
    }

    private static CompilationProvenanceDescriptor FixtureDescriptor(string version, string reference) =>
        new(
            ContractVersion: version,
            RepositoryId: "repo:test",
            ProjectUnitId: "src/App.csproj",
            AssemblyName: "App",
            MetadataIdentity: "App|1.0.0.0||",
            ModuleName: "App.dll",
            OptionsDigest: "sha256:" + new string('a', 64),
            Sources: ImmutableArray.Create(new CompilationSourceIdentity(
                "src/App.cs",
                "sha256:" + new string('0', 64),
                "Latest",
                "FEATURE,TRACE")),
            References: ImmutableArray.Create(reference),
            DescriptorDigest: string.Empty);

    private static string IndependentlyComputeDigest(string version, params string[] fields)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        AppendPart(version + "-context");
        AppendPart(version);
        foreach (var field in fields)
            AppendPart(field);
        return version + ":sha256:" + Convert.ToHexStringLower(hash.GetHashAndReset());

        void AppendPart(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            Span<byte> length = stackalloc byte[4];
            System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
            hash.AppendData(length);
            hash.AppendData(bytes);
        }
    }

    private static async Task<VerifiedCSharpSource> ReadTextSourceAsync(string path, string text) =>
        await ReadSourceAsync(path, Encoding.UTF8.GetBytes(text));

    private static async Task<VerifiedCSharpSource> ReadSourceAsync(
        string path,
        byte[] bytes,
        string? declaredHash = null) =>
        await VerifiedCSharpSource.ReadAsync(
            PlannedSource(path, bytes, declaredHash),
            CancellationToken.None);

    private static CodeGraphSource PlannedSource(string path, byte[] bytes, string? declaredHash = null) =>
        new(
            path,
            declaredHash ?? "sha256:" + Sha256(bytes),
            _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false)));

    private static string Sha256(byte[] bytes) =>
        Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static CSharpParseOptions ParseOptions(params string[] symbols) =>
        new(LanguageVersion.Latest, DocumentationMode.Diagnose, SourceCodeKind.Regular, symbols);

    private static SyntaxTree Tree(VerifiedCSharpSource source, CSharpParseOptions? options = null) =>
        CSharpSyntaxTree.ParseText(source.Text, options ?? ParseOptions(), source.Path);

    private static SyntaxTree Tree(string path, string text) =>
        CSharpSyntaxTree.ParseText(text, ParseOptions(), path);

    private static CSharpCompilation Compile(
        string assembly,
        IEnumerable<SyntaxTree> trees,
        IEnumerable<MetadataReference> references,
        CSharpCompilationOptions options) =>
        CSharpCompilation.Create(assembly, trees, references, options);

    private static VerifiedCSharpCompilation CreateVerified(
        string projectUnit,
        CSharpCompilation compilation,
        IReadOnlyCollection<VerifiedCSharpSource> sources,
        IReadOnlyCollection<VerifiedMetadataReference> metadataReferences,
        IReadOnlyCollection<VerifiedCSharpCompilation> dependencies) =>
        VerifiedCSharpCompilation.Create(
            RepositoryId, projectUnit, compilation, sources, metadataReferences, dependencies,
            CancellationToken.None);

    private static VerifiedCSharpCompilation CreateCaller(
        VerifiedCSharpSource source,
        VerifiedMetadataReference platform,
        VerifiedCSharpCompilation dependency,
        string projectUnit = "App/App.csproj")
    {
        var compilation = Compile(
            "App",
            [Tree(source)],
            [platform.Reference, dependency.Compilation.ToMetadataReference()],
            LibraryOptions);
        return CreateVerified(projectUnit, compilation, [source], [platform], [dependency]);
    }

    private static Task<VerifiedMetadataReference> CapturePlatformAsync() =>
        Task.FromResult(VerifiedMetadataReference.Capture(typeof(object).Assembly.Location, CancellationToken.None));

    private static ITypeSymbol ReferencedValueType(CSharpCompilation consumer)
    {
        var type = Assert.IsAssignableFrom<INamedTypeSymbol>(consumer.GetTypeByMetadataName("RefLib.Api"));
        return Assert.IsAssignableFrom<IMethodSymbol>(type.GetMembers("Value").Single()).ReturnType;
    }

    private static IMethodSymbol GetReferencedMethod(CSharpCompilation compilation, string typeName, string method)
    {
        var type = Assert.IsAssignableFrom<INamedTypeSymbol>(compilation.GetTypeByMetadataName(typeName));
        return Assert.IsAssignableFrom<IMethodSymbol>(type.GetMembers(method).Single());
    }

    private static CSharpCompilation ConsumerFor(MetadataReference reference) =>
        Compile(
            "Consumer",
            [CSharpSyntaxTree.ParseText("public sealed class Consumer {}", ParseOptions())],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location), reference],
            LibraryOptions);

    private static byte[] EmitLibrary(string member)
    {
        var tree = CSharpSyntaxTree.ParseText(
            "namespace RefLib; public static class Api { " + member + " }",
            ParseOptions(),
            "RefLib.cs");
        var compilation = Compile(
            "Same",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            LibraryOptions);
        using var output = new MemoryStream();
        var result = compilation.Emit(output);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        return output.ToArray();
    }

    private static string NewTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "Andirit-S2.1-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
