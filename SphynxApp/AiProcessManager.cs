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
        public event EventHandler<string>? OnOutputReceived;
        public event EventHandler? OnJobFinished;

        private static readonly Regex AnsiRegex = new Regex(@"\x1B\[[^@-_]*[@-_]|\x1B[@-_]", RegexOptions.Compiled);
        private const string JobFinishedTag = "[job finished]";

        public void Start(string fullCommand)
        {
            OnOutputReceived?.Invoke(this, $"\x1b[1;34m[System] Terminal ready for task execution...\x1b[0m\r\n");
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

        public void SendMessage(string text)
        {
            // 暫時留空，因為 Claude 改用 RunOnce 模式
        }

        public bool SendMessageToPid(int pid, string text) => true;

        private void HandleData(string data)
        {
            string plainText = AnsiRegex.Replace(data, "");
            if (plainText.Contains(JobFinishedTag))
            {
                OnJobFinished?.Invoke(this, EventArgs.Empty);
            }
            OnOutputReceived?.Invoke(this, data + "\r\n");
        }

        public void Stop() { }
    }
}
