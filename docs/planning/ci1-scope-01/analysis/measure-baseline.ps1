param([string]$RepositoryRoot = 'D:\projects\Andirit')
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $RepositoryRoot
$packet = Join-Path $RepositoryRoot 'docs/planning/ci1-scope-01'
$timer = [Diagnostics.Stopwatch]::StartNew()
$searches = foreach ($name in @('EnsurePublication','FindSymbolsByQualifiedNameAsync','FindDeclarationsWithProvenanceAsync','Positive','GetLatestPublicationAsync')) {
    $clock = [Diagnostics.Stopwatch]::StartNew()
    $lines = @(& rg -n -F --glob '*.cs' $name src/Andirit src/Andirit.Abstractions)
    $clock.Stop()
    $outputText = $lines -join [Environment]::NewLine
    [pscustomobject]@{Pattern=$name;Milliseconds=$clock.Elapsed.TotalMilliseconds;OutputUtf8Bytes=[Text.Encoding]::UTF8.GetByteCount($outputText);Lines=$lines}
}
$reads = foreach ($path in @('src/Andirit.Abstractions/CodeGraphBatch.cs','src/Andirit/CodeGraphQueryService.cs','src/Andirit/CodeGraphPublicationQuery.cs','src/Andirit/CodePublicationSnapshot.cs','src/Andirit/ICodeGraphStore.cs')) {
    $text = Get-Content -LiteralPath $path -Raw
    [pscustomobject]@{Path=$path;Utf8TextBytes=[Text.Encoding]::UTF8.GetByteCount($text);Sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
}
$timer.Stop()
$baseline = Get-Content -LiteralPath "$packet/evidence/real-baseline.json" -Raw | ConvertFrom-Json
[ordered]@{
    Purpose='Scripted retrieval baseline, not autonomous task completion or token savings'
    SourceCommit=$baseline.BaselineCommit
    Queries='Reuse the separately timed public-query envelopes in real-baseline.json; timings are not included in RetrievalWallMs'
    LogicalQueryCalls=20
    OpenPublicationCalls=1
    SourceSearchProcesses=5
    SourceFilesOpened=$reads.Count
    SourceTextBytes=($reads | Measure-Object Utf8TextBytes -Sum).Sum
    SearchOutputUtf8Bytes=($searches | Measure-Object OutputUtf8Bytes -Sum).Sum
    QueryEnvelopeUtf8Bytes=($baseline.Answers | Measure-Object SerializedEnvelopeUtf8Bytes -Sum).Sum
    RetrievalWallMs=$timer.Elapsed.TotalMilliseconds
    ActionableAnswerTime=$null
    Tokens=$null
    Searches=$searches
    Reads=$reads
} | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath "$packet/evidence/workflow-baseline.json" -Encoding utf8
$bindings = foreach($inputFile in $baseline.InputFiles) {
    $treeLine = & git ls-tree $baseline.BaselineCommit -- $inputFile.Path
    $expectedBlob = ($treeLine -split '\s+')[2]
    $actualBlob = & git hash-object -- $inputFile.Path
    if($expectedBlob -ne $actualBlob) { throw "Baseline source mismatch: $($inputFile.Path)" }
    [pscustomobject]@{Path=$inputFile.Path;GitBlob=$actualBlob;Sha256=(Get-FileHash -LiteralPath $inputFile.Path -Algorithm SHA256).Hash.ToLowerInvariant()}
}
$bindings | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath "$packet/evidence/source-binding.json" -Encoding utf8
Write-Output 'Source blobs verified against pinned baseline; retrieval measurements recorded.'
