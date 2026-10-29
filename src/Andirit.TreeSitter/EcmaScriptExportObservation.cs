namespace Andirit;

using System.Buffers;
using System.Globalization;
using System.Text;

/// <summary>Syntax facts for one supported written ECMAScript export occurrence.</summary>
internal sealed record EcmaScriptExportObservation(
    string SourcePath,
    string Language,
    string ExportedName,
    string? LocalName,
    EcmaScriptExportKind ExportKind,
    EcmaScriptExportForm ExportForm,
    bool TypeOnly,
    EcmaScriptNamespaceProjection Projection,
    CodeLocation NameLocation,
    CodeLocation ConstructLocation)
{
    /// <summary>
    /// Orders observations by written source occurrence and then by explicit
    /// syntax fields. No name-based deduplication is performed.
    /// </summary>
    internal static IComparer<EcmaScriptExportObservation> SourceOrderComparer { get; } =
        Comparer<EcmaScriptExportObservation>.Create(static (left, right) =>
        {
            var comparison = StringComparer.Ordinal.Compare(left.SourcePath, right.SourcePath);
            if (comparison != 0)
                return comparison;

            comparison = CompareLocation(left.NameLocation, right.NameLocation);
            if (comparison != 0)
                return comparison;

            comparison = CompareLocation(left.ConstructLocation, right.ConstructLocation);
            if (comparison != 0)
                return comparison;

            comparison = StringComparer.Ordinal.Compare(left.ExportedName, right.ExportedName);
            if (comparison != 0)
                return comparison;

            comparison = NullableOrdinal.Compare(left.LocalName, right.LocalName);
            if (comparison != 0)
                return comparison;

            comparison = left.ExportKind.CompareTo(right.ExportKind);
            if (comparison != 0)
                return comparison;

            comparison = left.ExportForm.CompareTo(right.ExportForm);
            if (comparison != 0)
                return comparison;

            comparison = left.TypeOnly.CompareTo(right.TypeOnly);
            return comparison != 0 ? comparison : left.Projection.CompareTo(right.Projection);
        });

    private static int CompareLocation(CodeLocation left, CodeLocation right)
    {
        var comparison = left.StartLine.CompareTo(right.StartLine);
        if (comparison != 0)
            return comparison;
        comparison = left.StartColumn.CompareTo(right.StartColumn);
        if (comparison != 0)
            return comparison;
        comparison = left.EndLine.CompareTo(right.EndLine);
        return comparison != 0 ? comparison : left.EndColumn.CompareTo(right.EndColumn);
    }

    private static class NullableOrdinal
    {
        internal static int Compare(string? left, string? right) =>
            left is null
                ? right is null ? 0 : -1
                : right is null ? 1 : StringComparer.Ordinal.Compare(left, right);
    }
}

internal enum EcmaScriptExportKind
{
    Named,
    Default
}

internal enum EcmaScriptExportForm
{
    DirectDeclaration,
    LocalName,
    LocalAlias,
    DefaultNamedDeclaration,
    DefaultAnonymousDeclaration,
    DefaultExpression
}

internal enum EcmaScriptNamespaceProjection
{
    Type,
    Value,
    Both,
    Unknown
}

/// <summary>Strict EC6C-A2 public-name validation, independent of graph identity.</summary>
internal static class EcmaScriptExportNameValidator
{
    internal const int MaximumScalarValues = 256;

    internal static bool IsValid(string? value, out int scalarCount)
    {
        scalarCount = 0;
        if (string.IsNullOrEmpty(value))
            return false;

        var hasNonWhitespaceScalar = false;
        var remaining = value.AsSpan();
        while (!remaining.IsEmpty)
        {
            if (Rune.DecodeFromUtf16(remaining, out var scalar, out var consumed) != OperationStatus.Done)
            {
                scalarCount = 0;
                return false;
            }

            scalarCount++;
            if (scalarCount > MaximumScalarValues ||
                Rune.GetUnicodeCategory(scalar) == UnicodeCategory.Control)
            {
                scalarCount = 0;
                return false;
            }

            hasNonWhitespaceScalar |= !Rune.IsWhiteSpace(scalar);
            remaining = remaining[consumed..];
        }

        return scalarCount > 0 && hasNonWhitespaceScalar;
    }
}

