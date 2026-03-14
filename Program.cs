using System; using System.Windows.Forms; using System.Diagnostics; using System.Runtime.InteropServices; using System.Threading; using System.IO;

namespace LineageHelper
{
    static class Program
    {
        [STAThread]
        static void Main() { Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Application.Run(new MainForm()); }
    }

    public class MainForm : Form
    {
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
        Button btnAttach, btnStart, btnStop, btnPause, btnAutoDetect;
        
        public MainForm()
        {
            this.Text = "天堂輔助程式 v1.2";
            this.Size = new System.Drawing.Size(450, 420);
            this.StartPosition = FormStartPosition.CenterScreen;
            
            var lbl1 = new Label { Text = "遊戲程序:", Left = 15, Top = 15 };
            txtProcess = new TextBox { Left = 85, Top = 15, Width = 130, Text = "Lineage" };
            btnAutoDetect = new Button { Text = "自動檢測", Left = 220, Top = 13, Width = 85 };
            btnAutoDetect.Click += BtnAutoDetect_Click;
            btnAttach = new Button { Text = "附加", Left = 310, Top = 13, Width = 70 };
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
            txtLog = new TextBox { Left = 15, Top = 255, Width = 410, Height = 110, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };
            txtLog.Font = new System.Drawing.Font("Consolas", 9);
            
            this.Controls.AddRange(new Control[] { lbl1, txtProcess, btnAutoDetect, btnAttach, lblStatus, btnStart, btnStop, btnPause, lblHotkey, grp, lblLog, txtLog });
            
            Log("=== 天堂輔助程式 v1.2 ===");
            Log("點擊「自動檢測」找遊戲");
        }
        
        void BtnAutoDetect_Click(object sender, EventArgs e)
        {
            Log("正在檢測遊戲程序...");
            
            // 常見的遊戲程序名稱
            string[] possibleNames = { "Purple", "Lineage", "LineageClassic", "LineageW", "Launcher" };
            
            // 先檢查運行的程序
            foreach (string name in possibleNames)
            {
                Process[] ps = Process.GetProcessesByName(name);
                if (ps.Length > 0)
                {
                    txtProcess.Text = name;
                    Log($"找到遊戲: {name}");
                    return;
                }
            }
            
            // 檢查 NCSOFT 資料夾
            string ncsoftPath = @"C:\Program Files (x86)\NCSOFT";
            if (Directory.Exists(ncsoftPath))
            {
                try
                {
                    foreach (string dir in Directory.GetDirectories(ncsoftPath))
                    {
                        string dirName = Path.GetFileName(dir);
                        Log($"檢測資料夾: {dirName}");
                        
                        foreach (string exe in Directory.GetFiles(dir, "*.exe"))
                        {
                            string exeName = Path.GetFileNameWithoutExtension(exe);
                            if (exeName.Contains("Lineage") || exeName.Contains("Purple"))
                            {
                                // 檢查是否正在運行
                                Process[] ps = Process.GetProcessesByName(exeName);
                                if (ps.Length > 0)
                                {
                                    txtProcess.Text = exeName;
                                    Log($"找到遊戲: {exeName}");
                                    return;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex) { Log($"檢測錯誤: {ex.Message}"); }
            }
            
            Log("未找到遊戲，請手動輸入");
            MessageBox.Show("請手動輸入遊戲程序名稱\n或確認遊戲已打開", "提示");
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
