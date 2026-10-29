using System.Security.Cryptography;
using System.Text;

namespace Andirit;

/// <summary>
/// Maps TypeScript and JavaScript syntax (as exposed by the pinned
/// <c>tree-sitter-typescript</c>/<c>tree-sitter-javascript</c> grammars) onto
/// Andirit graph facts. The two grammars share a common ancestry, so one profile
/// serves both: declarations, containment, structural imports, and invocation
/// names are observed from syntax. Module-reference shapes are captured for
/// the plugin-local resolver; the profile itself does not resolve them.
///
/// Everything emitted is structural evidence. The profile never emits
/// <c>calls</c> or <c>references</c>: invocations record the invoked name
/// through <c>invokes-name</c>, and imported modules become plain
/// declaration-less nodes. Declared symbols carry the language property so
/// discovery filters apply uniformly.
/// </summary>
internal sealed class EcmaScriptExtractionProfile : ITreeSitterExtractionProfile
{
    /// <summary>
    /// Extraction-logic revision, folded into the plugin version so profile
    /// fixes invalidate previous indexes even when the grammar pin is
    /// unchanged. Bump on any change to emitted facts.
    /// Revision history:
    /// 1: initial structural extraction for TypeScript and JavaScript.
    /// 2: TypeScript interface declarations and identifier-named method and
    ///    property signatures with modifiers, declarations, and containment
    ///    (path-scoped identities; see docs/ecmascript-symbol-identity.md).
    ///    JavaScript shares the revision and re-extracts with unchanged facts.
    /// 3: class declarations are path-scoped and class members use
    ///    owner-derived identities (methods, accessors, overload signatures);
    ///    interface member signatures migrate to owner-derived identity and
    ///    heritage placeholders become file-scoped.
    /// 4: identifier-named class fields emit `property` members with
    ///    declarations, containment, and written modifier metadata; their
    ///    initializer invocations are attributed to the field.
    /// 5: written heritage names use declaration-less identities in a
    ///    separate namespace from declared symbols, preventing name-only
    ///    convergence with same-file declarations.
    /// 6: named file-level variable declarators emit path-scoped field
    ///    declarations with written declaration-keyword and export metadata.
    /// 7: ECMAScript module-reference shapes feed the plugin-local
    ///    relative-v1 resolver and its `resolves-to` resolution unit.
    /// 8: EC-6B-R1 corrective pass: escaped specifiers are rejected without
    ///    truncation and `type` binding names are no longer treated as an
    ///    inline type-only modifier; emitted resolver facts changed (D-16).
    /// 9: publish accepted structural export entries and per-occurrence
    ///    `exports` edges from the internal syntax observations.
    /// </summary>
    internal const int ProfileRevision = 10;

    // Kinds both grammars can emit. `implements` exists only in TypeScript, so
    // JavaScript reports it as not-produced rather than claiming an empty set.
    private static readonly CodeEdgeKind[] SharedRelationshipKinds =
    [
        CodeEdgeKinds.Contains,
        CodeEdgeKinds.Declares,
        CodeEdgeKinds.Imports,
        CodeEdgeKinds.ResolvesTo,
        CodeEdgeKinds.InvokesName,
        CodeEdgeKinds.Inherits
    ];

    /// <summary>Relationship kinds the TypeScript grammar can emit.</summary>
    internal static readonly IReadOnlyList<CodeEdgeKind> TypeScriptRelationshipKinds =
        [.. SharedRelationshipKinds, CodeEdgeKinds.Implements];

    /// <summary>Relationship kinds the JavaScript grammar can emit.</summary>
    internal static readonly IReadOnlyList<CodeEdgeKind> JavaScriptRelationshipKinds =
        [.. SharedRelationshipKinds];

    private readonly string _path;
    private readonly string _language;
    private readonly string _providerId;
    private readonly string _providerVersion;
    private readonly TreeSitterExtractionResult _result;

    internal EcmaScriptExtractionProfile(
        string path,
        string languageKey,
        CodePluginId providerId,
        string providerVersion)
    {
        _path = path;
        _language = languageKey;
        _providerId = providerId.Value;
        // p9 adds export facts without changing the existing extraction facts.
        // Keep their evidence version at p8 so a p8→p9 re-index is byte-identical
        // for existing facts; the session/unit origin still uses p9 to invalidate
        // old publications. Export edges derive their own exports1 provider version.
        _providerVersion = ExistingFactProviderVersion(providerVersion);
        _result = new TreeSitterExtractionResult();
    }

    public string LanguageKey => _language;

    public int Revision => ProfileRevision;

    public TreeSitterExtractionResult Extract(
        TreeSitterSyntaxNode root,
        CodeNodeId fileNodeId,
        string path)
    {
        WalkChildren(root, new Scope(null, fileNodeId, fileNodeId), isFileLevel: true);
        _result.ExportObservations.Sort(EcmaScriptExportObservation.SourceOrderComparer);
        EcmaScriptExportGraphPublisher.Publish(
            _path,
            _language,
            fileNodeId,
            _providerVersion,
            _result);
        if (_language == "typescript")
            EcmaScriptInterfaceFacts.Publish(root, _path, fileNodeId, _providerVersion, _result);
        return _result;
    }