/// <summary>
/// Captures only file-scope export syntax from the same AST traversal used by
/// the ECMAScript graph extractor. It never resolves a written local name.
/// </summary>
internal static class EcmaScriptExportShapeCollector
{
    internal static void Capture(
        TreeSitterSyntaxNode statement,
        string sourcePath,
        string language,
        ICollection<EcmaScriptExportObservation> destination)
    {
        ArgumentNullException.ThrowIfNull(statement);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(language);
        ArgumentNullException.ThrowIfNull(destination);

        if (statement.IsError || statement.IsMissing ||
            statement.ChildByFieldName("source") is not null)
        {
            return;
        }

        if (FindDefaultToken(statement) is { } defaultToken)
        {
            CaptureDefault(statement, defaultToken, sourcePath, language, destination);
            return;
        }

        var declaration = statement.ChildByFieldName("declaration");
        if (declaration is not null)
        {
            CaptureDeclaration(statement, declaration, sourcePath, language, destination);
            return;
        }

        var clause = statement.NamedChildren.FirstOrDefault(child => child.Type == "export_clause");
        if (clause is not null && language == "javascript" && HasJavaScriptTypeOnlyPrefix(statement))
            return;
        if (clause is not null && !statement.Children.Any(child => child.Type == "*"))
            CaptureLocalList(statement, clause, sourcePath, language, destination);
    }

    private static bool HasJavaScriptTypeOnlyPrefix(TreeSitterSyntaxNode statement)
    {
        var children = statement.Children;
        var clauseIndex = -1;
        for (var index = 0; index < children.Count; index++)
        {
            if (children[index].Type == "export_clause")
            {
                clauseIndex = index;
                break;
            }
        }

        return clauseIndex > 0 && children.Take(clauseIndex).Any(child =>
            child.IsNamed && child.Type == "identifier" && child.Text == "type" ||
            child.Type == "ERROR" && child.IsError && child.Children.Count == 1 &&
            child.Children[0].IsNamed &&
            child.Children[0].Type == "identifier" &&
            child.Children[0].Text == "type");
    }
    private static void CaptureDeclaration(
        TreeSitterSyntaxNode statement,
        TreeSitterSyntaxNode declaration,
        string sourcePath,
        string language,
        ICollection<EcmaScriptExportObservation> destination)
    {
        switch (declaration.Type)
        {
            case "lexical_declaration":
            case "variable_declaration":
                CaptureVariables(statement, declaration, sourcePath, language, destination);
                return;
            case "function_declaration":
            case "function_signature":
            case "generator_function_declaration":
                CaptureDirectNamed(statement, declaration, sourcePath, language, destination, EcmaScriptNamespaceProjection.Value);
                return;
            case "class_declaration":
                CaptureDirectNamed(statement, declaration, sourcePath, language, destination, EcmaScriptNamespaceProjection.Both);
                return;
            case "abstract_class_declaration":
                if (language != "typescript")
                    return;
                CaptureDirectNamed(statement, declaration, sourcePath, language, destination, EcmaScriptNamespaceProjection.Both);
                return;
            case "interface_declaration":
            case "type_alias_declaration":
                if (language != "typescript")
                    return;
                CaptureDirectNamed(statement, declaration, sourcePath, language, destination, EcmaScriptNamespaceProjection.Type);
                return;
            case "enum_declaration":
                if (language != "typescript")
                    return;
                CaptureDirectNamed(statement, declaration, sourcePath, language, destination, EcmaScriptNamespaceProjection.Both);
                return;
            case "internal_module":
            case "module":
                if (language != "typescript")
                    return;
                CaptureDirectNamed(statement, declaration, sourcePath, language, destination, EcmaScriptNamespaceProjection.Unknown);
                return;
            default:
                // Ambient wrappers, export-equals, and unrecognized forms are
                // deliberately outside the accepted observation allowlist.
                return;
        }
    }

