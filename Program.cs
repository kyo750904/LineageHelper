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
        [DllImport("user32.dll")] static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")] static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")] static extern bool SetCursorPos(int X, int Y);
        [DllImport("user32.dll")] static extern void mouse_event(uint dwFlags, int dx, int dy, int dwData, UIntPtr dwExtraInfo);
        [DllImport("user32.dll")] static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        
        [StructLayout(LayoutKind.Sequential)]
        struct RECT { public int Left, Top, Right, Bottom; }
        
        const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        const uint MOUSEEVENTF_LEFTUP = 0x0004;
        const uint KEYEVENTF_KEYDOWN = 0x0000, KEYEVENTF_KEYUP = 0x0002;
        
        IntPtr gameWindowHandle;
        int winLeft, winTop, winWidth, winHeight;
        bool isAttached, botRunning = false, botPaused = false;
        
        TextBox txtLog;
        Label lblStatus;
        Button btnStart, btnStop, btnPause, btnDetect, btnTestClick;
        Thread botThread;
        Random rand = new Random();
        
        public MainForm()
        {
            this.Text = "天堂輔助程式 v2.0";
            this.Size = new System.Drawing.Size(500, 520);
            this.StartPosition = FormStartPosition.CenterScreen;
            
            var lblInfo = new Label { Text = "狀態:", Left = 15, Top = 15 };
            lblStatus = new Label { Text = "請點擊檢測視窗", Left = 60, Top = 15, Width = 400, ForeColor = System.Drawing.Color.Red, Font = new System.Drawing.Font("", 10) };
            
            btnDetect = new Button { Text = "1.檢測視窗", Left = 15, Top = 45, Width = 100, Height = 35 };
            btnDetect.Click += BtnDetect_Click;
            
            btnTestClick = new Button { Text = "2.測試點擊", Left = 125, Top = 45, Width = 100, Height = 35, Enabled = false };
            btnTestClick.Click += BtnTestClick_Click;
            
            btnStart = new Button { Text = "3.啟動", Left = 235, Top = 45, Width = 80, Height = 35, Enabled = false };
            btnStart.Click += BtnStart_Click;
            
            btnStop = new Button { Text = "停止", Left = 325, Top = 45, Width = 70, Height = 35 };
            btnStop.Click += BtnStop_Click;
            
            btnPause = new Button { Text = "暫停", Left = 400, Top = 45, Width = 70, Height = 35 };
            btnPause.Click += BtnPause_Click;
            
            var lblGuide = new Label { 
                Text = "使用說明:\n1.打開天堂遊戲(登入後)\n2.點擊[檢測視窗]\n3.點擊遊戲讓它變前景\n4.點擊[測試點擊]確認有反應\n5.點擊[啟動]開始掛機", 
                Left = 15, Top = 95, Width = 460, Height = 80,
                ForeColor = System.Drawing.Color.Gray
            };
            
            var grp = new GroupBox { Text = "快速測試點", Left = 15, Top = 180, Width = 460, Height = 80 };
            var btn1 = new Button { Text = "中央(400,300)", Left = 10, Top = 30, Width = 100 };
            btn1.Click += (s,e) => { MouseClick(400, 300); };
            var btn2 = new Button { Text = "上(400,150)", Left = 120, Top = 30, Width = 100 };
            btn2.Click += (s,e) => { MouseClick(400, 150); };
            var btn3 = new Button { Text = "下(400,450)", Left = 230, Top = 30, Width = 100 };
            btn3.Click += (s,e) => { MouseClick(400, 450); };
            var btn4 = new Button { Text = "左(200,300)", Left = 340, Top = 30, Width = 100 };
            btn4.Click += (s,e) => { MouseClick(200, 300); };
            grp.Controls.AddRange(new Control[] { btn1, btn2, btn3, btn4 });
            
            var grp2 = new GroupBox { Text = "角落測試", Left = 15, Top = 270, Width = 220, Height = 60 };
            var btn5 = new Button { Text = "左上", Left = 10, Top = 25, Width = 60 };
            btn5.Click += (s,e) => { MouseClick(50, 50); };
            var btn6 = new Button { Text = "右上", Left = 75, Top = 25, Width = 60 };
            btn6.Click += (s,e) => { MouseClick(750, 50); };
            var btn7 = new Button { Text = "左下", Left = 140, Top = 25, Width = 60 };
            btn7.Click += (s,e) => { MouseClick(50, 550); };
            grp2.Controls.AddRange(new Control[] { btn5, btn6, btn7 });
            
            txtLog = new TextBox { Left = 15, Top = 340, Width = 460, Height = 140, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };
            txtLog.Font = new System.Drawing.Font("Consolas", 9);
            
            this.Controls.AddRange(new Control[] { lblInfo, lblStatus, btnDetect, btnTestClick, btnStart, btnStop, btnPause, lblGuide, grp, grp2, txtLog });
            
            Log("=== 天堂輔助 v2.0 ===");
            Log("請確保用管理員身份執行");
        }
        
        void MouseClick(int x, int y)
        {
            if (gameWindowHandle == IntPtr.Zero) 
            {
                Log("請先檢測視窗!");
                return;
            }
            
            // 激活遊戲視窗
            SetForegroundWindow(gameWindowHandle);
            Thread.Sleep(300);
            
            // 計算螢幕座標
            int screenX = winLeft + x;
            int screenY = winTop + y;
            
            // 移動滑鼠
            SetCursorPos(screenX, screenY);
            Thread.Sleep(100);
            
            // 點擊
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            Thread.Sleep(rand.Next(50, 100));
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
            
            Log("點擊: (" + x + "," + y + ") -> 螢幕: (" + screenX + "," + screenY + ")");
        }
        
        void BtnDetect_Click(object sender, EventArgs e)
        {
            Log("=== 正在檢測視窗 ===");
            
            // 嘗試多種方式找視窗
            string[] titles = { 
                "lineage Classic",
                "lineage Classic -",
                "Lineage",
                "天堂經典版",
                "天堂"
            };
            
            // 先列出所有視窗
            Log("執行中的視窗:");
            Process[] procs = Process.GetProcesses();
            foreach (Process p in procs)
            {
                try {
                    string title = p.MainWindowTitle;
                    if (!string.IsNullOrEmpty(title) && title.Length > 2)
                    {
                        if (title.Contains("lineage") || title.Contains("Lineage") || title.Contains("天堂") || title.Contains("Purple"))
                        {
                            Log("  ★ " + title + " (" + p.ProcessName + ")");
                        }
                    }
                } catch {}
            }
            
            // 嘗試找視窗
            foreach (string title in titles)
            {
                gameWindowHandle = FindWindow(null, title);
                if (gameWindowHandle != IntPtr.Zero)
                {
                    RECT rect;
                    GetWindowRect(gameWindowHandle, out rect);
                    winLeft = rect.Left;
                    winTop = rect.Top;
                    winWidth = rect.Right - rect.Left;
                    winHeight = rect.Bottom - rect.Top;
                    
                    lblStatus.Text = "找到: " + title;
                    lblStatus.ForeColor = System.Drawing.Color.Green;
                    Log("✓ 找到視窗: " + title);
                    Log("  位置: " + winLeft + "," + winTop);
                    Log("  大小: " + winWidth + "x" + winHeight);
                    
                    btnTestClick.Enabled = true;
                    isAttached = true;
                    return;
                }
            }
            
            // 嘗試用類名找
            string[] classes = { "LineageWindow", "GWndClass" };
            foreach (string cls in classes)
            {
                gameWindowHandle = FindWindow(cls, null);
                if (gameWindowHandle != IntPtr.Zero)
                {
                    RECT rect;
                    GetWindowRect(gameWindowHandle, out rect);
                    winWidth = rect.Right - rect.Left;
                    winHeight = rect.Bottom - rect.Top;
                    
                    lblStatus.Text = "找到視窗(class)";
                    lblStatus.ForeColor = System.Drawing.Color.Green;
                    Log("✓ 找到: " + cls);
                    btnTestClick.Enabled = true;
                    isAttached = true;
                    return;
                }
            }
            
            Log("✗ 請手動點擊遊戲視窗讓它變前景，然後再試");
            lblStatus.Text = "請點擊遊戲";
            MessageBox.Show("請確認:\n1.天堂遊戲已打開\n2.用管理員身份執行\n3.點擊遊戲視窗讓它變前景", "提示");
        }
        
        void BtnTestClick_Click(object sender, EventArgs e)
        {
            if (!isAttached) return;
            
            Log("=== 測試點擊 ===");
            Log("現在點擊 [中央] 測試");
            MouseClick(400, 300);
            
            Thread.Sleep(500);
            Log("現在點擊 [上]");
            MouseClick(400, 150);
            
            Thread.Sleep(500);
            Log("現在點擊 [下]");
            MouseClick(400, 450);
            
            Log("請告訴我哪個位置有反應!");
        }
        
        void BtnStart_Click(object sender, EventArgs e)
        {
            if (!isAttached) { MessageBox.Show("請先檢測"); return; }
            if (botRunning) return;
            
            botRunning = true; botPaused = false;
            lblStatus.Text = "運行中...";
            lblStatus.ForeColor = System.Drawing.Color.Green;
            btnStart.Enabled = false;
            
            Log(">>> 啟動 <<<");
            
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
                    if (botPaused) { Thread.Sleep(500); continue; }
                    
                    cycle++;
                    
                    // 確保遊戲在前台
                    IntPtr current = GetForegroundWindow();
                    if (current != gameWindowHandle)
                    {
                        SetForegroundWindow(gameWindowHandle);
                        Thread.Sleep(300);
                    }
                    
                    // 移動點擊
                    int clickX = rand.Next(100, winWidth - 100);
                    int clickY = rand.Next(100, winHeight - 100);
                    
                    this.Invoke(new Action(() => MouseClick(clickX, clickY)));
                    
                    // 每次間隔
                    Thread.Sleep(rand.Next(1500, 3000));
                    
                    if (cycle % 10 == 0)
                    {
                        // 偶爾點擊兩下（可能是確認）
                        this.Invoke(new Action(() => {
                            MouseClick(400, 300);
                        }));
                    }
                    
                }
                catch (Exception ex) { Log("錯誤: " + ex.Message); }
            }
            this.Invoke(new Action(() => {
                btnStart.Enabled = true;
                Log("停止");
            }));
        }
        
        void BtnStop_Click(object sender, EventArgs e)
        {
            botRunning = false;
            lblStatus.Text = "已停止";
            lblStatus.ForeColor = System.Drawing.Color.Red;
            btnStart.Enabled = true;
            Log(">>> 停止 <<<");
        }
        
        void BtnPause_Click(object sender, EventArgs e)
        {
            if (!botRunning) return;
            botPaused = !botPaused;
            lblStatus.Text = botPaused ? "已暫停" : "運行中";
            Log(botPaused ? "|| 暫停" : "▶ 繼續");
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