    private void Walk(TreeSitterSyntaxNode node, Scope scope, bool isFileLevel = false)
    {
        // Error and missing nodes are recovery artifacts, not understood
        // structure: skip the subtree but keep visiting valid siblings. The
        // caller marks the file partial from the root error flag.
        if (node.IsError || node.IsMissing)
            return;

        switch (node.Type)
        {
            case "export_statement":
                if (isFileLevel && scope.Owner == ScopeOwner.File)
                    EcmaScriptExportShapeCollector.Capture(node, _path, _language, _result.ExportObservations);
                CaptureExportModuleReference(node);
                HandleExport(node, scope, isFileLevel);
                return;
            case "lexical_declaration":
            case "variable_declaration":
                HandleVariableDeclaration(node, scope, isFileLevel, isExported: false);
                WalkChildren(node, scope);
                return;
            case "import_statement":
                CaptureImportModuleReference(node);
                EmitImport(node, scope);
                return;
            case "import_type":
                _result.ModuleReferences.Add(new(null, "import-type", true, Location(node)));
                WalkChildren(node, scope);
                return;
            case "class_declaration":
            case "abstract_class_declaration":
                if (TryEmitNamed(
                    node,
                    node.ChildByFieldName("name"),
                    scope,
                    CodeNodeKinds.Type,
                    "class",
                    out var classScope,
                    fileScoped: true,
                    owner: ScopeOwner.Class))
                {
                    EmitHeritage(node, classScope.OwnerNode);
                    WalkChildren(node, classScope);
                }
                else
                {
                    WalkChildren(node, scope);
                }

                return;
            case "interface_declaration":
                if (TryEmitNamed(
                    node,
                    node.ChildByFieldName("name"),
                    scope,
                    CodeNodeKinds.Interface,
                    "interface",
                    out var interfaceScope,
                    fileScoped: true,
                    owner: ScopeOwner.Interface))
                {
                    EmitHeritage(node, interfaceScope.OwnerNode);
                    EmitInterfaceMembers(node, interfaceScope);
                }

                return;
            case "type_alias_declaration":
                TryEmitNamed(node, node.ChildByFieldName("name"), scope, CodeNodeKinds.Type, "type", out _);
                return;
            case "enum_declaration":
                TryEmitNamed(node, node.ChildByFieldName("name"), scope, CodeNodeKinds.Type, "enum", out var enumScope);
                WalkChildren(node, enumScope);
                return;
            case "internal_module":
            case "module":
                if (TryEmitNamed(node, node.ChildByFieldName("name"), scope, CodeNodeKinds.Namespace, "namespace", out var namespaceScope))
                    WalkChildren(node, namespaceScope);
                else
                    WalkChildren(node, scope);
                return;
            case "function_declaration":
            case "generator_function_declaration":
                if (TryEmitNamed(
                    node,
                    node.ChildByFieldName("name"),
                    scope,
                    CodeNodeKinds.Callable,
                    "callable",
                    out var functionScope,
                    owner: ScopeOwner.Callable))
                {
                    WalkChildren(node, functionScope);
                }
                else
                {
                    WalkChildren(node, scope);
                }

                return;
            case "method_definition":
                if (scope.Owner == ScopeOwner.Class)
                {
                    HandleClassMethod(node, scope, walkBody: true);
                    return;
                }

                // Object-literal and class-expression methods keep the
                // previous name-based callable form until they have an
                // identified owner declaration to derive identity from.
                if (TryEmitNamed(
                    node,
                    node.ChildByFieldName("name"),
                    scope,
                    CodeNodeKinds.Callable,
                    "callable",
                    out var methodScope,
                    owner: ScopeOwner.Callable))
                {
                    WalkChildren(node, methodScope);
                }
                else
                {
                    WalkChildren(node, scope);
                }

                return;
            case "method_signature":
                // A class-body overload signature belongs to its class; a
                // signature in a type literal is unowned and emits no facts.
                if (scope.Owner == ScopeOwner.Class)
                    HandleClassMethod(node, scope, walkBody: false);
                return;
            case "public_field_definition":
            case "field_definition":
                if (scope.Owner == ScopeOwner.Class)
                {
                    HandleClassField(node, scope);
                    return;
                }

                // A class-expression field has no identified owner yet: keep
                // observing its nested syntax under the enclosing scope.
                WalkChildren(node, scope);
                return;
            case "variable_declarator":
                HandleDeclarator(node, scope);
                return;
            case "call_expression":
                CaptureDynamicImport(node);
                TryEmitPrototypeCall(node, scope);
                EmitCall(node, scope);
                WalkChildren(node, scope);
                return;
            case "new_expression":
                EmitNew(node, scope);
                WalkChildren(node, scope);
                return;
            case "assignment_expression":
                TryEmitPrototypeAssignment(node, scope);
                WalkChildren(node, scope);
                return;
            default:
                WalkChildren(node, scope);
                return;
        }
    }

    private void HandleExport(TreeSitterSyntaxNode node, Scope scope, bool isFileLevel)
    {
        var declaration = node.ChildByFieldName("declaration");
        if (declaration is not null)
        {
            if (isFileLevel &&
                (declaration.Type is "lexical_declaration" or "variable_declaration"))
            {
                HandleVariableDeclaration(declaration, scope, isFileLevel, isExported: true);
                WalkChildren(declaration, scope);
            }
            else
            {
                Walk(declaration, scope, isFileLevel);
            }

            return;
        }

        var source = node.ChildByFieldName("source");
        if (source is not null)
        {
            if (StringLiteral(source) is { } target)
                EmitImportTarget(target, node, scope);
            return;
        }

        WalkChildren(node, scope);
    }

    private void CaptureImportModuleReference(TreeSitterSyntaxNode node)
    {
        if (node.ChildByFieldName("source") is { } source)
        {
            var wholeTypeOnly = HasStatementTypeModifier(node);
            var typeOnly = wholeTypeOnly || AllSpecifiersAreTypeOnly(node, "import_specifier");
            _result.ModuleReferences.Add(new(
                RawModuleSpecifier(source),
                wholeTypeOnly ? "import-type" : "import",
                typeOnly,
                Location(node)));
            return;
        }

        // TypeScript's `import x = require(...)` syntax is a module form, but
        // EC-6B does not resolve it. Count the observed site as unsupported.
        if (IsImportEqualsRequire(node.Text))
            _result.ModuleReferences.Add(new(null, "import", false, Location(node)));
    }

    private static bool IsImportEqualsRequire(string text)
    {
        var equals = text.IndexOf('=');
        if (equals < 0)
            return false;

        var offset = SkipTrivia(text, equals + 1);
        const string require = "require";
        if (!text.AsSpan(offset).StartsWith(require, StringComparison.Ordinal))
            return false;
        offset += require.Length;
        if (offset < text.Length && (char.IsLetterOrDigit(text[offset]) || text[offset] is '_' or '$'))
            return false;

        offset = SkipTrivia(text, offset);
        return offset < text.Length && text[offset] == '(';
    }