    private static void CaptureVariables(
        TreeSitterSyntaxNode statement,
        TreeSitterSyntaxNode declaration,
        string sourcePath,
        string language,
        ICollection<EcmaScriptExportObservation> destination)
    {
        foreach (var declarator in declaration.NamedChildren)
        {
            if (declarator.Type != "variable_declarator" || declarator.IsError ||
                ContainsErrorOrMissing(declarator) ||
                declarator.ChildByFieldName("name") is not { Type: "identifier" } nameToken ||
                !TryReadPublicName(nameToken, out var name))
            {
                continue;
            }

            Add(
                statement,
                nameToken,
                sourcePath,
                language,
                name,
                name,
                EcmaScriptExportKind.Named,
                EcmaScriptExportForm.DirectDeclaration,
                typeOnly: false,
                EcmaScriptNamespaceProjection.Value,
                destination);
        }
    }

    private static void CaptureDirectNamed(
        TreeSitterSyntaxNode statement,
        TreeSitterSyntaxNode declaration,
        string sourcePath,
        string language,
        ICollection<EcmaScriptExportObservation> destination,
        EcmaScriptNamespaceProjection projection)
    {
        if (ContainsErrorOrMissing(declaration) ||
            declaration.ChildByFieldName("name") is not { } nameToken ||
            !TryReadIdentifierName(nameToken, allowEscapes: false, out var name))
        {
            return;
        }

        Add(
            statement,
            nameToken,
            sourcePath,
            language,
            name,
            name,
            EcmaScriptExportKind.Named,
            EcmaScriptExportForm.DirectDeclaration,
            typeOnly: false,
            projection,
            destination);
    }

    private static void CaptureLocalList(
        TreeSitterSyntaxNode statement,
        TreeSitterSyntaxNode clause,
        string sourcePath,
        string language,
        ICollection<EcmaScriptExportObservation> destination)
    {
        var statementTypeOnly = EcmaScriptExtractionProfile.HasStatementTypeModifier(statement);
        if (language != "typescript" && statementTypeOnly)
            return;

        var children = clause.Children;
        for (var index = 0; index < children.Count; index++)
        {
            var child = children[index];
            if (child.Type == "export_specifier")
            {
                if (!IsTrailingAsRecoveryError(children, index))
                {
                    CaptureLocalSpecifier(
                        statement,
                        child,
                        sourcePath,
                        language,
                        statementTypeOnly,
                        destination);
                }

                continue;
            }

            // tree-sitter-typescript 0.23.2 recovers the valid contextual
            // binding `export { type }` as one ERROR node around the exact
            // unnamed `type` token. Preserve only that single-token grammar
            // shape; other error fragments remain unsupported.
            if (language == "typescript" && IsLoneContextualTypeError(child))
            {
                var typeToken = child.Children[0];
                if (TryReadPublicName(typeToken, out var name))
                {
                    Add(
                        statement,
                        typeToken,
                        sourcePath,
                        language,
                        name,
                        name,
                        string.Equals(name, "default", StringComparison.Ordinal)
                            ? EcmaScriptExportKind.Default
                            : EcmaScriptExportKind.Named,
                        EcmaScriptExportForm.LocalName,
                        statementTypeOnly,
                        statementTypeOnly
                            ? EcmaScriptNamespaceProjection.Type
                            : EcmaScriptNamespaceProjection.Unknown,
                        destination);
                }
            }
        }
    }

