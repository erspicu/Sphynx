namespace SphynxApp;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;
    private Microsoft.Web.WebView2.WinForms.WebView2 webViewTerminal;
    private System.Windows.Forms.TextBox txtInput;
    private System.Windows.Forms.Button btnSend;
    private System.Windows.Forms.ComboBox cmbAiTool;
    private System.Windows.Forms.Label lblTool;
    private System.Windows.Forms.TextBox txtPid;
    private System.Windows.Forms.TextBox txtMsg;
    private System.Windows.Forms.Button btnSendToPid;
    private System.Windows.Forms.Label lblPid;
    private System.Windows.Forms.Label lblMsg;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.webViewTerminal = new Microsoft.Web.WebView2.WinForms.WebView2();
        this.txtInput = new System.Windows.Forms.TextBox();
        this.btnSend = new System.Windows.Forms.Button();
        this.cmbAiTool = new System.Windows.Forms.ComboBox();
        this.lblTool = new System.Windows.Forms.Label();
        this.txtPid = new System.Windows.Forms.TextBox();
        this.txtMsg = new System.Windows.Forms.TextBox();
        this.btnSendToPid = new System.Windows.Forms.Button();
        this.lblPid = new System.Windows.Forms.Label();
        this.lblMsg = new System.Windows.Forms.Label();
        ((System.ComponentModel.ISupportInitialize)(this.webViewTerminal)).BeginInit();
        this.SuspendLayout();
        // 
        // webViewTerminal
        // 
        this.webViewTerminal.AllowExternalDrop = true;
        this.webViewTerminal.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
        this.webViewTerminal.CreationProperties = null;
        this.webViewTerminal.DefaultBackgroundColor = System.Drawing.Color.White;
        this.webViewTerminal.Location = new System.Drawing.Point(12, 45);
        this.webViewTerminal.Name = "webViewTerminal";
        this.webViewTerminal.Size = new System.Drawing.Size(776, 320);
        this.webViewTerminal.TabIndex = 0;
        this.webViewTerminal.ZoomFactor = 1D;
        // 
        // lblPid
        // 
        this.lblPid.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
        this.lblPid.AutoSize = true;
        this.lblPid.Location = new System.Drawing.Point(12, 378);
        this.lblPid.Name = "lblPid";
        this.lblPid.Size = new System.Drawing.Size(28, 15);
        this.lblPid.TabIndex = 5;
        this.lblPid.Text = "PID:";
        // 
        // txtPid
        // 
        this.txtPid.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
        this.txtPid.Location = new System.Drawing.Point(46, 375);
        this.txtPid.Name = "txtPid";
        this.txtPid.Size = new System.Drawing.Size(60, 23);
        this.txtPid.TabIndex = 6;
        // 
        // lblMsg
        // 
        this.lblMsg.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
        this.lblMsg.AutoSize = true;
        this.lblMsg.Location = new System.Drawing.Point(115, 378);
        this.lblMsg.Name = "lblMsg";
        this.lblMsg.Size = new System.Drawing.Size(32, 15);
        this.lblMsg.TabIndex = 7;
        this.lblMsg.Text = "Msg:";
        // 
        // txtMsg
        // 
        this.txtMsg.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
        this.txtMsg.Location = new System.Drawing.Point(153, 375);
        this.txtMsg.Name = "txtMsg";
        this.txtMsg.Size = new System.Drawing.Size(514, 23);
        this.txtMsg.TabIndex = 8;
        // 
        // btnSendToPid
        // 
        this.btnSendToPid.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
        this.btnSendToPid.Location = new System.Drawing.Point(673, 374);
        this.btnSendToPid.Name = "btnSendToPid";
        this.btnSendToPid.Size = new System.Drawing.Size(115, 23);
        this.btnSendToPid.TabIndex = 9;
        this.btnSendToPid.Text = "Send to PID";
        this.btnSendToPid.UseVisualStyleBackColor = true;
        this.btnSendToPid.Click += new System.EventHandler(this.btnSendToPid_Click);
        // 
        // txtInput
        // 
        this.txtInput.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
        this.txtInput.Location = new System.Drawing.Point(12, 408);
        this.txtInput.Name = "txtInput";
        this.txtInput.Size = new System.Drawing.Size(695, 23);
        this.txtInput.TabIndex = 1;
        // 
        // btnSend
        // 
        this.btnSend.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
        this.btnSend.Location = new System.Drawing.Point(713, 407);
        this.btnSend.Name = "btnSend";
        this.btnSend.Size = new System.Drawing.Size(75, 23);
        this.btnSend.TabIndex = 2;
        this.btnSend.Text = "Send";
        this.btnSend.UseVisualStyleBackColor = true;
        this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
        // 
        // cmbAiTool
        // 
        this.cmbAiTool.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.cmbAiTool.FormattingEnabled = true;
        this.cmbAiTool.Items.AddRange(new object[] {
            "Gemini CLI",
            "Claude Code"});
        this.cmbAiTool.Location = new System.Drawing.Point(100, 12);
        this.cmbAiTool.Name = "cmbAiTool";
        this.cmbAiTool.Size = new System.Drawing.Size(150, 23);
        this.cmbAiTool.TabIndex = 3;
        this.cmbAiTool.SelectedIndexChanged += new System.EventHandler(this.cmbAiTool_SelectedIndexChanged);
        // 
        // lblTool
        // 
        this.lblTool.AutoSize = true;
        this.lblTool.Location = new System.Drawing.Point(12, 15);
        this.lblTool.Name = "lblTool";
        this.lblTool.Size = new System.Drawing.Size(82, 15);
        this.lblTool.TabIndex = 4;
        this.lblTool.Text = "Active AI Tool:";
        // 
        // MainForm
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(800, 450);
        this.Controls.Add(this.btnSendToPid);
        this.Controls.Add(this.txtMsg);
        this.Controls.Add(this.lblMsg);
        this.Controls.Add(this.txtPid);
        this.Controls.Add(this.lblPid);
        this.Controls.Add(this.lblTool);
        this.Controls.Add(this.cmbAiTool);
        this.Controls.Add(this.btnSend);
        this.Controls.Add(this.txtInput);
        this.Controls.Add(this.webViewTerminal);
        this.Name = "MainForm";
        this.Text = "Sphynx AI DevOps Station";
        ((System.ComponentModel.ISupportInitialize)(this.webViewTerminal)).EndInit();
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}
