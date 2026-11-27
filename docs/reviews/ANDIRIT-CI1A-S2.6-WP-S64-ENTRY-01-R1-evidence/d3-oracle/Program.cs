using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Independent D3 graph oracle. It re-derives source facts from Roslyn using the
// documented legacy identity recipes; it does not call the production extractor.
if (args.Length != 2)
{
    Console.Error.WriteLine("usage: D3Oracle <corpus-root> <output.json>");
    return 2;
}

var root = Path.GetFullPath(args[0]);
var model = CorpusModel.Load(root);
model.Build();
var nodes = model.BuildNodes();
var edges = model.BuildEdges(nodes);
var output = new
{
    corpusRoot = root.Replace('\\', '/'),
    runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
    projects = model.Projects.Select(project => new
    {
        path = project.Path,
        assembly = project.AssemblyName,
        sources = project.SourcePaths,
        references = project.ProjectReferences,
        defines = project.Defines
    }).ToArray(),
    nodes = nodes.Values.OrderBy(node => node.Id, StringComparer.Ordinal).ToArray(),
    edges = edges.Values.OrderBy(edge => edge.Id, StringComparer.Ordinal).ToArray(),
    counts = new
    {
        projects = model.Projects.Count,
        nodes = nodes.Count,
        edges = edges.Count,
        byKind = edges.Values.GroupBy(edge => edge.Kind)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count())
    }
};
var json = JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine(json);
await File.WriteAllTextAsync(args[1], json);
return 0;

internal sealed record OracleNode(
    string Id, string Category, string Canonical, string Kind,
    string Project, string? Path, string Name, string QualifiedName);

internal sealed record OracleEdge(
    string Id, string Kind, string Source, string Target, string Quality,
    string? Discriminator, string? Path, int StartLine, int StartColumn,
    int EndLine, int EndColumn);

internal sealed class ProjectInfo
{
    public required string Path { get; init; }
    public required string FullPath { get; init; }
    public required string AssemblyName { get; init; }
    public required List<string> SourcePaths { get; init; }
    public required List<string> SourceFullPaths { get; init; }
    public required List<string> ProjectReferences { get; init; }
    public required List<string> Defines { get; init; }
    public required List<(string Name, string Version)> Packages { get; init; }
    public CSharpCompilation Compilation { get; set; } = null!;
    public List<SyntaxTree> Trees { get; } = [];
    public Dictionary<SyntaxTree, SemanticModel> Models { get; } = [];
}

internal sealed class CorpusModel
{
    private static readonly SymbolDisplayFormat Display =
        SymbolDisplayFormat.FullyQualifiedFormat;
    private readonly string _root;
    private readonly Dictionary<string, ProjectInfo> _projects = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _assemblyToProject = new(StringComparer.Ordinal);
    private readonly List<MetadataReference> _metadata;
    private CSharpCompilation? _external;

    private CorpusModel(string root, List<MetadataReference> metadata)
    {
        _root = root;
        _metadata = metadata;
    }

    public IReadOnlyList<ProjectInfo> Projects =>
        _projects.Values.OrderBy(project => project.Path, StringComparer.Ordinal).ToArray();

