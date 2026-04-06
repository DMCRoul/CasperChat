using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CasperChat.Client.Services
{
    public static class CaptureProtectionService
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

        public static bool Apply(Form form)
        {
            if (form == null || form.IsDisposed)
                return false;

            return SetWindowDisplayAffinity(form.Handle, WDA_EXCLUDEFROMCAPTURE);
        }
    }
}