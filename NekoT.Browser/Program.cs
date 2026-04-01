using System;
using System.Windows.Forms;

namespace NekoT.Browser;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }
}