    private static void CaptureLocalSpecifier(
        TreeSitterSyntaxNode statement,
        TreeSitterSyntaxNode specifier,
        string sourcePath,
        string language,
        bool statementTypeOnly,
        ICollection<EcmaScriptExportObservation> destination)
    {
        if (specifier.IsError || specifier.IsMissing)
            return;
        if (ContainsErrorOrMissing(specifier))
        {
            if (language == "typescript")
            {
                CaptureContextualTypeAliasRecovery(
                    statement,
                    specifier,
                    sourcePath,
                    language,
                    statementTypeOnly,
                    destination);
            }

            return;
        }

        var localToken = specifier.ChildByFieldName("name");
        if (localToken is null && IsLoneContextualTypeName(specifier))
            localToken = specifier.Children[0];
        if (localToken is null ||
            !TryReadWrittenLocalName(localToken, out var localName))
        {
            return;
        }

        var aliasToken = specifier.ChildByFieldName("alias");
        var publicToken = aliasToken ?? localToken;
        if (!TryReadPublicName(publicToken, out var exportedName))
            return;

        var inlineTypeOnly = EcmaScriptExtractionProfile.IsTypeOnlySpecifier(specifier);
        if (language != "typescript" && inlineTypeOnly)
            return;

        var typeOnly = statementTypeOnly || inlineTypeOnly;
        Add(
            statement,
            publicToken,
            sourcePath,
            language,
            exportedName,
            localName,
            string.Equals(exportedName, "default", StringComparison.Ordinal)
                ? EcmaScriptExportKind.Default
                : EcmaScriptExportKind.Named,
            aliasToken is null ? EcmaScriptExportForm.LocalName : EcmaScriptExportForm.LocalAlias,
            typeOnly,
            typeOnly ? EcmaScriptNamespaceProjection.Type : EcmaScriptNamespaceProjection.Unknown,
            destination);
    }

    private static void CaptureContextualTypeAliasRecovery(
        TreeSitterSyntaxNode statement,
        TreeSitterSyntaxNode specifier,
        string sourcePath,
        string language,
        bool statementTypeOnly,
        ICollection<EcmaScriptExportObservation> destination)
    {
        var children = specifier.Children;
        if (children.Count != 3 ||
            children[0].IsNamed ||
            children[0].IsError ||
            children[0].IsMissing ||
            children[0].Type != "type" ||
            children[0].Text != "type" ||
            children[1].Type != "ERROR" ||
            !children[1].IsError ||
            children[1].Children.Count != 1 ||
            !children[1].Children[0].IsNamed ||
            children[1].Children[0].Type != "identifier" ||
            children[1].Children[0].Text != "as" ||
            ContainsErrorOrMissing(children[2]) ||
            !TryReadWrittenLocalName(children[0], out var localName) ||
            !TryReadPublicName(children[2], out var exportedName))
        {
            return;
        }

        var typeOnly = statementTypeOnly;
        Add(
            statement,
            children[2],
            sourcePath,
            language,
            exportedName,
            localName,
            string.Equals(exportedName, "default", StringComparison.Ordinal)
                ? EcmaScriptExportKind.Default
                : EcmaScriptExportKind.Named,
            EcmaScriptExportForm.LocalAlias,
            typeOnly,
            typeOnly ? EcmaScriptNamespaceProjection.Type : EcmaScriptNamespaceProjection.Unknown,
            destination);
    }
    private static bool IsTrailingAsRecoveryError(
        IReadOnlyList<TreeSitterSyntaxNode> children,
        int specifierIndex) =>
        specifierIndex + 1 < children.Count &&
        children[specifierIndex + 1].IsError &&
        children[specifierIndex + 1].Text == "as";

