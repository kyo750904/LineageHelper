using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace LineageHelper
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
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("user32.dll")]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern int GetWindowThreadProcessId(IntPtr hWnd, out int processId);

        const uint PROCESS_ALL_ACCESS = 0x1F0FFF;
        const uint PROCESS_VM_READ = 0x0010;
        const uint PROCESS_VM_WRITE = 0x0020;
        
        IntPtr hProcess = IntPtr.Zero;
        int processId;
        bool isAttached = false;
        
        TextBox txtLog;
        Label lblStatus;
        
        public MainForm()
        {
            Text = "天堂輔助 v3.6 - 除錯版";
            Size = new System.Drawing.Size(450, 400);
            StartPosition = FormStartPosition.CenterScreen;
            
            lblStatus = new Label { Text = "狀態: 請附加", Left = 15, Top = 15, Width = 400, ForeColor = System.Drawing.Color.Red };
            
            var btn1 = new Button { Text = "附加到Purple", Left = 15, Top = 45, Width = 130, Height = 35 };
            btn1.Click += (s,e) => AttachProcess("Purple");
            
            var btn2 = new Button { Text = "附加到Lineage", Left = 155, Top = 45, Width = 130, Height = 35 };
            btn2.Click += (s,e) => AttachProcess("Lineage");
            
            var btn3 = new Button { Text = "附加到LineageClassic", Left = 295, Top = 45, Width = 130, Height = 35 };
            btn3.Click += (s,e) => AttachProcess("LineageClassic");
            
            var btn4 = new Button { Text = "讀取測試(0x00400000)", Left = 15, Top = 90, Width = 200, Height = 35 };
            btn4.Click += (s,e) => TestRead();
            
            var info = new Label 
            { 
                Text = "說明:\n1. 用管理員身份執行\n2. 附加到遊戲程序\n3. 嘗試讀取測試\n4. 看日誌結果", 
                Left = 15, Top = 135, Width = 400, Height = 60,
                ForeColor = System.Drawing.Color.Gray 
            };
            
            txtLog = new TextBox { Left = 15, Top = 200, Width = 400, Height = 160, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };
            txtLog.Font = new System.Drawing.Font("Consolas", 9);
            
            Controls.AddRange(new Control[] { lblStatus, btn1, btn2, btn3, btn4, info, txtLog });
            
            Log("=== v3.6 除錯版 ===");
        }
        
        void AttachProcess(string name)
        {
            Log($"=== 嘗試附加: {name} ===");
            
            try
            {
                Process[] ps = Process.GetProcessesByName(name);
                
                if (ps.Length == 0)
                {
                    Log($"✗ 找不到程序: {name}");
                    MessageBox.Show($"找不到 {name}\n請確認遊戲是否正在運行");
                    return;
                }
                
                processId = ps[0].Id;
                Log($"找到程序: {name} (PID: {processId})");
                
                // 嘗試用最高權限
                hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, processId);
                
                if (hProcess == IntPtr.Zero)
                {
                    // 嘗試用較低權限
                    hProcess = OpenProcess(PROCESS_VM_READ | PROCESS_VM_WRITE, false, processId);
                }
                
                if (hProcess != IntPtr.Zero)
                {
                    isAttached = true;
                    lblStatus.Text = $"已附加: {name} (PID:{processId})";
                    lblStatus.ForeColor = System.Drawing.Color.Green;
                    Log($"✓ 附加成功! Handle: {hProcess}");
                }
                else
                {
                    int err = Marshal.GetLastWin32Error();
                    Log($"✗ 附加失敗，錯誤碼: {err}");
                    Log("請用管理員身份執行!");
                    MessageBox.Show("請用管理員身份執行輔助程式");
                }
            }
            catch (Exception ex)
            {
                Log($"錯誤: {ex.Message}");
            }
        }
        
        void TestRead()
        {
            if (!isAttached)
            {
                MessageBox.Show("請先附加");
                return;
            }
            
            Log("=== 讀取測試 ===");
            
            // 嘗試讀取程序起始位置
            IntPtr baseAddr = new IntPtr(0x00400000); // PE 程序起始位置
            
            byte[] buffer = new byte[4];
            int bytesRead;
            
            // 讀取一小段
            if (ReadProcessMemory(hProcess, baseAddr, buffer, 4, out bytesRead))
            {
                Log($"✓ 讀取成功! bytes: {bytesRead}");
                Log($"  前4位元組: {buffer[0]:X2} {buffer[1]:X2} {buffer[2]:X2} {buffer[3]:X2}");
                
                // 這通常讀不到有意義的數據，但證明記憶體可讀
                Log("  -> 記憶體讀取正常!");
            }
            else
            {
                int err = Marshal.GetLastWin32Error();
                Log($"✗ 讀取失敗，錯誤碼: {err}");
            }
            
            // 嘗試讀取更多位置
            IntPtr[] testAddrs = new IntPtr[]
            {
                new IntPtr(0x00000000),
                new IntPtr(0x10000000),
                new IntPtr(0x20000000),
                new IntPtr(0x00A80000),
            };
            
            foreach (var addr in testAddrs)
            {
                if (ReadProcessMemory(hProcess, addr, buffer, 4, out bytesRead))
                {
                    Log($"✓ 可讀: {addr:X8}");
                }
            }
            
            // 列出一些模組
            try
            {
                Process p = Process.GetProcessById(processId);
                Log("=== 程式模組 ===");
                foreach (ProcessModule m in p.Modules)
                {
                    Log($"  {m.ModuleName}: {m.BaseAddress:X8} ({(m.ModuleMemorySize/1024)}KB)");
                }
            }
            catch (Exception ex)
            {
                Log($"列舉模組失敗: {ex.Message}");
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
        
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (hProcess != IntPtr.Zero)
                CloseHandle(hProcess);
            base.OnFormClosing(e);
        }
    }
}
