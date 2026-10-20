param([string]$ProjectRoot = (Split-Path $PSScriptRoot -Parent))
$ErrorActionPreference = 'Stop'
$dll = Join-Path $ProjectRoot 'UdpPeer/bin/Debug/net8.0/UdpPeer.dll'
$evidence = Join-Path $ProjectRoot 'Evidence'
New-Item -ItemType Directory -Path $evidence -Force | Out-Null
function Start-Peer([string]$id, [int]$localPort, [int]$remotePort) {
    $info = [System.Diagnostics.ProcessStartInfo]::new('dotnet')
    foreach ($argValue in @($dll, $id, '127.0.0.1', "$localPort", '127.0.0.1', "$remotePort")) { $info.ArgumentList.Add($argValue) }
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.RedirectStandardInput = $true
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    $p = [System.Diagnostics.Process]::Start($info)
    return @{ Process = $p; Output = $p.StandardOutput.ReadToEndAsync(); Errors = $p.StandardError.ReadToEndAsync() }
}
$results = @()
foreach ($condition in @(@{Name='paced'; Count=100; Interval=10}, @{Name='same-count-burst'; Count=100; Interval=0}, @{Name='burst'; Count=1000; Interval=0})) {
    $a = Start-Peer 'A' 19001 19002
    $b = Start-Peer 'B' 19002 19001
    try {
        Start-Sleep -Milliseconds 800
        $a.Process.StandardInput.WriteLine('move 3 5')
        $b.Process.StandardInput.WriteLine('move 8 2')
        Start-Sleep -Milliseconds 300
        $a.Process.StandardInput.WriteLine('move 4 6')
        Start-Sleep -Milliseconds 200
        $a.Process.StandardInput.WriteLine('resend 1')
        $a.Process.StandardInput.WriteLine('resend 2')
        $a.Process.StandardInput.WriteLine('raw nonsense')
        $a.Process.StandardInput.WriteLine('raw ')
        $a.Process.StandardInput.WriteLine('raw POS1|A|bad|x|y')
        Start-Sleep -Milliseconds 200
        $a.Process.StandardInput.WriteLine("load $($condition.Count) $($condition.Interval)")
        $b.Process.StandardInput.WriteLine("load $($condition.Count) $($condition.Interval)")
        Start-Sleep -Milliseconds ($condition.Count * $condition.Interval + 2000)
        foreach ($peer in @($a, $b)) { $peer.Process.StandardInput.WriteLine('status'); $peer.Process.StandardInput.WriteLine('quit') }
        foreach ($peer in @($a, $b)) { if (!$peer.Process.WaitForExit(10000)) { throw 'UDP process did not exit' } }
        $outA = $a.Output.GetAwaiter().GetResult(); $outB = $b.Output.GetAwaiter().GetResult()
        $errA = $a.Errors.GetAwaiter().GetResult(); $errB = $b.Errors.GetAwaiter().GetResult()
        Set-Content -LiteralPath (Join-Path $evidence "udp-$($condition.Name)-A.txt") -Value $outA -Encoding utf8
        Set-Content -LiteralPath (Join-Path $evidence "udp-$($condition.Name)-B.txt") -Value $outB -Encoding utf8
        if ($a.Process.ExitCode -ne 0 -or $b.Process.ExitCode -ne 0 -or $errA -or $errB) { throw "UDP execution error: $errA $errB" }
        foreach ($expected in @('APPLY A \(3,5\) seq=1', 'IGNORE A seq=1 latest=2', 'IGNORE A seq=2 latest=2', 'REJECT malformed')) {
            if ($outB -notmatch $expected) { throw "Missing expected output: $expected" }
        }
        if ($outA -notmatch 'APPLY B \(8,2\) seq=1') { throw 'Bidirectional position missing' }
        $final = $condition.Count - 1
        if ($outA -notmatch "REMOTE B \($final,-$final\) seq=$($condition.Count + 1)" -or
            $outB -notmatch "REMOTE A \($final,-$final\) seq=$($condition.Count + 2)") { throw 'Final position mismatch' }
        $results += "PASS $($condition.Name): count=$($condition.Count) intervalMs=$($condition.Interval)"
        $results += ($outA -split "`n" | Where-Object { $_ -match 'LOAD ' })
        $results += ($outA -split "`n" | Where-Object { $_ -match 'SELF |REMOTE ' } | Select-Object -Last 2)
        $results += ($outB -split "`n" | Where-Object { $_ -match 'LOAD ' })
        $results += ($outB -split "`n" | Where-Object { $_ -match 'SELF |REMOTE ' } | Select-Object -Last 2)
    }
    finally {
        foreach ($peer in @($a, $b)) { if (!$peer.Process.HasExited) { $peer.Process.Kill() }; $peer.Process.Dispose() }
    }
}
$results | Set-Content -LiteralPath (Join-Path $evidence 'udp-summary.txt') -Encoding utf8
$results
