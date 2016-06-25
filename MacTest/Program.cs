using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MacTest
{
    static class Program
    {
        [DllImport("kernal32.dll")]
        internal static extern bool AllocConsole();
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main(string[]args)
        {
            if (args.Length!=0)
            {
                if (args[0].ToLower()=="-debug")
                {
                    AllocConsole();
                    Console.WriteLine("MacChanger");
                }
            }
            Control.CheckForIllegalCrossThreadCalls = false;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
