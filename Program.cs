using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;

namespace LineageHelper
{
    static class Program
    {
        [STAThread]
        static void Main() 
        { 
            Application.EnableVisualStyles(); 
            Application.SetCompatibleTextRenderingDefault(false); 
            Application.Run(new MainForm()); 
        }
    }

    public class MainForm : Form
    {
        // ==================== 記憶體 API ====================
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll")]
        static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out int lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetExitCodeThread(IntPtr hThread, out int lpExitCode);

        // ==================== 視窗 API ====================
        [DllImport("user32.dll")]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern int GetWindowThreadProcessId(IntPtr hWnd, out int processId);

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        // ==================== 常數 ====================
        const uint PROCESS_ALL_ACCESS = 0x1F0FFF;
        const uint PROCESS_VM_READ = 0x0010;
        const uint PROCESS_VM_WRITE = 0x0020;
        const uint PROCESS_VM_OPERATION = 0x0008;
        const uint PROCESS_CREATE_THREAD = 0x0002;

        const uint MEM_COMMIT = 0x1000;
        const uint MEM_RESERVE = 0x2000;
        const uint PAGE_READWRITE = 0x04;

        const uint INFINITE = 0xFFFFFFFF;
        const int STILL_ACTIVE = 0x00000103;

        // ==================== 偏移量 (需要研究) ====================
        // 這些是範例偏移量，需要實際研究
        public static int[][] Offsets = new int[][]
        {
            new int[] { 0x12345678 }, // HP
            new int[] { 0x1234567C }, // MP
            new int[] { 0x12345680 }, // X座標
            new int[] { 0x12345684 }, // Y座標
            new int[] { 0x12345688 }, // 等級
        };

        // ==================== 變數 ====================
        IntPtr hProcess = IntPtr.Zero;
        int processId;
        bool isAttached = false;
        bool botRunning = false;
        
        TextBox txtLog;
        Label lblStatus;
        Button btnAttach, btnStart, btnStop;
        
        public MainForm()
        {
            Text = "天堂輔助 v3.5 - 記憶體控制版";
            Size = new System.Drawing.Size(450, 450);
            StartPosition = FormStartPosition.CenterScreen;
            
            // 狀態
            lblStatus = new Label { Text = "狀態: 未連接", Left = 15, Top = 15, Width = 400, ForeColor = System.Drawing.Color.Red };
            
            // 按鈕
            btnAttach = new Button { Text = "1. 附加到遊戲", Left = 15, Top = 45, Width = 130, Height = 35 };
            btnAttach.Click += BtnAttach_Click;
            
            btnStart = new Button { Text = "2. 讀取記憶體", Left = 155, Top = 45, Width = 130, Height = 35, Enabled = false };
            btnStart.Click += BtnStart_Click;
            
            btnStop = new Button { Text = "停止", Left = 295, Top = 45, Width = 100, Height = 35 };
            btnStop.Click += (s,e) => { botRunning = false; lblStatus.Text = "已停止"; };
            
            // 說明
            var info = new Label 
            { 
                Text = "功能說明:\n" +
                       "• 附加到遊戲程序\n" +
                       "• 讀取遊戲記憶體數據\n" +
                       "• (偏移量需要研究)", 
                Left = 15, Top = 90, Width = 400, Height = 60,
                ForeColor = System.Drawing.Color.Gray 
            };
            
            // 偏移量設定
            var grpOffset = new GroupBox { Text = "記憶體偏移量設定", Left = 15, Top = 155, Width = 400, Height = 120 };
            
            var lbl1 = new Label { Text = "HP偏移:", Left = 10, Top = 25 };
            var txtHP = new TextBox { Left = 70, Top = 22, Width = 100, Text = "0x00A8B7C4" };
            
            var lbl2 = new Label { Text = "MP偏移:", Left = 180, Top = 25 };
            var txtMP = new TextBox { Left = 240, Top = 22, Width = 100, Text = "0x00A8B7C8" };
            
            var lbl3 = new Label { Text = "X座標:", Left = 10, Top = 55 };
            var txtX = new TextBox { Left = 70, Top = 52, Width = 100, Text = "0x00A8B7D0" };
            
            var lbl4 = new Label { Text = "Y座標:", Left = 180, Top = 55 };
            var txtY = new TextBox { Left = 240, Top = 52, Width = 100, Text = "0x00A8B7D4" };
            
            var btnRead = new Button { Text = "讀取數據", Left = 150, Top = 80, Width = 100 };
            btnRead.Click += (s,e) => { ReadMemory(txtHP, txtMP, txtX, txtY); };
            
            grpOffset.Controls.AddRange(new Control[] { lbl1, txtHP, lbl2, txtMP, lbl3, txtX, lbl4, txtY, btnRead });
            
            // 日誌
            var lblLog = new Label { Text = "日誌:", Left = 15, Top = 285 };
            txtLog = new TextBox { Left = 15, Top = 305, Width = 400, Height = 100, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };
            txtLog.Font = new System.Drawing.Font("Consolas", 9);
            
            Controls.AddRange(new Control[] { lblStatus, btnAttach, btnStart, btnStop, info, grpOffset, lblLog, txtLog });
            
            Log("=== 天堂輔助 v3.5 ===");
            Log("記憶體控制版");
        }
        
