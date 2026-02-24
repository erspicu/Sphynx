$claudePath = "C:\Users\erspi\.local\bin\claude.exe"
$logFile = "claude_diag_log.txt"
"--- Claude Code Diagnostic Start ---" | Out-File $logFile

function Run-Test($name, $args) {
    "`n[Test: $name]" | Out-File $logFile -Append
    "Command: $claudePath $args" | Out-File $logFile -Append
    
    $pinfo = New-Object System.Diagnostics.ProcessStartInfo
    $pinfo.FileName = $claudePath
    $pinfo.Arguments = $args
    $pinfo.RedirectStandardOutput = $true
    $pinfo.RedirectStandardError = $true
    $pinfo.UseShellExecute = $false
    $pinfo.CreateNoWindow = $true
    
    $p = New-Object System.Diagnostics.Process
    $p.StartInfo = $pinfo
    
    $stdout = New-Object System.Text.StringBuilder
    $stderr = New-Object System.Text.StringBuilder
    
    $p.Start() | Out-Null
    
    # 讀取輸出的非同步模擬
    $task = Task.Run({
        while (-not $p.HasExited) {
            $line = $p.StandardOutput.ReadLine()
            if ($line) { $stdout.AppendLine("STDOUT: $line") | Out-Null }
            $err = $p.StandardError.ReadLine()
            if ($err) { $stderr.AppendLine("STDERR: $err") | Out-Null }
        }
    })

    Start-Sleep -Seconds 5
    
    if (-not $p.HasExited) {
        "Status: Hanging (Killed after 5s)" | Out-File $logFile -Append
        $p.Kill()
    } else {
        "Status: Exited with code $($p.ExitCode)" | Out-File $logFile -Append
    }
    
    "--- Captured Output ---" | Out-File $logFile -Append
    $stdout.ToString() | Out-File $logFile -Append
    "--- Captured Errors ---" | Out-File $logFile -Append
    $stderr.ToString() | Out-File $logFile -Append
}

Run-Test "Basic Version" "--version"
Run-Test "Interactive Attempt" "--dangerously-skip-permissions"
Run-Test "Help Menu" "--help"

Write-Host "診斷完成！結果已存至 $logFile"
