using System;
using System.Windows.Forms;
namespace ScreenshotMaker
{
    internal static class Program
    {
        
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Form1 main = new Form1();
            main.Hide();
            Application.Run(main);
        }
        
    }
}
