using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LowEndViet.com_VPS_Tool
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new form_LowEndVietFastVPSConfig(args));
        }
    }
}
