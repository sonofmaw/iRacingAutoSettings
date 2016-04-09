using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace iRacingSettingsServer
{
    public class ProcessStarter
    {
        #region Import Section

        private class NativeMethods
        {
            public static uint STANDARD_RIGHTS_REQUIRED = 0x000F0000;
            public static uint STANDARD_RIGHTS_READ = 0x00020000;
            public static uint TOKEN_ASSIGN_PRIMARY = 0x0001;
            public static uint TOKEN_DUPLICATE = 0x0002;
            public static uint TOKEN_IMPERSONATE = 0x0004;
            public static uint TOKEN_QUERY = 0x0008;
            public static uint TOKEN_QUERY_SOURCE = 0x0010;
            public static uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
            public static uint TOKEN_ADJUST_GROUPS = 0x0040;
            public static uint TOKEN_ADJUST_DEFAULT = 0x0080;
            public static uint TOKEN_ADJUST_SESSIONID = 0x0100;
            public static uint TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);
            public static uint TOKEN_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | TOKEN_ASSIGN_PRIMARY | TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY | TOKEN_QUERY_SOURCE | TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT | TOKEN_ADJUST_SESSIONID);

            public const uint NORMAL_PRIORITY_CLASS = 0x0020;

            public const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;


            public const uint MAX_PATH = 260;

            public const uint CREATE_NO_WINDOW = 0x08000000;

            public const uint INFINITE = 0xFFFFFFFF;

            [StructLayout(LayoutKind.Sequential)]
            public struct SECURITY_ATTRIBUTES
            {
                public int nLength;
                public IntPtr lpSecurityDescriptor;
                public int bInheritHandle;
            }

            public enum SECURITY_IMPERSONATION_LEVEL
            {
                SecurityAnonymous,
                SecurityIdentification,
                SecurityImpersonation,
                SecurityDelegation
            }

            public enum TOKEN_TYPE
            {
                TokenPrimary = 1,
                TokenImpersonation
            }

            public enum WTS_CONNECTSTATE_CLASS
            {
                WTSActive,
                WTSConnected,
                WTSConnectQuery,
                WTSShadow,
                WTSDisconnected,
                WTSIdle,
                WTSListen,
                WTSReset,
                WTSDown,
                WTSInit
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct STARTUPINFO
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

            [StructLayout(LayoutKind.Sequential)]
            public struct PROCESS_INFORMATION
            {
                public IntPtr hProcess;
                public IntPtr hThread;
                public int dwProcessId;
                public int dwThreadId;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct WTS_SESSION_INFO
            {
                public Int32 SessionID;

                [MarshalAs(UnmanagedType.LPStr)]
                public String pWinStationName;

                public WTS_CONNECTSTATE_CLASS State;
            }

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern uint WTSGetActiveConsoleSessionId();

            [DllImport("wtsapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool WTSQueryUserToken(int sessionId, out IntPtr tokenHandle);

            [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public extern static bool DuplicateTokenEx(IntPtr existingToken, uint desiredAccess, IntPtr tokenAttributes, SECURITY_IMPERSONATION_LEVEL impersonationLevel, TOKEN_TYPE tokenType, out IntPtr newToken);

            [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern bool CreateProcessAsUser(IntPtr token, string applicationName, string commandLine, ref SECURITY_ATTRIBUTES processAttributes, ref SECURITY_ATTRIBUTES threadAttributes, bool inheritHandles, uint creationFlags, IntPtr environment, string currentDirectory, ref STARTUPINFO startupInfo, out PROCESS_INFORMATION processInformation);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool CloseHandle(IntPtr handle);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern int GetLastError();

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern int WaitForSingleObject(IntPtr token, uint timeInterval);

            [DllImport("wtsapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern int WTSEnumerateSessions(System.IntPtr hServer, int Reserved, int Version, ref System.IntPtr ppSessionInfo, ref int pCount);

            [DllImport("userenv.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

            [DllImport("wtsapi32.dll", ExactSpelling = true, SetLastError = false)]
            public static extern void WTSFreeMemory(IntPtr memory);

            [DllImport("userenv.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);
        }

        #endregion

        public ProcessStarter()
        {

        }

        public ProcessStarter(string processName, string fullExeName)
        {
            processName_ = processName;
            processPath_ = fullExeName;
        }
        public ProcessStarter(string processName, string fullExeName, string arguments)
        {
            processName_ = processName;
            processPath_ = fullExeName;
            arguments_ = arguments;
        }

        public static IntPtr GetCurrentUserToken()
        {
            IntPtr primaryToken = IntPtr.Zero;
            IntPtr WTS_CURRENT_SERVER_HANDLE = IntPtr.Zero;

            int dwSessionId = 0;
            IntPtr hUserToken = IntPtr.Zero;
            IntPtr hTokenDup = IntPtr.Zero;

            IntPtr pSessionInfo = IntPtr.Zero;
            int dwCount = 0;

            NativeMethods.WTSEnumerateSessions(WTS_CURRENT_SERVER_HANDLE, 0, 1, ref pSessionInfo, ref dwCount);

            Int32 dataSize = Marshal.SizeOf(typeof(NativeMethods.WTS_SESSION_INFO));

            Int32 current = (int)pSessionInfo;
            for (int i = 0; i < dwCount; i++)
            {
                NativeMethods.WTS_SESSION_INFO si = (NativeMethods.WTS_SESSION_INFO)Marshal.PtrToStructure((System.IntPtr)current, typeof(NativeMethods.WTS_SESSION_INFO));
                if (NativeMethods.WTS_CONNECTSTATE_CLASS.WTSActive == si.State)
                {
                    dwSessionId = si.SessionID;
                    break;
                }

                current += dataSize;
            }

            NativeMethods.WTSFreeMemory(pSessionInfo);

            bool bRet = NativeMethods.WTSQueryUserToken(dwSessionId, out primaryToken);
            if (bRet == false)
            {
                return IntPtr.Zero;
            }

            return primaryToken;
        }

        public void Run()
        {

            IntPtr primaryToken = GetCurrentUserToken();
            if (primaryToken == IntPtr.Zero)
            {
                return;
            }
            NativeMethods.STARTUPINFO StartupInfo = new NativeMethods.STARTUPINFO();
            processInfo_ = new NativeMethods.PROCESS_INFORMATION();
            StartupInfo.cb = Marshal.SizeOf(StartupInfo);

            NativeMethods.SECURITY_ATTRIBUTES Security1 = new NativeMethods.SECURITY_ATTRIBUTES();
            NativeMethods.SECURITY_ATTRIBUTES Security2 = new NativeMethods.SECURITY_ATTRIBUTES();

            string command = "\"" + processPath_ + "\"";
            if ((arguments_ != null) && (arguments_.Length != 0))
            {
                command += " " + arguments_;
            }

            IntPtr lpEnvironment = IntPtr.Zero;
            bool resultEnv = NativeMethods.CreateEnvironmentBlock(out lpEnvironment, primaryToken, false);
            if (resultEnv != true)
            {
                int nError = NativeMethods.GetLastError();
            }

            NativeMethods.CreateProcessAsUser(primaryToken, null, command, ref Security1, ref Security2, false, NativeMethods.CREATE_NO_WINDOW | NativeMethods.NORMAL_PRIORITY_CLASS | NativeMethods.CREATE_UNICODE_ENVIRONMENT, lpEnvironment, null, ref StartupInfo, out processInfo_);

            NativeMethods.DestroyEnvironmentBlock(lpEnvironment);
            NativeMethods.CloseHandle(primaryToken);
        }

        public void Stop()
        {
            Process[] processes = Process.GetProcesses();
            foreach (Process current in processes)
            {
                if (current.ProcessName == processName_)
                {
                    current.Kill();
                }
            }
        }

        public int WaitForExit()
        {
            NativeMethods.WaitForSingleObject(processInfo_.hProcess, NativeMethods.INFINITE);
            int errorcode = NativeMethods.GetLastError();
            return errorcode;
        }

        private string processPath_ = string.Empty;
        private string processName_ = string.Empty;
        private string arguments_ = string.Empty;
        private NativeMethods.PROCESS_INFORMATION processInfo_;

        public string ProcessPath
        {
            get
            {
                return processPath_;
            }
            set
            {
                processPath_ = value;
            }
        }

        public string ProcessName
        {
            get
            {
                return processName_;
            }
            set
            {
                processName_ = value;
            }
        }

        public string Arguments
        {
            get
            {
                return arguments_;
            }
            set
            {
                arguments_ = value;
            }
        }
    }
}
