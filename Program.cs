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
        // API - 就像原始碼
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetForegroundWindow(IntPtr hWnd);
        
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetForegroundWindow();
        
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        
        // 視窗訊息
        const int WM_LBUTTONDOWN = 0x0201;
        const int WM_LBUTTONUP = 0x0202;
        const int WM_MOUSEMOVE = 0x0200;
        
        IntPtr hwnd;
        
        TextBox txtLog;
        Label lbl;
        Button btnDetect, btnStart;
        
        public MainForm()
        {
            Text = "天堂輔助 v3.1 - SendMessage";
            Size = new System.Drawing.Size(400, 400);
            StartPosition = FormStartPosition.CenterScreen;
            
            lbl = new Label { Text = "狀態: 等待偵測", Left = 15, Top = 15, Width = 360 };
            
            btnDetect = new Button { Text = "1. 偵測視窗", Left = 15, Top = 45, Width = 120, Height = 35 };
            btnDetect.Click += (s,e) => Detect();
            
            // 測試按鈕
            var btn1 = new Button { Text = "測試點擊(400,300)", Left = 15, Top = 90, Width = 170, Height = 30 };
            btn1.Click += (s,e) => TestClick(400, 300);
            
            var btn2 = new Button { Text = "測試點擊(200,300)", Left = 195, Top = 90, Width = 170, Height = 30 };
            btn2.Click += (s,e) => TestClick(200, 300);
            
            var btn3 = new Button { Text = "測試點擊(600,300)", Left = 15, Top = 125, Width = 170, Height = 30 };
            btn3.Click += (s,e) => TestClick(600, 300);
            
            var btn4 = new Button { Text = "測試點擊(400,200)", Left = 195, Top = 125, Width = 170, Height = 30 };
            btn4.Click += (s,e) => TestClick(400, 200);
            
            // 說明
            var info = new Label { 
                Text = "說明:\n1. 確保天堂遊戲正在運行\n2. 點擊[偵測視窗]\n3. 點擊[測試點擊]按鈕\n4. 觀察遊戲內是否有反應\n5. 告訴我哪裡有反應", 
                Left = 15, Top = 170, Width = 360, Height = 80, ForeColor = System.Drawing.Color.Gray 
            };
            
            txtLog = new TextBox { Left = 15, Top = 260, Width = 360, Height = 100, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };
            txtLog.Font = new System.Drawing.Font("Consolas", 9);
            
            Controls.AddRange(new Control[]{lbl, btnDetect, btn1, btn2, btn3, btn4, info, txtLog});
            
            Log("=== 天堂輔助 v3.1 ===");
            Log("使用 SendMessage 方式");
        }
        
        void Detect()
        {
            Log("=== 偵測 ===");
            
            // 嘗試多種方式找視窗
            string[] names = {"Purple", "Lineage", "LineageClassic"};
            foreach(string n in names)
            {
                try{
                    var ps = Process.GetProcessesByName(n);
                    if(ps.Length > 0 && ps[0].MainWindowHandle != IntPtr.Zero)
                    {
                        hwnd = ps[0].MainWindowHandle;
                        lbl.Text = $"找到: {n}";
                        Log($"✓ 找到程序: {n}");
                        return;
                    }
                }catch{}
            }
            
            // 用標題
            string[] titles = {"lineage Classic", "Lineage", "天堂"};
            foreach(string t in titles)
            {
                hwnd = FindWindow(null, t);
                if(hwnd != IntPtr.Zero)
                {
                    lbl.Text = $"找到: {t}";
                    Log($"✓ 找到視窗: {t}");
                    return;
                }
            }
            
            // 用當前
            hwnd = GetForegroundWindow();
            if(hwnd != IntPtr.Zero)
            {
                lbl.Text = "使用當前視窗";
                Log("使用當前視窗");
                return;
            }
            
            Log("✗ 未找到");
        }
        
        void TestClick(int x, int y)
        {
            if(hwnd == IntPtr.Zero)
            {
                hwnd = GetForegroundWindow();
            }
            
            if(hwnd == IntPtr.Zero)
            {
                Log("請先偵測!");
                return;
            }
            
            // 激活視窗
            SetForegroundWindow(hwnd);
            Thread.Sleep(300);
            
            // 使用 SendMessage 發送滑鼠點擊
            // lParam = y * 65536 + x (低位是x，高位是y)
            IntPtr lParam = (IntPtr)(y * 65536 + x);
            
            Log($"測試點擊: ({x}, {y})");
            
            // 滑鼠移動
            SendMessage(hwnd, WM_MOUSEMOVE, IntPtr.Zero, lParam);
            Thread.Sleep(50);
            
            // 按下左鍵
            SendMessage(hwnd, WM_LBUTTONDOWN, (IntPtr)1, lParam);  // wParam = 1 表示左鍵
            Thread.Sleep(100);
            
            // 放開左鍵
            SendMessage(hwnd, WM_LBUTTONUP, IntPtr.Zero, lParam);
            
            Log("已發送點擊");
        }
        
        void Log(string m)
        {
            if(txtLog.InvokeRequired)
                txtLog.Invoke(new Action(() => {
                    txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {m}\r\n");
                    txtLog.SelectionStart = txtLog.Text.Length;
                    txtLog.ScrollToCaret();
                }));
            else
                txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {m}\r\n");
        }
    }
}