        void BtnAttach_Click(object sender, EventArgs e)
        {
            Log("=== 附加到遊戲 ===");
            
            // 找遊戲視窗
            IntPtr hwnd = FindWindow(null, "lineage Classic");
            if (hwnd == IntPtr.Zero) hwnd = FindWindow(null, "Lineage");
            if (hwnd == IntPtr.Zero) hwnd = FindWindow(null, "天堂");
            
            if (hwnd == IntPtr.Zero)
            {
                // 嘗試用程序名稱
                string[] names = { "Purple", "Lineage", "LineageClassic" };
                foreach (string name in names)
                {
                    try
                    {
                        Process[] ps = Process.GetProcessesByName(name);
                        if (ps.Length > 0)
                        {
                            processId = ps[0].Id;
                            hwnd = ps[0].MainWindowHandle;
                            goto found;
                        }
                    }
                    catch { }
                }
                
                Log("✗ 找不到遊戲程序");
                MessageBox.Show("請確認遊戲已打開");
                return;
            }
            
            found:
            if (hwnd != IntPtr.Zero)
            {
                GetWindowThreadProcessId(hwnd, out processId);
                Log($"找到程序 ID: {processId}");
                
                // 打開程序
                hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, processId);
                
                if (hProcess != IntPtr.Zero)
                {
                    isAttached = true;
                    btnStart.Enabled = true;
                    lblStatus.Text = "狀態: 已附加";
                    lblStatus.ForeColor = System.Drawing.Color.Green;
                    Log("✓ 附加成功");
                }
                else
                {
                    // 嘗試用較低權限
                    hProcess = OpenProcess(PROCESS_VM_READ | PROCESS_VM_WRITE | PROCESS_CREATE_THREAD, false, processId);
                    if (hProcess != IntPtr.Zero)
                    {
                        isAttached = true;
                        btnStart.Enabled = true;
                        lblStatus.Text = "狀態: 已附加(限制)";
                        lblStatus.ForeColor = System.Drawing.Color.Orange;
                        Log("✓ 附加成功(限制模式)");
                    }
                    else
                    {
                        Log("✗ 附加失敗，請用管理員身份");
                        MessageBox.Show("請用管理員身份執行");
                    }
                }
            }
        }
        
        void BtnStart_Click(object sender, EventArgs e)
        {
            if (!isAttached)
            {
                MessageBox.Show("請先附加");
                return;
            }
            
            botRunning = true;
            lblStatus.Text = "狀態: 讀取中...";
            Log("開始讀取記憶體...");
            
            Thread t = new Thread(() => {
                while(botRunning)
                {
                    try
                    {
                        // 讀取範例偏移量 (需要改成正確的)
                        IntPtr baseAddr = new IntPtr(0x00A8B7C4);
                        
                        byte[] buffer = new byte[4];
                        int bytesRead;
                        
                        if (ReadProcessMemory(hProcess, baseAddr, buffer, 4, out bytesRead))
                        {
                            int hp = BitConverter.ToInt32(buffer, 0);
                            this.Invoke(new Action(() => {
                                lblStatus.Text = $"HP: {hp}";
                            }));
                        }
                        
                        Thread.Sleep(500);
                    }
                    catch (Exception ex)
                    {
                        this.Invoke(new Action(() => {
                            Log("錯誤: " + ex.Message);
                        }));
                    }
                }
            });
            t.IsBackground = true;
            t.Start();
        }
        
        void ReadMemory(TextBox txtHP, TextBox txtMP, TextBox txtX, TextBox txtY)
        {
            if (!isAttached)
            {
                MessageBox.Show("請先附加");
                return;
            }
            
            try
            {
                // 讀取 HP
                IntPtr hpAddr = new IntPtr(Convert.ToInt32(txtHP.Text, 16));
                byte[] hpBuf = new byte[4];
                int hr;
                if (ReadProcessMemory(hProcess, hpAddr, hpBuf, 4, out hr))
                {
                    int hp = BitConverter.ToInt32(hpBuf, 0);
                    Log($"HP: {hp}");
                }
                else
                {
                    Log("HP讀取失敗");
                }
                
                // 讀取 MP
                IntPtr mpAddr = new IntPtr(Convert.ToInt32(txtMP.Text, 16));
                byte[] mpBuf = new byte[4];
                if (ReadProcessMemory(hProcess, mpAddr, mpBuf, 4, out hr))
                {
                    int mp = BitConverter.ToInt32(mpBuf, 0);
                    Log($"MP: {mp}");
                }
                
                // 讀取 X
                IntPtr xAddr = new IntPtr(Convert.ToInt32(txtX.Text, 16));
                byte[] xBuf = new byte[4];
                if (ReadProcessMemory(hProcess, xAddr, xBuf, 4, out hr))
                {
                    float x = BitConverter.ToSingle(xBuf, 0);
                    Log($"X: {x}");
                }
                
                // 讀取 Y
                IntPtr yAddr = new IntPtr(Convert.ToInt32(txtY.Text, 16));
                byte[] yBuf = new byte[4];
                if (ReadProcessMemory(hProcess, yAddr, yBuf, 4, out hr))
                {
                    float y = BitConverter.ToSingle(yBuf, 0);
                    Log($"Y: {y}");
                }
            }
            catch (Exception ex)
            {
                Log("錯誤: " + ex.Message);
            }
        }
        
        void Log(string msg)
        {
            if (txtLog.InvokeRequired)
                txtLog.Invoke(new Action(() => {
                    txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\r\n");
                    txtLog.SelectionStart = txtLog.Text.Length;
                    txtLog.ScrollToCaret();
                }));
            else
            {
                txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\r\n");
            }
        }
        
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            botRunning = false;
            if (hProcess != IntPtr.Zero)
                CloseHandle(hProcess);
            base.OnFormClosing(e);
        }
    }
}
