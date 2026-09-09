using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ACEController.Win32API
{
    /// <summary>
    /// 提供获取CPU核心掩码的能力
    /// </summary>
    public static class CPUID
    {
        /// <summary>
        /// 枚举 <see cref="GetLogicalProcessorInformationEx"/> 函数的查询类型
        /// </summary>
        public enum LOGICAL_PROCESSOR_RELATIONSHIP
        {
            /// <summary>
            /// 指示查询物理核心信息
            /// </summary>
            RelationProcessorCore = 0
        }

        /// <summary>
        /// 包含处理器核心序列的结构体
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        public struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX
        {
            /// <summary>
            /// 数据类型 (当前环境无需读取)
            /// </summary>
            [FieldOffset(0)]
            public LOGICAL_PROCESSOR_RELATIONSHIP Relationship;
            /// <summary>
            /// 内部缓冲区大小
            /// </summary>
            [FieldOffset(4)]
            public uint Size;
            /// <summary>
            /// 包含具体信息的结构体
            /// </summary>
            [FieldOffset(8)]
            public PROCESSOR_RELATIONSHIP Processor;
        }

        /// <summary>
        /// 包含每个核心详细信息的结构体
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        public struct PROCESSOR_RELATIONSHIP
        {
            /// <summary>
            /// 位标志 (为0代表核心仅持有一个逻辑处理器，否则持有多个逻辑处理器)
            /// </summary>
            [FieldOffset(0)]
            public byte Flags;
            /// <summary>
            /// 核心效率值 (指示性能与功耗之间的内在权衡，效率值较高的核心本质上性能更高)
            /// </summary>
            [FieldOffset(1)]
            public byte EfficiencyClass;
            /// <summary>
            /// 处理器组数量
            /// </summary>
            [FieldOffset(22)]
            public ushort GroupCount;
            /// <summary>
            /// 处理器组掩码 (此结构数量不固定，由于仅处理单处理器组情况，故仅声明一个)
            /// </summary>
            [FieldOffset(24)]
            public GROUP_AFFINITY GroupMask;
        }

        /// <summary>
        /// 包含核心的组数据信息的结构体
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct GROUP_AFFINITY
        {
            /// <summary>
            /// 核心在处理器组中的掩码
            /// </summary>
            public UIntPtr Mask;
            /// <summary>
            /// 所在组的ID
            /// </summary>
            public ushort Group;
        }

        /// <summary>
        /// 内核查询函数，查询处理器信息
        /// </summary>
        /// <param name="RelationshipType">信息类型</param>
        /// <param name="Buffer">数据缓冲区</param>
        /// <param name="ReturnLength">返回的数据长度</param>
        /// <returns>返回函数是否执行成功</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetLogicalProcessorInformationEx(
            LOGICAL_PROCESSOR_RELATIONSHIP RelationshipType,
            IntPtr Buffer,
            ref uint ReturnLength);

        /// <summary>
        /// 获取单处理器组下的 <see href="ECore"/> 掩码
        /// </summary>
        /// <returns>返回执行结果</returns>
        public static short GetPCoreMaps(out ulong EMask)
        {
            // 初始化数据
            EMask = 0UL;
            uint bufferSize = 0;
            // 获取缓冲区大小
            GetLogicalProcessorInformationEx(LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore, IntPtr.Zero, ref bufferSize);
            if (bufferSize == 0) return (short)Marshal.GetLastWin32Error();
            // 读取内核信息
            IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);
            try
            {
                if (!GetLogicalProcessorInformationEx(LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore, buffer, ref bufferSize)) return (short)Marshal.GetLastWin32Error();
                int offset = 0;
                while (offset < bufferSize)
                {
                    // 按内存偏移读取
                    var header = Marshal.PtrToStructure<SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX>(
                        IntPtr.Add(buffer, offset));
                    if (header.Processor.GroupCount > 1) return -1;
                    if (header.Processor.Flags == 0)
                    {
                        EMask |= header.Processor.GroupMask.Mask.ToUInt64();
                    }
                    // 移动到下一个偏移量
                    offset += (int)header.Size;
                }
                return 0;
            }
            catch (Exception)
            {
                return -2;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }
}
