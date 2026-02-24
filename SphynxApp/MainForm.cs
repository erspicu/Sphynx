using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace SphynxApp
{
    public partial class MainForm : Form
    {
        private AiProcessManager? _aiManager;
        private TelegramBotManager? _tgManager;
        private bool _isWebViewReady = false;
        private bool _isInitialStart = true;

        public MainForm()
        {
            InitializeComponent();
            InitializeCustomComponents();
        }

        private System.Collections.Concurrent.ConcurrentQueue<string> _outputBuffer = new();
        private System.Windows.Forms.Timer? _flushTimer;

        private async void InitializeCustomComponents()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string htmlPath = Path.Combine(baseDir, "wwwroot", "terminal.html");
            if (!File.Exists(htmlPath)) {
                var dirInfo = new DirectoryInfo(baseDir);
                if (dirInfo.Parent?.Parent?.Parent != null) {
                    string altPath = Path.Combine(dirInfo.Parent.Parent.Parent.FullName, "wwwroot", "terminal.html");
                    if (File.Exists(altPath)) htmlPath = altPath;
                }
            }
            try {
                await webViewTerminal.EnsureCoreWebView2Async();
                webViewTerminal.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);
                _isWebViewReady = true;
            } catch { }

            _aiManager = new AiProcessManager();
            _aiManager.OnOutputReceived += (s, text) => {
                _outputBuffer.Enqueue(text);
            };
            
            _aiManager.OnJobFinished += async (s, e) => {
                if (_tgManager != null) await _tgManager.SendNotificationAsync("✅ 任務已完成！");
                WriteToXterm("\r\n\x1b[1;33m[System] Job finished.\x1b[0m\r\n");
            };

            // 初始化計時器 (60fps) 批次推送資料
            _flushTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _flushTimer.Tick += (s, e) => FlushOutputBuffer();
            _flushTimer.Start();

            cmbAiTool.SelectedIndex = 1;

            await Task.Delay(1500);
            StartSelectedTool();
            _isInitialStart = false;
        }

        private void FlushOutputBuffer()
        {
            if (!_isWebViewReady || _outputBuffer.IsEmpty) return;

            StringBuilder sb = new StringBuilder();
            while (_outputBuffer.TryDequeue(out string? part))
            {
                sb.Append(part);
            }

            if (sb.Length > 0)
            {
                string escaped = System.Web.HttpUtility.JavaScriptStringEncode(sb.ToString());
                webViewTerminal.CoreWebView2.ExecuteScriptAsync($"writeToTerminal('{escaped}')");
            }
        }

        private void cmbAiTool_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_isInitialStart) StartSelectedTool();
        }

        private void StartSelectedTool()
        {
            string selected = cmbAiTool.SelectedItem?.ToString() ?? "Gemini CLI";
            string geminiScriptPath = @"C:\Users\erspi\AppData\Roaming\npm\node_modules\@google\gemini-cli\node_modules\@google\gemini-cli-core\dist\index.js";
            string fullCommand = "";

            if (selected == "Gemini CLI") {
                if (File.Exists(geminiScriptPath)) {
                    fullCommand = $"node '{geminiScriptPath}'";
                } else {
                    WriteToXterm($"\x1b[1;31m[Error] 找不到 Gemini 腳本檔案\x1b[0m");
                    return;
                }
            } else if (selected == "Claude Code") {
                // 進入針對 Claude Code 優化的模式
                fullCommand = "echo 'Claude Code Station (Context-Aware) Ready.'"; 
            }

            if (!string.IsNullOrEmpty(fullCommand)) {
                _aiManager?.Start(fullCommand);
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            string cmd = txtInput.Text;
            if (string.IsNullOrWhiteSpace(cmd)) return;
            WriteToXterm($"\r\n\x1b[1;32m[User] {cmd}\x1b[0m\r\n");

            string selected = cmbAiTool.SelectedItem?.ToString() ?? "";
            if (selected == "Claude Code") {
                string claudePath = @"C:\Users\erspi\.local\bin\claude.exe";
                // 使用 RunOnce 模式配合 -c 繼承對話與 -p 強制輸出文字
                _aiManager?.RunOnce(claudePath, $"--dangerously-skip-permissions -c -p \"{cmd.Replace("\"", "\\\"")}\"");
            } else {
                _aiManager?.SendMessage(cmd);
            }
            
            txtInput.Clear();
        }

        private void btnSendToPid_Click(object sender, EventArgs e)
        {
            if (int.TryParse(txtPid.Text, out int pid))
            {
                string msg = txtMsg.Text;
                if (string.IsNullOrWhiteSpace(msg)) return;

                WriteToXterm($"\r\n\x1b[1;36m[System] Sending to PID {pid}: {msg}\x1b[0m\r\n");
                bool success = _aiManager?.SendMessageToPid(pid, msg) ?? false;
                
                if (!success)
                {
                    WriteToXterm($"\x1b[1;31m[Warning] Could not directly send to PID {pid}. (Process might not be managed or redirection is unavailable)\x1b[0m\r\n");
                }
                txtMsg.Clear();
            }
            else
            {
                WriteToXterm("\x1b[1;31m[Error] Invalid PID format.\x1b[0m\r\n");
            }
        }

        private void WriteToXterm(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            _outputBuffer.Enqueue(text);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _aiManager?.Stop();
            base.OnFormClosing(e);
        }
    }
}
