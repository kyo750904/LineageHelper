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
        
        int startX = 400, startY = 300; // 起始位置
        bool botRunning = false, botPaused = false;
        
        TextBox txtLog;
        Label lblStatus;
        Button btnStart, btnStop, btnPause;
        Thread botThread;
        Random rand = new Random();
        
        public MainForm()
        {
            this.Text = "天堂輔助 v2.2 - 按鍵精靈模式";
            this.Size = new System.Drawing.Size(500, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            
            lblStatus = new Label { Text = "狀態: 未啟動", Left = 15, Top = 15, Width = 460, Font = new System.Drawing.Font("", 12) };
            
            // 說明
            var lblGuide = new Label { 
                Text = "使用方法:\n1.打開天堂遊戲並登入\n2.移動角色到你想要的位置\n3.記錄這個位置作為起始點\n4.啟動後會在附近範圍內移動點擊", 
                Left = 15, Top = 45, Width = 460, Height = 60,
                ForeColor = System.Drawing.Color.Gray
            };
            
            // 位置設定
            var grpPos = new GroupBox { Text = "起始位置(相對於遊戲視窗)", Left = 15, Top = 110, Width = 220, Height = 70 };
            var lblX = new Label { Text = "X:", Left = 10, Top = 25 };
            var txtX = new TextBox { Left = 30, Top = 22, Width = 60, Text = "400" };
            var lblY = new Label { Text = "Y:", Left = 100, Top = 25 };
            var txtY = new TextBox { Left = 120, Top = 22, Width = 60, Text = "300" };
            var btnSetPos = new Button { Text = "設定", Left = 185, Top = 20, Width = 25, Height = 25 };
            btnSetPos.Click += (s,e) => { 
                try { 
                    startX = int.Parse(txtX.Text); 
                    startY = int.Parse(txtY.Text); 
                    Log("設定起始位置: (" + startX + "," + startY + ")"); 
                } catch {} 
            };
            grpPos.Controls.AddRange(new Control[] { lblX, txtX, lblY, txtY, btnSetPos });
            
            // 範圍設定
            var grpRange = new GroupBox { Text = "移動範圍", Left = 245, Top = 110, Width = 220, Height = 70 };
            var lblR = new Label { Text = "範圍:", Left = 10, Top = 25 };
            var txtRange = new TextBox { Left = 60, Top = 22, Width = 60, Text = "100" };
            var lblR2 = new Label { Text = "像素", Left = 125, Top = 25 };
            grpRange.Controls.AddRange(new Control[] { lblR, txtRange, lblR2 });
            
            // 控制按鈕
            btnStart = new Button { Text = "▶ 啟動", Left = 15, Top = 190, Width = 140, Height = 40, Font = new System.Drawing.Font("", 12) };
            btnStart.Click += BtnStart_Click;
            
            btnPause = new Button { Text = "⏸ 暫停", Left = 165, Top = 190, Width = 100, Height = 40 };
            btnPause.Click += BtnPause_Click;
            
            btnStop = new Button { Text = "⏹ 停止", Left = 275, Top = 190, Width = 100, Height = 40 };
            btnStop.Click += BtnStop_Click;
            
            // 快速移動測試
            var grpMove = new GroupBox { Text = "移動測試(點擊後角色會移動)", Left = 15, Top = 240, Width = 460, Height = 70 };
            var btn1 = new Button { Text = "←", Left = 10, Top = 25, Width = 50 };
            btn1.Click += (s,e) => { MoveTo(startX - 80, startY); };
            var btn2 = new Button { Text = "↑", Left = 70, Top = 25, Width = 50 };
            btn2.Click += (s,e) => { MoveTo(startX, startY - 80); };
            var btn3 = new Button { Text = "↓", Left = 130, Top = 25, Width = 50 };
            btn3.Click += (s,e) => { MoveTo(startX, startY + 80); };
            var btn4 = new Button { Text = "→", Left = 190, Top = 25, Width = 50 };
            btn4.Click += (s,e) => { MoveTo(startX + 80, startY); };
            var btn5 = new Button { Text = "原地", Left = 250, Top = 25, Width = 80 };
            btn5.Click += (s,e) => { MoveTo(startX, startY); };
            var btn6 = new Button { Text = "隨機", Left = 340, Top = 25, Width = 80 };
            btn6.Click += (s,e) => { 
                int range = 100;
                try { range = int.Parse(txtRange.Text); } catch {}
                MoveTo(startX + rand.Next(-range, range), startY + rand.Next(-range, range)); 
            };
            grpMove.Controls.AddRange(new Control[] { btn1, btn2, btn3, btn4, btn5, btn6 });
            
            // 鍵盤測試
            var grpKey = new GroupBox { Text = "鍵盤測試", Left = 15, Top = 320, Width = 460, Height = 60 };
            var btnA = new Button { Text = "A(攻擊)", Left = 10, Top = 25, Width = 70 };
            btnA.Click += (s,e) => { PressKey(0x41); Log("A鍵"); };
            var btnS = new Button { Text = "S(撿物)", Left = 90, Top = 25, Width = 70 };
            btnS.Click += (s,e) => { PressKey(0x53); Log("S鍵"); };
            var btnW = new Button { Text = "W", Left = 170, Top = 25, Width = 50 };
            btnW.Click += (s,e) => { PressKey(0x57); Log("W鍵"); };
            var btnF1 = new Button { Text = "F1", Left = 230, Top = 25, Width = 50 };
            btnF1.Click += (s,e) => { PressKey(0x70); Log("F1"); };
            grpKey.Controls.AddRange(new Control[] { btnA, btnS, btnW, btnF1 });
            
            txtLog = new TextBox { Left = 15, Top = 390, Width = 460, Height = 70, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };
            txtLog.Font = new System.Drawing.Font("Consolas", 9);
            
            this.Controls.AddRange(new Control[] { lblStatus, lblGuide, grpPos, grpRange, btnStart, btnPause, btnStop, grpMove, grpKey, txtLog });
            
            Log("=== 天堂輔助 v2.2 ===");
            Log("按鍵精靈模式");
        }
        
        void MoveTo(int x, int y)
        {
            // 獲取當前視窗
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) 
            {
                Log("請先點擊天堂遊戲視窗!");
                return;
            }
            
            // 獲取視窗位置
            RECT rect;
            GetWindowRect(hwnd, out rect);
            int winLeft = rect.Left;
            int winTop = rect.Top;
            
            // 計算螢幕座標
            int screenX = winLeft + x;
            int screenY = winTop + y;
            
            // 激活視窗
            SetForegroundWindow(hwnd);
            Thread.Sleep(200);
            
            // 移動到位置
            SetCursorPos(screenX, screenY);
            Thread.Sleep(100);
            
            // 點擊
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
            
            Log("移動: (" + x + "," + y + ")");
        }
        
        void PressKey(byte vk)
        {
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd != IntPtr.Zero)
            {
                SetForegroundWindow(hwnd);
                Thread.Sleep(100);
            }
            
            keybd_event(vk, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            Thread.Sleep(50);
            keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
        
        void BtnStart_Click(object sender, EventArgs e)
        {
            if (botRunning) return;
            
            // 先確認視窗
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
            {
                MessageBox.Show("請先點擊天堂遊戲視窗!");
                return;
            }
            
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
                    if (botPaused)
                    {
                        Thread.Sleep(500);
                        continue;
                    }
                    
                    cycle++;
                    
                    // 確保遊戲在前台
                    IntPtr hwnd = GetForegroundWindow();
                    if (hwnd != IntPtr.Zero)
                    {
                        SetForegroundWindow(hwnd);
                        Thread.Sleep(100);
                    }
                    
                    // 計算移動位置
                    int range = 100;
                    try { range = int.Parse("100"); } catch {}
                    
                    int newX = startX + rand.Next(-range, range);
                    int newY = startY + rand.Next(-range, range);
                    
                    // 移動
                    RECT rect;
                    GetWindowRect(hwnd, out rect);
                    int winLeft = rect.Left;
                    int winTop = rect.Top;
                    
                    int screenX = winLeft + newX;
                    int screenY = winTop + newY;
                    
                    SetCursorPos(screenX, screenY);
                    Thread.Sleep(50);
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                    Thread.Sleep(100);
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                    
                    this.Invoke(new Action(() => Log("移動: (" + newX + "," + newY + ")")));
                    
                    // 等待
                    Thread.Sleep(rand.Next(2000, 4000));
                    
                    // 偶爾攻擊
                    if (cycle % 5 == 0)
                    {
                        this.Invoke(new Action(() => {
                            PressKey(0x41);
                            Log("⚔ 攻擊");
                        }));
                    }
                    
                }
                catch (Exception ex) 
                { 
                    this.Invoke(new Action(() => Log("錯誤: " + ex.Message))); 
                }
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
            lblStatus.ForeColor = System.Drawing.Color.Black;
            btnStart.Enabled = true;
            Log(">>> 停止 <<<");
        }
        
        void BtnPause_Click(object sender, EventArgs e)
        {
            if (!botRunning) return;
            botPaused = !botPaused;
            lblStatus.Text = botPaused ? "已暫停" : "運行中...";
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
