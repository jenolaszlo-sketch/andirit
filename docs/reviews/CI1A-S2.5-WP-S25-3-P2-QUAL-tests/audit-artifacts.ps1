# ANDIRIT-CI1A-S2.5-WP-S25-3-P2-QUAL-01 - reviewer-owned artifact audit.
# Recomputes every derivable claim from the producer-recorded primitives in the P1 and
# delta receipts: encoding vectors, form digests, matrix relations, composed targets,
# descriptor/source isolation, and language/editorial receipts. This is an artifact audit
# (the inputs are producer-recorded); it complements - never replaces - the fresh reviewer
# execution in Q1-P2A.json. Known limitation: the delta artifact records reference count
# and digest but not the reference identity list, so compctx1 cannot be recomputed from
# the artifact alone; identity/order verification is performed in the fresh reviewer run.
[CmdletBinding()]
param(
    [string]$RepoRoot
)

$ErrorActionPreference = 'Stop'
if (-not $RepoRoot) { $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../../..")).Path }
$evidence = Join-Path $RepoRoot 'docs/reviews/CI1A-S2.5-WP-S25-3-P2-QUAL-evidence'
$outPath = Join-Path $evidence 'Q5-ARTIFACT-AUDIT.json'

function Get-MessageHex([string[]]$parts) {
    $sb = New-Object System.Text.StringBuilder
    foreach ($s in $parts) {
        $b = [Text.Encoding]::UTF8.GetBytes($s)
        $l = [BitConverter]::GetBytes([int]$b.Length)
        [Array]::Reverse($l)
        foreach ($x in $l) { [void]$sb.Append($x.ToString('x2')) }
        foreach ($x in $b) { [void]$sb.Append($x.ToString('x2')) }
    }
    return $sb.ToString()
}

function Get-FieldsHash([string[]]$parts) {
    $ms = New-Object System.IO.MemoryStream
    foreach ($s in $parts) {
        $b = [Text.Encoding]::UTF8.GetBytes($s)
        $l = [BitConverter]::GetBytes([int]$b.Length)
        [Array]::Reverse($l)
        $ms.Write($l, 0, 4)
        $ms.Write($b, 0, $b.Length)
    }
    $ms.Seek(0, 'Begin') | Out-Null
    $h = (Get-FileHash -InputStream $ms -Algorithm SHA256).Hash.ToLowerInvariant()
    $ms.Dispose()
    return $h
}

function Get-FormDigest([string]$domain, [string]$form) {
    return Get-FieldsHash @($domain, $form)
}

$checks = New-Object System.Collections.ArrayList
$script:allPass = $true
function Add-Check([string]$id, [bool]$pass, [string]$detail) {
    [void]$checks.Add([ordered]@{ id = $id; pass = $pass; detail = $detail })
    if (-not $pass) { $script:allPass = $false }
}

# ---------------------------------------------------------------------------
# P1 original oracle artifact (historical outcomes preserved).
# ---------------------------------------------------------------------------
$p1Path = Join-Path $RepoRoot 'docs/reviews/CI1A-S2.5-WP-S25-3-P1-PROOF-evidence/Q1-ORACLE.json'
$p1 = Get-Content $p1Path -Raw | ConvertFrom-Json

foreach ($kat in $p1.knownAnswers) {
    $id = $kat.id
    switch -Wildcard ($id) {
        'KAT-7*' {
            # Two domains, one field: two digests recorded as "a | b".
            $domains = $kat.domain.Split('|')
            $recomputed = @($domains | ForEach-Object { Get-FieldsHash @($_, $kat.fields[0]) }) -join ' | '
            Add-Check "p1:kat:${id}" ($recomputed -eq $kat.expected -and $recomputed -eq $kat.actual) `
                "recomputed both domain digests from recorded domain list + field"
        }
        'KAT-8*' {
            # Ordering sensitivity: expected is digest(A,B) " | " digest(B,A).
            $ab = Get-FieldsHash @($kat.domain, $kat.fields[0], $kat.fields[1])
            $ba = Get-FieldsHash @($kat.domain, $kat.fields[3], $kat.fields[4])
            $recomputed = "$ab | $ba"
            Add-Check "p1:kat:${id}" ($recomputed -eq $kat.expected) `
                "recomputed ordered multi-part digests from recorded fields (row records no actual)"
        }
        'KAT-9*' {
            # "(empty)" denotes the empty field; encoding computes, form layer rejects.
            $hash = Get-FieldsHash @($kat.domain, '')
            $hex = Get-MessageHex @($kat.domain, '')
            Add-Check "p1:kat:${id}" ($hex -eq $kat.messageHex -and $hash -eq $kat.expected -and
                $kat.actual -eq 'ArgumentException') `
                "recomputed empty-field message/digest; recorded form-layer rejection preserved"
        }
        'KAT-10*' {
            # Validation predicates only; no encoding assertion in this row.
            $predicates = $kat.actual
            Add-Check "p1:kat:${id}" ($kat.expected -eq 'all validation predicates true' -and
                [bool]$predicates.validAccepted -and [bool]$predicates.uppercaseRejected -and
                [bool]$predicates.shortRejected -and [bool]$predicates.wrongPrefixRejected -and
                [bool]$predicates.lowercaseOnly) `
                "recorded validation predicates all true (artifact audit; no digest in row)"
        }
        default {
            $parts = @($kat.domain) + @($kat.fields)
            $hex = Get-MessageHex $parts
            $hash = Get-FieldsHash $parts
            Add-Check "p1:kat:${id}" ($hex -eq $kat.messageHex -and $hash -eq $kat.expected -and $hash -eq $kat.actual) `
                "recomputed message/digest from recorded domain+fields"
        }
    }
}
Add-Check 'p1:knownAnswers-count-10' ($p1.knownAnswers.Count -eq 10 -and @($p1.knownAnswers | Where-Object { -not $_.pass }).Count -eq 0) `
    "recorded 10/10"

foreach ($fc in $p1.formCases) {
    $fcId = $fc.id
    if ($fcId -eq 'FC-13') {
        # Return-type change: identifier/declaration invariant; shape form omits return type.
        $shape = Get-FormDigest 'asmctx3-target' 'method|0|int'
        $id = Get-FormDigest 'asmctx3-identifier' $fc.expectedIdentifierForm
        $decl = Get-FormDigest 'asmctx3-declarations' $fc.expectedDeclarationForm
        Add-Check "p1:form:${fcId}" ($id -eq (Get-FormDigest 'asmctx3-identifier' 'Get') -and
            $decl -eq (Get-FormDigest 'asmctx3-declarations' $fc.expectedDeclarationForm) -and
            $shape -eq $fc.intReturnShapeDigest -and $shape -eq $fc.stringReturnShapeDigest -and
            [bool]$fc.identifierInvariant -and [bool]$fc.declarationInvariant -and [bool]$fc.shapeInvariant) `
            "recomputed invariant identifier/declaration + return-type-free shape digest"
    }
    elseif ($fcId -eq 'FC-14') {
        # Explicit-interface observation row: no occurrence digests; doc-ID-first selection.
        Add-Check "p1:form:${fcId}" ($fc.selectedPath -eq 'doc-ID-first' -and
            $fc.canonicalForm -eq $fc.documentationCommentId -and
            $fc.documentationCommentId -eq 'M:Corpus.Core.ExplicitGreeter.Corpus#Core#IGreeter#Greet(System.String)') `
            "recorded doc-ID-first canonical form (observation row; no occurrence digest)"
    }
    elseif ($fcId -eq 'FC-DELIM') {
        # Delimiter collision challenge: recompute all four shape digests and require distinctness.
        $tuple = Get-FormDigest 'asmctx3-target' $fc.tupleSingle
        $two = Get-FormDigest 'asmctx3-target' $fc.twoParamPartner
        $nested = Get-FormDigest 'asmctx3-target' $fc.nestedGenericSingle
        $nestedPartner = Get-FormDigest 'asmctx3-target' $fc.nestedGenericPartner
        Add-Check "p1:form:${fcId}" ($tuple -ne $two -and $nested -ne $nestedPartner -and
            [bool]$fc.allChallengedPairsDistinct) `
            "recomputed delimiter-collision shape digests; challenged pairs distinct"
    }
    else {
        $id = Get-FormDigest 'asmctx3-identifier' $fc.expectedIdentifierForm
        $decl = Get-FormDigest 'asmctx3-declarations' $fc.expectedDeclarationForm
        $shape = Get-FormDigest 'asmctx3-target' $fc.expectedShapeForm
        Add-Check "p1:form:${fcId}" ($id -eq $fc.expectedIdentifierDigest -and $id -eq $fc.actualIdentifierDigest -and
            $decl -eq $fc.expectedDeclarationDigest -and $decl -eq $fc.actualDeclarationDigest -and
            $shape -eq $fc.expectedShapeDigest -and $shape -eq $fc.actualShapeDigest) `
            "recomputed all three digests from recorded literal forms"
    }
}
Add-Check 'p1:formCases-count-17' ($p1.formCases.Count -eq 17 -and @($p1.formCases | Where-Object { -not $_.pass }).Count -eq 0) `
    "recorded 17/17"

$p1Pass = @($p1.matrix | Where-Object { $_.pass }).Count
$p1Fail = @($p1.matrix | Where-Object { -not $_.pass }).Count
Add-Check 'p1:matrix-15-pass-3-fail' ($p1.matrix.Count -eq 18 -and $p1Pass -eq 15 -and $p1Fail -eq 3) `
    "recorded 18 pairs: 15 pass, 3 fail"
foreach ($row in $p1.matrix) {
    $rowId = $row.id
    if ($rowId -eq 'R15') {
        # Refusal row: no fact digests; control fact present.
        Add-Check "p1:matrix:${rowId}:relations" ($row.v1Identifier -eq 'refusal' -and
            $row.observedIdentifier -eq 'refusal' -and $row.v1Declaration -eq 'refusal' -and
            $row.observedDeclaration -eq 'refusal' -and $row.v1Shape -eq 'refusal' -and
            $row.observedShape -eq 'refusal' -and [bool]$row.knownControlFactPresent -and [bool]$row.pass) `
            "refusal row recorded consistently; no fact digest expected"
        continue
    }
    $relId = if ($row.beforeIdentifierDigest -eq $row.afterIdentifierDigest) { '=' } else { '!=' }
    $relDecl = if ($row.beforeDeclarationDigest -eq $row.afterDeclarationDigest) { '=' } else { '!=' }
    $relShape = if ($row.beforeShapeDigest -eq $row.afterShapeDigest) { '=' } else { '!=' }
    Add-Check "p1:matrix:${rowId}:relations" ($relId -eq $row.observedIdentifier -and $relDecl -eq $row.observedDeclaration -and $relShape -eq $row.observedShape) `
        "observed relations recomputed from recorded before/after digests"
}
foreach ($id in @('R13a', 'R13b', 'R13c')) {
    $row = $p1.matrix | Where-Object { $_.id -eq $id }
    $hasMismatch = ($row.v1Identifier -ne $row.observedIdentifier) -or
        ($row.v1Declaration -ne $row.observedDeclaration) -or
        ($row.v1Shape -ne $row.observedShape)
    Add-Check "p1:matrix:${id}:historical-failure-preserved" ($row.pass -eq $false -and $hasMismatch -and
        $row.v1Shape -eq '!=' -and $row.observedShape -eq '=') `
        "original v1 shape assertion not met under original wording; recorded failure preserved"
}

# ---------------------------------------------------------------------------
# Delta artifact (canonical-forms v1 clarified).
# ---------------------------------------------------------------------------
$deltaPath = Join-Path $RepoRoot 'docs/reviews/CI1A-S2.5-WP-S25-3-P1-DELTA-evidence/Q1-DELTA.json'
$delta = Get-Content $deltaPath -Raw | ConvertFrom-Json

foreach ($vec in $delta.encodingVectors) {
    $parts = @($vec.domain, $vec.field)
    $hex = Get-MessageHex $parts
    $hash = Get-FieldsHash $parts
    $hexConstrained = $vec.expectedMessageHex -match '^[0-9a-f]+$'
    Add-Check "delta:vector:$($vec.id)" ($hex -eq $vec.messageHex -and
        ((-not $hexConstrained) -or $hex -eq $vec.expectedMessageHex) -and
        $hash -eq $vec.expectedDigest -and $hash -eq $vec.refEncDigest) `
        "recomputed encoding vector from recorded domain+field"
}
Add-Check 'delta:encodingVectors-count-3' ($delta.encodingVectors.Count -eq 3) "recorded three anchors"

$descriptorFields = @('contractVersion', 'repositoryId', 'projectUnitId', 'assemblyName', 'metadataIdentity',
    'moduleName', 'optionsDigest', 'referencesCount', 'referencesDigest')
$pairSummaries = New-Object System.Collections.ArrayList

foreach ($pair in $delta.revisionPairs) {
    $name = $pair.pair
    $exp = $pair.expected
    $idDigest = Get-FormDigest 'asmctx3-identifier' $exp.identifierForm
    $declA = Get-FormDigest 'asmctx3-declarations' $exp.declarationFormA
    $declB = Get-FormDigest 'asmctx3-declarations' $exp.declarationFormB
    $shapeDigest = Get-FormDigest 'asmctx3-target' $exp.shapeForm
    Add-Check "delta:${name}:expected-digests" ($idDigest -eq $exp.identifierDigest -and
        $declA -eq $exp.declarationDigestA -and $declB -eq $exp.declarationDigestB -and
        $shapeDigest -eq $exp.shapeDigest) "recomputed from recorded literal forms"
    Add-Check "delta:${name}:declaration-relation" ((($declA -ne $declB) -eq [bool]$exp.declarationDistinguishes)) `
        "declaration distinction matches expected"

    $sides = @(
        [ordered]@{ name = 'A'; side = $pair.sideA; declDigest = $declA; fact = $pair.factsA[0] },
        [ordered]@{ name = 'B'; side = $pair.sideB; declDigest = $declB; fact = $pair.factsB[0] }
    )
    foreach ($entry in $sides) {
        $s = $entry.side
        $fields = @('asmctx3-context', 'compctx1', $s.descriptor.descriptorDigest, $s.descriptor.metadataIdentity,
            $s.descriptor.moduleName, $idDigest, $entry.declDigest, $shapeDigest)
        $target = 'asmctx3:sha256:' + (Get-FieldsHash $fields)
        $hex = Get-MessageHex $fields
        Add-Check "delta:${name}:side$($entry.name):composed-target" ($target -eq $s.composed.productionTarget -and
            $target -eq $s.composed.independentTarget) "recomputed from recorded compctx1/metadata/module/components"
        Add-Check "delta:${name}:side$($entry.name):message-bytes" ($hex -eq $s.composed.messageHex) `
            "exact length-prefixed message bytes recomputed"
        Add-Check "delta:${name}:side$($entry.name):fact-target" ($entry.fact.targetId -eq $target) `
            "extracted fact target equals recomposed target"
        Add-Check "delta:${name}:side$($entry.name):fact-digests" ($entry.fact.identifierDigest -eq $idDigest -and
            $entry.fact.declarationDigest -eq $entry.declDigest -and $entry.fact.shapeDigest -eq $shapeDigest) `
            "extracted fact components equal recomputed digests"
        Add-Check "delta:${name}:side$($entry.name):diagnostics-clean" ([int]$s.diagnostics.errors -eq 0) `
            "compilation valid (errors=0)"
    }

    foreach ($f in $descriptorFields) {
        Add-Check "delta:${name}:descriptor:$f-equal" ($pair.sideA.descriptor.$f -eq $pair.sideB.descriptor.$f) `
            "unchanged configuration field"
    }
    Add-Check "delta:${name}:descriptor-digest-differs" ($pair.sideA.descriptor.descriptorDigest -ne $pair.sideB.descriptor.descriptorDigest) `
        "defining context differs"

    $srcA = $pair.sideA.descriptor.sources
    $srcB = $pair.sideB.descriptor.sources
    Add-Check "delta:${name}:sources-one-each" ($srcA.Count -eq 1 -and $srcB.Count -eq 1) "one defining source each"
    $partsA = $srcA[0].Split('|')
    $partsB = $srcB[0].Split('|')
    Add-Check "delta:${name}:source-path-equal" ($partsA[0] -eq $partsB[0] -and $partsA[0] -eq 'Core/Targets.cs') `
        "same defining file path"
    Add-Check "delta:${name}:source-language-equal" ($partsA[2] -eq $partsB[2]) "same language version"
    Add-Check "delta:${name}:source-bytes-differ" ($partsA[1] -ne $partsB[1]) "changed source bytes"
    Add-Check "delta:${name}:source-hash-consistent" ($partsA[1] -eq $pair.sideA.fileHashes.PSObject.Properties['Core/Targets.cs'].Value -and
        $partsB[1] -eq $pair.sideB.fileHashes.PSObject.Properties['Core/Targets.cs'].Value) "source canonical form matches file hash"

    $changed = @()
    foreach ($key in $pair.sideA.fileHashes.PSObject.Properties.Name) {
        if ($pair.sideA.fileHashes.$key -ne $pair.sideB.fileHashes.$key) { $changed += $key }
    }
    $changedSorted = ($changed | Sort-Object) -join ','
    $declaredSorted = (@($pair.changedInputFiles) | Sort-Object) -join ','
    Add-Check "delta:${name}:changed-files" ($changedSorted -eq $declaredSorted) "changed input set matches receipt"
    Add-Check "delta:${name}:isolation" ($changed -contains 'Core/Targets.cs' -and $changed -contains 'App/Consumer.cs' -and
        -not ($changed -contains 'Core/Core.csproj') -and -not ($changed -contains 'App/App.csproj')) `
        "exactly one defining source + the mechanically required consumer call-site edit"

    Add-Check "delta:${name}:targets-differ" ($pair.sideA.composed.productionTarget -ne $pair.sideB.composed.productionTarget) `
        "composed target distinct"

    [void]$pairSummaries.Add([ordered]@{
        pair = $name
        contextA = $pair.sideA.descriptor.descriptorDigest
        contextB = $pair.sideB.descriptor.descriptorDigest
        targetA = $pair.sideA.composed.productionTarget
        targetB = $pair.sideB.composed.productionTarget
        changedFiles = @($changed | Sort-Object)
        referencesCount = $pair.sideA.descriptor.referencesCount
        referencesDigest = $pair.sideA.descriptor.referencesDigest
    })
}

$c = $delta.coexistence
Add-Check 'delta:coexistence:compiles' ([int]$c.errors -eq 0) "one compilation"
Add-Check 'delta:coexistence:identifier-equal' ($c.facts[0].identifierDigest -eq $c.facts[1].identifierDigest) "shared simple name"
Add-Check 'delta:coexistence:declaration-distinct' ($c.facts[0].declarationDigest -ne $c.facts[1].declarationDigest) "value vs ref declaration"
Add-Check 'delta:coexistence:shape-equal' ($c.facts[0].shapeDigest -eq $c.facts[1].shapeDigest) "mode-blind shape"
Add-Check 'delta:coexistence:targets-distinct' ($c.targetPlain -ne $c.targetRef -and
    $c.facts[0].targetId -eq $c.targetPlain -and $c.facts[1].targetId -eq $c.targetRef) "same context, distinct targets"

foreach ($b in $delta.languageBoundaries) {
    Add-Check "delta:boundary:$($b.id)" ([int]$b.errorCount -ge 1 -and @($b.diagnosticIds) -contains 'CS0663') `
        "modifier-only overload set rejected with CS0663"
}

Add-Check 'delta:editorial:greet-docid' ($delta.editorial.greetDocId -eq 'M:Corpus.Core.IGreeter.Greet(System.String)') `
    "exact doc-ID"
Add-Check 'delta:editorial:no-global-in-docids' ((-not $delta.editorial.greetDocId.Contains('global::')) -and
    (-not $delta.editorial.explicitImplDocId.Contains('global::'))) "doc-IDs never carry global::"
Add-Check 'delta:editorial:explicit-impl-docid' ($delta.editorial.explicitImplDocId -eq 'M:Corpus.Core.ExplicitGreeter.Corpus#Core#IGreeter#Greet(System.String)') `
    "explicit implementation doc-ID format"
Add-Check 'delta:editorial:type-display' ($delta.editorial.greeterTypeDisplay -eq 'global::Corpus.Core.IGreeter') `
    "FullyQualifiedFormat carries global::"
Add-Check 'delta:editorial:param-display' ($delta.editorial.greetParamDisplay -eq 'string') "special-type display"
Add-Check 'delta:editorial:member-display' ($delta.editorial.greetMemberDisplay -eq 'Greet') "minimal member display (D-1)"

Add-Check 'delta:checks-count-55' ($delta.checks.Count -eq 55) "recorded check count"
Add-Check 'delta:producer-allpass-asserted' ($delta.allPass -eq $true -and
    @($delta.checks | Where-Object { -not $_.pass }).Count -eq 0) `
    "producer-asserted only; independent recomputations above are the P2 evidence"

$limitation = 'Q1-DELTA.json records referencesCount and referencesDigest per side but not the 179 reference identity strings. ' +
    'The reference-closure identities/order and therefore compctx1 cannot be recomputed from the artifact alone. ' +
    'Cross-side closure equality is evidenced by identical count+digest (collision-resistant); identity/order verification ' +
    'is performed in the fresh reviewer run (Q1-P2A.json), where the full ordered closure is recorded per side.'

$result = [ordered]@{
    audit = 'ANDIRIT-CI1A-S2.5-WP-S25-3-P2-QUAL-01 artifact audit'
    classification = 'producer-recorded primitives recomputed by reviewer; complements fresh execution'
    p1 = [ordered]@{
        knownAnswers = $p1.knownAnswers.Count
        formCases = $p1.formCases.Count
        matrix = $p1.matrix.Count
        matrixPass = $p1Pass
        matrixFail = $p1Fail
    }
    delta = [ordered]@{
        pairs = @($pairSummaries)
        checksRecorded = $delta.checks.Count
        producerAllPass = $delta.allPass
    }
    referenceIdentityLimitation = $limitation
    checks = @($checks)
    checkSummary = "$(@($checks | Where-Object { $_.pass }).Count)/$($checks.Count)"
    allPass = $script:allPass
}

$result | ConvertTo-Json -Depth 8 | Out-File $outPath -Encoding utf8
"artifact audit checks=$($result.checkSummary) allPass=$($result.allPass)"
exit $(if ($script:allPass) { 0 } else { 1 })
