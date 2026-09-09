using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static ACEController.Win32API.API.CoreAPI;

namespace ACEController.Win32API
{
    public static class E
    {
        public static bool SetProcessEcoQoS(int PID, bool bFlag)
        {
            IntPtr phwnd = OpenProcess(ProcessAccessFlags.PROCESS_QUERY_INFORMATION | ProcessAccessFlags.PROCESS_SET_INFORMATION, false, PID);
            if (phwnd != IntPtr.Zero)
            {
                IntPtr homo = IntPtr.Zero;
                try
                {
                    // 此结构有三个字段Version，ControlMask 和 StateMask
                    uint version = 1;
                    uint controlMask = 0x1; //非权重开关
                    uint stateMask = (uint)(bFlag ? 0x1 : 0x0);
                    int szControlBlock = 12; // 三个uint的大小
                    homo = Marshal.AllocHGlobal(szControlBlock);
                    Marshal.WriteInt32(homo, (int)version); //homo 指向内存块开头
                    Marshal.WriteInt32(homo + 4, (int)controlMask); // 将 controlMask 值写入第2字段地址，需将 homo 指针加4字节
                    Marshal.WriteInt32(homo + 8, (int)stateMask); // 将 stateMask 值写入第3个字段地址，需将 homo 指针加8个字节
                    bool result = true;
                    result &= SetProcessInformation(phwnd, PROCESS_INFORMATION_CLASS.ProcessPowerThrottling, homo, (uint)szControlBlock);
                    result &= SetPriorityClass(phwnd, (uint)(bFlag ? 0x40 : 0x20));
                    Marshal.FreeHGlobal(homo);
                    return result;
                }
                catch (Exception)
                {
                    if (homo != IntPtr.Zero) Marshal.FreeHGlobal(homo);
                    return false;
                }
                finally
                {
                    CloseHandle(phwnd);
                }
            }
            else
            {
                return false;
            }
                
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:删除未使用的参数", Justification = "<挂起>")]
        public static bool SetJobObject(string GUID, int PID, bool UNSAFE)
        {
            // if (UNSAFE) return true;
            var jobHandle = CreateJobObject(IntPtr.Zero, GUID);
            IntPtr phwnd = OpenProcess(
                ProcessAccessFlags.PROCESS_SET_QUOTA |
                ProcessAccessFlags.PROCESS_TERMINATE |
                ProcessAccessFlags.PROCESS_QUERY_INFORMATION |
                ProcessAccessFlags.PROCESS_SET_INFORMATION,
                false, PID);
            if (phwnd != IntPtr.Zero)
            {
                var handle = IntPtr.Zero;
                try
                {
                    if (jobHandle == IntPtr.Zero)
                    {
                        throw new ArgumentNullException($"Can Not Create JobObject [Name = {GUID}]");
                    }
                    SECURITY_ATTRIBUTES sa = new()
                    {
                        nLength = Marshal.SizeOf<SECURITY_ATTRIBUTES>(),
                        bInheritHandle = false
                    };
                    uint error = SetSecurityInfo(
                        jobHandle,
                        SE_OBJECT_TYPE.SE_KERNEL_OBJECT,
                        SECURITY_INFORMATION.DACL_SECURITY_INFORMATION,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        IntPtr.Zero
                    );
                    JOBOBJECT_CPU_RATE_CONTROL_INFORMATION cpuInfo = new()
                    {
                        ControlFlags = JOB_OBJECT_CPU_RATE_CONTROL_ENABLE | JOB_OBJECT_CPU_RATE_CONTROL_MIN_MAX_RATE,
                        DUMMYUNIONNAME = new JOBOBJECT_CPU_RATE_CONTROL_INFORMATION_UNION()
                        {
                            DUMMYSTRUCTNAME = new JOBOBJECT_CPU_RATE_CONTROL_INFORMATION_MIN_MAX_RATE()
                            {
                                MinRate = 1 * 100,
                                MaxRate = 10 * 100
                            }
                        }
                    };
                    handle = Marshal.AllocHGlobal(Marshal.SizeOf(cpuInfo));
                    Marshal.StructureToPtr(cpuInfo, handle, false);
                    if (!SetInformationJobObject(jobHandle, JobObjectInfoType.JobObjectCpuRateControlInformation, handle, (uint)Marshal.SizeOf(cpuInfo)))
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                    if (!AssignProcessToJobObject(jobHandle, phwnd))
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                    Marshal.FreeHGlobal(handle);
                }
                catch (Exception)
                {
                    if (Debugger.IsAttached)
                    {
                        IntPtr addr = GetProcAddress(LoadLibrary("kernel32.dll"), "QueryInformationJobObject");
                        if (addr != IntPtr.Zero)
                        {
                            var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
                            var infoSize = (uint)Marshal.SizeOf(info);
                            var infoPtr = Marshal.AllocHGlobal((int)infoSize);
                            bool result = QueryInformationJobObject(
                                jobHandle,
                                JobObjectInfoType.JobObjectExtendedLimitInformation,
                                infoPtr,
                                infoSize,
                                out uint returnLength
                            );
                            if (result)
                            {
                                info = Marshal.PtrToStructure<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>(infoPtr);
                                if ((info.BasicLimitInformation.LimitFlags & JOB_OBJECT_SECURITY_NO_ADMIN) != 0)
                                {
                                    Debug.Print("作业启用了SECURITY_NO_ADMIN限制");
                                }
                            }
                            else
                            {
                                int win32error = Marshal.GetLastWin32Error();
                                Win32Exception win32 = new(win32error);
                                Debug.Print(win32.Message);
                            }
                            Marshal.FreeHGlobal(infoPtr);
                        }
                    }
                    if (handle != IntPtr.Zero) Marshal.FreeHGlobal(handle);
                    if (IsProcessInJob(phwnd, IntPtr.Zero, out bool isInJob))
                    {
                        return isInJob;
                    }
                    return false;
                }
                finally
                {
                    if (jobHandle != IntPtr.Zero) CloseHandle(jobHandle);
                    if (phwnd != IntPtr.Zero) CloseHandle(phwnd);
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        private enum PROCESS_INFORMATION_CLASS
        {
            ProcessMemoryPriority,
            ProcessMemoryExhaustionInfo,
            ProcessAppMemoryInfo,
            ProcessInPrivateInfo,
            ProcessPowerThrottling,
            ProcessReservedValue1,
            ProcessTelemetryCoverageInfo,
            ProcessProtectionLevelInfo,
            ProcessLeapSecondInfo,
            ProcessInformationClassMax,
        }
        [DllImport("kernel32.dll")]
        private static extern bool SetProcessInformation([In] IntPtr hProcess,
            [In] PROCESS_INFORMATION_CLASS ProcessInformationClass, IntPtr ProcessInformation, uint ProcessInformationSize);
        [DllImport("kernel32.dll")]
        private static extern bool SetPriorityClass(IntPtr handle, uint priorityClass);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateJobObject(IntPtr securityAttributes, string name);
        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }
        public enum SE_OBJECT_TYPE
        {
            SE_KERNEL_OBJECT = 1
        }
        public enum SECURITY_INFORMATION : uint
        {
            DACL_SECURITY_INFORMATION = 0x00000004
        }
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern uint SetSecurityInfo(
            IntPtr handle,
            SE_OBJECT_TYPE objectType,
            SECURITY_INFORMATION securityInfo,
            IntPtr psidOwner,
            IntPtr psidGroup,
            IntPtr pDacl,
            IntPtr pSacl
        );
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern uint GetSecurityInfo(
            IntPtr handle,
            SE_OBJECT_TYPE objectType,
            SECURITY_INFORMATION securityInfo,
            out IntPtr psidOwner,
            out IntPtr psidGroup,
            out IntPtr pDacl,
            out IntPtr pSacl,
            out IntPtr pSecurityDescriptor
        );
        [StructLayout(LayoutKind.Sequential)]
        public struct JOBOBJECT_CPU_RATE_CONTROL_INFORMATION
        {
            public uint ControlFlags;
            public JOBOBJECT_CPU_RATE_CONTROL_INFORMATION_UNION DUMMYUNIONNAME;
        }
        [StructLayout(LayoutKind.Explicit)]
        public struct JOBOBJECT_CPU_RATE_CONTROL_INFORMATION_UNION
        {
            [FieldOffset(0)]
            public uint CpuRate;
            [FieldOffset(0)]
            public uint Weight;
            [FieldOffset(0)]
            public JOBOBJECT_CPU_RATE_CONTROL_INFORMATION_MIN_MAX_RATE DUMMYSTRUCTNAME;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct JOBOBJECT_CPU_RATE_CONTROL_INFORMATION_MIN_MAX_RATE
        {
            public ushort MinRate;
            public ushort MaxRate;
        }
        public const uint JOB_OBJECT_CPU_RATE_CONTROL_ENABLE = 0x00000001;
        public const uint JOB_OBJECT_CPU_RATE_CONTROL_MIN_MAX_RATE = 0x00000010;
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetInformationJobObject(
            IntPtr hJob,
            JobObjectInfoType infoType,
            IntPtr lpJobObjectInfo,
            uint cbJobObjectInfoLength);
        public enum JobObjectInfoType : long
        {
            JobObjectBasicLimitInformation = 2L,
            JobObjectSecurityLimitInformation = 5L,
            JobObjectExtendedLimitInformation = 9L,
            JobObjectCpuRateControlInformation = 15L
        }
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AssignProcessToJobObject(IntPtr jobHandle, IntPtr processHandle);
        [StructLayout(LayoutKind.Sequential)]
        public struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public ulong PerProcessUserTimeLimit;
            public ulong PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass32;
            public uint SchedulingClass;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }
        public const uint JOB_OBJECT_LIMIT_PRIORITY_CLASS = 0x00000001;
        public const uint JOB_OBJECT_LIMIT_PROCESS_TIME = 0x00000002;
        public const uint JOB_OBJECT_LIMIT_JOB_TIME = 0x00000004;
        public const uint JOB_OBJECT_LIMIT_ACTIVE_PROCESS = 0x00000008;
        public const uint JOB_OBJECT_LIMIT_AFFINITY = 0x00000010;
        public const uint JOB_OBJECT_LIMIT_MEMORY_LIMIT = 0x00000020;
        public const uint JOB_OBJECT_SECURITY_NO_ADMIN = 0x00000004;
        public const uint JOB_OBJECT_SECURITY_ONLY_TOKEN = 0x00000008;
        public const uint JOB_OBJECT_SECURITY_FILTER_TOKENS = 0x00000010;
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern IntPtr LoadLibrary(string lpFileName);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool QueryInformationJobObject(
            IntPtr hJob,
            JobObjectInfoType JobObjectInfoClass,
            IntPtr lpJobObjectInformation,
            uint cbJobObjectInformationLength,
            out uint lpReturnLength
        );
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool IsProcessInJob(IntPtr processHandle, IntPtr jobHandle, out bool result);
    }
}
