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
        // API
        [DllImport("user32.dll")] static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")] static extern bool ScreenToClient(IntPtr hWnd, out POINT lpPoint);
        [DllImport("user32.dll")] static extern bool ClientToScreen(IntPtr hWnd, out POINT lpPoint);
        [DllImport("user32.dll")] static extern bool SetCursorPos(int X, int Y);
        [DllImport("user32.dll")] static extern void mouse_event(uint dwFlags, int dx, int dy, int dwData, UIntPtr dwExtraInfo);
        [DllImport("user32.dll")] static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        
        [StructLayout(LayoutKind.Sequential)]
        struct RECT { public int Left, Top, Right, Bottom; }
        
        [StructLayout(LayoutKind.Sequential)]
        struct POINT { public int X, Y; }
        
        const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        const uint MOUSEEVENTF_LEFTUP = 0x0004;
        const uint KEYEVENTF_KEYDOWN = 0x0000, KEYEVENTF_KEYUP = 0x0002;
        
        IntPtr hwnd;
        int cx, cy; // 客戶區大小
        
        bool running = false, paused = false;
        TextBox txtLog;
        Label lbl;
        Button btnStart, btnStop, btnTest;
        Thread th;
        Random rnd = new Random();
        
        public MainForm()
        {
            Text = "天堂輔助 v3.0";
            Size = new System.Drawing.Size(450, 480);
            StartPosition = FormStartPosition.CenterScreen;
            
            // 狀態
            lbl = new Label { Text = "請點擊[偵測]然後點擊天堂", Left = 15, Top = 15, Width = 400, Font = new System.Drawing.Font("", 10) };
            
            // 按鈕
            var btnDetect = new Button { Text = "🔍 偵測", Left = 15, Top = 45, Width = 100, Height = 35 };
            btnDetect.Click += (s,e) => Detect();
            
            btnTest = new Button { Text = "🖱️ 測試點擊", Left = 125, Top = 45, Width = 100, Height = 35, Enabled = false };
            btnTest.Click += (s,e) => TestClick();
            
            btnStart = new Button { Text = "▶ 啟動", Left = 235, Top = 45, Width = 90, Height = 35 };
            btnStart.Click += (s,e) => Start();
            
            btnStop = new Button { Text = "⏹ 停止", Left = 335, Top = 45, Width = 80, Height = 35 };
            btnStop.Click += (s,e) => { running = false; lbl.Text = "已停止"; btnStart.Enabled = true; };
            
            // 說明
            var info = new Label { 
                Text = "操作說明:\n• 移動: 滑鼠左鍵點擊地面\n• 戰鬥: 按 A 鍵\n• 撿物: 按 S 鍵\n• 遊戲內用800x600視窗", 
                Left = 15, Top = 90, Width = 400, Height = 70, ForeColor = System.Drawing.Color.Gray 
            };
            
            // 測試區
            var g1 = new GroupBox { Text = "移動測試(確保遊戲在前)", Left = 15, Top = 170, Width = 400, Height = 60 };
            var b1 = new Button { Text = "←", Left = 10, Top = 25, Width = 50 };
            b1.Click += (s,e) => ClickAt(200, 300);
            var b2 = new Button { Text = "→", Left = 70, Top = 25, Width = 50 };
            b2.Click += (s,e) => ClickAt(600, 300);
            var b3 = new Button { Text = "上", Left = 130, Top = 25, Width = 50 };
            b3.Click += (s,e) => ClickAt(400, 150);
            var b4 = new Button { Text = "下", Left = 190, Top = 25, Width = 50 };
            b4.Click += (s,e) => ClickAt(400, 450);
            var b5 = new Button { Text = "原地", Left = 250, Top = 25, Width = 60 };
            b5.Click += (s,e) => ClickAt(400, 300);
            var b6 = new Button { Text = "隨機", Left = 320, Top = 25, Width = 60 };
            b6.Click += (s,e) => ClickAt(rnd.Next(100,700), rnd.Next(100,500));
            g1.Controls.AddRange(new Control[]{b1,b2,b3,b4,b5,b6});
            
            // 鍵盤
            var g2 = new GroupBox { Text = "鍵盤測試", Left = 15, Top = 240, Width = 400, Height = 60 };
            var k1 = new Button { Text = "A(戰)", Left = 10, Top = 25, Width = 50 };
            k1.Click += (s,e) => Key(0x41);
            var k2 = new Button { Text = "S(撿)", Left = 70, Top = 25, Width = 50 };
            k2.Click += (s,e) => Key(0x53);
            var k3 = new Button { Text = "W", Left = 130, Top = 25, Width = 50 };
            k3.Click += (s,e) => Key(0x57);
            var k4 = new Button { Text = "F1", Left = 190, Top = 25, Width = 50 };
            k4.Click += (s,e) => Key(0x70);
            g2.Controls.AddRange(new Control[]{k1,k2,k3,k4});
            
            // 日誌
            txtLog = new TextBox { Left = 15, Top = 310, Width = 400, Height = 130, Multiline=true, ScrollBars=ScrollBars.Vertical, ReadOnly=true };
            txtLog.Font = new System.Drawing.Font("Consolas", 9);
            
            Controls.AddRange(new Control[]{lbl, btnDetect, btnTest, btnStart, btnStop, info, g1, g2, txtLog});
            
            Log("=== 天堂輔助 v3.0 ===");
        }
        
        void Detect()
        {
            Log("=== 偵測 ===");
            
            // 用標題找
            string[] titles = {"lineage Classic", "Lineage", "天堂"};
            foreach(string t in titles)
            {
                hwnd = FindWindow(null, t);
                if(hwnd != IntPtr.Zero) goto found;
            }
            
            // 用程序名
            string[] names = {"Purple", "Lineage", "LineageClassic"};
            foreach(string n in names)
            {
                try{
                    var ps = Process.GetProcessesByName(n);
                    if(ps.Length > 0 && ps[0].MainWindowHandle != IntPtr.Zero)
                    {
                        hwnd = ps[0].MainWindowHandle;
                        goto found;
                    }
                }catch{}
            }
            
            // 用當前
            hwnd = GetForegroundWindow();
            
            found:
            if(hwnd != IntPtr.Zero)
            {
                RECT r;
                GetClientRect(hwnd, out r);
                cx = r.Right;
                cy = r.Bottom;
                
                lbl.Text = $"客戶區: {cx}x{cy}";
                btnTest.Enabled = true;
                Log($"✓ 視窗大小: {cx}x{cy}");
            }
            else
            {
                Log("✗ 未找到");
            }
        }
        
        void TestClick()
        {
            // 測試3個位置
            ClickAt(200, 300);
            Thread.Sleep(500);
            ClickAt(400, 300);
            Thread.Sleep(500);
            ClickAt(600, 300);
            Log("測試完成，請告訴我哪裡有反應");
        }
        
        void ClickAt(int x, int y)
        {
            if(hwnd == IntPtr.Zero)
            {
                hwnd = GetForegroundWindow();
                if(hwnd != IntPtr.Zero)
                {
                    RECT r; GetClientRect(hwnd, out r);
                    cx = r.Right; cy = r.Bottom;
                }
            }
            
            if(hwnd == IntPtr.Zero) { Log("請先偵測"); return; }
            
            // 激活
            SetForegroundWindow(hwnd);
            Thread.Sleep(200);
            
            // 轉換座標
            POINT p = new POINT { X = x, Y = y };
            ClientToScreen(hwnd, out p);
            
            SetCursorPos(p.X, p.Y);
            Thread.Sleep(50);
            
            // 按下
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            Thread.Sleep(100);
            // 放開
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
            
            Log($"點擊: ({x},{y})");
        }
        
        void Key(byte vk)
        {
            if(hwnd != IntPtr.Zero)
            {
                SetForegroundWindow(hwnd);
                Thread.Sleep(100);
            }
            
            keybd_event(vk, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            Thread.Sleep(50);
            keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            
            Log($"按鍵: {vk:X2}");
        }
        
        void Start()
        {
            if(hwnd == IntPtr.Zero) { MessageBox.Show("請先偵測"); return; }
            
            running = true;
            btnStart.Enabled = false;
            lbl.Text = "運行中...";
            Log(">>> 啟動 <<<");
            
            th = new Thread(Bot);
            th.IsBackground = true;
            th.Start();
        }
        
        void Bot()
        {
            int c = 0;
            while(running)
            {
                try{
                    if(paused) { Thread.Sleep(500); continue; }
                    
                    c++;
                    
                    SetForegroundWindow(hwnd);
                    Thread.Sleep(100);
                    
                    // 隨機位置
                    int x = rnd.Next(100, cx-100);
                    int y = rnd.Next(100, cy-100);
                    
                    // 點擊
                    POINT p = new POINT { X = x, Y = y };
                    ClientToScreen(hwnd, out p);
                    SetCursorPos(p.X, p.Y);
                    Thread.Sleep(30);
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                    Thread.Sleep(80);
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                    
                    this.Invoke(new Action(() => Log($"[{c}] 移動 ({x},{y})")));
                    
                    Thread.Sleep(rnd.Next(2000, 4000));
                    
                    // 攻擊
                    if(c % 5 == 0)
                    {
                        this.Invoke(new Action(() => {
                            Key(0x41);
                            Log("⚔ 戰鬥");
                        }));
                    }
                }
                catch(Exception ex) { this.Invoke(new Action(() => Log("Err:" + ex.Message))); }
            }
            
            this.Invoke(new Action(() => { btnStart.Enabled = true; Log("停止"); }));
        }
        
        void Log(string m)
        {
            if(txtLog.InvokeRequired) txtLog.Invoke(new Action(() => {
                txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {m}\r\n");
                txtLog.SelectionStart = txtLog.Text.Length;
                txtLog.ScrollToCaret();
            }));
        }
    }
}
