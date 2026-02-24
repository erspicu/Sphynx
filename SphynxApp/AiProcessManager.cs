using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SphynxApp
{
    public class AiProcessManager
    {
        private Process? _pwshProcess;
        private const string JobFinishedTag = "[job finished]";
        
        public event EventHandler<string>? OnOutputReceived;
        public event EventHandler? OnJobFinished;

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

            _pwshProcess.OutputDataReceived += (s, e) => HandleData(e.Data);
            _pwshProcess.ErrorDataReceived += (s, e) => HandleData(e.Data);

            try
            {
                _pwshProcess.Start();
                _pwshProcess.BeginOutputReadLine();
                _pwshProcess.BeginErrorReadLine();

                // 設定 PS 環境並執行指令
                _pwshProcess.StandardInput.WriteLine("$OutputEncoding = [System.Text.Encoding]::UTF8; [Console]::OutputEncoding = [System.Text.Encoding]::UTF8;");
                _pwshProcess.StandardInput.WriteLine(initialCmd);
                _pwshProcess.StandardInput.Flush();

                OnOutputReceived?.Invoke(this, $"\x1b[1;34m[System] Shell active. Sent: {initialCmd}\x1b[0m");
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
                // 顯式加上 \r\n 確保觸發 Enter
                _pwshProcess.StandardInput.Write(text + "\r\n");
                _pwshProcess.StandardInput.Flush();
            }
        }

        public bool SendMessageToPid(int pid, string text)
        {
            // 如果 PID 符合目前管理的 Shell 程序
            if (_pwshProcess != null && _pwshProcess.Id == pid)
            {
                SendMessage(text);
                return true;
            }

            // 這裡可以擴展：如果是子程序或是其他已知的 AI 程序
            // 目前先以管理中的程序為主
            OnOutputReceived?.Invoke(this, $"\x1b[1;33m[System] Attempting to send to PID {pid}...\x1b[0m");
            
            // 由於安全與權限限制，若不是本程式啟動的程序，無法直接寫入 StandardInput
            // 若未來有特定 API 或通訊協定，可以在此實作
            return false;
        }

        private void HandleData(string? data)
        {
            if (data == null) return;
            if (data.Contains(JobFinishedTag))
            {
                OnJobFinished?.Invoke(this, EventArgs.Empty);
                string cleaned = data.Replace(JobFinishedTag, "").Trim();
                if (!string.IsNullOrEmpty(cleaned)) OnOutputReceived?.Invoke(this, cleaned);
            }
            else
            {
                OnOutputReceived?.Invoke(this, data);
            }
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
                    
                    // 設定環境變數
                    p.StartInfo.EnvironmentVariables["CI"] = "true";
                    p.StartInfo.EnvironmentVariables["TERM"] = "xterm";

                    p.OutputDataReceived += (s, e) => { if (e.Data != null) OnOutputReceived?.Invoke(this, e.Data); };
                    p.ErrorDataReceived += (s, e) => { if (e.Data != null) OnOutputReceived?.Invoke(this, e.Data); };

                    p.Start();
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                    p.WaitForExit();
                    
                    OnOutputReceived?.Invoke(this, $"\x1b[1;33m[System] Process finished with code {p.ExitCode}.\x1b[0m");
                    OnJobFinished?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    OnOutputReceived?.Invoke(this, $"\x1b[1;31m[Critical Error] {ex.Message}\x1b[0m");
                }
            });
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
