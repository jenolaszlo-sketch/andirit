param(
    [Parameter(Mandatory)] [string] $RunId,
    [Parameter(Mandatory)] [string] $Sha,
    [Parameter(Mandatory)] [string] $Stage
)
$ErrorActionPreference = 'Stop'
$repository = 'jenolaszlo-sketch/andirit'
$api = "repos/$repository/actions"
$out = Join-Path $Stage "hosted/$RunId"
New-Item -ItemType Directory -Force "$out/download" | Out-Null
$run = gh api "$api/runs/$RunId" | ConvertFrom-Json
if ($LASTEXITCODE -ne 0 -or $run.head_sha -cne $Sha) { throw 'Wrong GitHub run or head SHA.' }
if ($run.status -ne 'completed') { exit 3 }
gh api "$api/runs/$RunId" > "$out/run.json"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
gh api "$api/runs/$RunId/jobs?per_page=100" > "$out/jobs.json"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
gh api "$api/runs/$RunId/artifacts?per_page=100" > "$out/artifacts.json"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
foreach ($job in (Get-Content -Raw "$out/jobs.json" | ConvertFrom-Json).jobs) {
    gh api --allow-escape-sequences "$api/jobs/$($job.id)/logs" | Out-File -Encoding utf8 "$out/job-$($job.id).log"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
foreach ($artifact in (Get-Content -Raw "$out/artifacts.json" | ConvertFrom-Json).artifacts) {
    $folder = "$out/download/$($artifact.name)"
    if (-not (Test-Path -LiteralPath $folder)) {
        gh run download $RunId --repo $repository --name $artifact.name --dir $folder
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
}
if ($run.conclusion -ne 'success') { throw "Hosted run concluded $($run.conclusion)." }
foreach ($target in @('ubuntu-latest|Linux|X64', 'windows-latest|Windows|X64', 'macos-latest|macOS|ARM64')) {
    $name, $os, $arch = $target.Split('|')
    $parameters = @{
        Results = "$out/download/test-results-$name"
        ExpectedSha = $Sha
        ExpectedRunId = $RunId
        ExpectedRunAttempt = $run.run_attempt
        ExpectedRunnerOS = $os
        ExpectedRunnerArch = $arch
    }
    & ./.github/scripts/verify-s63-results.ps1 @parameters > "$out/$name-verification.log"
    Get-Content "$out/$name-verification.log" -First 5
}
$env:PYTHONIOENCODING = 'utf-8'
python (Join-Path $Stage 'summarize-r2.py') $Stage $Sha $RunId > (Join-Path $Stage 'results-summary.txt')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
