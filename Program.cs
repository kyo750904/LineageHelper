using System; using System.Windows.Forms; using System.Diagnostics; using System.Runtime.InteropServices; using System.Threading;

namespace LineageHelper
{
    static class Program
    {
        [STAThread]
        static void Main() { Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Application.Run(new MainForm()); }
    }

    public class MainForm : Form
    {
        // Windows API
        [DllImport("kernel32.dll")] static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);
        [DllImport("kernel32.dll")] static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesRead);
        [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr hObject);
        [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        
        const uint PROCESS_VM_READ = 0x0010;
        const uint KEYEVENTF_KEYDOWN = 0x0000, KEYEVENTF_KEYUP = 0x0002;
        
        IntPtr hProcess; int processId; bool isAttached, botRunning = false, botPaused = false;
        
        TextBox txtProcess, txtLog;
        Label lblStatus;
        Button btnAttach, btnStart, btnStop, btnPause;
        
        public MainForm()
        {
            this.Text = "天堂輔助程式 v1.1";
            this.Size = new System.Drawing.Size(450, 380);
            this.StartPosition = FormStartPosition.CenterScreen;
            
            var lbl1 = new Label { Text = "遊戲程序:", Left = 15, Top = 15 };
            txtProcess = new TextBox { Left = 85, Top = 15, Width = 180, Text = "Lineage" };
            btnAttach = new Button { Text = "附加", Left = 275, Top = 13, Width = 70 };
            btnAttach.Click += BtnAttach_Click;
            
            lblStatus = new Label { Text = "狀態: 未連接", Left = 15, Top = 45, Width = 400, ForeColor = System.Drawing.Color.Red };
            
            btnStart = new Button { Text = "啟動", Left = 15, Top = 75, Width = 80 };
            btnStart.Click += BtnStart_Click;
            btnStop = new Button { Text = "停止", Left = 105, Top = 75, Width = 80 };
            btnStop.Click += BtnStop_Click;
            btnPause = new Button { Text = "暫停", Left = 195, Top = 75, Width = 80 };
            btnPause.Click += BtnPause_Click;
            
            var lblHotkey = new Label { Text = "熱鍵: F1隱藏 | E戰鬥 | Q老闆鍵 | W暫停", Left = 15, Top = 115, Width = 400, ForeColor = System.Drawing.Color.Gray };
            
            var grp = new GroupBox { Text = "快速測試", Left = 15, Top = 145, Width = 400, Height = 80 };
            var btnTest1 = new Button { Text = "按 F1", Left = 10, Top = 25, Width = 60 };
            btnTest1.Click += (s,e) => { keybd_event(0x70, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero); Thread.Sleep(50); keybd_event(0x70, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); Log("已發送 F1"); };
            var btnTest2 = new Button { Text = "按 E", Left = 75, Top = 25, Width = 60 };
            btnTest2.Click += (s,e) => { keybd_event(0x45, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero); Thread.Sleep(50); keybd_event(0x45, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); Log("已發送 E"); };
            var btnTest3 = new Button { Text = "按 Q", Left = 140, Top = 25, Width = 60 };
            btnTest3.Click += (s,e) => { keybd_event(0x51, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero); Thread.Sleep(50); keybd_event(0x51, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); Log("已發送 Q"); };
            var btnTest4 = new Button { Text = "按 W", Left = 205, Top = 25, Width = 60 };
            btnTest4.Click += (s,e) => { keybd_event(0x57, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero); Thread.Sleep(50); keybd_event(0x57, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); Log("已發送 W"); };
            grp.Controls.AddRange(new Control[] { btnTest1, btnTest2, btnTest3, btnTest4 });
            
            var lblLog = new Label { Text = "日誌:", Left = 15, Top = 235 };
            txtLog = new TextBox { Left = 15, Top = 255, Width = 410, Height = 80, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };
            txtLog.Font = new System.Drawing.Font("Consolas", 9);
            
            this.Controls.AddRange(new Control[] { lbl1, txtProcess, btnAttach, lblStatus, btnStart, btnStop, btnPause, lblHotkey, grp, lblLog, txtLog });
            
            Log("=== 天堂輔助程式 v1.0 ===");
            Log("1. 先打開天堂遊戲");
            Log("2. 輸入程序名並點擊附加");
            Log("3. 點擊啟動開始掛機");
        }
        
        void BtnAttach_Click(object sender, EventArgs e)
        {
            string name = txtProcess.Text.Trim();
            if (name == "") { MessageBox.Show("請輸入程序名稱"); return; }
            
            Process[] ps = Process.GetProcessesByName(name);
            if (ps.Length == 0) { MessageBox.Show("找不到程序: " + name + "\n請確認遊戲是否已打開"); return; }
            
            processId = ps[0].Id;
            hProcess = OpenProcess(PROCESS_VM_READ, false, processId);
            if (hProcess == IntPtr.Zero) { MessageBox.Show("無法附加，請用系統管理員身份執行"); return; }
            
            isAttached = true;
            lblStatus.Text = "狀態: 已連接";
            lblStatus.ForeColor = System.Drawing.Color.Green;
            Log("已附加到: " + name + " (PID:" + processId + ")");
        }
        
        void BtnStart_Click(object sender, EventArgs e)
        {
            if (!isAttached) { MessageBox.Show("請先附加到遊戲"); return; }
            botRunning = true; botPaused = false;
            lblStatus.Text = "狀態: 運行中";
            lblStatus.ForeColor = System.Drawing.Color.Green;
            Log("機器人已啟動");
        }
        
        void BtnStop_Click(object sender, EventArgs e)
        {
            botRunning = false;
            lblStatus.Text = "狀態: 已停止";
            lblStatus.ForeColor = System.Drawing.Color.Red;
            Log("機器人已停止");
        }
        
        void BtnPause_Click(object sender, EventArgs e)
        {
            if (!botRunning) { MessageBox.Show("請先啟動"); return; }
            botPaused = !botPaused;
            lblStatus.Text = botPaused ? "狀態: 已暫停" : "狀態: 運行中";
            Log(botPaused ? "已暫停" : "已繼續");
        }
        
        void Log(string msg)
        {
            if (txtLog.InvokeRequired) txtLog.Invoke(new Action(() => { txtLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + msg + "\r\n"); txtLog.SelectionStart = txtLog.Text.Length; txtLog.ScrollToCaret(); }));
            else { txtLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + msg + "\r\n"); txtLog.SelectionStart = txtLog.Text.Length; txtLog.ScrollToCaret(); }
        }
    }
}
