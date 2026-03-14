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
        // Windows API
        [DllImport("user32.dll")] static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")] static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] static extern void mouse_event(uint dwFlags, int dx, int dy, int dwData, UIntPtr dwExtraInfo);
        [DllImport("user32.dll")] static extern bool SetCursorPos(int X, int Y);
        
        [StructLayout(LayoutKind.Sequential)]
        struct RECT { public int Left, Top, Right, Bottom; }
        
        const int WM_LBUTTONDOWN = 0x0201;
        const int WM_LBUTTONUP = 0x0202;
        const int WM_MOUSEMOVE = 0x0200;
        const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        const uint MOUSEEVENTF_LEFTUP = 0x0004;
        
        IntPtr gameWindowHandle;
        bool isAttached, botRunning = false, botPaused = false;
        
        TextBox txtProcess, txtLog;
        Label lblStatus;
        Button btnAttach, btnStart, btnStop, btnPause, btnAutoDetect;
        Thread botThread;
        Random rand = new Random();
        
        public MainForm()
        {
            this.Text = "天堂輔助程式 v1.5";
            this.Size = new System.Drawing.Size(450, 480);
            this.StartPosition = FormStartPosition.CenterScreen;
            
            var lbl1 = new Label { Text = "遊戲視窗:", Left = 15, Top = 15 };
            txtProcess = new TextBox { Left = 85, Top = 15, Width = 130, Text = "" };
            txtProcess.PlaceholderText = "可留空自動搜尋";
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
            
            var lblInfo = new Label { Text = "說明: 先點擊遊戲視窗激活,再啟動", Left = 15, Top = 115, Width = 400, ForeColor = System.Drawing.Color.Gray };
            
            var grp = new GroupBox { Text = "手動測試(確保遊戲在前台)", Left = 15, Top = 145, Width = 400, Height = 100 };
            var btnTest1 = new Button { Text = "點擊中央", Left = 10, Top = 30, Width = 80 };
            btnTest1.Click += (s,e) => { GameClick(400, 300); Log("已點擊中央"); };
            var btnTest2 = new Button { Text = "點擊左上", Left = 100, Top = 30, Width = 80 };
            btnTest2.Click += (s,e) => { GameClick(200, 150); Log("已點擊左上"); };
            var btnTest3 = new Button { Text = "攻擊(Ctrl+A)", Left = 190, Top = 30, Width = 100 };
            btnTest3.Click += (s,e) => { SendKeys("a"); Log("已發送攻擊"); };
            var btnTest4 = new Button { Text = "撿物(Alt)", Left = 300, Top = 30, Width = 80 };
            btnTest4.Click += (s,e) => { Log("請手動按Alt"); };
            grp.Controls.AddRange(new Control[] { btnTest1, btnTest2, btnTest3, btnTest4 });
            
            var lblLog = new Label { Text = "日誌:", Left = 15, Top = 255 };
            txtLog = new TextBox { Left = 15, Top = 275, Width = 410, Height = 160, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };
            txtLog.Font = new System.Drawing.Font("Consolas", 9);
            
            this.Controls.AddRange(new Control[] { lbl1, txtProcess, btnAutoDetect, btnAttach, lblStatus, btnStart, btnStop, btnPause, lblInfo, grp, lblLog, txtLog });
            
            Log("=== 天堂輔助程式 v1.5 ===");
            Log("1. 開啟天堂遊戲");
            Log("2. 點擊「自動檢測」");
            Log("3. 點擊遊戲視窗激活(非常重要!)");
            Log("4. 點擊「啟動」");
        }
        
        void BtnAutoDetect_Click(object sender, EventArgs e)
        {
            Log("正在搜尋遊戲視窗...");
            
            // 嘗試多種方式找視窗
            string[] titles = { "天堂", "Lineage", "Purple", "天堂經典版" };
            
            foreach (string title in titles)
            {
                gameWindowHandle = FindWindow(null, title);
                if (gameWindowHandle != IntPtr.Zero)
                {
                    Log($"✓ 找到視窗: {title}");
                    RECT rect;
                    GetWindowRect(gameWindowHandle, out rect);
                    int width = rect.Right - rect.Left;
                    int height = rect.Bottom - rect.Top;
                    Log($"  視窗大小: {width}x{height}");
                    isAttached = true;
                    lblStatus.Text = "狀態: 已找到視窗";
                    lblStatus.ForeColor = System.Drawing.Color.Green;
                    return;
                }
            }
            
            // 嘗試用類名找
            string[] classes = { "LineageWindow", "GWndClass", "#32770" };
            foreach (string cls in classes)
            {
                gameWindowHandle = FindWindow(cls, null);
                if (gameWindowHandle != IntPtr.Zero)
                {
                    Log($"✓ 找到視窗(class): {cls}");
                    isAttached = true;
                    lblStatus.Text = "狀態: 已找到視窗";
                    lblStatus.ForeColor = System.Drawing.Color.Green;
                    return;
                }
            }
            
            Log("✗ 未找到遊戲視窗");
            MessageBox.Show("請手動輸入視窗標題，或確認遊戲已打開", "提示");
        }
        
        void BtnAttach_Click(object sender, EventArgs e)
        {
            string title = txtProcess.Text.Trim();
            if (title == "")
            {
                BtnAutoDetect_Click(sender, e);
                return;
            }
            
            gameWindowHandle = FindWindow(null, title);
            if (gameWindowHandle == IntPtr.Zero)
            {
                MessageBox.Show("找不到視窗: " + title);
                return;
            }
            
            isAttached = true;
            lblStatus.Text = "狀態: 已附加";
            lblStatus.ForeColor = System.Drawing.Color.Green;
            Log("已附加到: " + title);
        }
        
        void BtnStart_Click(object sender, EventArgs e)
        {
            if (!isAttached) { MessageBox.Show("請先檢測遊戲視窗"); return; }
            if (botRunning) return;
            
            botRunning = true; botPaused = false;
            lblStatus.Text = "狀態: 運行中";
            lblStatus.ForeColor = System.Drawing.Color.Green;
            Log(">>> 機器人啟動 <<<");
            Log("提示: 確保遊戲視窗在前台!");
            
            botThread = new Thread(BotLoop);
            botThread.IsBackground = true;
            botThread.Start();
        }
        
        void GameClick(int x, int y)
        {
            if (gameWindowHandle == IntPtr.Zero) return;
            
            // 先激活視窗
            SetForegroundWindow(gameWindowHandle);
            Thread.Sleep(100);
            
            // 發送點擊訊息
            IntPtr lParam = (IntPtr)((y << 16) | (x & 0xFFFF));
            SendMessage(gameWindowHandle, WM_LBUTTONDOWN, IntPtr.Zero, lParam);
            Thread.Sleep(rand.Next(50, 150));
            SendMessage(gameWindowHandle, WM_LBUTTONUP, IntPtr.Zero, lParam);
        }
        
        void BotLoop()
        {
            int cycle = 0;
            while (botRunning)
            {
                try
                {
                    if (botPaused) { Thread.Sleep(500); continue; }
                    
                    cycle++;
                    
                    // 確保遊戲視窗是前景
                    IntPtr current = GetForegroundWindow();
                    if (current != gameWindowHandle)
                    {
                        SetForegroundWindow(gameWindowHandle);
                        Thread.Sleep(200);
                    }
                    
                    // 隨機延遲
                    int delay = rand.Next(1000, 3000);
                    Thread.Sleep(delay);
                    
                    if (cycle % 3 == 0)
                    {
                        // 點擊移動 (在視窗中央區域隨機位置)
                        int clickX = rand.Next(200, 600);
                        int clickY = rand.Next(150, 450);
                        this.Invoke(new Action(() => {
                            GameClick(clickX, clickY);
                            Log($"↗ 移動點擊 ({clickX},{clickY})");
                        }));
                    }
                    
                    if (cycle % 10 == 0)
                    {
                        // 攻擊
                        this.Invoke(new Action(() => {
                            SendKeys("a");
                            Log("⚔ 攻擊");
                        }));
                    }
                    
                    if (cycle % 20 == 0)
                    {
                        // 撿物
                        this.Invoke(new Action(() => {
                            SendKeys("{F1}");
                            Thread.Sleep(200);
                            SendKeys("{F2}");
                            Log("📦 撿物");
                        }));
                    }
                    
                }
                catch (Exception ex) { Log("錯誤: " + ex.Message); }
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
