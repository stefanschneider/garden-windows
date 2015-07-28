using System;
using System.ComponentModel;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System;
using System.Collections;
using System.Text;
using System.DirectoryServices.AccountManagement;
using System.Data;
using System.Configuration;
using Microsoft.Win32.SafeHandles;

namespace TurnWin32ApiStartProcessIntoDotnet
{
    public class Program
    {
        public const UInt32 Infinite = 0xffffffff;
        public const Int32 Startf_UseStdHandles = 0x00000100;
        public const Int32 StdOutputHandle = -11;
        public const Int32 StdErrorHandle = -12;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct StartupInfo
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        public struct ProcessInformation
        {
            public IntPtr process;
            public IntPtr thread;
            public int processId;
            public int threadId;
        }


        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool CreateProcessWithLogonW(
            String userName,
            String domain,
            String password,
            UInt32 logonFlags,
            String applicationName,
            String commandLine,
            UInt32 creationFlags,
            UInt32 environment,
            String currentDirectory,
            ref   StartupInfo startupInfo,
            out  ProcessInformation processInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetExitCodeProcess(IntPtr process, ref UInt32 exitCode);

        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern UInt32 WaitForSingleObject(IntPtr handle, UInt32 milliseconds);

        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetStdHandle(IntPtr handle);

        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr handle);

        [Flags]
        enum CreationFlags : uint
        {
            CREATE_SUSPENDED = 0x00000004,
            CREATE_NEW_CONSOLE = 0x00000010,
            CREATE_NEW_PROCESS_GROUP = 0x00000200,
            CREATE_UNICODE_ENVIRONMENT = 0x00000400,
            CREATE_SEPARATE_WOW_VDM = 0x00000800,
            CREATE_DEFAULT_ERROR_MODE = 0x04000000,
        }

        [Flags]
        enum LogonFlags : uint
        {
            LOGON_WITH_PROFILE = 0x00000001,
            LOGON_NETCREDENTIALS_ONLY = 0x00000002
        }

        [DllImport("kernel32.dll")]
        static extern uint ResumeThread(IntPtr hThread);








