namespace Penghou.Andirit;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

/// <summary>
/// Publishes the accepted structural export-name slots and written
/// occurrences into the owning file's ordinary extraction result.
/// </summary>
internal static class EcmaScriptExportGraphPublisher
{
    internal const string Provider = "penghou.hetu.tree-sitter.ecmascript-export-surface";
    internal const string ProviderRevision = "exports1.local-v1";

    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    internal static void Publish(
        string sourcePath,
        string language,
        CodeNodeId fileNodeId,
        string pluginVersion,
        TreeSitterExtractionResult extraction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(language);
        ArgumentNullException.ThrowIfNull(fileNodeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginVersion);
        ArgumentNullException.ThrowIfNull(extraction);

        if (language is not ("javascript" or "typescript"))
            throw new ArgumentOutOfRangeException(nameof(language), language, "Expected an ECMAScript language key.");

        var packageVersionEnd = pluginVersion.IndexOf('+');
        var packageVersion = packageVersionEnd < 0
            ? pluginVersion
            : pluginVersion[..packageVersionEnd];
        if (string.IsNullOrWhiteSpace(packageVersion))
            throw new InvalidOperationException("The ECMAScript plugin version has no package-version prefix.");
        var providerVersion = $"{packageVersion}+{ProviderRevision}";

        foreach (var observation in extraction.ExportObservations)
        {
            if (!string.Equals(observation.SourcePath, sourcePath, StringComparison.Ordinal) ||
                !string.Equals(observation.Language, language, StringComparison.Ordinal) ||
                !string.Equals(observation.NameLocation.Path, sourcePath, StringComparison.Ordinal) ||
                !string.Equals(observation.ConstructLocation.Path, sourcePath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Export observation provenance does not match owning file '{sourcePath}'.");
            }

            // Validate before any UTF-8 encoding or identity construction.
            // The collector already enforces this; keep the publisher boundary
            // safe if a future internal caller supplies an invalid record.
            if (!EcmaScriptExportNameValidator.IsValid(observation.ExportedName, out _))
            {
                throw new InvalidOperationException(
                    $"Export observation in '{sourcePath}' has an invalid public name.");
            }

            var entry = CreateEntry(language, sourcePath, observation.ExportedName);
            AddEntry(extraction, entry, sourcePath);

            var occurrence = CreateOccurrence(
                language,
                fileNodeId,
                entry.Id,
                observation,
                providerVersion);
            AddOccurrence(extraction, occurrence, sourcePath);
        }
    }

    private static CodeGraphNode CreateEntry(
        string language,
        string sourcePath,
        string exportedName)
    {
        var id = new CodeNodeId(
            $"{language}:export:{Hash($"{sourcePath}\n{exportedName}")}");
        return new CodeGraphNode(
            id,
            CodeNodeKinds.Export,
            exportedName,
            exportedName,
            symbolId: null,
            properties: new Dictionary<string, CodePropertyValue>(StringComparer.Ordinal)
            {
                [CodePropertyKeys.Language] = new CodeTextProperty(language)
            });
    }

    private static CodeGraphEdge CreateOccurrence(
        string language,
        CodeNodeId fileNodeId,
        CodeNodeId entryNodeId,
        EcmaScriptExportObservation observation,
        string providerVersion)
    {
        var kind = ExportKindName(observation.ExportKind);
        var via = ExportViaName(observation.ExportForm);
        var projection = ProjectionName(observation.Projection);
        var localNamePresent = observation.LocalName is null ? "0" : "1";
        var localName = observation.LocalName ?? string.Empty;
        var typeOnly = observation.TypeOnly ? "true" : "false";
        var location = observation.NameLocation;
        var span = string.Create(
            CultureInfo.InvariantCulture,
            $"{location.StartLine}:{location.StartColumn}:{location.EndLine}:{location.EndColumn}");
        var discriminator = string.Join(
            '\n',
            "exports",
            fileNodeId.Value,
            entryNodeId.Value,
            observation.ExportedName,
            kind,
            via,
            localNamePresent,
            localName,
            typeOnly,
            projection,
            span);
        var id = new CodeEdgeId($"{language}:{Hash(discriminator)}");

        var properties = new Dictionary<string, CodePropertyValue>(StringComparer.Ordinal)
        {
            [CodePropertyKeys.ExportKind] = new CodeTextProperty(kind),
            [CodePropertyKeys.ExportVia] = new CodeTextProperty(via),
            [CodePropertyKeys.NamespaceProjection] = new CodeTextProperty(projection)
        };
        if (observation.LocalName is not null)
            properties[CodePropertyKeys.LocalName] = new CodeTextProperty(observation.LocalName);
        if (observation.TypeOnly)
            properties[CodePropertyKeys.TypeOnly] = new CodeBooleanProperty(true);

        return new CodeGraphEdge(
            id,
            fileNodeId,
            entryNodeId,
            CodeEdgeKinds.Exports,
            new CodeEvidence(
                EvidenceQuality.Structural,
                Provider,
                providerVersion,
                location),
            properties);
    }

    private static void AddEntry(
        TreeSitterExtractionResult extraction,
        CodeGraphNode candidate,
        string sourcePath)
    {
        if (extraction.Nodes.TryGetValue(candidate.Id.Value, out var existing))
        {
            if (!Equivalent(existing, candidate))
            {
                throw new InvalidOperationException(
                    $"Export entry identity collision '{candidate.Id.Value}' in '{sourcePath}'. " +
                    $"Existing fact: {Describe(existing)} Candidate fact: {Describe(candidate)}");
            }

            return;
        }

        extraction.Nodes.Add(candidate.Id.Value, candidate);
    }

    private static void AddOccurrence(
        TreeSitterExtractionResult extraction,
        CodeGraphEdge candidate,
        string sourcePath)
    {
        if (extraction.Edges.TryGetValue(candidate.Id.Value, out var existing))
        {
            if (!Equivalent(existing, candidate))
            {
                throw new InvalidOperationException(
                    $"Export occurrence identity collision '{candidate.Id.Value}' in '{sourcePath}'. " +
                    $"Existing fact: {Describe(existing)} Candidate fact: {Describe(candidate)}");
            }

            return;
        }

        extraction.Edges.Add(candidate.Id.Value, candidate);
        extraction.EdgeCounts[CodeEdgeKinds.Exports] =
            extraction.EdgeCounts.GetValueOrDefault(CodeEdgeKinds.Exports) + 1;
    }

    private static bool Equivalent(CodeGraphNode left, CodeGraphNode right) =>
        left.Id == right.Id &&
        left.Kind == right.Kind &&
        string.Equals(left.Name, right.Name, StringComparison.Ordinal) &&
        string.Equals(left.QualifiedName, right.QualifiedName, StringComparison.Ordinal) &&
        left.SymbolId == right.SymbolId &&
        PropertiesEqual(left.Properties, right.Properties);

    private static bool Equivalent(CodeGraphEdge left, CodeGraphEdge right) =>
        left.Id == right.Id &&
        left.SourceId == right.SourceId &&
        left.TargetId == right.TargetId &&
        left.Kind == right.Kind &&
        left.Evidence == right.Evidence &&
        PropertiesEqual(left.Properties, right.Properties);

    private static bool PropertiesEqual(
        IReadOnlyDictionary<string, CodePropertyValue> left,
        IReadOnlyDictionary<string, CodePropertyValue> right)
    {
        if (left.Count != right.Count)
            return false;

        foreach (var (key, leftValue) in left)
        {
            if (!right.TryGetValue(key, out var rightValue) || !PropertyEqual(leftValue, rightValue))
                return false;
        }

        return true;
    }

    private static bool PropertyEqual(CodePropertyValue left, CodePropertyValue right) =>
        (left, right) switch
        {
            (CodeTextProperty first, CodeTextProperty second) =>
                string.Equals(first.Value, second.Value, StringComparison.Ordinal),
            (CodeIntegerProperty first, CodeIntegerProperty second) => first.Value == second.Value,
            (CodeNumberProperty first, CodeNumberProperty second) => first.Value.Equals(second.Value),
            (CodeBooleanProperty first, CodeBooleanProperty second) => first.Value == second.Value,
            (CodeTextListProperty first, CodeTextListProperty second) =>
                first.Values.SequenceEqual(second.Values, StringComparer.Ordinal),
            _ => false
        };

    private static string Describe(CodeGraphNode node) =>
        $"node(Id={JsonSerializer.Serialize(node.Id.Value)}, Kind={JsonSerializer.Serialize(node.Kind.Value)}, " +
        $"Name={JsonSerializer.Serialize(node.Name)}, QualifiedName={JsonSerializer.Serialize(node.QualifiedName)}, " +
        $"SymbolId={JsonSerializer.Serialize(node.SymbolId?.Value)}, Properties={DescribeProperties(node.Properties)})";

    private static string Describe(CodeGraphEdge edge) =>
        $"edge(Id={JsonSerializer.Serialize(edge.Id.Value)}, SourceId={JsonSerializer.Serialize(edge.SourceId.Value)}, " +
        $"TargetId={JsonSerializer.Serialize(edge.TargetId.Value)}, Kind={JsonSerializer.Serialize(edge.Kind.Value)}, " +
        $"Evidence=(Quality={edge.Evidence.Quality}, Provider={JsonSerializer.Serialize(edge.Evidence.Provider)}, " +
        $"ProviderVersion={JsonSerializer.Serialize(edge.Evidence.ProviderVersion)}, " +
        $"Location={DescribeLocation(edge.Evidence.Location)}, Confidence={edge.Evidence.Confidence?.ToString(CultureInfo.InvariantCulture) ?? "null"}), " +
        $"Properties={DescribeProperties(edge.Properties)})";

    private static string DescribeProperties(IReadOnlyDictionary<string, CodePropertyValue> properties) =>
        "{" + string.Join(
            ", ",
            properties.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => $"{JsonSerializer.Serialize(pair.Key)}:{DescribeProperty(pair.Value)}")) + "}";

