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
        [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")] static extern bool SetCursorPos(int X, int Y);
        [DllImport("user32.dll")] static extern void mouse_event(uint dwFlags, int dx, int dy, int dwData, UIntPtr dwExtraInfo);
        
        [StructLayout(LayoutKind.Sequential)]
        struct RECT { public int Left, Top, Right, Bottom; }
        
        const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        const uint MOUSEEVENTF_LEFTUP = 0x0004;
        
        IntPtr gameWindowHandle;
        int winLeft, winTop, winWidth, winHeight;
        bool isAttached, botRunning = false, botPaused = false;
        
        TextBox txtLog;
        Label lblStatus;
        Button btnStart, btnStop, btnPause, btnDetect;
        Thread botThread;
        Random rand = new Random();
        
        public MainForm()
        {
            this.Text = "天堂輔助 v2.1";
            this.Size = new System.Drawing.Size(500, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            
            lblStatus = new Label { Text = "請點擊檢測視窗", Left = 15, Top = 15, Width = 460, ForeColor = System.Drawing.Color.Red, Font = new System.Drawing.Font("", 10) };
            
            btnDetect = new Button { Text = "1.檢測視窗", Left = 15, Top = 45, Width = 120, Height = 35 };
            btnDetect.Click += BtnDetect_Click;
            
            btnStart = new Button { Text = "2.啟動", Left = 145, Top = 45, Width = 80, Height = 35 };
            btnStart.Click += BtnStart_Click;
            
            btnStop = new Button { Text = "停止", Left = 235, Top = 45, Width = 70, Height = 35 };
            btnStop.Click += BtnStop_Click;
            
            btnPause = new Button { Text = "暫停", Left = 315, Top = 45, Width = 70, Height = 35 };
            btnPause.Click += BtnPause_Click;
            
            var lblGuide = new Label { 
                Text = "重要:\n1.用管理員身份執行輔助程式\n2.先打開天堂遊戲(登入後)\n3.點擊[檢測視窗]\n4.點擊遊戲讓它變前景\n5.點擊[測試]確認", 
                Left = 15, Top = 90, Width = 460, Height = 70,
                ForeColor = System.Drawing.Color.Gray
            };
            
            var grp = new GroupBox { Text = "測試點擊", Left = 15, Top = 165, Width = 460, Height = 70 };
            var btn1 = new Button { Text = "測試1", Left = 10, Top = 25, Width = 100 };
            btn1.Click += (s,e) => { TestClick(200, 300); };
            var btn2 = new Button { Text = "測試2", Left = 120, Top = 25, Width = 100 };
            btn2.Click += (s,e) => { TestClick(400, 200); };
            var btn3 = new Button { Text = "測試3", Left = 230, Top = 25, Width = 100 };
            btn3.Click += (s,e) => { TestClick(400, 400); };
            var btn4 = new Button { Text = "測試4", Left = 340, Top = 25, Width = 100 };
            btn4.Click += (s,e) => { TestClick(600, 300); };
            grp.Controls.AddRange(new Control[] { btn1, btn2, btn3, btn4 });
            
            txtLog = new TextBox { Left = 15, Top = 245, Width = 460, Height = 210, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };
            txtLog.Font = new System.Drawing.Font("Consolas", 9);
            
            this.Controls.AddRange(new Control[] { lblStatus, btnDetect, btnStart, btnStop, btnPause, lblGuide, grp, txtLog });
            
            Log("=== 天堂輔助 v2.1 ===");
            Log("用管理員身份執行!");
        }
        
        void TestClick(int x, int y)
        {
            if (gameWindowHandle == IntPtr.Zero) 
            {
                // 嘗試用當前景視窗
                gameWindowHandle = GetForegroundWindow();
                if (gameWindowHandle != IntPtr.Zero)
                {
                    RECT rect;
                    GetWindowRect(gameWindowHandle, out rect);
                    winLeft = rect.Left;
                    winTop = rect.Top;
                    winWidth = rect.Right - rect.Left;
                    winHeight = rect.Bottom - rect.Top;
                    Log("使用當前視窗: " + winWidth + "x" + winHeight);
                }
            }
            
            if (gameWindowHandle == IntPtr.Zero)
            {
                Log("請先檢測視窗!");
                return;
            }
            
            // 激活視窗
            SetForegroundWindow(gameWindowHandle);
            Thread.Sleep(300);
            
            // 移動滑鼠
            int screenX = winLeft + x;
            int screenY = winTop + y;
            SetCursorPos(screenX, screenY);
            Thread.Sleep(100);
            
            // 點擊
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
            
            Log("測試點擊: (" + x + "," + y + ")");
        }
        
        void BtnDetect_Click(object sender, EventArgs e)
        {
            Log("=== 檢測視窗 ===");
            
            // 先用前景視窗
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd != IntPtr.Zero)
            {
                RECT rect;
                GetWindowRect(hwnd, out rect);
                winLeft = rect.Left;
                winTop = rect.Top;
                winWidth = rect.Right - rect.Left;
                winHeight = rect.Bottom - rect.Top;
                
                // 檢查大小是否像遊戲
                if (winWidth >= 640 && winHeight >= 480)
                {
                    gameWindowHandle = hwnd;
                    lblStatus.Text = "找到視窗: " + winWidth + "x" + winHeight;
                    lblStatus.ForeColor = System.Drawing.Color.Green;
                    Log("✓ 使用當前視窗: " + winWidth + "x" + winHeight);
                    isAttached = true;
                    return;
                }
            }
            
            // 嘗試用程序名稱
            string[] names = { "Purple", "Lineage", "LineageClassic", "LineageW" };
            foreach (string name in names)
            {
                try {
                    Process[] ps = Process.GetProcessesByName(name);
                    if (ps.Length > 0 && ps[0].MainWindowHandle != IntPtr.Zero)
                    {
                        gameWindowHandle = ps[0].MainWindowHandle;
                        RECT rect;
                        GetWindowRect(gameWindowHandle, out rect);
                        winLeft = rect.Left;
                        winTop = rect.Top;
                        winWidth = rect.Right - rect.Left;
                        winHeight = rect.Bottom - rect.Top;
                        
                        lblStatus.Text = "找到: " + name;
                        lblStatus.ForeColor = System.Drawing.Color.Green;
                        Log("✓ 找到程序: " + name + " (" + winWidth + "x" + winHeight + ")");
                        isAttached = true;
                        return;
                    }
                } catch {}
            }
            
            Log("✗ 請手動:");
            Log("  1.點擊天堂遊戲視窗");
            Log("  2.再點測試按鈕");
            lblStatus.Text = "請點擊遊戲後測試";
        }
        
        void BtnStart_Click(object sender, EventArgs e)
        {
            if (!isAttached) 
            {
                // 嘗試用當前視窗
                gameWindowHandle = GetForegroundWindow();
                if (gameWindowHandle != IntPtr.Zero)
                {
                    RECT rect;
                    GetWindowRect(gameWindowHandle, out rect);
                    winLeft = rect.Left;
                    winTop = rect.Top;
                    winWidth = rect.Right - rect.Left;
                    winHeight = rect.Bottom - rect.Top;
                    isAttached = true;
                    Log("使用當前視窗");
                }
            }
            
            if (!isAttached) { MessageBox.Show("請先檢測"); return; }
            if (botRunning) return;
            
            botRunning = true;
            lblStatus.Text = "運行中";
            lblStatus.ForeColor = System.Drawing.Color.Green;
            btnStart.Enabled = false;
            
            botThread = new Thread(BotLoop);
            botThread.IsBackground = true;
            botThread.Start();
            
            Log(">>> 啟動 <<<");
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
                    
                    // 確保前台
                    SetForegroundWindow(gameWindowHandle);
                    Thread.Sleep(200);
                    
                    // 隨機點擊
                    int x = rand.Next(100, winWidth - 100);
                    int y = rand.Next(100, winHeight - 100);
                    
                    this.Invoke(new Action(() => {
                        int screenX = winLeft + x;
                        int screenY = winTop + y;
                        SetCursorPos(screenX, screenY);
                        Thread.Sleep(50);
                        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                        Thread.Sleep(100);
                        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                    }));
                    
                    Thread.Sleep(rand.Next(2000, 4000));
                    
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
