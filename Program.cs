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
        [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr hObject);
        [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] static extern void mouse_event(uint dwFlags, int dx, int dy, int dwData, UIntPtr dwExtraInfo);
        [DllImport("user32.dll")] static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        [DllImport("user32.dll")] static extern int GetSystemMetrics(int nIndex);
        
        const uint PROCESS_VM_READ = 0x0010;
        const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        const uint MOUSEEVENTF_LEFTUP = 0x0004;
        const uint MOUSEEVENTF_MOVE = 0x0001;
        const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
        const uint KEYEVENTF_KEYDOWN = 0x0000, KEYEVENTF_KEYUP = 0x0002;
        
        IntPtr hProcess; int processId; bool isAttached, botRunning = false, botPaused = false;
        
        TextBox txtProcess, txtLog;
        Label lblStatus;
        Button btnAttach, btnStart, btnStop, btnPause, btnAutoDetect;
        Thread botThread;
        Random rand = new Random();
        
        public MainForm()
        {
            this.Text = "天堂輔助程式 v1.4";
            this.Size = new System.Drawing.Size(450, 450);
            this.StartPosition = FormStartPosition.CenterScreen;
            
            var lbl1 = new Label { Text = "遊戲程序:", Left = 15, Top = 15 };
            txtProcess = new TextBox { Left = 85, Top = 15, Width = 130, Text = "Purple" };
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
            
            var lblHotkey = new Label { Text = "滑鼠左鍵:點擊移動 | 攻擊鍵:ctrl", Left = 15, Top = 115, Width = 400, ForeColor = System.Drawing.Color.Gray };
            
            var grp = new GroupBox { Text = "手動測試", Left = 15, Top = 145, Width = 400, Height = 80 };
            var btnTest1 = new Button { Text = "滑鼠左鍵", Left = 10, Top = 25, Width = 80 };
            btnTest1.Click += (s,e) => { MouseClick(); Log("已點擊"); };
            var btnTest2 = new Button { Text = "攻擊", Left = 100, Top = 25, Width = 80 };
            btnTest2.Click += (s,e) => { keybd_event(0x11, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero); Thread.Sleep(100); keybd_event(0x11, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); Log("Ctrl攻擊"); };
            var btnTest3 = new Button { Text = "撿物", Left = 190, Top = 25, Width = 80 };
            btnTest3.Click += (s,e) => { keybd_event(0x12, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero); Thread.Sleep(100); keybd_event(0x12, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); Log("Alt撿物"); };
            var btnTestF1 = new Button { Text = "F1隱藏", Left = 280, Top = 25, Width = 80 };
            btnTestF1.Click += (s,e) => { keybd_event(0x70, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero); Thread.Sleep(50); keybd_event(0x70, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); Log("F1"); };
            grp.Controls.AddRange(new Control[] { btnTest1, btnTest2, btnTest3, btnTestF1 });
            
            var lblLog = new Label { Text = "日誌:", Left = 15, Top = 235 };
            txtLog = new TextBox { Left = 15, Top = 255, Width = 410, Height = 150, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };
            txtLog.Font = new System.Drawing.Font("Consolas", 9);
            
            this.Controls.AddRange(new Control[] { lbl1, txtProcess, btnAutoDetect, btnAttach, lblStatus, btnStart, btnStop, btnPause, lblHotkey, grp, lblLog, txtLog });
            
            Log("=== 天堂輔助程式 v1.4 ===");
            Log("1. 打開天堂遊戲");
            Log("2. 點擊「自動檢測」");
            Log("3. 點擊「啟動」開始掛機");
            Log("提示: 遊戲用滑鼠移動");
        }
        
        void MouseClick()
        {
            // 獲取螢幕解析度
            int screenWidth = GetSystemMetrics(0);  // SM_CXSCREEN
            int screenHeight = GetSystemMetrics(1); // SM_CYSCREEN
            
            // 隨機移動滑鼠位置
            int randX = rand.Next(100, screenWidth - 100);
            int randY = rand.Next(100, screenHeight - 100);
            
            // 移動滑鼠 (使用絕對座標)
            mouse_event(MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE, 
                       randX * 65535 / screenWidth, 
                       randY * 65535 / screenHeight, 
                       0, UIntPtr.Zero);
            Thread.Sleep(100);
            
            // 點擊左鍵
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            Thread.Sleep(rand.Next(50, 150));
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
        }
        
        void BtnAutoDetect_Click(object sender, EventArgs e)
        {
            Log("正在檢測遊戲程序...");
            
            string[] possibleNames = { "Purple", "Lineage", "LineageClassic", "LineageW", "Launcher" };
            
            foreach (string name in possibleNames)
            {
                try
                {
                    Process[] ps = Process.GetProcessesByName(name);
                    if (ps.Length > 0)
                    {
                        txtProcess.Text = name;
                        Log($"✓ 找到遊戲: {name}");
                        
                        processId = ps[0].Id;
                        hProcess = OpenProcess(PROCESS_VM_READ, false, processId);
                        if (hProcess != IntPtr.Zero)
                        {
                            isAttached = true;
                            lblStatus.Text = "狀態: 已連接";
                            lblStatus.ForeColor = System.Drawing.Color.Green;
                            Log($"✓ 已附加 (PID:{processId})");
                            BtnStart_Click(sender, e);
                            return;
                        }
                    }
                }
                catch { }
            }
            
            Log("✗ 未找到遊戲");
            MessageBox.Show("請確認遊戲已打開", "提示");
        }
        
        void BtnAttach_Click(object sender, EventArgs e)
        {
            string name = txtProcess.Text.Trim();
            if (name == "") { MessageBox.Show("請輸入程序名稱"); return; }
            
            try
            {
                Process[] ps = Process.GetProcessesByName(name);
                if (ps.Length == 0) { MessageBox.Show("找不到程序: " + name); return; }
                
                processId = ps[0].Id;
                hProcess = OpenProcess(PROCESS_VM_READ, false, processId);
                if (hProcess == IntPtr.Zero) { MessageBox.Show("無法附加，請用系統管理員身份執行"); return; }
                
                isAttached = true;
                lblStatus.Text = "狀態: 已連接";
                lblStatus.ForeColor = System.Drawing.Color.Green;
                Log("已附加: " + name);
            }
            catch (Exception ex) { MessageBox.Show("錯誤: " + ex.Message); }
        }
        
        void BtnStart_Click(object sender, EventArgs e)
        {
            if (!isAttached) { MessageBox.Show("請先附加到遊戲"); return; }
            if (botRunning) return;
            
            botRunning = true; botPaused = false;
            lblStatus.Text = "狀態: 運行中";
            lblStatus.ForeColor = System.Drawing.Color.Green;
            Log(">>> 機器人啟動 <<<");
            
            botThread = new Thread(BotLoop);
            botThread.IsBackground = true;
            botThread.Start();
        }
        
        void BotLoop()
        {
            int cycle = 0;
            while (botRunning)
            {
                try
                {
                    if (botPaused)
                    {
                        Thread.Sleep(500);
                        continue;
                    }
                    
                    cycle++;
                    
                    // 隨機延遲 模擬人類
                    int delay = rand.Next(800, 2500);
                    Thread.Sleep(delay);
                    
                    if (cycle % 5 == 0)
                    {
                        // 點擊移動
                        this.Invoke(new Action(() => {
                            try {
                                MouseClick();
                                Log("↗ 移動點擊");
                            } catch {}
                        }));
                    }
                    
                    if (cycle % 15 == 0)
                    {
                        // 攻擊
                        this.Invoke(new Action(() => {
                            try {
                                keybd_event(0x11, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero); // Ctrl
                                Thread.Sleep(rand.Next(50, 150));
                                keybd_event(0x11, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                                Log("⚔ 攻擊");
                            } catch {}
                        }));
                    }
                    
                    if (cycle % 25 == 0)
                    {
                        // 撿物
                        this.Invoke(new Action(() => {
                            try {
                                keybd_event(0x12, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero); // Alt
                                Thread.Sleep(rand.Next(50, 150));
                                keybd_event(0x12, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                                Log("📦 撿物");
                            } catch {}
                        }));
                    }
                    
                }
                catch { }
            }
            Log("機器人已停止");
        }
        
        void BtnStop_Click(object sender, EventArgs e)
        {
            botRunning = false;
            lblStatus.Text = "狀態: 已停止";
            lblStatus.ForeColor = System.Drawing.Color.Red;
            Log(">>> 機器人停止 <<<");
        }
        
        void BtnPause_Click(object sender, EventArgs e)
        {
            if (!botRunning) { MessageBox.Show("請先啟動"); return; }
            botPaused = !botPaused;
            lblStatus.Text = botPaused ? "狀態: 已暫停" : "狀態: 運行中";
            Log(botPaused ? "|| 已暫停" : "▶ 已繼續");
        }
        
        void Log(string msg)
        {
            if (txtLog.InvokeRequired) txtLog.Invoke(new Action(() => { 
                txtLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + msg + "\r\n"); 
                txtLog.SelectionStart = txtLog.Text.Length; 
                txtLog.ScrollToCaret(); 
            }));
            else { 
                txtLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + msg + "\r\n"); 
                txtLog.SelectionStart = txtLog.Text.Length; 
                txtLog.ScrollToCaret(); 
            }
        }
    }
}