    private static int SkipTrivia(string text, int offset)
    {
        while (offset < text.Length)
        {
            if (char.IsWhiteSpace(text[offset]))
            {
                offset++;
                continue;
            }

            if (offset + 1 < text.Length && text[offset] == '/' && text[offset + 1] == '/')
            {
                offset += 2;
                while (offset < text.Length && text[offset] is not ('\r' or '\n'))
                    offset++;
                continue;
            }

            if (offset + 1 < text.Length && text[offset] == '/' && text[offset + 1] == '*')
            {
                var end = text.IndexOf("*/", offset + 2, StringComparison.Ordinal);
                if (end < 0)
                    return text.Length;
                offset = end + 2;
                continue;
            }

            break;
        }

        return offset;
    }

    private void CaptureExportModuleReference(TreeSitterSyntaxNode node)
    {
        if (node.ChildByFieldName("source") is { } source)
        {
            var wholeTypeOnly = HasStatementTypeModifier(node);
            var moduleReference = wholeTypeOnly
                ? "export-type-from"
                : HasPrefixTokens(node.Text, "export", "*", "as")
                    ? "export-namespace-from"
                    : HasPrefixTokens(node.Text, "export", "*")
                        ? "export-star"
                        : "export-from";
            var typeOnly = wholeTypeOnly || AllSpecifiersAreTypeOnly(node, "export_specifier");
            _result.ModuleReferences.Add(new(
                RawModuleSpecifier(source),
                moduleReference,
                typeOnly,
                Location(node)));
            return;
        }

        // `export =` is deliberately outside relative-v1.
        if (HasPrefixTokens(node.Text, "export", "="))
            _result.ModuleReferences.Add(new(null, "export-from", false, Location(node)));
    }

    private void CaptureDynamicImport(TreeSitterSyntaxNode node)
    {
        var function = node.ChildByFieldName("function");
        if (function is { Text: "import" })
            _result.ModuleReferences.Add(new(null, "import", false, Location(node)));
        else if (function is not null && CalleeName(function) == "require")
        {
            var first = node.ChildByFieldName("arguments")?.NamedChildren.FirstOrDefault();
            _result.ModuleReferences.Add(new(
                first is { Type: "string" } ? RawModuleSpecifier(first) : null,
                "require",
                false,
                Location(node)));
        }
    }

    private static string? RawModuleSpecifier(TreeSitterSyntaxNode source)
    {
        if (source.Type != "string")
            return null;

        // Escape sequences split a string literal into several fragments, so a
        // fragment-only read would silently truncate the written specifier and
        // bypass the policy's backslash rejection (EC6B-GLM-F02). relative-v1
        // rejects escape sequences instead of decoding them.
        if (source.NamedChildren.Any(child => child.Type == "escape_sequence"))
            return null;

        var text = source.Text;
        if (text.Length < 2 || text[0] is not ('\'' or '"') || text[^1] != text[0])
            return null;
        var raw = text[1..^1];

        // The written, unescaped content is the policy input. Escape
        // sequences were rejected above rather than decoded. Overlong text is
        // intentionally discarded before it reaches facts.
        return raw.Length <= 256 ? raw : null;
    }

    private static bool AllSpecifiersAreTypeOnly(TreeSitterSyntaxNode node, string specifierType)
    {
        var specifiers = Descendants(node).Where(child => child.Type == specifierType).ToArray();
        return specifiers.Length > 0 && specifiers.All(IsTypeOnlySpecifier);
    }

