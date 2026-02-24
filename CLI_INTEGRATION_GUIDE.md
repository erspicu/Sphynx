# 🛠️ Sphynx AI Station: CLI 整合與 GUI 管道實踐手冊

本文件記錄了在 C# WinForms GUI 中整合互動式 CLI 工具（如 Gemini CLI 與 Claude Code）的技術挑戰、解決方案與實踐心得。

---

## 1. 核心技術架構
Sphynx 系統採用 **C# `System.Diagnostics.Process` -> PowerShell (`pwsh`) -> xterm.js (WebView2)** 的多層轉發架構：
- **通訊層**：利用 C# 重導向 `StandardInput` / `StandardOutput`。
- **介面層**：透過 `xterm.js` 處理 ANSI 轉義序列（Escape Sequences），實現顏色與樣式渲染。

---

## 2. 核心障礙：TTY 管道死結 (The TTY Conflict)
在整合 **Claude Code** 時，遇到了 CLI 工具與 GUI 管道之間的本質衝突。

### 🚨 現象與原因
1. **Raw Mode 需求**：現代互動式 CLI 工具（使用 `Ink` 或 `React-Blessed` 庫）要求「原始終端（Raw TTY）」來進行 UI 渲染。
2. **管道非終端**：C# 重導向建立的是 **匿名管道 (Anonymous Pipes)**，不具備 TTY 特性。
3. **靜默掛起 (Hanging)**：工具因偵測到「非終端環境」而拒絕輸出，或在背景等待一個看不見的授權確認。

---

## 3. 實踐模式對比

### 【模式 A】獨立程序執行模式 (RunOnce Strategy) —— *Claude Code 採納*
每次發送指令時，啟動一個全新的、短暫的 CLI 實例。
- **解決方案**：
    - 使用 `-p` (print) 旗標強制非互動輸出。
    - 使用 `-c` (continue) 旗標讀取歷史紀錄，實現「跨程序對話記憶」。
    - 使用 `--dangerously-skip-permissions` 跳過隱藏的權限牆。
- **環境變數**：設定 `$env:CI='true'` 與 `$env:TERM='xterm'` 誘導工具進入自動化相容模式。

### 【模式 B】持久管道模式 (Persistent Shell) —— *Gemini CLI 採納*
啟動一個長駐的 PowerShell 實例，持續接收指令。
- **適用場景**：簡單的腳本化 CLI 或不要求 Raw TTY 的工具。
- **技術細節**：必須處理好 `StandardInput` 的換行符號（`
`），否則指令會堆積在緩衝區。

---

## 4. 關鍵技術修補 (Hacks)

### 🌐 編碼修復
必須在 PowerShell 啟動之初強制設定 UTF-8，否則跨管道傳輸中文會產生亂碼：
```powershell
$OutputEncoding = [System.Text.Encoding]::UTF8;
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8;
```

### ⌨️ 虛擬 Enter 敲擊
在 C# 中寫入指令時，必須確保緩衝區刷新：
```csharp
_pwshProcess.StandardInput.Write(text + "
");
_pwshProcess.StandardInput.Flush();
```

---

## 5. 總結建議
針對不同的 CLI 工具，應採取不同的整合路徑：

| 工具特性 | 整合建議 | 關鍵策略 |
| :--- | :--- | :--- |
| **互動式 UI 介面 (如 Claude Code)** | **獨立執行 (RunOnce)** | `CI=true`, `-p`, `-c` |
| **標準文本輸入輸出 (如 Gemini CLI)** | **持久管道 (Persistent)** | `UTF8`, `Flush()` |

---
*Last Updated: 2026-02-24*
