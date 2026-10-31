// ============================================================================
// CI-1-SCOPE-R3 oracle — the audit pipeline.
//
// For each manifest row the oracle derives the applicable semantic facts from
// the source and the compilation context, then compares them against the typed
// assertions claimed by the declared label. Malformed input produces
// structured diagnostics; nothing throws.
// ============================================================================

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text.Json.Nodes;

namespace CI1ScopeOracleR3;

public static class R3Audit
{
    public static AuditResult Audit(JsonNode data, OracleEnvironment env, IReadOnlyDictionary<string, string>? ownersOverride = null)
    {
        var sitesOut = new List<SiteAuditR3>();
        var globalErrors = new List<GlobalError>();
        var owners = ownersOverride ?? Frozen.ExpectedOwners;

        if (data["Sites"] is not JsonArray sitesArray)
        {
            globalErrors.Add(new GlobalError("MANIFEST_STRUCTURE_INVALID",
                $"top-level 'Sites' must be an array (found {(data["Sites"] is null ? "nothing" : data["Sites"]!.GetType().Name)})"));
            return new AuditResult(sitesOut, globalErrors);
        }

        var expectedKeys = Frozen.ExpectedCorpusKeys.Select(k => Sem.RowKey(k.Case, k.Path, k.Span)).ToHashSet(StringComparer.Ordinal);
        var seenKeys = new Dictionary<string, (string Lexeme, string Target, string Outcome)>(StringComparer.Ordinal);
        var seenPathSpan = new HashSet<string>(StringComparer.Ordinal);
        var declCache = new Dictionary<(string Comp, string Doc), List<ISymbol>>();

        List<ISymbol> Resolve(string compName, string docId)
        {
            if (!declCache.TryGetValue((compName, docId), out var list))
            {
                list = Sem.ResolveDeclarationsByDoc(env.ByName[compName], docId);
                declCache[(compName, docId)] = list;
            }
            return list;
        }

        static string Describe(DefDescriptor d)
            => d.TreePath is null
                ? $"{d.DocId} in {d.Compilation} [{d.AssemblyIdentity}]"
                : $"{d.DocId} in {d.Compilation} [{d.AssemblyIdentity}] at {d.TreePath}({d.DeclStart}..{d.DeclEnd})";

        int rowIndex = -1;
        foreach (var siteNode in sitesArray)
        {
            rowIndex++;
            if (siteNode is not JsonObject site)
            {
                globalErrors.Add(new GlobalError("MANIFEST_STRUCTURE_INVALID", $"Sites[{rowIndex}] is not an object"));
                continue;
            }

            var errors = new List<SiteError>();
            var quals = new List<Qualification>();
            void Add(string code, string detail) => errors.Add(new SiteError(code, detail));

            // ---- structure-safe field extraction ----
            string? ExtractString(string field)
            {
                if (!site.ContainsKey(field))
                {
                    Add("MANIFEST_STRUCTURE_INVALID", $"Sites[{rowIndex}]: required field '{field}' is missing");
                    return null;
                }
                if (site[field] is not JsonValue v || !v.TryGetValue<string>(out var s))
                {
                    Add("MANIFEST_STRUCTURE_INVALID", $"Sites[{rowIndex}]: field '{field}' must be a string");
                    return null;
                }
                return s;
            }

            var cs = ExtractString("Case");
            var p = ExtractString("FixturePath");
            var lexeme = ExtractString("Lexeme");
            var declaredTarget = ExtractString("ExpectedTargetSourceSignature");
            var declaredOutcomeText = ExtractString("ExpectedOutcome");
            var repositoryPath = ExtractString("RepositoryPath");
            var basis = ExtractString("Basis");

            int[]? span = null;
            if (!site.ContainsKey("Span"))
            {
                Add("MANIFEST_STRUCTURE_INVALID", $"Sites[{rowIndex}]: required field 'Span' is missing");
            }
            else if (site["Span"] is not JsonArray arr)
            {
                Add("MANIFEST_STRUCTURE_INVALID", $"Sites[{rowIndex}]: field 'Span' must be an array of 4 integers");
            }
            else if (arr.Count != 4)
            {
                Add("COORDINATE_LEXEME_MISMATCH", "invalid coordinate: span must have 4 elements");
            }
            else
            {
                var list = new List<int>();
                var bad = false;
                foreach (var v in arr)
                {
                    if (v is JsonValue iv && iv.TryGetValue<int>(out var i)) list.Add(i);
                    else { bad = true; break; }
                }
                if (bad) Add("MANIFEST_STRUCTURE_INVALID", $"Sites[{rowIndex}]: field 'Span' must contain integers");
                else span = list.ToArray();
            }

            SiteAuditR3 Blank(OccurrenceCategory cat, string compilation) => new(
                cs ?? "?", p ?? "?", span ?? Array.Empty<int>(), lexeme ?? "?", compilation,
                declaredTarget ?? "?", declaredOutcomeText ?? "?", cat,
                null, null, false, Array.Empty<string>(), Array.Empty<string?>(), "None",
                null, null, "?", false,
                DispatchFacet.NotApplicable, false, false,
                new ConditionalActivity(false, false, false, false),
                null, null,
                OutcomeClass.Unrecognized, OutcomeClass.Unrecognized, null, null,
                repositoryPath, "not-evaluated", "not-evaluated", errors, quals);

            // Malformed structure short-circuits the row (never throws, never cascades).
            if (errors.Any(e => e.Code == "MANIFEST_STRUCTURE_INVALID") || cs is null || p is null || span is null)
            {
                sitesOut.Add(Blank(OccurrenceCategory.Unknown, "?"));
                continue;
            }

            var key = Sem.RowKey(cs, p, span);

            // ---- corpus universe: expected selection identity ----
            var inUniverse = expectedKeys.Contains(key);
            if (!inUniverse)
                Add("CORPUS_ROW_UNEXPECTED", $"occurrence '{key}' is not part of the frozen 32-row selection universe");
            if (seenKeys.TryGetValue(key, out var prev))
            {
                if (prev.Lexeme != lexeme || prev.Target != declaredTarget || prev.Outcome != declaredOutcomeText)
                    Add("CORPUS_CONTRADICTION", $"duplicate occurrence '{key}' is declared with contradictory fields");
                else
                    Add("DUPLICATE_OCCURRENCE", "same occurrence key audited twice");
            }
            else
            {
                seenKeys[key] = (lexeme ?? "", declaredTarget ?? "", declaredOutcomeText ?? "");
            }
            if (!seenPathSpan.Add(p + ":" + string.Join(',', span)))
                Add("DUPLICATE_OCCURRENCE", "same fixture path and span already audited");

            // ---- coordinates: one-based UTF-16, end-exclusive ----
            var (_, text) = env.ReadFixture(p);
            var st = SourceText.From(text);
            int start = 0, end = 0;
            var coordinateOk = true;
            try
            {
                start = st.Lines[span[0] - 1].Start + span[1] - 1;
                end = st.Lines[span[2] - 1].Start + span[3] - 1;
                if (end < start) { Add("COORDINATE_LEXEME_MISMATCH", "end before start"); coordinateOk = false; }
                else if (st.ToString(new TextSpan(start, end - start)) != lexeme)
                {
                    Add("COORDINATE_LEXEME_MISMATCH", "span text differs from declared lexeme");
                    coordinateOk = false;
                }
            }
            catch
            {
                Add("COORDINATE_LEXEME_MISMATCH", "invalid coordinate");
                coordinateOk = false;
            }

            // ---- context selection (frozen mapping, oracle-owned) ----
            var c = p == "Repeated.cs" ? env.ByName["repeated"]
                : p.Contains("AfterDelete") ? env.ByName["deleted"]
                : p.Contains("AfterShift") ? env.ByName["shifted"]
                : p.Contains("CrossProject") ? env.ByName["app"]
                : cs == "C20" ? env.ByName["enabled"]
                : env.ByName["acceptance"];
            var compilationName = c.AssemblyName!;

            if (!coordinateOk)
            {
                sitesOut.Add(Blank(OccurrenceCategory.Unknown, compilationName));
                continue;
            }

            // ---- syntax node, category, binding ----
            if (c.SyntaxTrees.SingleOrDefault(t => t.FilePath == p) is not CSharpSyntaxTree tree)
            {
                Add("SYNTAX_MISSING", $"fixture '{p}' is not part of the selected compilation '{compilationName}'");
                sitesOut.Add(Blank(OccurrenceCategory.Unknown, compilationName));
                continue;
            }
            var model = c.GetSemanticModel(tree, ignoreAccessibility: false);
            var root = tree.GetRoot();
            var node = root.DescendantNodes().Where(n => n.Span.Start == start && n.Span.End == end)
                .OrderBy(n => n is InvocationExpressionSyntax ? 0 : 1).FirstOrDefault();
            node ??= root.DescendantNodes().OfType<ConstructorInitializerSyntax>()
                .SingleOrDefault(n => n.Span.Contains(start) && n.Span.End == end);
            if (node is null)
            {
                Add("SYNTAX_MISSING", "no syntax node matches the span");
                sitesOut.Add(Blank(OccurrenceCategory.Unknown, compilationName));
                continue;
            }

            var category = OccurrenceCategory.Unknown;
            SymbolInfo info = default;
            ISymbol? target = null;
            if (node is TypeOfExpressionSyntax typeOf)
            {
                category = OccurrenceCategory.TypeOf;
                target = model.GetTypeInfo(typeOf.Type).Type;
            }
            else if (node is ConstructorInitializerSyntax)
            {
                category = OccurrenceCategory.Constructor;
                info = model.GetSymbolInfo(node);
                target = info.Symbol;
            }
            else if (node is ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax)
            {
                category = OccurrenceCategory.Constructor;
                info = model.GetSymbolInfo(node);
                target = info.Symbol;
            }
            else if (node is InvocationExpressionSyntax inv && inv.Expression.ToString() == "nameof")
            {
                category = OccurrenceCategory.NameOf;
                info = model.GetSymbolInfo(inv.ArgumentList.Arguments[0].Expression);
                target = info.Symbol ?? model.GetMemberGroup(inv.ArgumentList.Arguments[0].Expression).SingleOrDefault();
            }
            else if (node is InvocationExpressionSyntax)
            {
                category = OccurrenceCategory.OrdinaryInvocation;
                info = model.GetSymbolInfo(node);
                target = info.Symbol;
            }
            else if (node is MemberAccessExpressionSyntax or IdentifierNameSyntax)
            {
                info = model.GetSymbolInfo(node);
                target = info.Symbol;
                category = target is IMethodSymbol ? OccurrenceCategory.MethodGroup : OccurrenceCategory.Unknown;
            }
            else
            {
                info = model.GetSymbolInfo(node);
                target = info.Symbol;
                Add("SYNTAX_CATEGORY_UNKNOWN", "unrecognised occurrence category");
            }

            // ---- independently derived semantic facts ----
            var boundDoc = Sem.Doc(target);
            var candidateDocs = info.CandidateSymbols.Select(Sem.Doc).ToArray();
            var boundEligible = Sem.EligibleLocal(target, env.SourceCompilations);
            var boundDef = Sem.DescribeDefinition(target, env);
            var boundSourceDeclarations = target is null ? Array.Empty<string>()
                : Sem.Def(target).Locations.Where(l => l.IsInSource && l.SourceTree is not null)
                    .Select(l => l.SourceTree!.FilePath).Distinct().ToArray();
            var derivedDispatch = category == OccurrenceCategory.OrdinaryInvocation
                ? Sem.DeriveDispatch(target, node)
                : DispatchFacet.NotApplicable;

            // Cross-project: defining compilation differs from the audited one AND
            // the audited compilation references it as a source-project edge.
            var derivedCrossProject = false;
            if (boundDef is { } bd0 && bd0.Compilation is not ("metadata" or "unknown") && bd0.Compilation != compilationName)
            {
                var edges = env.ReferenceEdges.TryGetValue(compilationName, out var e) ? e : Array.Empty<string>();
                if (edges.Contains(bd0.Compilation)) derivedCrossProject = true;
                else Add("TARGET_IDENTITY_UNVERIFIED",
                    $"bound definition resolves in compilation '{bd0.Compilation}' which the audited compilation '{compilationName}' does not reference as a project");
            }
            var derivedAfterReplacement = env.ReplacementCompilations.Contains(compilationName);

            // Conditional activity — inspected for EVERY row, independent of label.
            var activeDefault = Sem.ActiveAt(env, p, start, end, Array.Empty<string>());
            var activeExtra = Sem.ActiveAt(env, p, start, end, new[] { "CI1_EXTRA" });
            var cond = new ConditionalActivity(activeDefault, activeExtra, true, activeExtra && !activeDefault);

            // ---- derived outcome class (source/compiler evidence only) ----
            var derived = OutcomeClass.Unrecognized;
            string? derivedSubtype = null;
            switch (category)
            {
                case OccurrenceCategory.Constructor: derived = OutcomeClass.Unsupported; derivedSubtype = "constructor"; break;
                case OccurrenceCategory.MethodGroup: derived = OutcomeClass.Unsupported; derivedSubtype = "method-group"; break;
                case OccurrenceCategory.TypeOf: derived = OutcomeClass.Unsupported; derivedSubtype = "type-reference"; break;
                case OccurrenceCategory.NameOf: derived = OutcomeClass.Unsupported; derivedSubtype = "nameof-reference"; break;
                default:
                    if (target is IMethodSymbol) derived = boundEligible ? OutcomeClass.Confirmed : OutcomeClass.External;
                    else if (info.CandidateSymbols.Length > 0) derived = OutcomeClass.Ambiguous;
                    else derived = OutcomeClass.Unresolved;
                    break;
            }

            // ---- declared outcome mapping (exhaustive recognized vocabulary) ----
            ClaimedOutcome claimed;
            if (declaredOutcomeText is null || !Frozen.OutcomeVocabulary.TryGetValue(declaredOutcomeText, out var parsed))
            {
                claimed = new ClaimedOutcome(OutcomeClass.Unrecognized, null, DispatchFacet.NotApplicable, false, false, ConditionalClaim.None, false);
                Add("OUTCOME_UNRECOGNIZED", $"declared outcome '{declaredOutcomeText}' is not in the recognised vocabulary");
            }
            else claimed = parsed;

            // ---- exact target identity ----
            DefDescriptor? expectedDef = null;
            string expectedProject = "?";
            var symbolEqualityAgrees = false;
            if (declaredTarget == "unknown")
            {
                expectedProject = "none";
                if (target is not null || candidateDocs.Length != 0)
                    Add("TARGET_IDENTITY_MISMATCH", "expected no target and no candidates");
            }
            else if (declaredTarget == "unknown-string-or-Exception-overload")
            {
                expectedProject = "none";
                if (target is not null || candidateDocs.Length != 2
                    || !candidateDocs.Contains("M:CI1Corpus.Ambiguous.Pick(System.String)")
                    || !candidateDocs.Contains("M:CI1Corpus.Ambiguous.Pick(System.Exception)"))
                    Add("TARGET_IDENTITY_MISMATCH", "expected exactly the string/Exception overload candidates");
            }
            else if (declaredTarget is null || !Frozen.TargetTable.TryGetValue(declaredTarget, out var exp))
            {
                Add("TARGET_UNRECOGNIZED", $"declared target '{declaredTarget}' is not in the frozen target table");
            }
            else
            {
                expectedProject = exp.DeclaringProject;
                if (boundDoc != exp.DocId)
                {
                    Add("TARGET_IDENTITY_MISMATCH", $"bound '{boundDoc}' does not match expected '{exp.DocId}'");
                }
                else if (exp.DeclaringProject == "metadata")
                {
                    if (boundDef is { } bdm && bdm.Compilation != "metadata")
                        Add("TARGET_DEFINING_PROJECT_MISMATCH",
                            $"declared target is a metadata-only external definition but the bound definition resolves in source-built compilation '{bdm.Compilation}'");
                    var sourceDeclComps = env.SourceCompilations
                        .Where(sc => Resolve(sc.AssemblyName!, exp.DocId).Count > 0).Select(sc => sc.AssemblyName!).ToArray();
                    if (sourceDeclComps.Length > 0)
                        Add("TARGET_IDENTITY_UNVERIFIED",
                            $"declared external target is also declared by source-built compilation(s) {string.Join(",", sourceDeclComps)}; exact identity cannot be established");
                }
                else
                {
                    var expectedCompName = exp.DeclaringProject == "audited" ? compilationName : exp.DeclaringProject;
                    if (!env.ByName.ContainsKey(expectedCompName))
                    {
                        Add("TARGET_IDENTITY_UNVERIFIED", $"expected declaring project '{expectedCompName}' is not a known source compilation");
                    }
                    else
                    {
                        var decls = Resolve(expectedCompName, exp.DocId);
                        if (decls.Count != 1)
                        {
                            Add("TARGET_IDENTITY_UNVERIFIED",
                                $"expected declaring compilation '{expectedCompName}' contains {decls.Count} declarations with documentation ID '{exp.DocId}'; exact correspondence cannot be established");
                        }
                        else
                        {
                            expectedDef = Sem.DescribeDefinition(decls[0], env);
                            if (boundDef is null || expectedDef is null)
                            {
                                Add("TARGET_IDENTITY_UNVERIFIED", "bound or expected definition could not be described");
                            }
                            else if (boundDef.Compilation != expectedDef.Compilation)
                            {
                                Add("TARGET_DEFINING_PROJECT_MISMATCH",
                                    $"bound definition resolves in compilation '{boundDef.Compilation}' but the expected definition is in '{expectedDef.Compilation}'");
                            }
                            else if (!Sem.DescriptorEquals(boundDef, expectedDef))
                            {
                                Add("TARGET_IDENTITY_MISMATCH",
                                    $"bound definition {Describe(boundDef)} does not equal the expected definition {Describe(expectedDef)}");
                            }
                            symbolEqualityAgrees = SymbolEqualityComparer.Default.Equals(Sem.Def(target!), decls[0]);
                        }
                    }
                }
                if (exp.EligibleLocal && !boundEligible)
                    Add("TARGET_ELIGIBILITY_MISMATCH", "expected an eligible indexed local definition but the bound target is not eligible local");
                if (exp.Kind == TargetKind.Constructor && (target is not IMethodSymbol cm || cm.MethodKind != MethodKind.Constructor))
                    Add("TARGET_KIND_MISMATCH", "expected a constructor binding");
                if (exp.Kind == TargetKind.TypeToken && target is not INamedTypeSymbol)
                    Add("TARGET_KIND_MISMATCH", "expected a type token binding");
            }

            // ---- outcome agreement: class, subtype, conditional activity, provenance ----
            if (claimed.Recognized)
            {
                if (claimed.Class != derived)
                {
                    Add("OUTCOME_CLASS_MISMATCH", $"declared class {claimed.Class} does not match derived class {derived}");
                }
                else if (claimed.Class == OutcomeClass.Unsupported && claimed.Subtype != derivedSubtype)
                {
                    Add("OUTCOME_SUBTYPE_MISMATCH", $"declared subtype '{claimed.Subtype}' does not match derived subtype '{derivedSubtype}'");
                }
                else
                {
                    // Conditional-compilation activity, checked independently of the label.
                    if (claimed.Conditional == ConditionalClaim.None && cond.ConditionalOnly)
                        Add("CONDITIONAL_ACTIVITY_MISMATCH",
                            "declared outcome asserts no conditional activity but the occurrence requires CI1_EXTRA (inactive in the default configuration)");
                    if (claimed.Conditional == ConditionalClaim.InactiveDefaultConfirmedWithCI1Extra)
                    {
                        if (!cond.ConditionalOnly)
                            Add("CONDITIONAL_ACTIVITY_MISMATCH", "declared outcome asserts default-inactive/CI1_EXTRA-only activity but the occurrence is not conditional");
                        if (cond.ActiveInDefault)
                            Add("DEFAULT_CONTEXT_ACTIVE", "conditional site is active in the default context");
                    }
                    // Provenance facets for uniquely bound confirmed/external outcomes.
                    if (claimed.Class is OutcomeClass.Confirmed or OutcomeClass.External)
                    {
                        var diffs = new List<string>();
                        if (claimed.Dispatch != derivedDispatch) diffs.Add($"dispatch: claimed {claimed.Dispatch} vs derived {derivedDispatch}");
                        if (claimed.CrossProject != derivedCrossProject) diffs.Add($"cross-project: claimed {claimed.CrossProject} vs derived {derivedCrossProject}");
                        if (claimed.AfterReplacement != derivedAfterReplacement) diffs.Add($"after-replacement: claimed {claimed.AfterReplacement} vs derived {derivedAfterReplacement}");
                        if (diffs.Count > 0)
                            Add("OUTCOME_PROVENANCE_MISMATCH", "outcome provenance does not match derived evidence: " + string.Join("; ", diffs));
                    }
                }
            }

            // ---- lexical owner identity ----
            var derivedOwner = Sem.LexicalOwner(model, node);
            string? expectedOwnerNow = null;
            if (inUniverse)
            {
                var ownerKey = $"{cs}|{p}|{span[0]},{span[1]}";
                if (!owners.TryGetValue(ownerKey, out var expectedOwner))
                {
                    Add("OWNER_EXPECTATION_MISSING", $"no frozen owner expectation for occurrence '{ownerKey}'");
                }
                else
                {
                    expectedOwnerNow = expectedOwner;
                    if (!string.Equals(derivedOwner, expectedOwner, StringComparison.Ordinal))
                        Add("OWNER_MISMATCH", $"lexical owner is '{derivedOwner ?? "(none)"}' but the frozen expectation is '{expectedOwner}'");
                }
            }

            // ---- repository path (authorized fixture mapping) ----
            var repoVerdict = "not-evaluated";
            if (inUniverse && repositoryPath is not null)
            {
                if (!Frozen.AuthorizedRepositoryPaths.TryGetValue(p, out var authorized))
                {
                    Add("REPOSITORY_PATH_MISMATCH", $"fixture '{p}' has no authorized repository mapping");
                    repoVerdict = "unauthorized-fixture";
                }
                else if (repositoryPath != authorized)
                {
                    Add("REPOSITORY_PATH_MISMATCH",
                        $"declared RepositoryPath '{repositoryPath}' does not match the authorized mapping '{authorized}' for fixture '{p}'");
                    repoVerdict = "mismatch";
                }
                else repoVerdict = "verified";
            }

            // ---- basis provenance classification ----
            var basisVerdict = "not-evaluated";
            if (basis is not null)
            {
                if (basis == Frozen.BasisPlain || basis == Frozen.BasisConditional)
                {
                    basisVerdict = "recognized; coordinate claim corroborated by the span check; selection and no-extractor-output claims are human-reviewed, not mechanically qualified";
                    quals.Add(new Qualification("BASIS_NOT_MECHANICALLY_QUALIFIED",
                        "basis is a human-reviewed provenance description; only its coordinate claim is corroborated mechanically"));
                }
                else
                {
                    Add("BASIS_UNRECOGNIZED", "declared basis does not match the recognized provenance vocabulary for this corpus");
                    basisVerdict = "unrecognized";
                }
            }

            // ---- explicit lifecycle qualification (never silently dropped) ----
            if (claimed.LifecycleAfterReplacement || derivedAfterReplacement)
            {
                quals.Add(new Qualification("LIFECYCLE_NOT_QUALIFIED",
                    "the after-replacement lifecycle assertion (publication replacement, stale-fact retirement, lifecycle correctness) is not established by single-compilation evidence; the confirmable facets (replacement-snapshot context, binding, exact identity) are verified; the lifecycle claim is recorded as human-reviewed / not yet qualified"));
            }

            sitesOut.Add(new SiteAuditR3(
                cs, p, span, lexeme ?? "", compilationName,
                declaredTarget ?? "?", declaredOutcomeText ?? "?", category,
                boundDoc, target?.ContainingAssembly?.Identity.ToString(), boundEligible,
                boundSourceDeclarations, candidateDocs, info.CandidateReason.ToString(),
                boundDef, expectedDef, expectedProject, symbolEqualityAgrees,
                derivedDispatch, derivedCrossProject, derivedAfterReplacement, cond,
                derivedOwner, expectedOwnerNow,
                derived, claimed.Class, derivedSubtype, claimed.Subtype,
                repositoryPath, repoVerdict, basisVerdict, errors, quals));
        }

        // ---- corpus completeness: expected keys must all be present ----
        foreach (var expected in Frozen.ExpectedCorpusKeys)
        {
            var k = Sem.RowKey(expected.Case, expected.Path, expected.Span);
            if (!seenKeys.ContainsKey(k))
                globalErrors.Add(new GlobalError("CORPUS_ROW_MISSING", $"expected occurrence '{k}' is absent from the manifest"));
        }

        return new AuditResult(sitesOut, globalErrors);
    }
}
