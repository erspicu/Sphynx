namespace Sphynx;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;

    // ── Controls ───────────────────────────────────────────────
    private Microsoft.Web.WebView2.WinForms.WebView2 webViewTerminal = null!;
    private Panel     panelBottom  = null!;
    private Panel     panelStatus  = null!;
    private TextBox   txtInput     = null!;
    private Button    btnSend      = null!;
    private Button    btnEnter     = null!;
    private Button    btnStop      = null!;
    private Button    btnClear     = null!;
    private Label     lblStatus    = null!;
    private Label     lblInputHint = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components      = new System.ComponentModel.Container();
        webViewTerminal = new Microsoft.Web.WebView2.WinForms.WebView2();
        panelBottom     = new Panel();
        panelStatus     = new Panel();
        txtInput        = new TextBox();
        btnSend         = new Button();
        btnEnter        = new Button();
        btnStop         = new Button();
        btnClear        = new Button();
        lblStatus       = new Label();
        lblInputHint    = new Label();

        ((System.ComponentModel.ISupportInitialize)webViewTerminal).BeginInit();
        panelBottom.SuspendLayout();
        panelStatus.SuspendLayout();
        SuspendLayout();

        // ════════════════════════════════════════════════════════
        // webViewTerminal — 填滿主視窗（Dock = Fill）
        // ════════════════════════════════════════════════════════
        webViewTerminal.Dock                   = DockStyle.Fill;
        webViewTerminal.BackColor              = Color.FromArgb(13, 17, 23);
        webViewTerminal.CreationProperties     = null;
        webViewTerminal.DefaultBackgroundColor = Color.FromArgb(13, 17, 23);
        webViewTerminal.Name                   = "webViewTerminal";
        webViewTerminal.ZoomFactor             = 1D;

        // ════════════════════════════════════════════════════════
        // panelBottom — 底部輸入列
        //
        // 使用 Dock 佈局，完全避免手動計算寬度：
        //   [lblInputHint][txtInput (Fill)][btnSend][btnEnter][btnStop][btnClear]
        //
        // Padding = (left=8, top=10, right=8, bottom=10) → 有效高度 32px
        // ════════════════════════════════════════════════════════
        panelBottom.Dock      = DockStyle.Bottom;
        panelBottom.Height    = 52;
        panelBottom.BackColor = Color.FromArgb(22, 27, 34);
        panelBottom.Padding   = new Padding(8, 10, 8, 10);
        panelBottom.Name      = "panelBottom";

        // ── 右側按鈕區：獨立 Panel，DockStyle.Right，固定寬度 ──
        // 改用內嵌 Panel 而非 DockStyle.Right on each button，
        // 確保按鈕群組無論視窗多寬都整齊靠右。
        var panelBtnRight = new Panel();
        panelBtnRight.Dock      = DockStyle.Right;
        panelBtnRight.Width     = 256;
        panelBtnRight.BackColor = Color.FromArgb(22, 27, 34);
        panelBtnRight.Padding   = new Padding(4, 0, 0, 0);

        // 按鈕垂直置中：content 32px，按鈕 28px → y = (32-28)/2 = 2
        const int btnY = 2;

        // ── btnSend ──────────────────────────────────────────
        btnSend.Location  = new Point(4, btnY);
        btnSend.Size      = new Size(60, 28);
        btnSend.Text      = "送出";
        btnSend.BackColor = Color.FromArgb(35, 134, 54);
        btnSend.ForeColor = Color.White;
        btnSend.FlatStyle = FlatStyle.Flat;
        btnSend.FlatAppearance.BorderSize = 0;
        btnSend.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
        btnSend.Name      = "btnSend";
        btnSend.Click    += btnSend_Click;

        // ── btnEnter ─────────────────────────────────────────
        btnEnter.Location  = new Point(68, btnY);
        btnEnter.Size      = new Size(64, 28);
        btnEnter.Text      = "↵ Enter";
        btnEnter.BackColor = Color.FromArgb(56, 76, 110);
        btnEnter.ForeColor = Color.FromArgb(158, 202, 255);
        btnEnter.FlatStyle = FlatStyle.Flat;
        btnEnter.FlatAppearance.BorderSize = 0;
        btnEnter.Font      = new Font("Segoe UI", 9f);
        btnEnter.Name      = "btnEnter";
        btnEnter.Click    += btnEnter_Click;

        // ── btnStop ───────────────────────────────────────────
        btnStop.Location  = new Point(136, btnY);
        btnStop.Size      = new Size(60, 28);
        btnStop.Text      = "Ctrl+C";
        btnStop.BackColor = Color.FromArgb(218, 54, 51);
        btnStop.ForeColor = Color.White;
        btnStop.FlatStyle = FlatStyle.Flat;
        btnStop.FlatAppearance.BorderSize = 0;
        btnStop.Font      = new Font("Segoe UI", 9f);
        btnStop.Name      = "btnStop";
        btnStop.Click    += btnStop_Click;

        // ── btnClear ──────────────────────────────────────────
        btnClear.Location  = new Point(200, btnY);
        btnClear.Size      = new Size(52, 28);
        btnClear.Text      = "清除";
        btnClear.BackColor = Color.FromArgb(48, 54, 61);
        btnClear.ForeColor = Color.FromArgb(201, 209, 217);
        btnClear.FlatStyle = FlatStyle.Flat;
        btnClear.FlatAppearance.BorderSize = 0;
        btnClear.Font      = new Font("Segoe UI", 9f);
        btnClear.Name      = "btnClear";
        btnClear.Click    += btnClear_Click;

        panelBtnRight.Controls.AddRange(
            new Control[] { btnSend, btnEnter, btnStop, btnClear });

        // ── lblInputHint ──────────────────────────────────────
        lblInputHint.Dock      = DockStyle.Fill;
        lblInputHint.Text      = "指令:";
        lblInputHint.ForeColor = Color.FromArgb(100, 121, 167);
        lblInputHint.Font      = new Font("Segoe UI", 9f);
        lblInputHint.TextAlign = ContentAlignment.MiddleCenter;
        lblInputHint.Name      = "lblInputHint";

        // ── txtInput ──────────────────────────────────────────
        txtInput.Dock          = DockStyle.Fill;
        txtInput.BackColor     = Color.FromArgb(13, 17, 23);
        txtInput.ForeColor     = Color.FromArgb(201, 209, 217);
        txtInput.BorderStyle   = BorderStyle.FixedSingle;
        txtInput.Font          = new Font("Cascadia Code", 10f);
        txtInput.PlaceholderText = "輸入給 Claude 的任務指令… (Enter 送出)";
        txtInput.Name          = "txtInput";
        txtInput.KeyDown      += txtInput_KeyDown;

        // ── TableLayoutPanel：確保 label 與 textbox 欄位不重疊 ──
        // Dock=Left + Dock=Fill 共存時 WinForms 會讓 Fill 先佔滿整個區域
        // 再把 Left label 疊上去，導致 TextBox 左側被遮蔽。
        // TableLayoutPanel 以欄寬切割保證絕對不重疊。
        var tblInput = new TableLayoutPanel();
        tblInput.Dock        = DockStyle.Fill;
        tblInput.Margin      = new Padding(0);
        tblInput.Padding     = new Padding(0);
        tblInput.ColumnCount = 2;
        tblInput.RowCount    = 1;
        tblInput.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46f)); // label
        tblInput.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f)); // textbox
        tblInput.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        tblInput.Controls.Add(lblInputHint, 0, 0);
        tblInput.Controls.Add(txtInput,     1, 0);

        // ── 加入 panelBottom：Right 先加，Fill 最後加 ─────────
        panelBottom.Controls.Add(panelBtnRight);   // Dock=Right
        panelBottom.Controls.Add(tblInput);        // Dock=Fill（tblInput 取代直接放 txtInput）

        // ════════════════════════════════════════════════════════
        // panelStatus — 最底部狀態列
        // ════════════════════════════════════════════════════════
        panelStatus.Dock      = DockStyle.Bottom;
        panelStatus.Height    = 24;
        panelStatus.BackColor = Color.FromArgb(31, 111, 235);
        panelStatus.Name      = "panelStatus";

        lblStatus.Dock      = DockStyle.Fill;
        lblStatus.Text      = "Sphynx 正在初始化…";
        lblStatus.ForeColor = Color.White;
        lblStatus.Font      = new Font("Segoe UI", 8.5f);
        lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        lblStatus.Padding   = new Padding(6, 0, 0, 0);
        lblStatus.Name      = "lblStatus";

        panelStatus.Controls.Add(lblStatus);

        // ════════════════════════════════════════════════════════
        // MainForm
        // ════════════════════════════════════════════════════════
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode       = AutoScaleMode.Font;
        BackColor           = Color.FromArgb(13, 17, 23);
        ClientSize          = new Size(1280, 820);
        MinimumSize         = new Size(1280, 820);
        MaximumSize         = new Size(1280, 820);   // 鎖定視窗大小
        FormBorderStyle     = FormBorderStyle.FixedSingle;
        MaximizeBox         = false;
        Text                = "Sphynx — AI-Native Personal DevOps Station";
        Name                = "MainForm";
        StartPosition       = FormStartPosition.CenterScreen;

        // 加入順序決定 Dock 優先：Bottom 先加，Fill 最後加
        Controls.Add(webViewTerminal);   // Dock=Fill  → 最後排，填滿上方空間
        Controls.Add(panelBottom);       // Dock=Bottom
        Controls.Add(panelStatus);       // Dock=Bottom（先加 → 在最下方）

        Load        += MainForm_Load;
        FormClosing += MainForm_FormClosing;

        ((System.ComponentModel.ISupportInitialize)webViewTerminal).EndInit();
        panelBottom.ResumeLayout(false);
        panelStatus.ResumeLayout(false);
        ResumeLayout(false);
    }
}