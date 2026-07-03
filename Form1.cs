using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dfust.Hotkeys;
using Gma.System.MouseKeyHook;
using Microsoft.Win32;
using System.Diagnostics;
namespace ScreenshotMaker
{
    public partial class Form1 : Form,IDisposable
    {
        
        HotkeyCollection hotkeyCollectionWinShiftS = new HotkeyCollection(Enums.Scope.Global);
        HotkeyCollection hotkeyCollectionPrtSc = new HotkeyCollection(Enums.Scope.Global);
        string pathForImages = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + "\\Screenshots";
        private IKeyboardMouseEvents m_GlobalHook = Hook.GlobalEvents();
        string configFilePath = Path.GetDirectoryName(Application.ExecutablePath) + "\\config.txt";
        private const string StartupRegistryKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string ApplicationName = "ScreenshotMaker";
        public Form1()
        {
            InitializeComponent();
            ShowInTaskbar = false;
            notifyIcon.Visible = true;
            Hide();
            WindowState = FormWindowState.Minimized;
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            LoadState();
            hotkeyCollectionWinShiftS.RegisterHotkey(Keys.LWin | Keys.Shift | Keys.S, null, handled: true);
            hotkeyCollectionPrtSc.RegisterHotkey(Keys.PrintScreen,null, handled: true);
            Directory.CreateDirectory(pathForImages);
            
            notifyIcon.ContextMenu = new ContextMenu();
            notifyIcon.ContextMenu.MenuItems.Add("Open", (object sender, EventArgs e) => { Show(); WindowState = FormWindowState.Normal; notifyIcon.Visible = false;ShowInTaskbar = true; });
            notifyIcon.ContextMenu.MenuItems.Add("Exit", (object sender, EventArgs e) => { Close(); });
        }

        private void WaitForConfigAccess(string configPath,int maxAttempts = 10, int delayMilliseconds = 1000)
        {
            string configDirectory = Path.GetDirectoryName(configFilePath);

            // Create the directory if it doesn't exist
            if (!Directory.Exists(configDirectory))
            {
                Directory.CreateDirectory(configDirectory);
            }

            // Create the config file if it doesn't exist
            if (!File.Exists(configFilePath))
            {
                File.Create(configFilePath).Close();
            }
            int attemps = 0;
            while(attemps < maxAttempts)
            {
                try
                {
                    using(var fs = File.Open(configPath, FileMode.Open))
                    {
                        return;
                    }
                }catch(IOException){
                    Thread.Sleep(delayMilliseconds);
                    attemps++;
                }
            }

        }
        private void LoadState()
        {
            WaitForConfigAccess(configFilePath);
            using (StreamReader reader = new StreamReader(configFilePath))
            {
                StartWithWindowsCheckBox.Checked = bool.Parse(reader.ReadLine());
                CapturePrtScCheckBox.Checked = bool.Parse(reader.ReadLine());
                CaptureWinShiftSCheckBox.Checked = bool.Parse(reader.ReadLine());
            }
            var screenshotsCount = Directory.GetFiles(pathForImages).Count();
            label1.Text = screenshotsCount.ToString();
            fetchSizeOfFolder();
        }
        private void fetchSizeOfFolder()
        {
            long sizeofFolder = GetFolderSize(pathForImages);
            sizeofFolder = sizeofFolder / 1048576;
            label2.Text = sizeofFolder.ToString() + " Mb";
        }
        static long GetFolderSize(string folderPath)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(folderPath);

            // Calculate the total size of files in the directory and its subdirectories
            long size = directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                            .Sum(file => file.Length);

            return size;
        }
        private void SaveState()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(configFilePath))
                {
                    writer.WriteLine(StartWithWindowsCheckBox.Checked);
                    writer.WriteLine(CapturePrtScCheckBox.Checked);
                    writer.WriteLine(CaptureWinShiftSCheckBox.Checked);
                }
            }
            catch (Exception ex)
            {
                // Handle any exception that may occur during saving
                MessageBox.Show(ex.Message);
            }
        }


        protected override void OnResize(EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                notifyIcon.Text = label1.Text;
                notifyIcon.Visible = true;
            }
            base.OnResize(e);
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
            notifyIcon.Visible = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
            notifyIcon.Visible = true;
        }
        protected override void OnClosed(EventArgs e)
        {
            SaveState();
            m_GlobalHook.Dispose();
            hotkeyCollectionWinShiftS.Dispose();
            base.OnClosed(e);
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (CaptureWinShiftSCheckBox.Checked)
            {
                hotkeyCollectionWinShiftS.HotkeyTriggered += saveScreashot;
                return;
            }
            hotkeyCollectionWinShiftS.HotkeyTriggered -= saveScreashot;

        }

        private void saveScreashot(HotKeyEventArgs obj)
        {
            m_GlobalHook.MouseUp += M_GlobalHook_MouseUpFinished;
        }
        private async void saveScreanshotSimple(HotKeyEventArgs obj)
        {
            await ProcessScreenshotAsync();
        }
        private async void M_GlobalHook_MouseUpFinished(object sender, MouseEventArgs e)
        {
            await ProcessScreenshotAsync();
            m_GlobalHook.MouseUp -= M_GlobalHook_MouseUpFinished;
        }

        private async Task ProcessScreenshotAsync()
        {
            await Task.Delay(1000);
            if (Clipboard.ContainsImage())
            {
                var screenshot = Clipboard.GetImage();
                pictureBox1.Image = screenshot;
                var screenshotsCount = Directory.GetFiles(pathForImages).Count();
                string pathForScreenshot = pathForImages + "\\" + (screenshotsCount + 1).ToString() + ".png";
                screenshot.Save(pathForScreenshot);
                screenshotsCount = Directory.GetFiles(pathForImages).Count();
                label1.Text = screenshotsCount.ToString();
                notifyIcon.Text = screenshotsCount.ToString();
                fetchSizeOfFolder();
            }
        }

        private void CapturePrtScCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if(CapturePrtScCheckBox.Checked)
            {
                hotkeyCollectionPrtSc.HotkeyTriggered += saveScreanshotSimple;
                return;
            }
            hotkeyCollectionPrtSc.HotkeyTriggered -= saveScreanshotSimple;
        }


        private void SetStartup(bool enable)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true))
            {
                if (enable)
                {
                    key.SetValue(ApplicationName, Application.ExecutablePath.Replace("/","\\"));
                }
                else
                {
                    key.DeleteValue(ApplicationName, false);
                }
            }
        }
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            SetStartup(StartWithWindowsCheckBox.Checked);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", pathForImages);
        }
    }
}