    private static bool IsLoneContextualTypeError(TreeSitterSyntaxNode node)
    {
        if (node.Type != "ERROR" || !node.IsError || node.IsMissing || node.Children.Count != 1)
            return false;

        var token = node.Children[0];
        return !token.IsNamed &&
            !token.IsError &&
            !token.IsMissing &&
            token.Type == "type" &&
            token.Text == "type";
    }
    private static void CaptureDefault(
        TreeSitterSyntaxNode statement,
        TreeSitterSyntaxNode defaultToken,
        string sourcePath,
        string language,
        ICollection<EcmaScriptExportObservation> destination)
    {
        if (ContainsErrorOrMissing(defaultToken))
            return;

        var declaration = statement.ChildByFieldName("declaration");
        if (declaration is not null)
        {
            if (ContainsErrorOrMissing(declaration) ||
                declaration.Type is not ("function_declaration" or "generator_function_declaration" or "class_declaration" or "abstract_class_declaration"))
            {
                return;
            }

            var localToken = declaration.ChildByFieldName("name");
            var localName = localToken is not null &&
                TryReadWrittenLocalName(localToken, out var writtenLocalName)
                    ? writtenLocalName
                    : null;
            if (localToken is not null && localName is null)
                return;

            var isClass = declaration.Type is "class_declaration" or "abstract_class_declaration";
            Add(
                statement,
                defaultToken,
                sourcePath,
                language,
                "default",
                localName,
                EcmaScriptExportKind.Default,
                localToken is null
                    ? EcmaScriptExportForm.DefaultAnonymousDeclaration
                    : EcmaScriptExportForm.DefaultNamedDeclaration,
                typeOnly: false,
                isClass ? EcmaScriptNamespaceProjection.Both : EcmaScriptNamespaceProjection.Value,
                destination);
            return;
        }

        var value = statement.ChildByFieldName("value");
        if (value is null || ContainsErrorOrMissing(value))
            return;

        if (value.Type is "function_expression" or "generator_function")
        {
            Add(
                statement,
                defaultToken,
                sourcePath,
                language,
                "default",
                localName: null,
                EcmaScriptExportKind.Default,
                EcmaScriptExportForm.DefaultAnonymousDeclaration,
                typeOnly: false,
                EcmaScriptNamespaceProjection.Value,
                destination);
            return;
        }

        if (value.Type is "class" or "class_expression")
        {
            Add(
                statement,
                defaultToken,
                sourcePath,
                language,
                "default",
                localName: null,
                EcmaScriptExportKind.Default,
                EcmaScriptExportForm.DefaultAnonymousDeclaration,
                typeOnly: false,
                EcmaScriptNamespaceProjection.Both,
                destination);
            return;
        }

        var expressionLocalName = value.Type is ("identifier" or "type_identifier") &&
            TryReadWrittenLocalName(value, out var identifierName)
                ? identifierName
                : null;
        Add(
            statement,
            defaultToken,
            sourcePath,
            language,
            "default",
            expressionLocalName,
            EcmaScriptExportKind.Default,
            EcmaScriptExportForm.DefaultExpression,
            typeOnly: false,
            EcmaScriptNamespaceProjection.Value,
            destination);
    }

    private static bool ContainsErrorOrMissing(TreeSitterSyntaxNode node)
    {
        if (node.IsError || node.IsMissing)
            return true;

        return node.Children.Any(ContainsErrorOrMissing);
    }
    private static TreeSitterSyntaxNode? FindDefaultToken(TreeSitterSyntaxNode statement) =>
        statement.Children.FirstOrDefault(child =>
            !child.IsNamed && child.Type == "default" && !child.IsError && !child.IsMissing);

    private static bool IsLoneContextualTypeName(TreeSitterSyntaxNode specifier)
    {
        var children = specifier.Children;
        return children.Count == 1 &&
            !children[0].IsNamed &&
            !children[0].IsError &&
            !children[0].IsMissing &&
            children[0].Type == "type" &&
            children[0].Text == "type";
    }

    private static bool TryReadPublicName(TreeSitterSyntaxNode token, out string name)
    {
        name = string.Empty;
        if (token.Type == "string")
        {
            if (!TryReadUnescapedString(token, out name))
                return false;
        }
        else if (!TryReadIdentifierName(token, allowEscapes: false, out name))
        {
            return false;
        }

        return EcmaScriptExportNameValidator.IsValid(name, out _);
    }

    private static bool TryReadWrittenLocalName(TreeSitterSyntaxNode token, out string name)
    {
        name = string.Empty;
        if (ContainsErrorOrMissing(token))
            return false;

        if (token.Type == "string")
            return TryReadUnescapedString(token, out name);

        if (!IsIdentifierNameToken(token, allowEscapes: true))
            return false;

        name = token.Text;
        return name.Length > 0;
    }

