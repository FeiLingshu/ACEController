using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using static ACEController.Win32API.API.CoreAPI;

namespace ACEController.Win32API
{
    public static class Core
    {
        /// <summary>
        /// 获取核心参数并生成Map
        /// </summary>
        /// <returns>返回核心Map映射</returns>
        public static ulong GetCoreMap()
        {
            try
            {
                ulong basic = 0UL;
                using (Process self = Process.GetCurrentProcess())
                {
                    _ = GetBasicMap(self.Handle, out basic);
                }
                short report = CPUID.GetPCoreMaps(out ulong map);
                if (report == 0 && map > 0 && map < basic)
                {
                    return map;
                }
                else
                {
                    return 1UL << (Environment.ProcessorCount - 1);
                }
            }
            catch (Exception)
            {
                return 1UL << (Environment.ProcessorCount - 1);
            }
        }

        /// <summary>
        /// 获取核心亲和性掩码数据
        /// </summary>
        /// <param name="hProcess">目标进程句柄</param>
        /// <param name="mask">可用最大掩码</param>
        /// <returns>返回当前核心掩码</returns>
        private static ulong GetBasicMap(IntPtr hProcess, out ulong mask)
        {
            if (GetProcessAffinityMask(hProcess, out UIntPtr map, out UIntPtr _mask))
            {
                mask = _mask.ToUInt64();
                return map.ToUInt64();
            }
            mask = 0UL;
            return 0UL;
        }

        /// <summary>
        /// 设置指定进程的CPU核心亲和性
        /// </summary>
        /// <param name="pid">目标进程ID</param>
        /// <param name="map">包含核心信息的Map映射</param>
        /// <returns>返回操作是否成功</returns>
        public static bool SetProcess(int pid, ulong map)
        {
            IntPtr hProcess = OpenProcess(
                ProcessAccessFlags.PROCESS_SET_INFORMATION | ProcessAccessFlags.PROCESS_QUERY_INFORMATION,
                false,
                pid);
            try
            {
                if (hProcess == IntPtr.Zero)
                {
                    return false;
                }
                if (SetProcessAffinityMask(hProcess, map) == 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            finally
            {
                CloseHandle(hProcess);
            }
        }

        /// <summary>
        /// 设置当前线程的CPU核心亲和性
        /// </summary>
        /// <param name="map">包含核心信息的Map映射</param>
        /// <returns>返回操作是否成功</returns>
        public static bool SetThread(ulong map)
        {
            IntPtr thwnd = GetCurrentThread();
            try
            {
                if (thwnd == IntPtr.Zero)
                {
                    return false;
                }
                if (SetThreadAffinityMask(thwnd, map) == 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            finally
            {
                // Do Nothing ...
            }
        }

        /// <summary>
        /// 强制重新分配CPU核心
        /// </summary>
        /// <returns>返回重新分配后当前线程所在的核心编号</returns>
        public static int ReFlash()
        {
            SwitchToThread();
            return GetCurrentProcessorNumber();
        }

        /// <summary>
        /// 获取掩码中包含的CPU核心数量
        /// </summary>
        /// <param name="mask">CPU核心掩码</param>
        /// <param name="flag">指示CPU核心掩码是否有效</param>
        /// <returns>返回掩码中包含的CPU核心数量</returns>
        public static int CountEnabledCores(ulong mask, bool flag)
        {
            if (!flag)
            {
                return Environment.ProcessorCount;
            }
            int count = 0;
            while (mask != 0)
            {
                count += (int)(mask & 1);
                mask >>= 1;
            }
            return count;
        }
    }
}
