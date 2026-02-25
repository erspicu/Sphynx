namespace Sphynx;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // 啟用 Windows 視覺樣式
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // .NET 6+ WinForms 高 DPI 支援
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        // 全域未處理例外捕捉（避免程式閃退）
        Application.ThreadException += (_, e) =>
        {
            MessageBox.Show(
                $"未預期的執行緒例外：\n{e.Exception.Message}\n\n{e.Exception.StackTrace}",
                "Sphynx 錯誤",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            MessageBox.Show(
                $"嚴重未處理例外（程式即將關閉）：\n{ex?.Message ?? e.ExceptionObject?.ToString()}",
                "Sphynx 致命錯誤",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        };

        Application.Run(new MainForm());
    }
}
