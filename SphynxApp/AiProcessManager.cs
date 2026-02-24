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

            // 極致環境偽裝
            _pwshProcess.StartInfo.EnvironmentVariables["TERM"] = "xterm-256color";
            _pwshProcess.StartInfo.EnvironmentVariables["FORCE_COLOR"] = "1";

            _pwshProcess.OutputDataReceived += (s, e) => HandleData(e.Data);
            _pwshProcess.ErrorDataReceived += (s, e) => HandleData(e.Data);

            try
            {
                _pwshProcess.Start();
                _pwshProcess.BeginOutputReadLine();
                _pwshProcess.BeginErrorReadLine();

                _pwshProcess.StandardInput.WriteLine("$OutputEncoding = [System.Text.Encoding]::UTF8; [Console]::OutputEncoding = [System.Text.Encoding]::UTF8;");
                _pwshProcess.StandardInput.WriteLine(initialCmd);
                _pwshProcess.StandardInput.Flush();

                OnOutputReceived?.Invoke(this, $"\x1b[1;34m[System] Terminal active. Environment ready.\x1b[0m\r\n");
            }
            catch (Exception ex)
            {
                OnOutputReceived?.Invoke(this, $"\x1b[1;31m[Critical Error] {ex.Message}\x1b[0m\r\n");
            }
        }

        public void SendMessage(string text)
        {
            if (_pwshProcess != null && !_pwshProcess.HasExited)
            {
                _pwshProcess.StandardInput.Write(text + "\r\n");
                _pwshProcess.StandardInput.Flush();
            }
        }

        public bool SendMessageToPid(int pid, string text)
        {
            SendMessage(text);
            return true;
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
                    p.StartInfo.EnvironmentVariables["CI"] = "true";

                    p.OutputDataReceived += (s, e) => { if (e.Data != null) HandleData(e.Data); };
                    p.ErrorDataReceived += (s, e) => { if (e.Data != null) HandleData(e.Data); };

                    p.Start();
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                    p.WaitForExit();
                    
                    OnOutputReceived?.Invoke(this, $"\x1b[1;33m[System] Task complete.\x1b[0m\r\n");
                    OnJobFinished?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    OnOutputReceived?.Invoke(this, $"\x1b[1;31m[Error] {ex.Message}\x1b[0m\r\n");
                }
            });
        }

        private void HandleData(string? data)
        {
            if (data == null) return;
            string plainText = AnsiRegex.Replace(data, "");
            if (plainText.Contains(JobFinishedTag)) OnJobFinished?.Invoke(this, EventArgs.Empty);
            OnOutputReceived?.Invoke(this, data);
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