    /// <summary>
    /// Detects an explicit statement-level <c>type</c> modifier from the
    /// grammar structure. A <c>type</c> token that is a binding name (for
    /// example <c>import type from './m'</c>) is part of the import clause and
    /// never appears as a statement keyword (EC6B-GLM-F03).
    /// </summary>
    internal static bool HasStatementTypeModifier(TreeSitterSyntaxNode node)
    {
        var children = node.Children;
        for (var index = 0; index < children.Count; index++)
        {
            var child = children[index];
            if (!child.IsNamed && child.Type == "type")
                return true;

            // The pinned grammar parses `export type * from` and
            // `export type * as ns from` through error recovery: the keyword
            // sits inside an error node directly before the star token or the
            // namespace export clause.
            if (child.IsError &&
                child.Children.Count > 0 &&
                !child.Children[0].IsNamed &&
                child.Children[0].Type == "type" &&
                index + 1 < children.Count &&
                ((!children[index + 1].IsNamed && children[index + 1].Type == "*") ||
                 (children[index + 1].IsNamed && children[index + 1].Type == "namespace_export")))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Detects an explicit inline <c>type</c> modifier on one import/export
    /// specifier. <c>{ type }</c> and <c>{ type as x }</c> name the binding
    /// <c>type</c>; only <c>{ type x }</c>, <c>{ type x as y }</c>, and the
    /// all-<c>as</c> three-token form are type-only output (EC6B-GLM-F03).
    /// </summary>
    internal static bool IsTypeOnlySpecifier(TreeSitterSyntaxNode specifier)
    {
        var children = specifier.Children;
        for (var index = 0; index < children.Count; index++)
        {
            if (children[index].IsNamed || children[index].Type != "type")
                continue;
            if (index + 1 >= children.Count)
                return false;

            var next = children[index + 1];
            if (!next.IsNamed)
                return false;
            if (next.Type == "string")
                return true;
            if (next.Type != "identifier")
                return false;
            if (next.Text != "as")
                return true;

            // `type as X` names the binding `type`; TypeScript's parser only
            // treats the three-token `type as as as` form as a modifier.
            return index + 3 < children.Count &&
                !children[index + 2].IsNamed &&
                children[index + 2].Type == "as" &&
                children[index + 3].IsNamed &&
                children[index + 3].Type == "identifier";
        }

        return false;
    }

    private static IEnumerable<TreeSitterSyntaxNode> Descendants(TreeSitterSyntaxNode node)
    {
        foreach (var child in node.NamedChildren)
        {
            yield return child;
            foreach (var descendant in Descendants(child))
                yield return descendant;
        }
    }

    private static bool HasPrefixTokens(string text, params string[] tokens)
    {
        var offset = 0;
        foreach (var token in tokens)
        {
            while (offset < text.Length && char.IsWhiteSpace(text[offset]))
                offset++;
            if (!text.AsSpan(offset).StartsWith(token, StringComparison.Ordinal))
                return false;
            offset += token.Length;
        }

        return offset == text.Length || char.IsWhiteSpace(text[offset]) ||
            tokens[^1] is "*" or "=" || text[offset] is '{' or ';';
    }

    /// <summary>
    /// Emits one independently discoverable declaration for each simple,
    /// named declarator in a file-level <c>const</c>, <c>let</c>, or
    /// <c>var</c> declaration. The existing declarator walk remains
    /// responsible for callable-valued variables and initializer invocation
    /// attribution.
    /// </summary>
    private void HandleVariableDeclaration(
        TreeSitterSyntaxNode declaration,
        Scope scope,
        bool isFileLevel,
        bool isExported)
    {
        if (!isFileLevel || scope.Owner != ScopeOwner.File)
            return;

        string? keyword = null;
        foreach (var child in declaration.Children)
        {
            if (!child.IsNamed && (child.Text is "const" or "let" or "var"))
            {
                keyword = child.Text;
                break;
            }
        }

        if (keyword is null)
            return;

        foreach (var declarator in declaration.NamedChildren)
        {
            if (declarator.Type != "variable_declarator" || declarator.HasError)
                continue;

            var nameNode = declarator.ChildByFieldName("name");
            if (nameNode is not { Type: "identifier" } || BoundedText(nameNode) is not { } name)
                continue;

            var value = declarator.ChildByFieldName("value");
            if (value is { IsError: true } or { IsMissing: true } ||
                (value is null && HasToken(declarator, "=")) ||
                (keyword == "const" && value is null))
            {
                continue;
            }

            // Top-level variable identities are scoped by source path and
            // discriminator. This separates modules and all unrelated symbol
            // kinds without using physical offsets or claiming binding.
            var hash = Hash($"variable\n{_path}\n{name}");
            var symbolId = new CodeSymbolId($"{_language}:{hash}");
            var nodeId = new CodeNodeId($"{_language}:symbol:{hash}");
            var properties = LanguageProperties();
            var modifiers = new SortedSet<string>(StringComparer.Ordinal) { keyword };
            if (isExported)
                modifiers.Add("export");
            properties[CodePropertyKeys.Modifiers] = new CodeTextProperty(string.Join(" ", modifiers));

            // Physical declarations retain their own syntax metadata. If
            // duplicate same-file declarations disagree, the shared symbol
            // advertises only modifiers written on every occurrence.
            var declarationProperties = new Dictionary<string, CodePropertyValue>(properties, StringComparer.Ordinal);
            if (_result.Nodes.TryGetValue(nodeId.Value, out var existing) && existing.SymbolId is not null)
            {
                var commonModifiers = new SortedSet<string>(modifiers, StringComparer.Ordinal);
                if (existing.Properties.TryGetValue(CodePropertyKeys.Modifiers, out var existingModifiers))
                {
                    commonModifiers.IntersectWith(
                        ((CodeTextProperty)existingModifiers).Value.Split(' ', StringSplitOptions.RemoveEmptyEntries));
                }
                else
                {
                    commonModifiers.Clear();
                }

                var commonProperties = LanguageProperties();
                if (commonModifiers.Count > 0)
                {
                    commonProperties[CodePropertyKeys.Modifiers] =
                        new CodeTextProperty(string.Join(" ", commonModifiers));
                }

                _result.Nodes[nodeId.Value] = new CodeGraphNode(
                    nodeId,
                    CodeNodeKinds.Field,
                    name,
                    name,
                    symbolId,
                    commonProperties);
            }
            else
            {
                AddNode(new CodeGraphNode(
                    nodeId,
                    CodeNodeKinds.Field,
                    name,
                    name,
                    symbolId,
                    properties));
            }

            AddDeclaration(symbolId, nodeId, Location(declarator), declarationProperties);
            var nameLocation = Location(nameNode);
            AddEdge(CodeEdgeKinds.Declares, scope.FileNode, nodeId, nameLocation, $"declares:{nodeId.Value}");
            if (scope.OwnerNode != nodeId)
                AddEdge(CodeEdgeKinds.Contains, scope.OwnerNode, nodeId, nameLocation, $"contains:{nodeId.Value}");
        }
    }

    private void HandleDeclarator(TreeSitterSyntaxNode node, Scope scope)
    {
        var value = node.ChildByFieldName("value");
        if (value is not null && IsFunctionValue(value) &&
            TryEmitNamed(
                node,
                node.ChildByFieldName("name"),
                scope,
                CodeNodeKinds.Callable,
                "callable",
                out var callableScope,
                owner: ScopeOwner.Callable))
        {
            WalkChildren(value, callableScope);
            return;
        }

        WalkChildren(node, scope);
    }

    private static bool IsFunctionValue(TreeSitterSyntaxNode node) =>
        node.Type is "arrow_function" or "function_expression" or "function" or "generator_function";

    private void EmitImport(TreeSitterSyntaxNode node, Scope scope)
    {
        if (node.ChildByFieldName("source") is { } source &&
            StringLiteral(source) is { } target)
        {
            EmitImportTarget(target, node, scope);
        }
    }

    private void EmitImportTarget(string target, TreeSitterSyntaxNode locationNode, Scope scope)
    {
        var nodeId = new CodeNodeId($"{_language}:import:{Hash(target)}");
        AddNode(new CodeGraphNode(nodeId, CodeNodeKinds.Type, ShortName(target), target));
        AddEdge(CodeEdgeKinds.Imports, scope.OwnerNode, nodeId, Location(locationNode), $"imports:{nodeId.Value}");
    }

    private void EmitCall(TreeSitterSyntaxNode node, Scope scope)
    {
        var target = CalleeName(node.ChildByFieldName("function"));
        if (target is null)
            return;
        if (target == "require")
        {
            TryEmitRequire(node, scope);
            return;
        }

        EmitInvocation(target, node, scope);
    }

    private void EmitNew(TreeSitterSyntaxNode node, Scope scope)
    {
        if (CalleeName(node.ChildByFieldName("constructor")) is { } target)
            EmitInvocation(target, node, scope);
    }

    private void TryEmitRequire(TreeSitterSyntaxNode node, Scope scope)
    {
        var first = node.ChildByFieldName("arguments")?.NamedChildren.FirstOrDefault();
        if (first is { Type: "string" } && StringLiteral(first) is { } target)
            EmitImportTarget(target, node, scope);
    }

    private void EmitInvocation(string target, TreeSitterSyntaxNode locationNode, Scope scope)
    {
        var nodeId = new CodeNodeId($"{_language}:invoked:{Hash(target)}");
        AddNode(new CodeGraphNode(nodeId, CodeNodeKinds.Callable, target));
        AddEdge(CodeEdgeKinds.InvokesName, scope.OwnerNode, nodeId, Location(locationNode), $"invokes:{nodeId.Value}");
    }

    /// <summary>
    /// Emits structural <c>inherits</c>/<c>implements</c> from class or
    /// interface heritage. TypeScript wraps heritage in explicit clauses;
    /// JavaScript places the base type directly under <c>class_heritage</c>.
    /// Expression bases (mixins) carry no stable name and are skipped rather
    /// than guessed.
    /// </summary>
    private void EmitHeritage(TreeSitterSyntaxNode definition, CodeNodeId owner)
    {
        var heritage = FindChild(definition, "class_heritage")
            ?? FindChild(definition, "extends_type_clause");
        if (heritage is null)
            return;

        foreach (var clause in heritage.NamedChildren)
        {
            switch (clause.Type)
            {
                case "extends_clause":
                    if (clause.ChildByFieldName("value") is { } value && HeritageName(value) is { } baseName)
                        EmitHeritageEdge(CodeEdgeKinds.Inherits, owner, baseName, value, EvidenceQuality.Structural);
                    break;
                case "extends_type_clause":
                    foreach (var type in clause.NamedChildren)
                    {
                        if (HeritageName(type) is { } interfaceName)
                            EmitHeritageEdge(CodeEdgeKinds.Inherits, owner, interfaceName, type, EvidenceQuality.Structural);
                    }

                    break;
                case "implements_clause":
                    foreach (var type in clause.NamedChildren)
                    {
                        if (HeritageName(type) is { } implementedName)
                            EmitHeritageEdge(CodeEdgeKinds.Implements, owner, implementedName, type, EvidenceQuality.Structural);
                    }

                    break;
                default:
                    // JavaScript: the base type sits directly under class_heritage.
                    if (HeritageName(clause) is { } directBase)
                        EmitHeritageEdge(CodeEdgeKinds.Inherits, owner, directBase, clause, EvidenceQuality.Structural);
                    break;
            }
        }
    }

    /// <summary>
    /// Recognizes the well-known prototype-inheritance idioms. These are not
    /// grammar-level inheritance, so they are emitted as <c>Derived</c>
    /// evidence: Andirit computed them deterministically from an exact syntax
    /// pattern, never as resolved or structural fact.
    /// </summary>
    private void TryEmitPrototypeAssignment(TreeSitterSyntaxNode node, Scope scope)
    {
        var left = node.ChildByFieldName("left");
        var right = node.ChildByFieldName("right");
        if (PrototypeOwner(left) is { } child && PrototypeBase(right) is { } parent)
            EmitHeritageEdge(CodeEdgeKinds.Inherits, scope.OwnerNode, parent, node, EvidenceQuality.Derived, child);
    }

    private void TryEmitPrototypeCall(TreeSitterSyntaxNode node, Scope scope)
    {
        var target = CalleeName(node.ChildByFieldName("function"));
        var arguments = node.ChildByFieldName("arguments")?.NamedChildren;
        if (arguments is not { Count: >= 2 })
            return;

        if (target == "util.inherits")
        {
            if (HeritageName(arguments[0]) is { } child && HeritageName(arguments[1]) is { } parent)
                EmitHeritageEdge(CodeEdgeKinds.Inherits, scope.OwnerNode, parent, node, EvidenceQuality.Derived, child);
        }
        else if (target == "Object.setPrototypeOf")
        {
            if (PrototypeOwner(arguments[0]) is { } child && PrototypeOwner(arguments[1]) is { } parent)
                EmitHeritageEdge(CodeEdgeKinds.Inherits, scope.OwnerNode, parent, node, EvidenceQuality.Derived, child);
        }
    }

    private void EmitHeritageEdge(
        CodeEdgeKind kind,
        CodeNodeId defaultOwner,
        string targetName,
        TreeSitterSyntaxNode locationNode,
        EvidenceQuality quality,
        string? sourceName = null)
    {
        var source = sourceName is null ? defaultOwner : StructuralTypeNode(sourceName);
        var target = StructuralTypeNode(targetName);
        AddEdge(kind, source, target, Location(locationNode), $"{kind.Value}:{source.Value}:{target.Value}", quality);
    }

    /// <summary>
    /// Creates a declaration-less type node for a written heritage name. A
    /// dedicated identity namespace and source path keep it separate from
    /// declared symbols and candidates in other files. The node preserves the
    /// written name; the relationship preserves its source location and
    /// evidence quality. Tree-sitter does not bind heritage names semantically.
    /// </summary>
    private CodeNodeId StructuralTypeNode(string name)
    {
        var nodeId = new CodeNodeId($"{_language}:heritage-target:{Hash($"{_path}\n{name}")}");
        AddNode(new CodeGraphNode(nodeId, CodeNodeKinds.Type, ShortName(name), name));
        return nodeId;
    }

    private static string? HeritageName(TreeSitterSyntaxNode? node) =>
        node?.Type switch
        {
            "identifier" or "type_identifier" => BoundedText(node),
            "member_expression" => MemberName(node),
            _ => null
        };

    private static string? PrototypeOwner(TreeSitterSyntaxNode? node)
    {
        if (node is null || node.Type != "member_expression")
            return null;
        var property = node.ChildByFieldName("property");
        if (property is null || BoundedText(property) != "prototype")
            return null;
        return HeritageName(node.ChildByFieldName("object"));
    }

    private static string? PrototypeBase(TreeSitterSyntaxNode? node)
    {
        if (node is null)
            return null;
        if (node.Type == "new_expression")
            return HeritageName(node.ChildByFieldName("constructor"));
        if (node.Type == "call_expression" && CalleeName(node.ChildByFieldName("function")) == "Object.create")
        {
            var first = node.ChildByFieldName("arguments")?.NamedChildren.FirstOrDefault();
            return PrototypeOwner(first) ?? HeritageName(first);
        }

        return null;
    }

    private static TreeSitterSyntaxNode? FindChild(TreeSitterSyntaxNode node, string type)
    {
        foreach (var child in node.NamedChildren)
        {
            if (child.Type == type)
                return child;
        }

        return null;
    }

    private bool TryEmitNamed(
        TreeSitterSyntaxNode definition,
        TreeSitterSyntaxNode? nameNode,
        Scope scope,
        CodeNodeKind kind,
        string discriminator,
        out Scope childScope,
        bool fileScoped = false,
        ScopeOwner owner = ScopeOwner.Other)
    {
        childScope = scope;
        if (nameNode is null || BoundedText(nameNode) is not { } name)
            return false;

        // Identity hashes the discriminator, the lexical qualified name, and
        // — for file-scoped declarations (classes, interfaces) — the source
        // path. Declaration merging is semantic and cannot be verified
        // structurally, so same-name declarations from separate modules must
        // not silently converge. See docs/ecmascript-symbol-identity.md.
        var qualified = Qualify(scope.Container, name);
        var identity = fileScoped ? $"{_path}\n{qualified}" : qualified;
        var hash = Hash($"{discriminator}\n{identity}");
        var symbolId = new CodeSymbolId($"{_language}:{hash}");
        var nodeId = new CodeNodeId($"{_language}:symbol:{hash}");
        var location = Location(nameNode);
        AddNode(new CodeGraphNode(
            nodeId,
            kind,
            name,
            qualified,
            symbolId,
            LanguageProperties()));
        // The declaration covers the whole definition so enclosing-symbol
        // queries resolve body positions; edges keep the precise name span.
        AddDeclaration(symbolId, nodeId, Location(definition));
        AddEdge(CodeEdgeKinds.Declares, scope.FileNode, nodeId, location, $"declares:{nodeId.Value}");
        if (scope.OwnerNode != nodeId)
            AddEdge(CodeEdgeKinds.Contains, scope.OwnerNode, nodeId, location, $"contains:{nodeId.Value}");
        childScope = new Scope(qualified, nodeId, scope.FileNode, owner);
        return true;
    }

    /// <summary>
    /// Emits one named class method, accessor, or method overload signature.
    /// Identity is owner-derived — the owning class node identity plus member
    /// kind, static or instance ownership, and the written name — so unrelated
    /// same-named classes, static and instance members, and methods versus
    /// accessors never converge. A method overload signature and its
    /// implementation share one callable symbol with one physical declaration
    /// each; accessors stay callable members, never fields. Computed, string,
    /// numeric, and private names are skipped rather than guessed.
    /// </summary>
    private void HandleClassMethod(TreeSitterSyntaxNode node, Scope classScope, bool walkBody)
    {
        // A member the parser only recovered partially is not understood
        // structure: skip it rather than emit a fragment.
        if (node.HasError)
            return;

        var nameNode = node.ChildByFieldName("name");
        if (nameNode is not { Type: "property_identifier" } || BoundedText(nameNode) is not { } name)
            return;

        var (isStatic, isAccessor) = MethodShape(node);
        var memberScope = EmitClassMember(
            node,
            classScope,
            CodeNodeKinds.Callable,
            isAccessor ? "accessor" : "method",
            name,
            nameNode,
            isStatic,
            LanguageProperties(),
            ScopeOwner.Callable);
        if (walkBody)
            WalkChildren(node, memberScope);
    }

    /// <summary>
    /// Emits one named class field from a TypeScript
    /// <c>public_field_definition</c> or JavaScript <c>field_definition</c>.
    /// Fields are <c>property</c> members with owner-derived identity: member
    /// kind <c>field</c> plus static or instance ownership, so a field never
    /// converges with a same-named method, accessor, or field of another
    /// class. Computed, string, numeric, and <c>#private</c> names and
    /// <c>accessor</c> auto-accessors are skipped rather than guessed.
    /// Initializer invocations are attributed to the field, never to the
    /// class or an unrelated enclosing callable.
    /// </summary>
    private void HandleClassField(TreeSitterSyntaxNode node, Scope classScope)
    {
        // A member the parser only recovered partially is not understood
        // structure: skip it rather than emit a fragment.
        if (node.HasError)
            return;

        // TypeScript names the field with `name`; JavaScript uses `property`.
        var nameNode = node.ChildByFieldName("name") ?? node.ChildByFieldName("property");
        if (nameNode is not { Type: "property_identifier" } || BoundedText(nameNode) is not { } name)
            return;
        // `accessor x` declares an auto-accessor whose accessor semantics this
        // slice does not model as an ordinary field.
        if (HasToken(node, "accessor"))
            return;

        var memberScope = EmitClassMember(
            node,
            classScope,
            CodeNodeKinds.Property,
            "field",
            name,
            nameNode,
            HasToken(node, "static"),
            FieldProperties(node),
            ScopeOwner.Other);
        WalkChildren(node, memberScope);
    }

    /// <summary>
    /// Gets bounded written metadata for one class field through the existing
    /// property contract: <c>modifiers</c> holds sorted written keywords
    /// (<c>abstract</c>, <c>declare</c>, <c>optional</c>, <c>override</c>,
    /// <c>readonly</c>, <c>static</c>) and <c>access</c> holds an explicitly
    /// written accessibility keyword. No default accessibility is fabricated.
    /// </summary>
    private Dictionary<string, CodePropertyValue> FieldProperties(TreeSitterSyntaxNode node)
    {
        var properties = LanguageProperties();
        var modifiers = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var child in node.Children)
        {
            if (!child.IsNamed)
            {
                switch (child.Text)
                {
                    case "?":
                        modifiers.Add("optional");
                        break;
                    case "readonly":
                        modifiers.Add("readonly");
                        break;
                    case "static":
                        modifiers.Add("static");
                        break;
                    case "abstract":
                        modifiers.Add("abstract");
                        break;
                    case "declare":
                        modifiers.Add("declare");
                        break;
                }

                continue;
            }

            switch (child.Type)
            {
                case "accessibility_modifier":
                    if (BoundedText(child) is { } access)
                        properties[CodePropertyKeys.Access] = new CodeTextProperty(access);
                    break;
                case "override_modifier":
                    modifiers.Add("override");
                    break;
            }
        }

        if (modifiers.Count > 0)
            properties[CodePropertyKeys.Modifiers] = new CodeTextProperty(string.Join(" ", modifiers));
        return properties;
    }

    /// <summary>Gets whether the node carries an anonymous token with this exact text.</summary>
    private static bool HasToken(TreeSitterSyntaxNode node, string text)
    {
        foreach (var child in node.Children)
        {
            if (!child.IsNamed && string.Equals(child.Text, text, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Emits one class member with owner-derived identity:
    /// <c>sha256(member-kind + "\n" + owner-node-id + "\n" + ownership +
    /// "\n" + written-name)</c>. The declaration covers the whole member so
    /// enclosing-symbol queries resolve bodies and initializers, while edges
    /// keep the precise name span.
    /// </summary>
    private Scope EmitClassMember(
        TreeSitterSyntaxNode definition,
        Scope classScope,
        CodeNodeKind kind,
        string memberKind,
        string name,
        TreeSitterSyntaxNode nameNode,
        bool isStatic,
        IReadOnlyDictionary<string, CodePropertyValue> properties,
        ScopeOwner memberScopeOwner)
    {
        var ownership = isStatic ? "static" : "instance";
        var hash = Hash($"{memberKind}\n{classScope.OwnerNode.Value}\n{ownership}\n{name}");
        var symbolId = new CodeSymbolId($"{_language}:{hash}");
        var nodeId = new CodeNodeId($"{_language}:symbol:{hash}");
        var qualified = Qualify(classScope.Container, name);
        var location = Location(nameNode);
        AddNode(new CodeGraphNode(nodeId, kind, name, qualified, symbolId, properties));
        AddDeclaration(symbolId, nodeId, Location(definition));
        AddEdge(CodeEdgeKinds.Declares, classScope.FileNode, nodeId, location, $"declares:{nodeId.Value}");
        AddEdge(CodeEdgeKinds.Contains, classScope.OwnerNode, nodeId, location, $"contains:{nodeId.Value}");
        return new Scope(qualified, nodeId, classScope.FileNode, memberScopeOwner);
    }

    /// <summary>
    /// Gets the written static and accessor shape of a class method definition
    /// or signature. The JavaScript grammar lexes <c>static get</c> before a
    /// newline as a single token, so a written static getter is detected from
    /// that token as well.
    /// </summary>
    private static (bool IsStatic, bool IsAccessor) MethodShape(TreeSitterSyntaxNode node)
    {
        var isStatic = false;
        var isAccessor = false;
        foreach (var child in node.Children)
        {
            if (child.IsNamed)
                continue;
            switch (child.Text)
            {
                case "get" or "set":
                    isAccessor = true;
                    break;
                case "static":
                    isStatic = true;
                    break;
                default:
                    if (child.Text.StartsWith("static", StringComparison.Ordinal))
                    {
                        isStatic = true;
                        isAccessor = true;
                    }

                    break;
            }
        }

        return (isStatic, isAccessor);
    }

    /// <summary>
    /// Emits identifier-named method and property signatures declared directly
    /// in an interface body. These are declarations, not executable code:
    /// nothing here emits invocations, calls, references, or resolved type
    /// relations. Unsupported members (computed, string, numeric, or private
    /// names and call, construct, or index signatures) are skipped rather than
    /// guessed, member type expressions are not traversed, and signatures the
    /// parser only recovered partially emit no facts.
    /// </summary>
    private void EmitInterfaceMembers(TreeSitterSyntaxNode definition, Scope interfaceScope)
    {
        if (definition.ChildByFieldName("body") is not { } body)
            return;

        var children = body.Children;
        for (var index = 0; index < children.Count; index++)
        {
            var member = children[index];
            CodeNodeKind kind;
            string discriminator;
            switch (member.Type)
            {
                case "method_signature":
                    kind = CodeNodeKinds.Callable;
                    discriminator = "interface-method";
                    break;
                case "property_signature":
                    kind = CodeNodeKinds.Property;
                    discriminator = "interface-property";
                    break;
                default:
                    continue;
            }

            // A member the parser only recovered partially — or one directly
            // followed by an error or missing sibling — is not understood
            // structure: skip it rather than emit a fragment.
            var next = index + 1 < children.Count ? children[index + 1] : null;
            if (member.HasError || next is { IsError: true } or { IsMissing: true })
                continue;

            TryEmitInterfaceMember(member, interfaceScope, kind, discriminator);
        }
    }

    /// <summary>
    /// Emits one interface member signature. Identity is owner-derived —
    /// <c>sha256(kind + "\n" + owner-node-id + "\n" + written-name)</c> — so a
    /// signature never converges with a same-named class member or function;
    /// repeated signatures of one name stay one symbol with one physical
    /// declaration each. The declaration covers the whole signature so
    /// enclosing-symbol queries resolve it, while edges keep the precise name
    /// span.
    /// </summary>
    private void TryEmitInterfaceMember(
        TreeSitterSyntaxNode member,
        Scope interfaceScope,
        CodeNodeKind kind,
        string discriminator)
    {
        var nameNode = member.ChildByFieldName("name");
        if (nameNode is not { Type: "property_identifier" } ||
            BoundedText(nameNode) is not { } name)
        {
            return;
        }

        var qualified = Qualify(interfaceScope.Container, name);
        var hash = Hash($"{discriminator}\n{interfaceScope.OwnerNode.Value}\n{name}");
        var symbolId = new CodeSymbolId($"{_language}:{hash}");
        var nodeId = new CodeNodeId($"{_language}:symbol:{hash}");
        var location = Location(nameNode);
        AddNode(new CodeGraphNode(
            nodeId,
            kind,
            name,
            qualified,
            symbolId,
            MemberProperties(member)));
        AddDeclaration(symbolId, nodeId, Location(member));
        AddEdge(CodeEdgeKinds.Declares, interfaceScope.FileNode, nodeId, location, $"declares:{nodeId.Value}");
        AddEdge(CodeEdgeKinds.Contains, interfaceScope.OwnerNode, nodeId, location, $"contains:{nodeId.Value}");
    }

    /// <summary>
    /// Gets bounded written modifier metadata for an interface member through
    /// the existing <c>modifiers</c> property contract: the anonymous
    /// <c>?</c> marker becomes <c>optional</c> and the <c>readonly</c> token
    /// becomes <c>readonly</c>. Other keywords are not recorded in this slice.
    /// </summary>
    private Dictionary<string, CodePropertyValue> MemberProperties(TreeSitterSyntaxNode member)
    {
        var properties = LanguageProperties();
        var modifiers = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var child in member.Children)
        {
            if (child.IsNamed)
                continue;
            switch (child.Text)
            {
                case "?":
                    modifiers.Add("optional");
                    break;
                case "readonly":
                    modifiers.Add("readonly");
                    break;
            }
        }

        if (modifiers.Count > 0)
            properties[CodePropertyKeys.Modifiers] = new CodeTextProperty(string.Join(" ", modifiers));
        return properties;
    }

    private static string? CalleeName(TreeSitterSyntaxNode? node)
    {
        if (node is null)
            return null;

        return node.Type switch
        {
            "identifier" => BoundedText(node),
            "property_identifier" => BoundedText(node),
            "private_property_identifier" => BoundedText(node),
            "this" => "this",
            "super" => "super",
            "member_expression" => MemberName(node),
            _ => null
        };
    }

    private static string? MemberName(TreeSitterSyntaxNode member)
    {
        var target = CalleeName(member.ChildByFieldName("object"));
        var property = CalleeName(member.ChildByFieldName("property"));
        return target is not null && property is not null
            ? $"{target}.{property}"
            : null;
    }

    private static string? StringLiteral(TreeSitterSyntaxNode node)
    {
        foreach (var child in node.NamedChildren)
        {
            if (child.Type == "string_fragment")
                return BoundedText(child);
        }

        var text = node.Text;
        if (text.Length >= 2 && (text[0] == '"' || text[0] == '\'') && text[^1] == text[0])
            text = text[1..^1];
        return text.Length is > 0 and <= 256 ? text : null;
    }

    private static string ShortName(string target)
    {
        var separator = Math.Max(target.LastIndexOf('/'), target.LastIndexOf(':'));
        return separator >= 0 && separator < target.Length - 1
            ? target[(separator + 1)..]
            : target;
    }

    private static string Qualify(string? container, string name) =>
        container is null ? name : $"{container}.{name}";

    /// <summary>Gets source text bounded for identifiers: pathological names never become facts.</summary>
    private static string? BoundedText(TreeSitterSyntaxNode node)
    {
        var text = node.Text;
        return text.Length is > 0 and <= 256 ? text : null;
    }

    private Dictionary<string, CodePropertyValue> LanguageProperties() =>
        new()
        {
            [CodePropertyKeys.Language] = new CodeTextProperty(_language)
        };

    private CodeLocation Location(TreeSitterSyntaxNode node) =>
        new(
            _path,
            node.StartRow + 1,
            node.StartColumn + 1,
            node.EndRow + 1,
            node.EndColumn + 1);

    private static string ExistingFactProviderVersion(string pluginVersion)
    {
        const string currentRevision = ".p10";
        const string existingFactRevision = ".p8";
        if (!pluginVersion.EndsWith(currentRevision, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The p10 ECMAScript profile requires a p10 plugin version while preserving p8 evidence for existing facts.");
        }

        return string.Concat(pluginVersion.AsSpan(0, pluginVersion.Length - currentRevision.Length), existingFactRevision);
    }

    private void WalkChildren(TreeSitterSyntaxNode node, Scope scope, bool isFileLevel = false)
    {
        foreach (var child in node.NamedChildren)
            Walk(child, scope, isFileLevel);
    }

    private void AddNode(CodeGraphNode node)
    {
        // Import and invocation targets arrive without symbol identity and may
        // later converge with a real declaration carrying the same stable
        // identity. The symbol-bearing fact always wins; identical facts
        // collapse.
        if (_result.Nodes.TryGetValue(node.Id.Value, out var existing))
        {
            if (existing.SymbolId is not null || node.SymbolId is null)
                return;
        }

        _result.Nodes[node.Id.Value] = node;
    }

    private void AddDeclaration(
        CodeSymbolId symbolId,
        CodeNodeId nodeId,
        CodeLocation location,
        IReadOnlyDictionary<string, CodePropertyValue>? properties = null)
    {
        var id = new CodeDeclarationId(
            $"{_language}:{Hash($"{symbolId.Value}\n{_path}\n{location.StartLine}:{location.StartColumn}:{location.EndLine}:{location.EndColumn}")}");
        _result.Declarations[id.Value] = new CodeGraphDeclaration(id, symbolId, nodeId, location, properties);
    }

    private void AddEdge(
        CodeEdgeKind kind,
        CodeNodeId source,
        CodeNodeId target,
        CodeLocation location,
        string discriminator,
        EvidenceQuality quality = EvidenceQuality.Structural)
    {
        var id = new CodeEdgeId(
            $"{_language}:{Hash($"{kind.Value}\n{source.Value}\n{target.Value}\n{discriminator}\n{location.StartLine}:{location.StartColumn}:{location.EndLine}:{location.EndColumn}")}");
        if (_result.Edges.TryAdd(id.Value, new CodeGraphEdge(
                id,
                source,
                target,
                kind,
                new CodeEvidence(
                    quality,
                    _providerId,
                    _providerVersion,
                    location))))
            _result.EdgeCounts[kind] = _result.EdgeCounts.GetValueOrDefault(kind) + 1;
    }

    private static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    /// <summary>Lexical scope while walking one file: container, owner, and file nodes.</summary>
    /// <param name="Container">Lexical container path used for display names.</param>
    /// <param name="OwnerNode">Node that owns declarations made in this scope.</param>
    /// <param name="FileNode">File node every declaration is attached to.</param>
    /// <param name="Owner">
    /// What kind of declaration owns this scope. Class scopes turn class-body
    /// members into owner-derived member identities.
    /// </param>
    private sealed record Scope(
        string? Container,
        CodeNodeId OwnerNode,
        CodeNodeId FileNode,
        ScopeOwner Owner = ScopeOwner.File);

    /// <summary>What kind of declaration owns one lexical scope.</summary>
    private enum ScopeOwner
    {
        File,
        Class,
        Interface,
        Callable,
        /// <summary>Type aliases, enums, namespaces, and other owners.</summary>
        Other
    }
}
