using System;
using System.Windows.Forms;
using TycoonGame.UI;

namespace TycoonGame
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Set high DPI mode for crisp modern rendering
            Application.SetHighDpiMode(HighDpiMode.SystemAware);

            Application.Run(new MainForm());
        }
    }
}