    private static string DescribeProperty(CodePropertyValue property) => property switch
    {
        CodeTextProperty text => JsonSerializer.Serialize(text.Value),
        CodeBooleanProperty boolean => boolean.Value ? "true" : "false",
        CodeIntegerProperty integer => integer.Value.ToString(CultureInfo.InvariantCulture),
        CodeNumberProperty number => number.Value.ToString("R", CultureInfo.InvariantCulture),
        CodeTextListProperty list => JsonSerializer.Serialize(list.Values),
        _ => JsonSerializer.Serialize(property.ToString())
    };

    private static string DescribeLocation(CodeLocation? location) => location is null
        ? "null"
        : $"({JsonSerializer.Serialize(location.Path)},{location.StartLine},{location.StartColumn},{location.EndLine},{location.EndColumn})";

    private static string ExportKindName(EcmaScriptExportKind kind) => kind switch
    {
        EcmaScriptExportKind.Named => "named",
        EcmaScriptExportKind.Default => "default",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown export kind.")
    };

    private static string ExportViaName(EcmaScriptExportForm form) => form switch
    {
        EcmaScriptExportForm.DirectDeclaration or
        EcmaScriptExportForm.DefaultNamedDeclaration or
        EcmaScriptExportForm.DefaultAnonymousDeclaration => "direct",
        EcmaScriptExportForm.LocalName or
        EcmaScriptExportForm.LocalAlias => "local",
        EcmaScriptExportForm.DefaultExpression => "expression",
        _ => throw new ArgumentOutOfRangeException(nameof(form), form, "Unknown export form.")
    };

    private static string ProjectionName(EcmaScriptNamespaceProjection projection) => projection switch
    {
        EcmaScriptNamespaceProjection.Type => "type",
        EcmaScriptNamespaceProjection.Value => "value",
        EcmaScriptNamespaceProjection.Both => "both",
        EcmaScriptNamespaceProjection.Unknown => "unknown",
        _ => throw new ArgumentOutOfRangeException(nameof(projection), projection, "Unknown namespace projection.")
    };

    private static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(StrictUtf8.GetBytes(value)));
}
