using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

class DiagnosticTest {
    static async Task Main() {
        string claudePath = @"C:\Users\erspi\.local\bin\claude.exe";
        string logFile = "claude_diag_log.txt";
        File.WriteAllText(logFile, "--- Claude Code Diagnostic Start ---
");

        await RunTest("Basic Help", claudePath, "--help");
        await RunTest("Interactive Attempt", claudePath, "--dangerously-skip-permissions");
        await RunTest("Print Attempt", claudePath, "-p "hello"");

        Console.WriteLine($"診斷完成！請查看 {logFile}");
    }

    static async Task RunTest(string testName, string path, string args) {
        File.AppendAllText("claude_diag_log.txt", $"
[Test: {testName}]
Command: {path} {args}
");
        
        var startInfo = new ProcessStartInfo {
            FileName = path,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        startInfo.EnvironmentVariables["TERM"] = "xterm-256color";
        startInfo.EnvironmentVariables["FORCE_COLOR"] = "1";

        using var p = new Process { StartInfo = startInfo };
        StringBuilder output = new StringBuilder();
        StringBuilder error = new StringBuilder();

        p.OutputDataReceived += (s, e) => { if (e.Data != null) output.AppendLine("STDOUT: " + e.Data); };
        p.ErrorDataReceived += (s, e) => { if (e.Data != null) error.AppendLine("STDERR: " + e.Data); };

        try {
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            // 給予 5 秒測試時間
            await Task.Delay(5000);
            
            if (!p.HasExited) {
                File.AppendAllText("claude_diag_log.txt", "Status: Process still running (Hanging...)
");
                p.Kill();
            } else {
                File.AppendAllText("claude_diag_log.txt", $"Status: Exited with code {p.ExitCode}
");
            }

            File.AppendAllText("claude_diag_log.txt", "--- Captured Output ---
" + output.ToString());
            File.AppendAllText("claude_diag_log.txt", "--- Captured Errors ---
" + error.ToString());
        } catch (Exception ex) {
            File.AppendAllText("claude_diag_log.txt", "EXCEPTION: " + ex.Message + "
");
        }
    }
}
