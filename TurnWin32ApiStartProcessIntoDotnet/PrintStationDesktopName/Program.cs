using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace PrintStationDesktopName
{
    class Program
    {
        /*
        public sealed class SafeWindowStationHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
        {
            public SafeWindowStationHandle()
                : base(true)
            {
            }

            protected override bool ReleaseHandle()
            {
                return true; //SafeNativeMethods.CloseWindowStation(handle);
            }
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern SafeWindowStationHandle GetProcessWindowStation();
        */

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int MessageBox(IntPtr hWnd, String text, String caption, int options);


        static void Main(string[] args)
        {
            MessageBox(IntPtr.Zero, "Text", "Caption", 0);

            // var wsh = GetProcessWindowStation();
            // Console.WriteLine(wsh.ToString());

            // Console.ReadLine();
        }
    }
}
