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
        
        IntPtr gameWindow;
        int winLeft, winTop, winWidth, winHeight;
        int startX, startY;
        bool botRunning = false, botPaused = false;
        
        TextBox txtLog, txtX, txtY, txtRange, txtDelay;
        Label lblStatus, lblCoords;
        Button btnDetect, btnStart, btnStop, btnPause;
        Thread botThread;
        Random rand = new Random();
        
        public MainForm()
        {
            this.Text = "天堂輔助 v2.4 - 純滑鼠移動";
            this.Size = new System.Drawing.Size(520, 520);
            this.StartPosition = FormStartPosition.CenterScreen;
            
            // 偵測
            btnDetect = new Button { Text = "🔍 自動偵測", Left = 15, Top = 15, Width = 130, Height = 35 };
            btnDetect.Click += BtnDetect_Click;
            
            lblCoords = new Label { Text = "視窗: 未偵測", Left = 155, Top = 20, Width = 340, ForeColor = System.Drawing.Color.Blue };
            
            // 起始位置
            var grpPos = new GroupBox { Text = "起始位置(滑鼠點擊位置)", Left = 15, Top = 60, Width = 240, Height = 70 };
            var lblX = new Label { Text = "X:", Left = 10, Top = 25 };
            txtX = new TextBox { Left = 30, Top = 22, Width = 60, Text = "400" };
            var lblY = new Label { Text = "Y:", Left = 100, Top = 25 };
            txtY = new TextBox { Left = 120, Top = 22, Width = 60, Text = "300" };
            var btnSet = new Button { Text = "設", Left = 185, Top = 20, Width = 40 };
            btnSet.Click += (s,e) => { SetStartPos(); };
            grpPos.Controls.AddRange(new Control[] { lblX, txtX, txtY, lblY, btnSet });
            
            // 範圍和延遲
            var grpSet = new GroupBox { Text = "設定", Left = 265, Top = 60, Width = 220, Height = 70 };
            var lblR = new Label { Text = "範圍:", Left = 10, Top = 25 };
            txtRange = new TextBox { Left = 55, Top = 22, Width = 50, Text = "80" };
            var lblD = new Label { Text = "延遲:", Left = 110, Top = 25 };
            txtDelay = new TextBox { Left = 155, Top = 22, Width = 50, Text = "2000" };
            grpSet.Controls.AddRange(new Control[] { lblR, txtRange, lblD, txtDelay });
            
            // 控制
            lblStatus = new Label { Text = "狀態: 待命", Left = 15, Top = 140, Width = 470, Font = new System.Drawing.Font("", 10) };
            
            btnStart = new Button { Text = "▶ 啟動", Left = 15, Top = 165, Width = 150, Height = 40, Font = new System.Drawing.Font("", 12) };
            btnStart.Click += BtnStart_Click;
            
            btnPause = new Button { Text = "⏸ 暫停", Left = 175, Top = 165, Width = 80, Height = 40 };
            btnPause.Click += (s,e) => { if(botRunning){ botPaused = !botPaused; lblStatus.Text = botPaused?"已暫停":"運行中"; } };
            
            btnStop = new Button { Text = "⏹ 停止", Left = 265, Top = 165, Width = 80, Height = 40 };
            btnStop.Click += (s,e) => { botRunning = false; lblStatus.Text = "已停止"; btnStart.Enabled = true; Log("停止"); };
            
            // 測試按鈕
            var grpTest = new GroupBox { Text = "測試移動(確保遊戲在前景)", Left = 15, Top = 215, Width = 470, Height = 70 };
            var btn1 = new Button { Text = "← 左", Left = 10, Top = 25, Width = 70 };
            btn1.Click += (s,e) => { int r=80; try{r=int.Parse(txtRange.Text);}catch{} MoveClick(startX-r, startY); };
            var btn2 = new Button { Text = "↑ 上", Left = 90, Top = 25, Width = 70 };
            btn2.Click += (s,e) => { int r=80; try{r=int.Parse(txtRange.Text);}catch{} MoveClick(startX, startY-r); };
            var btn3 = new Button { Text = "↓ 下", Left = 170, Top = 25, Width = 70 };
            btn3.Click += (s,e) => { int r=80; try{r=int.Parse(txtRange.Text);}catch{} MoveClick(startX, startY+r); };
            var btn4 = new Button { Text = "→ 右", Left = 250, Top = 25, Width = 70 };
            btn4.Click += (s,e) => { int r=80; try{r=int.Parse(txtRange.Text);}catch{} MoveClick(startX+r, startY); };
            var btn5 = new Button { Text = "原地", Left = 330, Top = 25, Width = 70 };
            btn5.Click += (s,e) => { MoveClick(startX, startY); };
            var btn6 = new Button { Text = "隨機", Left = 390, Top = 25, Width = 60 };
            btn6.Click += (s,e) => { int r=80; try{r=int.Parse(txtRange.Text);}catch{} MoveClick(startX+rand.Next(-r,r), startY+rand.Next(-r,r)); };
            grpTest.Controls.AddRange(new Control[] { btn1, btn2, btn3, btn4, btn5, btn6 });
            
            // 說明
            var lblHelp = new Label { 
                Text = "說明: 這是純滑鼠移動輔助程式。\n移動方式: 在遊戲內點擊地面進行移動。", 
                Left = 15, Top = 290, Width = 470, Height = 40, ForeColor = System.Drawing.Color.Gray 
            };
            
            txtLog = new TextBox { Left = 15, Top = 335, Width = 470, Height = 150, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };
            txtLog.Font = new System.Drawing.Font("Consolas", 9);
            
            this.Controls.AddRange(new Control[] { btnDetect, lblCoords, grpPos, grpSet, lblStatus, btnStart, btnPause, btnStop, grpTest, lblHelp, txtLog });
            
            Log("=== 天堂輔助 v2.4 ===");
            Log("純滑鼠移動版本");
            Log("1.偵測視窗 2.設定位置 3.測試 4.啟動");
        }
        
        void SetStartPos()
        {
            try { startX = int.Parse(txtX.Text); startY = int.Parse(txtY.Text); Log($"設定起始: ({startX}, {startY})"); } catch {}
        }
        
        void BtnDetect_Click(object sender, EventArgs e)
        {
            Log("=== 偵測 ===");
            
            // 用程序名稱
            string[] names = { "Purple", "Lineage", "LineageClassic", "LineageW" };
            foreach (string name in names)
            {
                try {
                    Process[] ps = Process.GetProcessesByName(name);
                    if (ps.Length > 0 && ps[0].MainWindowHandle != IntPtr.Zero)
                    {
                        gameWindow = ps[0].MainWindowHandle;
                        RECT rect; GetWindowRect(gameWindow, out rect);
                        winLeft = rect.Left; winTop = rect.Top;
                        winWidth = rect.Right - rect.Left;
                        winHeight = rect.Bottom - rect.Top;
                        
                        lblCoords.Text = $"視窗: {name} ({winLeft},{winTop}) {winWidth}x{winHeight}";
                        lblStatus.Text = "已偵測";
                        
                        startX = winWidth / 2;
                        startY = winHeight / 2;
                        txtX.Text = startX.ToString();
                        txtY.Text = startY.ToString();
                        
                        Log($"✓ 找到: {name}");
                        Log($"  螢幕位置: ({winLeft}, {winTop})");
                        Log($"  大小: {winWidth} x {winHeight}");
                        Log($"  起始位置: ({startX}, {startY})");
                        return;
                    }
                } catch {}
            }
            
            // 用標題
            string[] titles = { "lineage Classic", "Lineage", "天堂" };
            foreach (string title in titles)
            {
                gameWindow = FindWindow(null, title);
                if (gameWindow != IntPtr.Zero)
                {
                    RECT rect; GetWindowRect(gameWindow, out rect);
                    winWidth = rect.Right - rect.Left;
                    winHeight = rect.Bottom - rect.Top;
                    
                    startX = winWidth / 2;
                    startY = winHeight / 2;
                    txtX.Text = startX.ToString();
                    txtY.Text = startY.ToString();
                    
                    lblCoords.Text = $"視窗: {title} {winWidth}x{winHeight}";
                    Log($"✓ 找到: {title}");
                    return;
                }
            }
            
            // 用當前視窗
            gameWindow = GetForegroundWindow();
            if (gameWindow != IntPtr.Zero)
            {
                RECT rect; GetWindowRect(gameWindow, out rect);
                winLeft = rect.Left; winTop = rect.Top;
                winWidth = rect.Right - rect.Left;
                winHeight = rect.Bottom - rect.Top;
                
                lblCoords.Text = $"當前: {winWidth}x{winHeight}";
                startX = winWidth / 2;
                startY = winHeight / 2;
                txtX.Text = startX.ToString();
                txtY.Text = startY.ToString();
                
                Log("使用當前視窗");
                return;
            }
            
            Log("✗ 未找到");
        }
        
        void MoveClick(int x, int y)
        {
            if (gameWindow == IntPtr.Zero)
            {
                gameWindow = GetForegroundWindow();
                if (gameWindow != IntPtr.Zero)
                {
                    RECT rect; GetWindowRect(gameWindow, out rect);
                    winLeft = rect.Left; winTop = rect.Top;
                }
            }
            
            if (gameWindow == IntPtr.Zero)
            {
                Log("請先偵測!");
                return;
            }
            
            // 激活
            SetForegroundWindow(gameWindow);
            Thread.Sleep(200);
            
            // 計算螢幕座標
            int screenX = winLeft + x;
            int screenY = winTop + y;
            
            SetCursorPos(screenX, screenY);
            Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
            
            Log($"點擊: ({x}, {y}) -> 螢幕: ({screenX}, {screenY})");
        }
        
        void BtnStart_Click(object sender, EventArgs e)
        {
            SetStartPos();
            
            if (gameWindow == IntPtr.Zero)
            {
                gameWindow = GetForegroundWindow();
            }
            
            if (gameWindow == IntPtr.Zero)
            {
                MessageBox.Show("請先偵測視窗");
                return;
            }
            
            botRunning = true;
            botPaused = false;
            btnStart.Enabled = false;
            lblStatus.Text = "運行中";
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
                    int range = 80, delay = 2000;
                    try { range = int.Parse(txtRange.Text); } catch {}
                    try { delay = int.Parse(txtDelay.Text); } catch {}
                    
                    int newX = startX + rand.Next(-range, range);
                    int newY = startY + rand.Next(-range, range);
                    
                    // 點擊
                    int screenX = winLeft + newX;
                    int screenY = winTop + newY;
                    
                    SetCursorPos(screenX, screenY);
                    Thread.Sleep(30);
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                    Thread.Sleep(80);
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                    
                    this.Invoke(new Action(() => Log($"[{cycle}] 移動: ({newX},{newY})")));
                    
                    Thread.Sleep(delay);
                    
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
