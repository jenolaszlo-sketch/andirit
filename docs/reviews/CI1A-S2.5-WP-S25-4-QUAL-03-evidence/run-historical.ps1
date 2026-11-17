param([Parameter(Mandatory)][string]$Work)
$ErrorActionPreference = 'Stop'
$env:NUGET_PACKAGES = Join-Path $env:USERPROFILE '.nuget/packages'
$env:DOTNET_NOLOGO = '1'
$root = "$Work/production"
$evidence = "$Work/evidence/historical"
$stores = "$Work/historical-stores-exact"
New-Item -ItemType Directory -Path $evidence,$stores -Force | Out-Null
Set-Location -LiteralPath $root
function Invoke-Logged([string]$Name, [string[]]$Cli) {
    $log = "$evidence/$Name.log"
    "Command: dotnet $($Cli -join ' ')" | Set-Content -LiteralPath $log
    & dotnet @Cli 2>&1 | Out-File -LiteralPath $log -Append
    $code = $LASTEXITCODE
    "Exit code: $code" | Add-Content -LiteralPath $log
    Write-Host "$Name = $code"
    return $code
}
function Hash($Path) {
    if (Test-Path -LiteralPath $Path -PathType Leaf) {
        return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
    }
}
$p = 'docs/reviews/CI1A-S2.5-S25-SQ04-Q1-tests'
$build = Invoke-Logged sq04-build @('build',"$p/Sq04QualProbe.csproj",'-c','Release','-p:NuGetAudit=false')
if ($build -eq 0) {
    $config = Get-ChildItem -Path "$p/bin/Release/net10.0" -Filter '*.runtimeconfig.json' | Select-Object -First 1
    $dll = Join-Path $config.DirectoryName ($config.Name.Replace('.runtimeconfig.json','.dll'))
    $master = 'D:/projects/Andirit/docs/reviews/CI1A-S2.5-WP-S25-1-BIND-01-evidence/fixtures'
    $records = foreach ($case in @(
        @{id='pre-rem'; files=@('prerem/prerem-authentic.ltdb','prerem/prerem-authentic.ltdb-wal'); expected=@('7646b98288a9803cff6f9930c78a3e7df5c676f6fc4e0cd2df2af5e79bc68885','8ca170cdc55a4428a5506bcb447a0667ab10c92c77ef149ae36460a5603aeab4')},
        @{id='graph-only'; files=@('baseline/baseline-graph-only.master.db'); expected=@('f21a72bee2c2e378e16766ca7b84c29010ab4d50360bf3111a82706f947781e0')}
    )) {
        New-Item -Path "$stores/$($case.id)" -ItemType Directory -Force | Out-Null
        $entries = @(for ($i=0; $i -lt $case.files.Count; $i++) {
            $from = Join-Path $master $case.files[$i]
            $to = Join-Path "$stores/$($case.id)" (Split-Path -Leaf $from)
            Copy-Item -LiteralPath $from -Destination $to
            $before = Hash $from
            $copyBefore = Hash $to
            if ($before -cne $case.expected[$i] -or $copyBefore -cne $before) { throw "Historical hash mismatch for $from" }
            [ordered]@{master=$from;copy=$to;masterBefore=$before;copyBefore=$copyBefore;bytes=(Get-Item -LiteralPath $to).Length}
        })
        $code = Invoke-Logged "sq04-$($case.id)" @($dll,$case.id,$entries[0].copy,"$evidence/sq04-$($case.id).json")
        foreach ($entry in $entries) {
            $entry.masterAfter = Hash $entry.master
            $entry.copyAfter = Hash $entry.copy
            if ($entry.masterAfter -cne $entry.masterBefore) { throw "Master altered: $($entry.master)" }
        }
        [ordered]@{case=$case.id;exit=$code;files=$entries}
    }
    $records | ConvertTo-Json -Depth 6 | Set-Content "$evidence/sq04-integrity.json"
}