    private static bool TryReadIdentifierName(
        TreeSitterSyntaxNode token,
        bool allowEscapes,
        out string name)
    {
        name = string.Empty;
        if (ContainsErrorOrMissing(token) ||
            !IsIdentifierNameToken(token, allowEscapes))
        {
            return false;
        }

        var text = token.Text;
        if (!allowEscapes && text.Contains("\\", StringComparison.Ordinal))
            return false;
        if (!EcmaScriptExportNameValidator.IsValid(text, out _) ||
            token.Type is not ("identifier" or "type_identifier") && !IsIdentifierText(text))
        {
            return false;
        }

        name = text;
        return true;
    }

    private static bool IsIdentifierNameToken(TreeSitterSyntaxNode token, bool allowEscapes)
    {
        if (ContainsErrorOrMissing(token) || token.Children.Count != 0)
            return false;

        if (token.Type is "identifier" or "type_identifier")
            return allowEscapes || !token.Text.Contains("\\", StringComparison.Ordinal);

        // Reserved words can be unnamed leaf nodes in a grammar name field.
        // The field position proves the name role; this check rejects syntax
        // punctuation without guessing a local binding.
        return !token.IsNamed && IsIdentifierText(token.Text) &&
            (allowEscapes || !token.Text.Contains("\\", StringComparison.Ordinal));
    }

    private static bool IsIdentifierText(string text)
    {
        if (text.Length == 0)
            return false;

        var first = true;
        var remaining = text.AsSpan();
        while (!remaining.IsEmpty)
        {
            if (Rune.DecodeFromUtf16(remaining, out var scalar, out var consumed) != OperationStatus.Done)
                return false;

            var category = Rune.GetUnicodeCategory(scalar);
            var validStart = scalar.Value is '$' or '_' || category is
                UnicodeCategory.UppercaseLetter or
                UnicodeCategory.LowercaseLetter or
                UnicodeCategory.TitlecaseLetter or
                UnicodeCategory.ModifierLetter or
                UnicodeCategory.OtherLetter or
                UnicodeCategory.LetterNumber;
            var validContinue = validStart || scalar.Value is 0x200c or 0x200d || category is
                UnicodeCategory.NonSpacingMark or
                UnicodeCategory.SpacingCombiningMark or
                UnicodeCategory.DecimalDigitNumber or
                UnicodeCategory.ConnectorPunctuation;
            if (first ? !validStart : !validContinue)
                return false;

            first = false;
            remaining = remaining[consumed..];
        }

        return !first;
    }

    private static bool TryReadUnescapedString(TreeSitterSyntaxNode token, out string value)
    {
        value = string.Empty;
        if (ContainsErrorOrMissing(token) ||
            token.NamedChildren.Any(child =>
                child.Type is "escape_sequence" or "ERROR" || child.IsError || child.IsMissing))
        {
            return false;
        }

        var text = token.Text;
        if (text.Length < 2 || text[0] is not ('\'' or '"') || text[^1] != text[0])
            return false;

        var content = text[1..^1];
        if (content.Contains("\\", StringComparison.Ordinal))
            return false;

        value = content;
        return true;
    }

    private static void Add(
        TreeSitterSyntaxNode statement,
        TreeSitterSyntaxNode nameToken,
        string sourcePath,
        string language,
        string exportedName,
        string? localName,
        EcmaScriptExportKind exportKind,
        EcmaScriptExportForm exportForm,
        bool typeOnly,
        EcmaScriptNamespaceProjection projection,
        ICollection<EcmaScriptExportObservation> destination)
    {
        destination.Add(new(
            sourcePath,
            language,
            exportedName,
            localName,
            exportKind,
            exportForm,
            typeOnly,
            projection,
            Location(nameToken, sourcePath),
            Location(statement, sourcePath)));
    }

    private static CodeLocation Location(TreeSitterSyntaxNode node, string path) =>
        new(path, node.StartRow + 1, node.StartColumn + 1, node.EndRow + 1, node.EndColumn + 1);
}
