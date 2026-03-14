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
        [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")] static extern IntPtr PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] static extern bool SetCursorPos(int X, int Y);
        [DllImport("user32.dll")] static extern void mouse_event(uint dwFlags, int dx, int dy, int dwData, UIntPtr dwExtraInfo);
        [DllImport("user32.dll")] static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        
        [StructLayout(LayoutKind.Sequential)]
        struct RECT { public int Left, Top, Right, Bottom; }
        
        const int WM_LBUTTONDOWN = 0x0201;
        const int WM_LBUTTONUP = 0x0202;
        const int WM_MOUSEMOVE = 0x0200;
        const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        const uint MOUSEEVENTF_LEFTUP = 0x0004;
        const uint MOUSEEVENTF_MOVE = 0x0001;
        const uint KEYEVENTF_KEYDOWN = 0x0000, KEYEVENTF_KEYUP = 0x0002;
        
        IntPtr gameWindowHandle;
        int winWidth = 800, winHeight = 600;
        bool isAttached, botRunning = false, botPaused = false;
        
        TextBox txtProcess, txtLog, txtX1, txtY1, txtX2, txtY2;
        Label lblStatus;
        Button btnStart, btnStop, btnPause, btnAutoDetect, btnTestClick;
        Thread botThread;
        Random rand = new Random();
        
        public MainForm()
        {
            this.Text = "天堂輔助程式 v1.8";
            this.Size = new System.Drawing.Size(480, 560);
            this.StartPosition = FormStartPosition.CenterScreen;
            
            var lbl1 = new Label { Text = "遊戲視窗:", Left = 15, Top = 15 };
            txtProcess = new TextBox { Left = 85, Top = 15, Width = 200, Text = "lineage Classic" };
            btnAutoDetect = new Button { Text = "自動檢測", Left = 290, Top = 13, Width = 85 };
            btnAutoDetect.Click += BtnAutoDetect_Click;
            
            lblStatus = new Label { Text = "狀態: 未連接", Left = 15, Top = 45, Width = 440, ForeColor = System.Drawing.Color.Red };
            
            btnStart = new Button { Text = "啟動", Left = 15, Top = 75, Width = 80 };
            btnStart.Click += BtnStart_Click;
            btnStop = new Button { Text = "停止", Left = 105, Top = 75, Width = 80 };
            btnStop.Click += BtnStop_Click;
            btnPause = new Button { Text = "暫停", Left = 195, Top = 75, Width = 80 };
            btnPause.Click += BtnPause_Click;
            
            var grpRange = new GroupBox { Text = "點擊範圍(相對於遊戲視窗)", Left = 15, Top = 110, Width = 440, Height = 70 };
            var lblX = new Label { Text = "X:", Left = 10, Top = 25 };
            txtX1 = new TextBox { Left = 25, Top = 22, Width = 50, Text = "100" };
            var lblX2 = new Label { Text = "~", Left = 80, Top = 25 };
            txtX2 = new TextBox { Left = 95, Top = 22, Width = 50, Text = "700" };
            var lblY = new Label { Text = "Y:", Left = 160, Top = 25 };
            txtY1 = new TextBox { Left = 175, Top = 22, Width = 50, Text = "100" };
            var lblY2 = new Label { Text = "~", Left = 230, Top = 25 };
            txtY2 = new TextBox { Left = 245, Top = 22, Width = 50, Text = "500" };
            grpRange.Controls.AddRange(new Control[] { lblX, txtX1, lblX2, txtX2, lblY, txtY1, lblY2, txtY2 });
            
            var grp = new GroupBox { Text = "測試點擊(確保遊戲在前景)", Left = 15, Top = 190, Width = 440, Height = 80 };
            btnTestClick = new Button { Text = "測試點擊", Left = 10, Top = 30, Width = 100 };
            btnTestClick.Click += (s,e) => { TestClick(); };
            var btnClick1 = new Button { Text = "左(100,300)", Left = 120, Top = 30, Width = 90 };
            btnClick1.Click += (s,e) => { GameClickClient(100, 300); Log("左"); };
            var btnClick2 = new Button { Text = "中(400,300)", Left = 220, Top = 30, Width = 90 };
            btnClick2.Click += (s,e) => { GameClickClient(400, 300); Log("中"); };
            var btnClick3 = new Button { Text = "右(700,300)", Left = 320, Top = 30, Width = 90 };
            btnClick3.Click += (s,e) => { GameClickClient(700, 300); Log("右"); };
            grp.Controls.AddRange(new Control[] { btnTestClick, btnClick1, btnClick2, btnClick3 });
            
            var grpKeys = new GroupBox { Text = "快捷鍵測試", Left = 15, Top = 280, Width = 440, Height = 60 };
            var btnKey1 = new Button { Text = "A(攻擊)", Left = 10, Top = 25, Width = 70 };
            btnKey1.Click += (s,e) => { PressKey(0x41); Log("A"); };
            var btnKey2 = new Button { Text = "S(撿物)", Left = 85, Top = 25, Width = 70 };
            btnKey2.Click += (s,e) => { PressKey(0x53); Log("S"); };
            var btnKey3 = new Button { Text = "W(前)", Left = 160, Top = 25, Width = 50 };
            btnKey3.Click += (s,e) => { PressKey(0x57); Log("W"); };
            var btnKey4 = new Button { Text = "F1", Left = 220, Top = 25, Width = 50 };
            btnKey4.Click += (s,e) => { PressKey(0x70); Log("F1"); };
            grpKeys.Controls.AddRange(new Control[] { btnKey1, btnKey2, btnKey3, btnKey4 });
            
            var lblLog = new Label { Text = "日誌:", Left = 15, Top = 350 };
            txtLog = new TextBox { Left = 15, Top = 370, Width = 440, Height = 140, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };
            txtLog.Font = new System.Drawing.Font("Consolas", 9);
            
            this.Controls.AddRange(new Control[] { lbl1, txtProcess, btnAutoDetect, lblStatus, btnStart, btnStop, btnPause, grpRange, grp, grpKeys, lblLog, txtLog });
            
            Log("=== 天堂輔助程式 v1.8 ===");
            Log("視窗: lineage Classic");
            Log("1. 開遊戲 2.自動檢測 3.點擊遊戲激活 4.啟動");
        }
        
        void TestClick()
        {
            // 測試三個位置
            GameClickClient(200, 300);
            Thread.Sleep(300);
            GameClickClient(400, 300);
            Thread.Sleep(300);
            GameClickClient(600, 300);
            Log("測試完成");
        }
        
        void PressKey(byte vk)
        {
            keybd_event(vk, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            Thread.Sleep(50);
            keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
        
        // 使用 Client 座標（相對於遊戲視窗左上角）
        void GameClickClient(int x, int y)
        {
            if (gameWindowHandle == IntPtr.Zero) return;
            
            // 激活遊戲視窗
            SetForegroundWindow(gameWindowHandle);
            Thread.Sleep(200);
            
            // 使用 PostMessage 發送點擊（client coordinates）
            IntPtr lParam = (IntPtr)((y << 16) | (x & 0xFFFF));
            PostMessage(gameWindowHandle, WM_LBUTTONDOWN, IntPtr.Zero, lParam);
            Thread.Sleep(rand.Next(50, 100));
            PostMessage(gameWindowHandle, WM_LBUTTONUP, IntPtr.Zero, lParam);
            
            Log("點擊: (" + x + "," + y + ")");
        }
        
        // 使用 Screen 座標（絕對位置）
        void GameClickScreen(int screenX, int screenY)
        {
            if (gameWindowHandle == IntPtr.Zero) return;
            
            // 先獲取視窗位置
            RECT winRect;
            GetWindowRect(gameWindowHandle, out winRect);
            
            // 計算相對於視窗的位置
            int clientX = screenX - winRect.Left;
            int clientY = screenY - winRect.Top;
            
            // 激活視窗
            SetForegroundWindow(gameWindowHandle);
            Thread.Sleep(200);
            
            // 使用 SendMessage
            IntPtr lParam = (IntPtr)((clientY << 16) | (clientX & 0xFFFF));
            SendMessage(gameWindowHandle, WM_LBUTTONDOWN, IntPtr.Zero, lParam);
            Thread.Sleep(rand.Next(50, 100));
            SendMessage(gameWindowHandle, WM_LBUTTONUP, IntPtr.Zero, lParam);
            
            Log("螢幕點擊: (" + screenX + "," + screenY + ") -> Client: (" + clientX + "," + clientY + ")");
        }
        
        void BtnAutoDetect_Click(object sender, EventArgs e)
        {
            Log("正在搜尋遊戲視窗...");
            
            string[] titles = { "lineage Classic", "lineage Classic -", "Lineage", "天堂" };
            
            foreach (string title in titles)
            {
                gameWindowHandle = FindWindow(null, title);
                if (gameWindowHandle != IntPtr.Zero)
                {
                    txtProcess.Text = title;
                    
                    RECT winRect, clientRect;
                    GetWindowRect(gameWindowHandle, out winRect);
                    GetClientRect(gameWindowHandle, out clientRect);
                    
                    winWidth = winRect.Right - winRect.Left;
                    winHeight = winRect.Bottom - winRect.Top;
                    
                    Log("✓ 找到: " + title);
                    Log("  視窗: " + winWidth + "x" + winHeight);
                    Log("  Client: " + clientRect.Right + "x" + clientRect.Bottom);
                    
                    // 自動設定範圍
                    txtX1.Text = (clientRect.Right / 4).ToString();
                    txtX2.Text = (clientRect.Right * 3 / 4).ToString();
                    txtY1.Text = (clientRect.Bottom / 4).ToString();
                    txtY2.Text = (clientRect.Bottom * 3 / 4).ToString();
                    
                    isAttached = true;
                    lblStatus.Text = "狀態: 已連接";
                    lblStatus.ForeColor = System.Drawing.Color.Green;
                    return;
                }
            }
            
            Log("✗ 未找到，請手動輸入");
        }
        
        void BtnStart_Click(object sender, EventArgs e)
        {
            if (!isAttached) { MessageBox.Show("請先檢測遊戲視窗"); return; }
            if (botRunning) return;
            
            botRunning = true; botPaused = false;
            lblStatus.Text = "狀態: 運行中";
            lblStatus.ForeColor = System.Drawing.Color.Green;
            Log(">>> 啟動 <<<");
            Log("請確保遊戲在前台!");
            
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
                    
                    // 隨機延遲
                    Thread.Sleep(rand.Next(1000, 2500));
                    
                    if (cycle % 2 == 0)
                    {
                        int x1 = int.Parse(txtX1.Text);
                        int x2 = int.Parse(txtX2.Text);
                        int y1 = int.Parse(txtY1.Text);
                        int y2 = int.Parse(txtY2.Text);
                        
                        int clickX = rand.Next(x1, x2);
                        int clickY = rand.Next(y1, y2);
                        
                        this.Invoke(new Action(() => {
                            GameClickClient(clickX, clickY);
                            Log("↗ 移動 (" + clickX + "," + clickY + ")");
                        }));
                    }
                    
                    if (cycle % 6 == 0)
                    {
                        this.Invoke(new Action(() => {
                            PressKey(0x41);  // A
                            Log("⚔ 攻擊(A)");
                        }));
                    }
                    
                }
                catch (Exception ex) { Log("錯誤: " + ex.Message); }
            }
            Log("停止");
        }
        
        void BtnStop_Click(object sender, EventArgs e)
        {
            botRunning = false;
            lblStatus.Text = "狀態: 已停止";
            lblStatus.ForeColor = System.Drawing.Color.Red;
            Log(">>> 停止 <<<");
        }
        
        void BtnPause_Click(object sender, EventArgs e)
        {
            if (!botRunning) { MessageBox.Show("請先啟動"); return; }
            botPaused = !botPaused;
            lblStatus.Text = botPaused ? "狀態: 已暫停" : "狀態: 運行中";
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
