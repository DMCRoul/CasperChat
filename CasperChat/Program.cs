using CasperChat.Client.Forms;
using System;
using System.Windows.Forms;

namespace CasperChat.Client
{
    internal static partial class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new ChatForm());
        }
    }
}