    public static CorpusModel Load(string root)
    {
        var tpa = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty;
        var metadata = tpa.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.Ordinal)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToList();
        var model = new CorpusModel(root, metadata);
        foreach (var full in Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
                     .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                         !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                     .OrderBy(path => path, StringComparer.Ordinal))
        {
            model.AddProject(full);
        }
        return model;
    }

    private void AddProject(string fullPath)
    {
        var document = XDocument.Load(fullPath);
        var project = new ProjectInfo
        {
            Path = Relative(fullPath),
            FullPath = fullPath,
            AssemblyName = Value(document, "AssemblyName") ??
                Path.GetFileNameWithoutExtension(fullPath),
            SourcePaths = [],
            SourceFullPaths = [],
            ProjectReferences = [],
            Defines = Value(document, "DefineConstants")?
                .Split([';', ','], StringSplitOptions.RemoveEmptyEntries)
                .Select(value => value.Trim())
                .Where(value => value.Length > 0)
                .ToList() ?? [],
            Packages = document.Descendants("PackageReference")
                .Select(element => (
                    Name: element.Attribute("Include")?.Value ?? string.Empty,
                    Version: element.Attribute("Version")?.Value ??
                        element.Element("Version")?.Value ?? string.Empty))
                .Where(package => package.Name.Length > 0)
                .ToList()
        };
        var projectDirectory = Path.GetDirectoryName(fullPath)!;
        var compile = document.Descendants("Compile")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => Path.GetFullPath(Path.Combine(projectDirectory, value!)))
            .Where(File.Exists)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var sources = compile.Count > 0
            ? compile
            : Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                    !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                .ToList();
        foreach (var source in sources.OrderBy(path => Relative(path), StringComparer.Ordinal))
        {
            project.SourcePaths.Add(Relative(source));
            project.SourceFullPaths.Add(source);
        }
        foreach (var reference in document.Descendants("ProjectReference")
                     .Select(element => element.Attribute("Include")?.Value)
                     .Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            var full = Path.GetFullPath(Path.Combine(projectDirectory, reference!));
            project.ProjectReferences.Add(Relative(full));
        }
        _projects.Add(project.Path, project);
        _assemblyToProject[project.AssemblyName] = project.Path;
    }

    public void Build()
    {
        if (File.Exists(Path.Combine(_root, "External", "ExternalLib.cs")))
        {
            var tree = CSharpSyntaxTree.ParseText(
                File.ReadAllText(Path.Combine(_root, "External", "ExternalLib.cs")),
                new CSharpParseOptions(LanguageVersion.CSharp14, DocumentationMode.Diagnose),
                Path.Combine(_root, "External", "ExternalLib.cs"));
            _external = CSharpCompilation.Create(
                "Corpus.External",
                [tree],
                _metadata,
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Release,
                    nullableContextOptions: NullableContextOptions.Disable,
                    deterministic: true,
                    concurrentBuild: false));
        }

        var built = new HashSet<string>(StringComparer.Ordinal);
        foreach (var project in Projects)
            BuildProject(project, built, new HashSet<string>(StringComparer.Ordinal));
    }

    private void BuildProject(
        ProjectInfo project, HashSet<string> built, HashSet<string> active)
    {
        if (built.Contains(project.Path))
            return;
        if (!active.Add(project.Path))
            throw new InvalidOperationException("project reference cycle at " + project.Path);
        foreach (var reference in project.ProjectReferences)
        {
            if (_projects.TryGetValue(reference, out var dependency))
                BuildProject(dependency, built, active);
        }

        var parse = new CSharpParseOptions(
            LanguageVersion.CSharp14,
            documentationMode: DocumentationMode.Diagnose,
            preprocessorSymbols: project.Defines);
        var trees = project.SourceFullPaths.Select(path =>
            CSharpSyntaxTree.ParseText(File.ReadAllText(path), parse, path)).ToList();
        var references = new List<MetadataReference>(_metadata);
        foreach (var reference in project.ProjectReferences)
        {
            if (_projects.TryGetValue(reference, out var dependency))
                references.Add(dependency.Compilation.ToMetadataReference());
        }
        if (_external is not null)
            references.Add(_external.ToMetadataReference());
        project.Compilation = CSharpCompilation.Create(
            project.AssemblyName,
            trees,
            references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release,
                nullableContextOptions: NullableContextOptions.Disable,
                deterministic: true,
                concurrentBuild: false));
        project.Trees.AddRange(trees);
        foreach (var tree in trees)
            project.Models[tree] = project.Compilation.GetSemanticModel(tree, ignoreAccessibility: false);
        built.Add(project.Path);
        active.Remove(project.Path);
    }

    public Dictionary<string, OracleNode> BuildNodes()
    {
        var nodes = new Dictionary<string, OracleNode>(StringComparer.Ordinal);
        var edges = new Dictionary<string, OracleEdge>(StringComparer.Ordinal);
        foreach (var project in Projects)
        {
            var projectId = NodeId("project", project.Path);
            AddNode(nodes, new OracleNode(
                projectId, "project", project.Path, "project", project.Path,
                project.Path, project.AssemblyName, project.Path));
            foreach (var package in project.Packages)
            {
                var canonical = $"nuget:{package.Name}\n{package.Version}";
                var packageId = NodeId("package", canonical);
                AddNode(nodes, new OracleNode(
                    packageId, "package", canonical, "package", project.Path,
                    null, package.Name, "nuget:" + package.Name));
                AddEdge(edges, "depends-on", projectId, packageId, "structural",
                    $"package:{package.Name}", project.Path, 1, 1, 1, 1);
            }
            foreach (var reference in project.ProjectReferences)
            {
                if (!_projects.ContainsKey(reference))
                    continue;
                AddEdge(edges, "depends-on", projectId,
                    NodeId("project", reference), "structural", null,
                    project.Path, 1, 1, 1, 1);
            }

            foreach (var tree in project.Trees)
            {
                var path = Relative(tree.FilePath);
                var fileId = NodeId("file", path);
                AddNode(nodes, new OracleNode(
                    fileId, "file", path, "file", project.Path, path,
                    Path.GetFileName(path), path));
                AddEdge(edges, "contains", projectId, fileId, "structural",
                    null, path, 1, 1, 1, 1);
                var semantic = project.Models[tree];
                var syntaxRoot = tree.GetRoot();
                foreach (var syntax in syntaxRoot.DescendantNodes(descendIntoTrivia: false))
                {
                    var symbol = SupportedSymbol(syntax, semantic);
                    if (symbol is null)
                        continue;
                    var kind = NodeKind(symbol);
                    if (kind is null)
                        continue;
                    var canonical = CanonicalKey(symbol);
                    var nodeId = NodeId("symbol", project.Path + "\n" + canonical);
                    var location = Location(path, syntax);
                    AddNode(nodes, new OracleNode(
                        nodeId, "symbol", canonical, kind, project.Path, path,
                        DisplayName(symbol), QualifiedName(symbol)));
                    var declaration = "csharp:" + Hash(
                        $"{project.Path}\n{canonical}\n{path}\n{location.StartLine}:{location.StartColumn}:{location.EndLine}:{location.EndColumn}");
                    AddEdge(edges, "declares", fileId, nodeId, "structural",
                        declaration, path, location.StartLine, location.StartColumn,
                        location.EndLine, location.EndColumn);
                    var containing = symbol.ContainingSymbol;
                    if (containing is not null &&
                        containing is not IAssemblySymbol and not IModuleSymbol &&
                        containing is not INamespaceSymbol { IsGlobalNamespace: true })
                    {
                        var containingId = NodeId(
                            "symbol", project.Path + "\n" + CanonicalKey(containing));
                        if (nodes.ContainsKey(containingId))
                            AddEdge(edges, "contains", containingId, nodeId, "structural",
                                null, path, location.StartLine, location.StartColumn,
                                location.EndLine, location.EndColumn);
                    }
                }
            }
        }
        _structuralEdges = edges;
        return nodes;
    }

    private Dictionary<string, OracleEdge> _structuralEdges = new(StringComparer.Ordinal);

    public Dictionary<string, OracleEdge> BuildEdges(Dictionary<string, OracleNode> nodes)
    {
        var edges = new Dictionary<string, OracleEdge>(_structuralEdges, StringComparer.Ordinal);
        foreach (var project in Projects)
        {
            foreach (var tree in project.Trees)
            {
                var path = Relative(tree.FilePath);
                var semantic = project.Models[tree];
                foreach (var syntax in tree.GetRoot().DescendantNodes(descendIntoTrivia: false))
                {
                    switch (syntax)
                    {
                        case TypeDeclarationSyntax declaration:
                            HandleBaseTypes(project, path, declaration, semantic, nodes, edges);
                            break;
                        case InvocationExpressionSyntax invocation:
                            HandleInvocation(project, path, invocation, semantic, nodes, edges);
                            break;
                        case ObjectCreationExpressionSyntax creation:
                            HandleCreation(project, path, creation, semantic, nodes, edges);
                            break;
                        case ImplicitObjectCreationExpressionSyntax creation:
                            HandleConstructor(project, path, creation, semantic, nodes, edges);
                            break;
                        case ConstructorInitializerSyntax initializer:
                            HandleConstructor(project, path, initializer, semantic, nodes, edges);
                            break;
                        case UsingDirectiveSyntax usingDirective:
                            HandleUsing(project, path, usingDirective, semantic, nodes, edges);
                            break;
                        case AttributeSyntax attribute:
                            HandleAttribute(project, path, attribute, semantic, nodes, edges);
                            break;
                        case SimpleNameSyntax name when
                            name.FirstAncestorOrSelf<UsingDirectiveSyntax>() is null:
                            HandleReference(project, path, name, semantic, nodes, edges);
                            break;
                        case PredefinedTypeSyntax predefined:
                            HandleReference(project, path, predefined, semantic, nodes, edges);
                            break;
                    }
                }
            }
        }
        return edges;
    }

    private void HandleBaseTypes(
        ProjectInfo project, string path, TypeDeclarationSyntax declaration,
        SemanticModel semantic, Dictionary<string, OracleNode> nodes,
        Dictionary<string, OracleEdge> edges)
    {
        if (declaration.BaseList is null ||
            semantic.GetDeclaredSymbol(declaration) is not INamedTypeSymbol sourceType)
            return;
        var source = SourceNode(project, sourceType, nodes);
        if (source is null)
            return;
        foreach (var baseType in declaration.BaseList.Types)
        {
            if (semantic.GetTypeInfo(baseType.Type).Type is not INamedTypeSymbol target)
                continue;
            var kind = sourceType.TypeKind == TypeKind.Interface
                ? "inherits"
                : target.TypeKind == TypeKind.Interface ? "implements" : "inherits";
            var targetId = TargetNode(project, target, nodes);
            if (targetId is null)
                continue;
            var location = Location(path, baseType);
            AddEdge(edges, kind, source, targetId, "resolved", null,
                path, location.StartLine, location.StartColumn,
                location.EndLine, location.EndColumn);
        }
    }

    private void HandleInvocation(
        ProjectInfo project, string path, InvocationExpressionSyntax invocation,
        SemanticModel semantic, Dictionary<string, OracleNode> nodes,
        Dictionary<string, OracleEdge> edges)
    {
        var info = semantic.GetSymbolInfo(invocation);
        if (info.Symbol is not IMethodSymbol method)
            return;
        var source = SourceNode(project, EnclosingCallable(invocation, semantic), nodes);
        var target = TargetNode(project, method.ReducedFrom ?? method, nodes);
        if (source is null || target is null)
            return;
        var location = Location(path, invocation);
        AddEdge(edges, "calls", source, target, "resolved", null,
            path, location.StartLine, location.StartColumn,
            location.EndLine, location.EndColumn);
    }

    private void HandleCreation(
        ProjectInfo project, string path, ObjectCreationExpressionSyntax creation,
        SemanticModel semantic, Dictionary<string, OracleNode> nodes,
        Dictionary<string, OracleEdge> edges)
    {
        if (semantic.GetSymbolInfo(creation).Symbol is not IMethodSymbol constructor)
            return;
        AddCall(project, path, creation, constructor, semantic, nodes, edges);
    }

    private void HandleConstructor(
        ProjectInfo project, string path, SyntaxNode syntax,
        SemanticModel semantic, Dictionary<string, OracleNode> nodes,
        Dictionary<string, OracleEdge> edges)
    {
        if (semantic.GetSymbolInfo(syntax).Symbol is not IMethodSymbol constructor)
            return;
        AddCall(project, path, syntax, constructor, semantic, nodes, edges);
    }

    private void AddCall(
        ProjectInfo project, string path, SyntaxNode syntax, IMethodSymbol constructor,
        SemanticModel semantic, Dictionary<string, OracleNode> nodes,
        Dictionary<string, OracleEdge> edges)
    {
        var source = SourceNode(project, EnclosingCallable(syntax, semantic), nodes);
        var target = TargetNode(project, constructor, nodes);
        if (source is null || target is null)
            return;
        var location = Location(path, syntax);
        AddEdge(edges, "calls", source, target, "resolved", null,
            path, location.StartLine, location.StartColumn,
            location.EndLine, location.EndColumn);
    }

    private void HandleReference(
        ProjectInfo project, string path, SyntaxNode syntax,
        SemanticModel semantic, Dictionary<string, OracleNode> nodes,
        Dictionary<string, OracleEdge> edges)
    {
        var info = semantic.GetSymbolInfo(syntax);
        var symbol = info.Symbol is IAliasSymbol alias ? alias.Target : info.Symbol;
        if (symbol is null || symbol is INamespaceSymbol)
            return;
        var source = SourceNode(project, EnclosingReferenceOwner(syntax, semantic), nodes)
            ?? NodeId("file", path);
        if (!nodes.ContainsKey(source))
            return;
        var target = TargetNode(project, symbol, nodes);
        if (target is null)
            return;
        var location = Location(path, syntax);
        AddEdge(edges, "references", source, target, "resolved", null,
            path, location.StartLine, location.StartColumn,
            location.EndLine, location.EndColumn);
    }

    private void HandleAttribute(
        ProjectInfo project, string path, AttributeSyntax attribute,
        SemanticModel semantic, Dictionary<string, OracleNode> nodes,
        Dictionary<string, OracleEdge> edges)
    {
        if (semantic.GetSymbolInfo(attribute).Symbol is not IMethodSymbol constructor)
            return;
        var source = SourceNode(project, EnclosingReferenceOwner(attribute, semantic), nodes)
            ?? NodeId("file", path);
        if (!nodes.ContainsKey(source))
            return;
        var target = TargetNode(project, constructor.ContainingType, nodes);
        if (target is null)
            return;
        var location = Location(path, attribute);
        AddEdge(edges, "references", source, target, "resolved", null,
            path, location.StartLine, location.StartColumn,
            location.EndLine, location.EndColumn);
    }

    private void HandleUsing(
        ProjectInfo project, string path, UsingDirectiveSyntax directive,
        SemanticModel semantic, Dictionary<string, OracleNode> nodes,
        Dictionary<string, OracleEdge> edges)
    {
        if (directive.Name is null)
            return;
        var info = semantic.GetSymbolInfo(directive.Name);
        var target = info.Symbol is IAliasSymbol alias ? alias.Target : info.Symbol;
        if (target is not INamespaceSymbol and not INamedTypeSymbol)
            return;
        var isGlobal = directive.GlobalKeyword != default;
        var containingNamespace = directive.Ancestors()
            .OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
        var scope = isGlobal ? "project" : containingNamespace is null ? "file" : "namespace";
        var source = isGlobal
            ? NodeId("project", project.Path)
            : containingNamespace is not null
                ? SourceNode(project, semantic.GetDeclaredSymbol(containingNamespace), nodes)
                : NodeId("file", path);
        if (source is null || !nodes.ContainsKey(source))
            return;
        var targetId = TargetNode(project, target, nodes);
        if (targetId is null)
            return;
        var aliasName = directive.Alias?.Name.Identifier.ValueText ?? string.Empty;
        var isStatic = directive.StaticKeyword != default;
        var location = Location(path, directive);
        AddEdge(edges, "imports", source, targetId, "resolved",
            $"{scope}:{aliasName}:{isStatic}:{isGlobal}",
            path, location.StartLine, location.StartColumn,
            location.EndLine, location.EndColumn);
    }

    private string? SourceNode(
        ProjectInfo project, ISymbol? symbol, Dictionary<string, OracleNode> nodes)
    {
        if (symbol is null)
            return null;
        return TargetNode(project, symbol, nodes);
    }

    private string? TargetNode(
        ProjectInfo current, ISymbol target, Dictionary<string, OracleNode> nodes)
    {
        var normalized = target is IMethodSymbol method
            ? method.ReducedFrom ?? method.OriginalDefinition
            : target.OriginalDefinition;
        var canonical = CanonicalKey(normalized);
        var assembly = normalized.ContainingAssembly;
        ProjectInfo? defining = null;
        if (assembly is null)
        {
            var local = NodeId("symbol", current.Path + "\n" + canonical);
            if (nodes.ContainsKey(local))
                return local;
            return null;
        }
        if (SymbolEqualityComparer.Default.Equals(assembly, current.Compilation.Assembly))
        {
            defining = current;
        }
        else
        {
            foreach (var project in _projects.Values)
            {
                if (SymbolEqualityComparer.Default.Equals(
                        assembly, project.Compilation.Assembly))
                {
                    defining = project;
                    break;
                }
            }
        }
        if (defining is null)
            return null;
        var id = NodeId("symbol", defining.Path + "\n" + canonical);
        return nodes.ContainsKey(id) ? id : null;
    }

    private static ISymbol? EnclosingCallable(SyntaxNode node, SemanticModel semantic)
    {
        foreach (var ancestor in node.Ancestors())
        {
            switch (ancestor)
            {
                case LocalFunctionStatementSyntax local:
                    return semantic.GetDeclaredSymbol(local);
                case BaseMethodDeclarationSyntax method:
                    return semantic.GetDeclaredSymbol(method);
                case PropertyDeclarationSyntax property:
                    return semantic.GetDeclaredSymbol(property);
                case BaseTypeDeclarationSyntax:
                case BaseNamespaceDeclarationSyntax:
                    return null;
            }
        }
        return null;
    }

    private static ISymbol? EnclosingReferenceOwner(SyntaxNode node, SemanticModel semantic)
    {
        foreach (var ancestor in node.Ancestors())
        {
            switch (ancestor)
            {
                case LocalFunctionStatementSyntax local:
                    return semantic.GetDeclaredSymbol(local);
                case BaseMethodDeclarationSyntax method:
                    return semantic.GetDeclaredSymbol(method);
                case BasePropertyDeclarationSyntax property:
                    return semantic.GetDeclaredSymbol(property);
                case FieldDeclarationSyntax field when
                    field.Declaration.Variables.FirstOrDefault() is { } variable:
                    return semantic.GetDeclaredSymbol(variable);
                case VariableDeclaratorSyntax variable when
                    variable.Parent?.Parent is FieldDeclarationSyntax:
                    return semantic.GetDeclaredSymbol(variable);
                case BaseTypeDeclarationSyntax type:
                    return semantic.GetDeclaredSymbol(type);
            }
        }
        return null;
    }

    private static ISymbol? SupportedSymbol(SyntaxNode syntax, SemanticModel semantic) =>
        syntax switch
        {
            BaseNamespaceDeclarationSyntax value => semantic.GetDeclaredSymbol(value),
            BaseTypeDeclarationSyntax value => semantic.GetDeclaredSymbol(value),
            DelegateDeclarationSyntax value => semantic.GetDeclaredSymbol(value),
            BaseMethodDeclarationSyntax value => semantic.GetDeclaredSymbol(value),
            LocalFunctionStatementSyntax value => semantic.GetDeclaredSymbol(value),
            BasePropertyDeclarationSyntax value => semantic.GetDeclaredSymbol(value),
            VariableDeclaratorSyntax value when value.Parent?.Parent is FieldDeclarationSyntax =>
                semantic.GetDeclaredSymbol(value),
            ParameterSyntax value when value.Parent?.Parent is
                BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax or
                DelegateDeclarationSyntax or BasePropertyDeclarationSyntax =>
                semantic.GetDeclaredSymbol(value),
            _ => null
        };

    private static string? NodeKind(ISymbol symbol) => symbol switch
    {
        INamespaceSymbol => "namespace",
        INamedTypeSymbol named when named.TypeKind == TypeKind.Interface => "interface",
        INamedTypeSymbol => "type",
        IMethodSymbol => "callable",
        IPropertySymbol => "property",
        IFieldSymbol => "field",
        IParameterSymbol => "parameter",
        _ => null
    };

    private static string CanonicalKey(ISymbol symbol)
    {
        var documentation = symbol.GetDocumentationCommentId();
        if (documentation is not null)
            return documentation;
        if (symbol is IParameterSymbol parameter)
            return $"P:{CanonicalKey(parameter.ContainingSymbol)}:{parameter.Ordinal}:{parameter.Name}";
        return $"{symbol.Kind}:{symbol.ToDisplayString(Display)}";
    }

    private static string DisplayName(ISymbol symbol) => symbol is IMethodSymbol
    {
        MethodKind: MethodKind.Constructor or MethodKind.StaticConstructor
    } method
        ? method.ContainingType.Name
        : symbol.Name;

    private static string QualifiedName(ISymbol symbol) =>
        symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

    private static string NodeId(string category, string canonical) =>
        $"csharp:{category}:{Hash(canonical)}";

    private static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static (int StartLine, int StartColumn, int EndLine, int EndColumn)
        Location(string path, SyntaxNode syntax)
    {
        var span = syntax.GetLocation().GetLineSpan().Span;
        return (span.Start.Line + 1, span.Start.Character + 1,
            span.End.Line + 1, span.End.Character + 1);
    }

    private static void AddNode(Dictionary<string, OracleNode> nodes, OracleNode node)
    {
        nodes.TryAdd(node.Id, node);
    }

    private static void AddEdge(
        Dictionary<string, OracleEdge> edges, string kind, string source,
        string target, string quality, string? discriminator, string? path,
        int startLine, int startColumn, int endLine, int endColumn)
    {
        var id = "csharp:" + Hash($"{kind}\n{source}\n{target}\n{discriminator}");
        edges.TryAdd(id, new OracleEdge(
            id, kind, source, target, quality, discriminator, path,
            startLine, startColumn, endLine, endColumn));
    }

    private static string? Value(XDocument document, string name) =>
        document.Descendants(name).Select(element => element.Value).FirstOrDefault();

    private string Relative(string full) =>
        Path.GetRelativePath(_root, full).Replace('\\', '/');
}
