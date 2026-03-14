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
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        
        [StructLayout(LayoutKind.Sequential)]
        struct RECT { public int Left, Top, Right, Bottom; }
        
        const int WM_LBUTTONDOWN = 0x0201;
        const int WM_LBUTTONUP = 0x0202;
        const uint KEYEVENTF_KEYDOWN = 0x0000, KEYEVENTF_KEYUP = 0x0002;
        
        IntPtr gameWindowHandle;
        int winWidth = 800, winHeight = 600;
        bool isAttached, botRunning = false, botPaused = false;
        
        TextBox txtProcess, txtLog, txtX1, txtY1, txtX2, txtY2;
        Label lblStatus;
        Button btnAttach, btnStart, btnStop, btnPause, btnAutoDetect;
        Thread botThread;
        Random rand = new Random();
        
        public MainForm()
        {
            this.Text = "天堂輔助程式 v1.7";
            this.Size = new System.Drawing.Size(480, 550);
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
            
            var grpRange = new GroupBox { Text = "點擊範圍(視窗內座標)", Left = 15, Top = 110, Width = 440, Height = 70 };
            var lblX = new Label { Text = "X:", Left = 10, Top = 25 };
            txtX1 = new TextBox { Left = 25, Top = 22, Width = 50, Text = "200" };
            var lblX2 = new Label { Text = "~", Left = 80, Top = 25 };
            txtX2 = new TextBox { Left = 95, Top = 22, Width = 50, Text = "600" };
            var lblY = new Label { Text = "Y:", Left = 160, Top = 25 };
            txtY1 = new TextBox { Left = 175, Top = 22, Width = 50, Text = "200" };
            var lblY2 = new Label { Text = "~", Left = 230, Top = 25 };
            txtY2 = new TextBox { Left = 245, Top = 22, Width = 50, Text = "400" };
            grpRange.Controls.AddRange(new Control[] { lblX, txtX1, lblX2, txtX2, lblY, txtY1, lblY2, txtY2 });
            
            var grp = new GroupBox { Text = "手動測試", Left = 15, Top = 190, Width = 440, Height = 80 };
            var btnTest1 = new Button { Text = "點擊隨機位置", Left = 10, Top = 30, Width = 100 };
            btnTest1.Click += (s,e) => { TestClick(); };
            var btnTest2 = new Button { Text = "攻擊(A)", Left = 120, Top = 30, Width = 80 };
            btnTest2.Click += (s,e) => { PressKey(0x41); Log("已按A"); };
            var btnTest3 = new Button { Text = "F1", Left = 210, Top = 30, Width = 50 };
            btnTest3.Click += (s,e) => { PressKey(0x70); Log("已按F1"); };
            var btnTest4 = new Button { Text = "登入", Left = 270, Top = 30, Width = 80 };
            btnTest4.Click += (s,e) => { GameClick(400, 460); Log("點擊登入"); };
            grp.Controls.AddRange(new Control[] { btnTest1, btnTest2, btnTest3, btnTest4 });
            
            var lblInfo = new Label { Text = "說明: 1.開遊戲 2.自動檢測 3.點擊遊戲激活 4.啟動", Left = 15, Top = 280, Width = 440, ForeColor = System.Drawing.Color.Gray };
            
            var lblLog = new Label { Text = "日誌:", Left = 15, Top = 300 };
            txtLog = new TextBox { Left = 15, Top = 320, Width = 440, Height = 180, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };
            txtLog.Font = new System.Drawing.Font("Consolas", 9);
            
            this.Controls.AddRange(new Control[] { lbl1, txtProcess, btnAutoDetect, lblStatus, btnStart, btnStop, btnPause, grpRange, grp, lblInfo, lblLog, txtLog });
            
            Log("=== 天堂輔助程式 v1.7 ===");
            Log("視窗標題: lineage Classic");
            Log("請點擊自動檢測");
        }
        
        void TestClick()
        {
            int x1 = int.Parse(txtX1.Text);
            int x2 = int.Parse(txtX2.Text);
            int y1 = int.Parse(txtY1.Text);
            int y2 = int.Parse(txtY2.Text);
            
            int x = rand.Next(x1, x2);
            int y = rand.Next(y1, y2);
            
            GameClick(x, y);
            Log("測試點擊: (" + x + ", " + y + ")");
        }
        
        void PressKey(byte vk)
        {
            keybd_event(vk, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            Thread.Sleep(50);
            keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
        
        void BtnAutoDetect_Click(object sender, EventArgs e)
        {
            Log("正在搜尋遊戲視窗...");
            
            // 帥帥的標題
            string[] titles = { 
                "lineage Classic", 
                "lineage Classic -",
                "Lineage",
                "天堂"
            };
            
            foreach (string title in titles)
            {
                gameWindowHandle = FindWindow(null, title);
                if (gameWindowHandle != IntPtr.Zero)
                {
                    txtProcess.Text = title;
                    RECT rect;
                    GetWindowRect(gameWindowHandle, out rect);
                    winWidth = rect.Right - rect.Left;
                    winHeight = rect.Bottom - rect.Top;
                    
                    Log("✓ 找到視窗: " + title);
                    Log("  大小: " + winWidth + "x" + winHeight);
                    
                    isAttached = true;
                    lblStatus.Text = "狀態: 已找到 (" + winWidth + "x" + winHeight + ")";
                    lblStatus.ForeColor = System.Drawing.Color.Green;
                    
                    // 自動設定點擊範圍
                    txtX1.Text = (winWidth / 4).ToString();
                    txtX2.Text = (winWidth * 3 / 4).ToString();
                    txtY1.Text = (winHeight / 4).ToString();
                    txtY2.Text = (winHeight * 3 / 4).ToString();
                    
                    return;
                }
            }
            
            // 用程序名稱找
            string[] names = { "Purple", "Lineage", "LineageClassic" };
            foreach (string name in names)
            {
                try {
                    Process[] ps = Process.GetProcessesByName(name);
                    if (ps.Length > 0 && ps[0].MainWindowHandle != IntPtr.Zero)
                    {
                        gameWindowHandle = ps[0].MainWindowHandle;
                        txtProcess.Text = name;
                        RECT rect;
                        GetWindowRect(gameWindowHandle, out rect);
                        winWidth = rect.Right - rect.Left;
                        winHeight = rect.Bottom - rect.Top;
                        
                        Log("✓ 找到程序: " + name);
                        isAttached = true;
                        lblStatus.Text = "狀態: 已找到";
                        lblStatus.ForeColor = System.Drawing.Color.Green;
                        return;
                    }
                } catch {}
            }
            
            Log("✗ 未找到，請手動輸入標題");
        }
        
        void BtnStart_Click(object sender, EventArgs e)
        {
            if (!isAttached) { MessageBox.Show("請先檢測遊戲視窗"); return; }
            if (botRunning) return;
            
            botRunning = true; botPaused = false;
            lblStatus.Text = "狀態: 運行中";
            lblStatus.ForeColor = System.Drawing.Color.Green;
            Log(">>> 機器人啟動 <<<");
            Log("請確保遊戲視窗在前台!");
            
            botThread = new Thread(BotLoop);
            botThread.IsBackground = true;
            botThread.Start();
        }
        
        void GameClick(int x, int y)
        {
            if (gameWindowHandle == IntPtr.Zero) return;
            
            SetForegroundWindow(gameWindowHandle);
            Thread.Sleep(150);
            
            IntPtr lParam = (IntPtr)((y << 16) | (x & 0xFFFF));
            SendMessage(gameWindowHandle, WM_LBUTTONDOWN, IntPtr.Zero, lParam);
            Thread.Sleep(rand.Next(80, 200));
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
                    
                    // 保持遊戲在前台
                    IntPtr current = GetForegroundWindow();
                    if (current != gameWindowHandle)
                    {
                        SetForegroundWindow(gameWindowHandle);
                        Thread.Sleep(200);
                    }
                    
                    // 隨機延遲
                    int delay = rand.Next(800, 2000);
                    Thread.Sleep(delay);
                    
                    if (cycle % 2 == 0)
                    {
                        int x1 = int.Parse(txtX1.Text);
                        int x2 = int.Parse(txtX2.Text);
                        int y1 = int.Parse(txtY1.Text);
                        int y2 = int.Parse(txtY2.Text);
                        
                        int clickX = rand.Next(x1, x2);
                        int clickY = rand.Next(y1, y2);
                        
                        this.Invoke(new Action(() => {
                            GameClick(clickX, clickY);
                            Log("↗ 移動 (" + clickX + "," + clickY + ")");
                        }));
                    }
                    
                    if (cycle % 8 == 0)
                    {
                        this.Invoke(new Action(() => {
                            PressKey(0x41);
                            Log("⚔ 攻擊(A)");
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
