param([string]$ProjectRoot = (Split-Path $PSScriptRoot -Parent))
$ErrorActionPreference = 'Stop'
$exe = Join-Path $ProjectRoot 'CoinGame/Builds/CoinLink.exe'
$evidence = Join-Path $ProjectRoot 'Evidence'
New-Item -ItemType Directory -Path $evidence -Force | Out-Null
$rawLogs = Join-Path $ProjectRoot 'CoinGame/Logs'
New-Item -ItemType Directory -Path $rawLogs -Force | Out-Null
$hostLog = Join-Path $rawLogs 'smoke-host.log'
$clientLog = Join-Path $rawLogs 'smoke-client.log'
function Start-Game([string]$mode, [string]$log) {
    $info = [System.Diagnostics.ProcessStartInfo]::new($exe)
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    foreach ($value in @('-batchmode', '-nographics', $mode, '-logFile', $log)) { $info.ArgumentList.Add($value) }
    return [System.Diagnostics.Process]::Start($info)
}
$hostProcess = Start-Game '--smoke-host' $hostLog
$clientProcess = $null
try {
    Start-Sleep -Seconds 2
    $clientProcess = Start-Game '--smoke-client' $clientLog
    if (!$clientProcess.WaitForExit(40000)) { throw 'Client integration test timed out' }
    if (!$hostProcess.WaitForExit(35000)) { throw 'Host integration test timed out' }
    $hostText = Get-Content -LiteralPath $hostLog -Raw
    $clientText = Get-Content -LiteralPath $clientLog -Raw
    $hostEvidence = $hostText -split "`n" | Where-Object { $_ -match '^SMOKE_|^COIN peer=' }
    $clientEvidence = $clientText -split "`n" | Where-Object { $_ -match '^SMOKE_|^REPLY ' }
    $hostEvidence | Set-Content -LiteralPath (Join-Path $evidence 'coin-host.txt') -Encoding utf8
    $clientEvidence | Set-Content -LiteralPath (Join-Path $evidence 'coin-client.txt') -Encoding utf8
    if ($hostText -match 'SMOKE_FAIL|Exception' -or $clientText -match 'SMOKE_FAIL|Exception' -or
        $hostText -notmatch 'SMOKE_HOST_DONE' -or $clientText -notmatch 'SMOKE_RECONNECTED') { throw 'Coin integration test failed; see Evidence/coin-*.txt' }
    $hostEvidence
    $clientEvidence
}
finally {
    foreach ($process in @($hostProcess, $clientProcess)) { if ($null -ne $process) { if (!$process.HasExited) { $process.Kill() }; $process.Dispose() } }
}

