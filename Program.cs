using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;

namespace LineageBot
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        // ==================== Windows API ====================
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        
        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        
        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);
        
        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, int dx, int dy, int dwData, UIntPtr dwExtraInfo);
        
        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        
        [DllImport("gdi32.dll")]
        static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        
        [DllImport("gdi32.dll")]
        static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);
        
        [DllImport("gdi32.dll")]
        static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);
        
        [DllImport("gdi32.dll")]
        static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);
        
        [DllImport("gdi32.dll")]
        static extern bool DeleteDC(IntPtr hdc);
        
        [DllImport("gdi32.dll")]
        static extern bool DeleteObject(IntPtr hObject);
        
        [StructLayout(LayoutKind.Sequential)]
        struct RECT { public int Left, Top, Right, Bottom; }
        
        const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        const uint MOUSEEVENTF_LEFTUP = 0x0004;
        const uint SRCCOPY = 0x00CC0020;
        const uint KEYEVENTF_KEYDOWN = 0x0000;
        const uint KEYEVENTF_KEYUP = 0x0002;
        
        // ==================== 變數 ====================
        IntPtr gameWindow;
        int winLeft, winTop, winWidth, winHeight;
        bool isRunning = false;
        
        // 控制項
        PictureBox picScreen;
        TextBox txtLog;
        Button btnCapture, btnDetect, btnStart, btnStop;
        Label lblHP, lblMP, lblStatus;
        
        // 機器人執行緒
        Thread botThread;
        Random rand = new Random();
        
        public MainForm()
        {
            Text = "天堂機器人 v4.0 - 螢幕截圖版";
            Size = new System.Drawing.Size(900, 700);
            StartPosition = FormStartPosition.CenterScreen;
            
            // 狀態列
            lblStatus = new Label { Text = "狀態: 待命", Left = 15, Top = 15, Width = 200 };
            lblHP = new Label { Text = "HP: -", Left = 230, Top = 15, Width = 100, ForeColor = Color.Red };
            lblMP = new Label { Text = "MP: -", Left = 350, Top = 15, Width = 100, ForeColor = Color.Blue };
            
            // 遊戲畫面顯示
            picScreen = new PictureBox 
            { 
                Left = 15, Top = 45, 
                Width = 640, Height = 480,
                SizeMode = PictureBoxSizeMode.StretchImage,
                BorderStyle = BorderStyle.FixedSingle
            };
            
            // 按鈕
            btnCapture = new Button { Text = "截圖", Left = 15, Top = 540, Width = 100, Height = 35 };
            btnCapture.Click += (s,e) => CaptureScreen();
            
            btnDetect = new Button { Text = "OCR辨識", Left = 125, Top = 540, Width = 100, Height = 35 };
            btnDetect.Click += (s,e) => DetectGameData();
            
            btnStart = new Button { Text = "▶ 啟動", Left = 235, Top = 540, Width = 100, Height = 35 };
            btnStart.Click += (s,e) => StartBot();
            
            btnStop = new Button { Text = "⏹ 停止", Left = 345, Top = 540, Width = 100, Height = 35 };
            btnStop.Click += (s,e) => StopBot();
            
            // 設定區
            var grp = new GroupBox { Text = "設定", Left = 670, Top = 45, Width = 200, Height = 150 };
            
            var lbl1 = new Label { Text = "移動間隔(ms):", Left = 10, Top = 25 };
            var txtDelay = new TextBox { Left = 10, Top = 45, Width = 80, Text = "3000" };
            
            var lbl2 = new Label { Text = "攻擊間隔(次):", Left = 10, Top = 70 };
            var txtAttackDelay = new TextBox { Left = 10, Top = 90, Width = 80, Text = "5" };
            
            var chkHeal = new CheckBox { Text = "自動補血", Left = 10, Top = 115, Checked = true };
            
            grp.Controls.AddRange(new Control[] { lbl1, txtDelay, lbl2, txtAttackDelay, chkHeal });
            
            // 日誌
            var lblLog = new Label { Text = "日誌:", Left = 670, Top = 210 };
            txtLog = new TextBox { Left = 670, Top = 230, Width = 200, Height = 350, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };
            txtLog.Font = new Font("Consolas", 8);
            
            Controls.AddRange(new Control[] { 
                lblStatus, lblHP, lblMP, picScreen, 
                btnCapture, btnDetect, btnStart, btnStop,
                grp, lblLog, txtLog 
            });
            
            Log("=== 天堂機器人 v4.0 ===");
            Log("螢幕截圖 + OCR 版本");
        }
        
        // ==================== 螢幕截圖 ====================
        Bitmap CaptureWindow(IntPtr hwnd)
        {
            RECT rect;
            GetWindowRect(hwnd, out rect);
            
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            
            IntPtr hdcScreen = GetDC(IntPtr.Zero);
            IntPtr hdcMem = CreateCompatibleDC(hdcScreen);
            IntPtr hBitmap = CreateCompatibleBitmap(hdcScreen, width, height);
            IntPtr hOld = SelectObject(hdcMem, hBitmap);
            
            BitBlt(hdcMem, 0, 0, width, height, hdcScreen, rect.Left, rect.Top, SRCCOPY);
            
            SelectObject(hdcMem, hOld);
            
            Bitmap bmp = Image.FromHbitmap(hBitmap);
            
            DeleteObject(hBitmap);
            DeleteDC(hdcMem);
            ReleaseDC(IntPtr.Zero, hdcScreen);
            
            return bmp;
        }
        
        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hwnd);
        
        [DllImport("user32.dll")]
        static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);
        
        void CaptureScreen()
        {
            try {
                // 找遊戲視窗
                if (gameWindow == IntPtr.Zero)
                {
                    string[] names = { "Purple", "Lineage", "LineageClassic" };
                    foreach (string name in names)
                    {
                        try {
                            Process[] ps = Process.GetProcessesByName(name);
                            if (ps.Length > 0 && ps[0].MainWindowHandle != IntPtr.Zero)
                            {
                                gameWindow = ps[0].MainWindowHandle;
                                break;
                            }
                        } catch {}
                    }
                }
                
                if (gameWindow == IntPtr.Zero)
                {
                    gameWindow = GetForegroundWindow();
                }
                
                if (gameWindow != IntPtr.Zero)
                {
                    RECT rect;
                    GetWindowRect(gameWindow, out rect);
                    winLeft = rect.Left;
                    winTop = rect.Top;
                    winWidth = rect.Right - rect.Left;
                    winHeight = rect.Bottom - rect.Top;
                    
                    Bitmap bmp = CaptureWindow(gameWindow);
                    picScreen.Image = bmp;
                    
                    Log($"截圖成功: {winWidth}x{winHeight}");
                }
                else
                {
                    Log("找不到遊戲視窗");
                }
            }
            catch (Exception ex)
            {
                Log($"截圖失敗: {ex.Message}");
            }
        }
        
        // ==================== OCR / 數據辨識 ====================
        void DetectGameData()
        {
            if (picScreen.Image == null)
            {
                CaptureScreen();
            }
            
            if (picScreen.Image == null) return;
            
            // 這裡需要OCR庫來辨識HP/MP
            // 目前先用顏色偵測或區域分析
            
            Log("=== 數據偵測 ===");
            Log("注意: 需要安裝OCR庫才能辨識文字");
            Log("目前提供基礎框架");
            
            // 顯示截圖讓使用者確認
            MessageBox.Show("截圖完成！\n請確認畫面是否正確。\n\n要完整實現OCR功能，需要：\n1. PaddleOCR\n2. Tesseract OCR\n3. 或其他OCR庫", "提示");
        }
        
        // ==================== 機器人核心 ====================
        void StartBot()
        {
            if (isRunning) return;
            
            if (gameWindow == IntPtr.Zero)
            {
                CaptureScreen();
            }
            
            if (gameWindow == IntPtr.Zero)
            {
                MessageBox.Show("請先截圖");
                return;
            }
            
            isRunning = true;
            lblStatus.Text = "狀態: 運行中";
            lblStatus.ForeColor = Color.Green;
            
            Log(">>> 機器人啟動 <<<");
            
            botThread = new Thread(BotLoop);
            botThread.IsBackground = true;
            botThread.Start();
        }
        
        void StopBot()
        {
            isRunning = false;
            lblStatus.Text = "狀態: 已停止";
            lblStatus.ForeColor = Color.Black;
            
            Log(">>> 機器人停止 <<<");
        }
        
        void BotLoop()
        {
            int cycle = 0;
            while (isRunning)
            {
                try
                {
                    cycle++;
                    
                    // 確保遊戲在前台
                    SetForegroundWindow(gameWindow);
                    Thread.Sleep(100);
                    
                    // 截圖分析（可選）
                    if (cycle % 10 == 0)
                    {
                        this.Invoke(new Action(() => {
                            // 這裡可以做圖像辨識找怪物
                        }));
                    }
                    
                    // 移動
                    if (cycle % 3 == 0)
                    {
                        int x = rand.Next(200, winWidth - 200);
                        int y = rand.Next(100, winHeight - 100);
                        
                        int screenX = winLeft + x;
                        int screenY = winTop + y;
                        
                        SetCursorPos(screenX, screenY);
                        Thread.Sleep(50);
                        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                        Thread.Sleep(100);
                        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                        
                        this.Invoke(new Action(() => Log($"移動: ({x},{y})")));
                    }
                    
                    // 攻擊
                    if (cycle % 5 == 0)
                    {
                        keybd_event(0x41, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero); // A
                        Thread.Sleep(50);
                        keybd_event(0x41, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                        
                        this.Invoke(new Action(() => Log("⚔ 攻擊")));
                    }
                    
                    Thread.Sleep(rand.Next(2000, 4000));
                }
                catch (Exception ex)
                {
                    this.Invoke(new Action(() => Log($"錯誤: {ex.Message}")));
                }
            }
        }
        
        void Log(string msg)
        {
            if (txtLog.InvokeRequired)
                txtLog.Invoke(new Action(() => {
                    txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\r\n");
                    txtLog.SelectionStart = txtLog.Text.Length;
                    txtLog.ScrollToCaret();
                }));
            else
            {
                txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\r\n");
            }
        }
    }
}