        [return: MarshalAs(UnmanagedType.Bool)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CloseWindowStation(IntPtr hWinsta);

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern SafeWindowStationHandle2 GetProcessWindowStation();

        public sealed class SafeWindowStationHandle2 : SafeHandleZeroOrMinusOneIsInvalid
        {
            public SafeWindowStationHandle2()
                : base(true)
            {
            }

            protected override bool ReleaseHandle()
            {
                return CloseWindowStation(handle);
            }
        }

        [DllImport("user32.dll", EntryPoint = "CreateWindowStation", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr CreateWindowStation(
                        [MarshalAs(UnmanagedType.LPWStr)] string name,
                        [MarshalAs(UnmanagedType.U4)] int reserved,      // must be zero.
                        [MarshalAs(UnmanagedType.U4)] WINDOWS_STATION_ACCESS_MASK desiredAccess,
                        [MarshalAs(UnmanagedType.LPStruct)] SecurityAttributes attributes);

        [StructLayout(LayoutKind.Sequential)]
        public class SecurityAttributes
        {
            #region Struct members
            [MarshalAs(UnmanagedType.U4)]
            private int mStuctLength;

            private IntPtr mSecurityDescriptor;

            [MarshalAs(UnmanagedType.U4)]
            private bool mInheritHandle;
            #endregion

            public SecurityAttributes()
            {
                mStuctLength = Marshal.SizeOf(typeof(SecurityAttributes));
                mSecurityDescriptor = IntPtr.Zero;
            }

            public IntPtr SecurityDescriptor
            {
                get { return mSecurityDescriptor; }
                set { mSecurityDescriptor = value; }
            }

            public bool Inherit
            {
                get { return mInheritHandle; }
                set { mInheritHandle = value; }
            }
        }
        [Flags]
        public enum WINDOWS_STATION_ACCESS_MASK : uint
        {
            WINSTA_NONE = 0,

            WINSTA_ENUMDESKTOPS = 0x0001,
            WINSTA_READATTRIBUTES = 0x0002,
            WINSTA_ACCESSCLIPBOARD = 0x0004,
            WINSTA_CREATEDESKTOP = 0x0008,
            WINSTA_WRITEATTRIBUTES = 0x0010,
            WINSTA_ACCESSGLOBALATOMS = 0x0020,
            WINSTA_EXITWINDOWS = 0x0040,
            WINSTA_ENUMERATE = 0x0100,
            WINSTA_READSCREEN = 0x0200,

            WINSTA_ALL_ACCESS = (WINSTA_ENUMDESKTOPS | WINSTA_READATTRIBUTES | WINSTA_ACCESSCLIPBOARD |
                            WINSTA_CREATEDESKTOP | WINSTA_WRITEATTRIBUTES | WINSTA_ACCESSGLOBALATOMS |
                            WINSTA_EXITWINDOWS | WINSTA_ENUMERATE | WINSTA_READSCREEN), // | STANDARD_ACCESS.STANDARD_RIGHTS_REQUIRED),
        }

        public abstract class BaseSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            protected BaseSafeHandle(IntPtr handle, bool ownsHandle)
                : base(ownsHandle)
            {
                SetHandle(handle);
            }

            protected abstract bool CloseNativeHandle(IntPtr handle);

            protected override bool ReleaseHandle()
            {
                if (IsInvalid)
                {
                    return false;
                }
                bool closed = CloseNativeHandle(this.handle);
                if (closed)
                {
                    SetHandle(IntPtr.Zero);
                }
                return closed;
            }

        }

        public class SafeWindowStationHandle : BaseSafeHandle
        {
            public SafeWindowStationHandle(IntPtr handle, bool ownsHandle)
                : base(handle, ownsHandle)
            { }

            protected override bool CloseNativeHandle(IntPtr handle)
            {
                return CloseWindowStation(handle);
            }
        }

        public static SafeWindowStationHandle CreateWindowStation(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Invalid window station name", "name");
            }

            IntPtr handle = CreateWindowStation(name, 0, WINDOWS_STATION_ACCESS_MASK.WINSTA_ALL_ACCESS, null);
            if (handle == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error);
            }

            SafeWindowStationHandle safeHandle = new SafeWindowStationHandle(handle, true);

            return safeHandle;
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetProcessWindowStation(IntPtr hWinSta);





        public static void Main()
        {
            StartupInfo startupInfo = new StartupInfo();
            startupInfo.cb = Marshal.SizeOf(startupInfo);
            startupInfo.lpReserved = null;
            startupInfo.dwFlags &= Startf_UseStdHandles;
            startupInfo.hStdOutput = (IntPtr) StdOutputHandle;
            startupInfo.hStdError = (IntPtr) StdErrorHandle;
            startupInfo.lpDesktop = null;

            UInt32 exitCode = 123456;
            ProcessInformation processInfo = new ProcessInformation();

            String command = @"c:\windows\system32\ping.exe 127.0.0.1";
            //command = @"C:\Users\greenhouse\Documents\Visual Studio 2013\Projects\TurnWin32ApiStartProcessIntoDotnet\PrintStationDesktopName\bin\Debug\PrintStationDesktopName.exe";
            //command = @"C:\output\PrintStationDesktopName.exe";
            String domain = System.Environment.MachineName;
            String currentDirectory = System.IO.Directory.GetCurrentDirectory();

            String username = "otheruser3"; // "greenhouse";
            String password = "Awe1Some!Pwd"; // "cat9lives";
            startupInfo.lpDesktop = username;


            //var defaultWinStation = GetProcessWindowStation();
            //var newWinStation = CreateWindowStation(username);
            //SetProcessWindowStation(newWinStation.DangerousGetHandle());
            //CloseWindowStation(defaultWinStation.DangerousGetHandle());

            //try
            //{
            //    using (var context = new PrincipalContext(ContextType.Machine))
            //    {
            //        var user = new UserPrincipal(context, username, password, true);
            //        user.DisplayName = "Test User " + username;
            //        user.Save();
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.ToString());
            //}

            //String username = "greenhouse";
            //String password = "cat9lives";

            bool createSucc = false;
            try
            {
                createSucc = CreateProcessWithLogonW(
                    username,
                    domain,
                    password,
                    (UInt32)LogonFlags.LOGON_WITH_PROFILE,
                    null,
                    command,
                    (UInt32)CreationFlags.CREATE_SUSPENDED,
                    (UInt32)0,
                    @"C:\output", // currentDirectory,
                    ref startupInfo,
                    out processInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            if (!createSucc)
            {
                var err = Marshal.GetLastWin32Error();
                Console.WriteLine(err);
            }

            var p = Process.GetProcessById((int)processInfo.processId);

            ResumeThread(processInfo.thread);

            Console.WriteLine("Running ...");
            WaitForSingleObject(processInfo.process, Infinite);
            GetExitCodeProcess(processInfo.process, ref exitCode);

            Console.WriteLine("Exit code: {0}", exitCode);

            CloseHandle(processInfo.process);
            CloseHandle(processInfo.thread);
        }
    }
}