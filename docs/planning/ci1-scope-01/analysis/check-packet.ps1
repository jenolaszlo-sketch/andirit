param([string]$RepositoryRoot = 'D:\projects\Andirit')
$ErrorActionPreference='Stop'
Set-Location -LiteralPath $RepositoryRoot
$packet=Join-Path $RepositoryRoot 'docs/planning/ci1-scope-01'
$checks=[Collections.Generic.List[string]]::new()
$required=@('README.md','existing-capability-inventory.md','occurrence-representation-analysis.md','consumer-scenarios.md','scope-proposal.md','query-contract-proposal.md','acceptance-contract.md','performance-evaluation-plan.md','architecture-decisions.md','implementation-sequence.md','final-handoff.md')
foreach($name in $required) {
    if(!(Test-Path -LiteralPath "$packet/$name")) {throw "Missing $name"}
}
$checks.Add('all-eleven-required-documents')
$jsonExamples=0
foreach($file in Get-ChildItem -LiteralPath $packet -Filter '*.md') {
    $text=Get-Content -LiteralPath $file.FullName -Raw
    foreach($link in [regex]::Matches($text,'\[[^\]]+\]\(([^)]+)\)')) {
        $target=$link.Groups[1].Value.Split('#')[0]
        if($target -match '^[a-zA-Z]+:' -or !$target) {continue}
        $resolved=[IO.Path]::GetFullPath((Join-Path $file.DirectoryName $target))
        if(!(Test-Path -LiteralPath $resolved)) {throw "Broken link in $($file.Name): $target"}
    }
    foreach($example in [regex]::Matches($text,'(?s)~~~json\s*(.*?)\s*~~~')) {
        $value=$example.Groups[1].Value | ConvertFrom-Json
        if(!$value.request.contractVersion) {throw 'Example missing version'}
        $jsonExamples++
    }
}
if($jsonExamples -ne 2) {throw "Expected two JSON responses, got $jsonExamples"}
$checks.Add('all-local-document-links-resolve')
$checks.Add('two-proposed-json-responses-parse')
$manifest=Get-Content -LiteralPath "$packet/fixtures/expected-sites.json" -Raw | ConvertFrom-Json
foreach($site in $manifest.Sites) {
    $lines=Get-Content -LiteralPath (Join-Path "$packet/fixtures" $site.FixturePath)
    $s=$site.Span
    $actual=$lines[$s[0]-1].Substring($s[1]-1,$s[3]-$s[1])
    if($actual -cne $site.Lexeme) {throw "Span mismatch $($site.Case): $actual"}
}
$checks.Add("source-selected-site-spans-valid:$($manifest.Sites.Count)")
$fixture=Get-Content -LiteralPath "$packet/evidence/fixture-baseline.json" -Raw | ConvertFrom-Json
$logger=$fixture.Answers | Where-Object Requested -eq 'Logger.Write(string)'
if($logger.OracleInvocations.Count -ne 3 -or $logger.Callers.Count -ne 1 -or $logger.References.Count -ne 1 -or $logger.CallsTruncated) {throw 'Baseline collapse report inconsistent'}
$checks.Add('public-api-baseline-three-sites-one-call-edge-one-reference-edge')
$real=Get-Content -LiteralPath "$packet/evidence/real-baseline.json" -Raw | ConvertFrom-Json
if($real.InputFiles.Count -ne 47 -or ($real.Answers | Where-Object Error)) {throw 'Real baseline selection/input mismatch'}
$positive=$real.Answers | Where-Object SelectedQualifiedName -eq 'Andirit.CodeGraphBatchLimits.Positive(int, string)'
if($positive.OracleInvocations.Count -ne 8 -or $positive.Callers.Count -ne 1) {throw 'Real repeated-site mismatch'}
$checks.Add('real-five-target-selection-and-eight-to-one-reproduction')
foreach($binding in (Get-Content -LiteralPath "$packet/evidence/source-binding.json" -Raw | ConvertFrom-Json)) {
    $actual=& git hash-object -- $binding.Path
    if($actual -ne $binding.GitBlob) {throw "Source changed: $($binding.Path)"}
}
$checks.Add('all-47-real-source-blobs-still-match')
& git diff --quiet cd2b7fb5d73f4dc462b0ef7fcadfd75a91c118a2 -- src tests Directory.Build.props Directory.Build.targets .github .editorconfig docs/decisions docs/implementation docs/remediation docs/reviews
if($LASTEXITCODE -ne 0) {throw 'Unauthorized or historical source diff'}
$checks.Add('production-tests-schema-packages-history-unchanged')
$paths=@(& git diff --name-only cd2b7fb5d73f4dc462b0ef7fcadfd75a91c118a2)
$paths+=@(& git ls-files --others --exclude-standard)
foreach($path in $paths) {
    if($path -ne 'docs/agent-code-intelligence-roadmap.md' -and !$path.StartsWith('docs/planning/ci1-scope-01/')) {throw "Unexpected changed path: $path"}
}
$checks.Add('delivery-path-allowlist')
[ordered]@{Kind='author planning consistency checks; NOT independent qualification';Checks=$checks;ProposedJsonExamples=$jsonExamples;ExpectedSourceSites=$manifest.Sites.Count;Status='passed'} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath "$packet/evidence/packet-checks.json" -Encoding utf8
Write-Output ($checks -join [Environment]::NewLine)
