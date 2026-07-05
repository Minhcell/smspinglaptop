using System;
using System.Windows.Forms;

namespace VTV_SMS_Ping;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.SetCompatibleTextRenderingDefault(false);
        Application.EnableVisualStyles();
        Application.Run(new frmMain());
    }
}
