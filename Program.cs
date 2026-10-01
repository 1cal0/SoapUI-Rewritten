using System;
using System.Windows.Forms;
using SoapUI.UI;

namespace SoapUI
{
    /// <summary>
    /// Application entry point. Keeps Main() minimal - all startup
    /// behaviour lives in <see cref="MainUI"/>.
    /// </summary>
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
            Application.Run(new MainUI());
        }
    }
}
