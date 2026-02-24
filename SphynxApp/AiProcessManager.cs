using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SphynxApp
{
    public class AiProcessManager
    {
        private Process? _pwshProcess;
        private const string JobFinishedTag = "[job finished]";
        
        public event EventHandler<string>? OnOutputReceived;
        public event EventHandler? OnJobFinished;

        // ANSI 碼正規表達式，用於剝離控制碼以便檢查文字內容
        private static readonly Regex AnsiRegex = new Regex(@"\x1B\[[^@-_]*[@-_]|\x1B[@-_]", RegexOptions.Compiled);

        public void Start(string initialCmd)
        {
            Stop(); 

            string shell = IsCommandAvailable("pwsh.exe") ? "pwsh.exe" : "powershell.exe";

            _pwshProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = shell,
                    Arguments = "-NoProfile",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                }
            };

            // 關鍵核心：偽裝成支援 ANSI 的現代終端機
            // 這能誘導 Claude Code 進入全彩色/互動模式
            _pwshProcess.StartInfo.EnvironmentVariables["TERM"] = "xterm-256color";
            _pwshProcess.StartInfo.EnvironmentVariables["FORCE_COLOR"] = "3"; 
            _pwshProcess.StartInfo.EnvironmentVariables["CI"] = "false"; 

            _pwshProcess.OutputDataReceived += (s, e) => HandleData(e.Data);
            _pwshProcess.ErrorDataReceived += (s, e) => HandleData(e.Data);

            try
            {
                _pwshProcess.Start();
                _pwshProcess.BeginOutputReadLine();
                _pwshProcess.BeginErrorReadLine();

                // 初始化 PowerShell 環境
                _pwshProcess.StandardInput.WriteLine("$OutputEncoding = [System.Text.Encoding]::UTF8; [Console]::OutputEncoding = [System.Text.Encoding]::UTF8;");
                _pwshProcess.StandardInput.WriteLine(initialCmd);
                _pwshProcess.StandardInput.Flush();

                OnOutputReceived?.Invoke(this, $"\x1b[1;34m[System] Terminal Engine Active (ANSI/TTY Mode). Executing: {initialCmd}\x1b[0m");
            }
            catch (Exception ex)
            {
                OnOutputReceived?.Invoke(this, $"\x1b[1;31m[Critical Error] {ex.Message}\x1b[0m");
            }
        }

        public void SendMessage(string text)
        {
            if (_pwshProcess != null && !_pwshProcess.HasExited)
            {
                // 模擬真實按鍵輸入
                _pwshProcess.StandardInput.Write(text + "\r\n");
                _pwshProcess.StandardInput.Flush();
            }
        }

        public bool SendMessageToPid(int pid, string text)
        {
            if (_pwshProcess != null && _pwshProcess.Id == pid)
            {
                SendMessage(text);
                return true;
            }
            return false;
        }

        private void HandleData(string? data)
        {
            if (data == null) return;

            // 1. 剝離 ANSI 碼以進行邏輯檢查
            string plainText = AnsiRegex.Replace(data, "");
            
            if (plainText.Contains(JobFinishedTag))
            {
                OnJobFinished?.Invoke(this, EventArgs.Empty);
                if (plainText.Trim() == JobFinishedTag) return; // 如果這行只有標籤，則過濾
            }

            // 2. 傳送原始資料（含 ANSI 碼）給 xterm.js 進行渲染
            OnOutputReceived?.Invoke(this, data);
        }

        public void RunOnce(string fileName, string arguments)
        {
            Task.Run(() =>
            {
                try
                {
                    using var p = new Process();
                    p.StartInfo.FileName = fileName;
                    p.StartInfo.Arguments = arguments;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.RedirectStandardError = true;
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                    p.StartInfo.StandardErrorEncoding = Encoding.UTF8;
                    
                    p.StartInfo.EnvironmentVariables["TERM"] = "xterm-256color";
                    p.StartInfo.EnvironmentVariables["FORCE_COLOR"] = "1";

                    p.OutputDataReceived += (s, e) => { if (e.Data != null) HandleData(e.Data); };
                    p.ErrorDataReceived += (s, e) => { if (e.Data != null) HandleData(e.Data); };

                    p.Start();
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                    p.WaitForExit();
                    
                    OnOutputReceived?.Invoke(this, $"\x1b[1;33m[System] Task finished (Code {p.ExitCode}).\x1b[0m");
                    OnJobFinished?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    OnOutputReceived?.Invoke(this, $"\x1b[1;31m[Critical Error] {ex.Message}\x1b[0m");
                }
            });
        }

        private bool IsCommandAvailable(string cmd)
        {
            try
            {
                using (var p = new Process())
                {
                    p.StartInfo.FileName = "where"; p.StartInfo.Arguments = cmd;
                    p.StartInfo.UseShellExecute = false; p.StartInfo.CreateNoWindow = true;
                    p.Start(); p.WaitForExit(); return p.ExitCode == 0;
                }
            }
            catch { return false; }
        }

        public void Stop()
        {
            if (_pwshProcess != null && !_pwshProcess.HasExited)
            {
                try { _pwshProcess.Kill(true); } catch { }
                _pwshProcess.Dispose();
                _pwshProcess = null;
            }
        }
    }
}
