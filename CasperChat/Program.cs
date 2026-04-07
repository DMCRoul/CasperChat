using System;
using System.Windows.Forms;
using CasperChat.Client.Forms;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new ChatForm());
    }
}