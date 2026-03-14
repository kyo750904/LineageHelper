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
        [DllImport("user32.dll")] static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        
        [StructLayout(LayoutKind.Sequential)]
        struct RECT { public int Left, Top, Right, Bottom; }
        
        const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        const uint MOUSEEVENTF_LEFTUP = 0x0004;
        const uint KEYEVENTF_KEYDOWN = 0x0000, KEYEVENTF_KEYUP = 0x0002;
        
        IntPtr gameWindow;
        int winLeft, winTop, winWidth, winHeight;
        int startX = 400, startY = 300;
        bool botRunning = false, botPaused = false;
        
        TextBox txtLog, txtX, txtY, txtRange;
        Label lblStatus, lblCoords;
        Button btnDetect, btnStart, btnStop, btnPause;
        Thread botThread;
        Random rand = new Random();
        
        public MainForm()
        {
            this.Text = "天堂輔助 v2.3 - 自動偵測";
            this.Size = new System.Drawing.Size(500, 520);
            this.StartPosition = FormStartPosition.CenterScreen;
            
            lblStatus = new Label { Text = "狀態: 請點擊偵測", Left = 15, Top = 15, Width = 460, Font = new System.Drawing.Font("", 10) };
            
            // 偵測按鈕
            btnDetect = new Button { Text = "🔍 自動偵測視窗", Left = 15, Top = 45, Width = 150, Height = 35, Font = new System.Drawing.Font("", 10) };
            btnDetect.Click += BtnDetect_Click;
            
            // 座標顯示
            lblCoords = new Label { Text = "視窗座標: 未偵測", Left = 175, Top = 50, Width = 290, ForeColor = System.Drawing.Color.Blue, Font = new System.Drawing.Font("", 9) };
            
            // 起始位置
            var grpPos = new GroupBox { Text = "起始位置(客戶區座標)", Left = 15, Top = 90, Width = 220, Height = 70 };
            var lblX = new Label { Text = "X:", Left = 10, Top = 25 };
            txtX = new TextBox { Left = 30, Top = 22, Width = 60, Text = "400" };
            var lblY = new Label { Text = "Y:", Left = 100, Top = 25 };
            txtY = new TextBox { Left = 120, Top = 22, Width = 60, Text = "300" };
            var btnSet = new Button { Text = "設定", Left = 185, Top = 20, Width = 25, Height = 25 };
            btnSet.Click += (s,e) => { 
                try { startX = int.Parse(txtX.Text); startY = int.Parse(txtY.Text); Log("設定: (" + startX + "," + startY + ")"); } catch {} 
            };
            grpPos.Controls.AddRange(new Control[] { lblX, txtX, lblY, txtY, btnSet });
            
            // 範圍
            var grpRange = new GroupBox { Text = "移動範圍", Left = 245, Top = 90, Width = 220, Height = 70 };
            var lblR = new Label { Text = "範圍:", Left = 10, Top = 25 };
            txtRange = new TextBox { Left = 60, Top = 22, Width = 60, Text = "80" };
            var lblR2 = new Label { Text = "像素", Left = 125, Top = 25 };
            grpRange.Controls.AddRange(new Control[] { lblR, txtRange, lblR2 });
            
            // 控制
            btnStart = new Button { Text = "▶ 啟動", Left = 15, Top = 170, Width = 140, Height = 40, Font = new System.Drawing.Font("", 12), Enabled = false };
            btnStart.Click += BtnStart_Click;
            btnPause = new Button { Text = "⏸", Left = 165, Top = 170, Width = 50, Height = 40 };
            btnPause.Click += (s,e) => { if(!botRunning) return; botPaused = !botPaused; lblStatus.Text = botPaused ? "已暫停" : "運行中..."; Log(botPaused?"||":"▶"); };
            btnStop = new Button { Text = "⏹", Left = 225, Top = 170, Width = 50, Height = 40 };
            btnStop.Click += (s,e) => { botRunning = false; lblStatus.Text = "已停止"; btnStart.Enabled = true; Log("停止"); };
            
            // 移動測試
            var grpMove = new GroupBox { Text = "移動測試(確保遊戲在前景)", Left = 15, Top = 220, Width = 460, Height = 70 };
            var btn1 = new Button { Text = "←", Left = 10, Top = 25, Width = 50 };
            btn1.Click += (s,e) => { MoveClick(startX - 80, startY); };
            var btn2 = new Button { Text = "↑", Left = 70, Top = 25, Width = 50 };
            btn2.Click += (s,e) => { MoveClick(startX, startY - 80); };
            var btn3 = new Button { Text = "↓", Left = 130, Top = 25, Width = 50 };
            btn3.Click += (s,e) => { MoveClick(startX, startY + 80); };
            var btn4 = new Button { Text = "→", Left = 190, Top = 25, Width = 50 };
            btn4.Click += (s,e) => { MoveClick(startX + 80, startY); };
            var btn5 = new Button { Text = "原地", Left = 250, Top = 25, Width = 80 };
            btn5.Click += (s,e) => { MoveClick(startX, startY); };
            var btn6 = new Button { Text = "隨機", Left = 340, Top = 25, Width = 80 };
            btn6.Click += (s,e) => { 
                int range = 80; try { range = int.Parse(txtRange.Text); } catch {}
                MoveClick(startX + rand.Next(-range, range), startY + rand.Next(-range, range)); 
            };
            grpMove.Controls.AddRange(new Control[] { btn1, btn2, btn3, btn4, btn5, btn6 });
            
            // 鍵盤
            var grpKey = new GroupBox { Text = "鍵盤", Left = 15, Top = 300, Width = 460, Height = 60 };
            var btnA = new Button { Text = "A", Left = 10, Top = 25, Width = 50 };
            btnA.Click += (s,e) => { PressKey(0x41); };
            var btnS = new Button { Text = "S", Left = 70, Top = 25, Width = 50 };
            btnS.Click += (s,e) => { PressKey(0x53); };
            var btnW = new Button { Text = "W", Left = 130, Top = 25, Width = 50 };
            btnW.Click += (s,e) => { PressKey(0x57); };
            var btnF1 = new Button { Text = "F1", Left = 190, Top = 25, Width = 50 };
            btnF1.Click += (s,e) => { PressKey(0x70); };
            var btnF2 = new Button { Text = "F2", Left = 250, Top = 25, Width = 50 };
            btnF2.Click += (s,e) => { PressKey(0x71); };
            grpKey.Controls.AddRange(new Control[] { btnA, btnS, btnW, btnF1, btnF2 });
            
            txtLog = new TextBox { Left = 15, Top = 370, Width = 460, Height = 120, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };
            txtLog.Font = new System.Drawing.Font("Consolas", 9);
            
            this.Controls.AddRange(new Control[] { lblStatus, btnDetect, lblCoords, grpPos, grpRange, btnStart, btnPause, btnStop, grpMove, grpKey, txtLog });
            
            Log("=== 天堂輔助 v2.3 ===");
            Log("1.打開天堂遊戲");
            Log("2.點[自動偵測視窗]");
            Log("3.設定起始位置");
            Log("4.測試移動");
        }
        
        void BtnDetect_Click(object sender, EventArgs e)
        {
            Log("=== 偵測視窗 ===");
            
            // 用程序名稱找
            string[] names = { "Purple", "Lineage", "LineageClassic" };
            foreach (string name in names)
            {
                try {
                    Process[] ps = Process.GetProcessesByName(name);
                    if (ps.Length > 0 && ps[0].MainWindowHandle != IntPtr.Zero)
                    {
                        gameWindow = ps[0].MainWindowHandle;
                        
                        RECT rect;
                        GetWindowRect(gameWindow, out rect);
                        winLeft = rect.Left;
                        winTop = rect.Top;
                        winWidth = rect.Right - rect.Left;
                        winHeight = rect.Bottom - rect.Top;
                        
                        lblCoords.Text = $"視窗: {winLeft},{winTop} 大小:{winWidth}x{winHeight}";
                        lblStatus.Text = "找到: " + name;
                        lblStatus.ForeColor = System.Drawing.Color.Green;
                        btnStart.Enabled = true;
                        
                        // 自動設定起始位置為中央
                        startX = winWidth / 2;
                        startY = winHeight / 2;
                        txtX.Text = startX.ToString();
                        txtY.Text = startY.ToString();
                        
                        Log($"✓ 找到: {name}");
                        Log($"  螢幕位置: ({winLeft}, {winLeft})");
                        Log($"  視窗大小: {winWidth} x {winHeight}");
                        Log($"  建議起始: ({startX}, {startY})");
                        return;
                    }
                } catch {}
            }
            
            // 用標題找
            string[] titles = { "lineage Classic", "Lineage", "天堂" };
            foreach (string title in titles)
            {
                gameWindow = FindWindow(null, title);
                if (gameWindow != IntPtr.Zero)
                {
                    RECT rect;
                    GetWindowRect(gameWindow, out rect);
                    winWidth = rect.Right - rect.Left;
                    winHeight = rect.Bottom - rect.Top;
                    
                    lblCoords.Text = $"視窗: {rect.Left},{rect.Top} 大小:{winWidth}x{winHeight}";
                    lblStatus.Text = "找到: " + title;
                    lblStatus.ForeColor = System.Drawing.Color.Green;
                    btnStart.Enabled = true;
                    
                    startX = winWidth / 2;
                    startY = winHeight / 2;
                    txtX.Text = startX.ToString();
                    txtY.Text = startY.ToString();
                    
                    Log($"✓ 找到: {title}");
                    return;
                }
            }
            
            // 用當前視窗
            gameWindow = GetForegroundWindow();
            if (gameWindow != IntPtr.Zero)
            {
                RECT rect;
                GetWindowRect(gameWindow, out rect);
                winWidth = rect.Right - rect.Left;
                winHeight = rect.Bottom - rect.Top;
                
                lblCoords.Text = $"當前: {rect.Left},{rect.Top} 大小:{winWidth}x{winHeight}";
                lblStatus.Text = "使用當前視窗";
                lblStatus.ForeColor = System.Drawing.Color.Blue;
                btnStart.Enabled = true;
                
                startX = winWidth / 2;
                startY = winHeight / 2;
                txtX.Text = startX.ToString();
                txtY.Text = startY.ToString();
                
                Log("使用當前視窗");
                return;
            }
            
            Log("✗ 未找到，請點擊天堂遊戲後再試");
            lblStatus.Text = "請手動選擇";
        }
        
        void MoveClick(int x, int y)
        {
            if (gameWindow == IntPtr.Zero)
            {
                gameWindow = GetForegroundWindow();
                if (gameWindow != IntPtr.Zero)
                {
                    RECT rect;
                    GetWindowRect(gameWindow, out rect);
                    winLeft = rect.Left;
                    winTop = rect.Top;
                    winWidth = rect.Right - rect.Left;
                    winHeight = rect.Bottom - rect.Top;
                }
            }
            
            if (gameWindow == IntPtr.Zero)
            {
                Log("請先偵測視窗!");
                return;
            }
            
            // 激活
            SetForegroundWindow(gameWindow);
            Thread.Sleep(200);
            
            // 螢幕座標
            int screenX = winLeft + x;
            int screenY = winTop + y;
            
            SetCursorPos(screenX, screenY);
            Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
            
            Log($"移動: ({x}, {y})");
        }
        
        void PressKey(byte vk)
        {
            if (gameWindow != IntPtr.Zero)
            {
                SetForegroundWindow(gameWindow);
                Thread.Sleep(100);
            }
            keybd_event(vk, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            Thread.Sleep(50);
            keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
        
        void BtnStart_Click(object sender, EventArgs e)
        {
            if (gameWindow == IntPtr.Zero)
            {
                gameWindow = GetForegroundWindow();
            }
            
            if (gameWindow == IntPtr.Zero)
            {
                MessageBox.Show("請先偵測視窗");
                return;
            }
            
            try { startX = int.Parse(txtX.Text); startY = int.Parse(txtY.Text); } catch {}
            
            botRunning = true;
            botPaused = false;
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
                    
                    // 保持前台
                    SetForegroundWindow(gameWindow);
                    Thread.Sleep(100);
                    
                    // 計算位置
                    int range = 80;
                    try { range = int.Parse(txtRange.Text); } catch {}
                    
                    int newX = startX + rand.Next(-range, range);
                    int newY = startY + rand.Next(-range, range);
                    
                    // 移動
                    int screenX = winLeft + newX;
                    int screenY = winTop + newY;
                    
                    SetCursorPos(screenX, screenY);
                    Thread.Sleep(50);
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                    Thread.Sleep(100);
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                    
                    this.Invoke(new Action(() => Log($"移動: ({newX},{newY})")));
                    
                    Thread.Sleep(rand.Next(2000, 4000));
                    
                    // 偶爾攻擊
                    if (cycle % 5 == 0)
                    {
                        this.Invoke(new Action(() => {
                            PressKey(0x41);
                            Log("⚔");
                        }));
                    }
                }
                catch (Exception ex) { this.Invoke(new Action(() => Log("錯誤: " + ex.Message))); }
            }
            
            this.Invoke(new Action(() => { btnStart.Enabled = true; Log("停止"); }));
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
