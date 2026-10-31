$ErrorActionPreference = 'Stop'
Set-Location C:\Windows\Temp\opencode\comparison\out

function Load($name) { Get-Content $name -Raw | ConvertFrom-Json }
function Key($e) { "{0}|{1}|{2}" -f $e.kind.value, $e.sourceId.value, $e.targetId.value }
function EdgeId($e) { $e.id.value }

foreach ($pair in @(
    @{ Name = 'mixed';     Base = 'mixed.baseline.json';     Current = 'mixed.current.json' },
    @{ Name = 'ambiguous'; Base = 'ambiguous.baseline.json'; Current = 'ambiguous.current.json' }
)) {
    $base = Load $pair.Base
    $curr = Load $pair.Current
    $baseMap = @{}
    foreach ($e in $base.edges) { $baseMap[(Key $e)] = $e }
    $currMap = @{}
    foreach ($e in $curr.edges) { $currMap[(Key $e)] = $e }
    $nodeMap = @{}
    foreach ($n in $curr.nodes) { $nodeMap[$n.id.value] = $n }

    Write-Output "=== $($pair.Name) ==="
    Write-Output ("baseline edges: {0}  current edges: {1}" -f $base.edges.Count, $curr.edges.Count)
    foreach ($k in $baseMap.Keys | Where-Object { -not $currMap.ContainsKey($_) } | Sort-Object) {
        $e = $baseMap[$k]
        $src = $nodeMap[$e.sourceId.value]; $tgt = $nodeMap[$e.targetId.value]
        Write-Output ("  REMOVED {0}  src={1}  tgt={2} ({3})" -f $e.kind.value, $src.qualifiedName, $tgt.qualifiedName, $tgt.id.value)
    }
    foreach ($k in $currMap.Keys | Where-Object { -not $baseMap.ContainsKey($_) } | Sort-Object) {
        $e = $currMap[$k]
        $src = $nodeMap[$e.sourceId.value]; $tgt = $nodeMap[$e.targetId.value]
        Write-Output ("  ADDED   {0}  src={1}  tgt={2} ({3})" -f $e.kind.value, $src.qualifiedName, $tgt.qualifiedName, $tgt.id.value)
    }

    # stable-edge identity: every edge surviving in both must keep the identical Id
    $stable = $true
    foreach ($k in $baseMap.Keys) {
        if (-not $currMap.ContainsKey($k)) { continue }
        if ((EdgeId $baseMap[$k]) -ne (EdgeId $currMap[$k])) { $stable = $false; Write-Output "  ID CHANGED: $k" }
    }
    Write-Output ("  surviving-edge IDs stable: {0}" -f $stable)

    Write-Output "  coverage (kind state edges unresolved internal cross external ambiguous):"
    foreach ($c in $curr.coverage) {
        Write-Output ("    {0} {1} e={2} u={3} i={4} x={5} ext={6} amb={7}" -f $c.relationshipKind, $c.state, $c.edgesEmitted, $c.unresolvedTargets, $c.internalTargets, $c.crossProjectTargets, $c.externalTargets, $c.ambiguousTargets)
    }
    Write-Output ("  coverage baseline calls: " + (($base.coverage | Where-Object { $_.relationshipKind -eq 'calls' } | ForEach-Object { "e={0} u={1} i={2} x={3} ext={4} amb={5}" -f $_.edgesEmitted, $_.unresolvedTargets, $_.internalTargets, $_.crossProjectTargets, $_.externalTargets, $_.ambiguousTargets })))
    if ($curr.error) { Write-Output "  error: $($curr.error)" }
}
