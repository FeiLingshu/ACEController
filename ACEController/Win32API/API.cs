using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace ACEController.Win32API
{
    public static class API
    {
        /// <summary>
        /// 表示窗口样式
        /// </summary>
        public const int GWL_STYLE = -16;

        /// <summary>
        /// 表示窗口具有大小调整边框
        /// </summary>
        public const long WS_THICKFRAME = 0x00040000L;

        /// <summary>
        /// 表示窗口具有最大化窗口
        /// </summary>
        public const long WS_MAXIMIZEBOX = 0x00010000L;

        /// <summary>
        /// 获取窗口属性
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="nIndex">属性类型索引</param>
        /// <returns>返回当前窗口属性</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern long GetWindowLongPtr(IntPtr hWnd, long nIndex);

        /// <summary>
        /// 设置窗口属性
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="nIndex">属性类型索引</param>
        /// <param name="dwNewLong">新的窗口属性</param>
        /// <returns>返回之前的窗口属性</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern long SetWindowLongPtr(IntPtr hWnd, int nIndex, long dwNewLong);

        /// <summary>
        /// 获取窗口菜单句柄
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="bRevert">是否恢复到保存的窗口菜单副本</param>
        /// <returns>窗口菜单句柄</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        /// <summary>
        /// 表示窗口移动菜单项
        /// </summary>
        public const int SC_MOVE = 0xF010;
        /// <summary>
        /// 表示窗口调整大小菜单项
        /// </summary>
        public const int SC_SIZE = 0xF000;
        /// <summary>
        /// 表示窗口关闭菜单项
        /// </summary>
        public const int SC_CLOSE = 0xF060;
        /// <summary>
        /// 表示控制台窗口菜单项#1
        /// </summary>
        public const int SC_DOSA = 0xFFF8;
        /// <summary>
        /// 表示控制台窗口菜单项#2
        /// </summary>
        public const int SC_DOSB = 0xFFF7;
        /// <summary>
        /// 表示通过命令常量查找菜单项
        /// </summary>
        public const int MF_BYCOMMAND = 0;

        /// <summary>
        /// 移除窗口菜单的菜单项
        /// </summary>
        /// <param name="hMenu">窗口菜单句柄</param>
        /// <param name="nPos">窗口菜单项的索引/常量</param>
        /// <param name="flags">指示查找菜单项的方式</param>
        /// <returns>返回操作是否成功</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RemoveMenu(IntPtr hMenu, int nPos, int flags);

        #region 控制台相关互操作声明

        /// <summary>
        /// 获取控制台窗口句柄
        /// </summary>
        /// <returns>返回当前进程绑定的控制台窗口句柄</returns>
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();

        /// <summary>
        /// 为当前进程绑定新的控制台窗口
        /// </summary>
        /// <returns>返回操作是否成功</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AllocConsole();

        /// <summary>
        /// 指示绑定目标为当前进程
        /// </summary>
        public const uint ATTACH_PARENT_PROCESS = 0xFFFFFFFF;

        /// <summary>
        /// 将现有的控制台窗口绑定到当前进程
        /// </summary>
        /// <param name="dwDesiredAccess">指示绑定目标</param>
        /// <returns>返回操作是否成功</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AttachConsole(uint dwDesiredAccess);

        /// <summary>
        /// 释放(解除绑定)当前进程的控制台窗口
        /// </summary>
        /// <returns>返回操作是否成功</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FreeConsole();

        /// <summary>
        /// 获取指定进程的父进程
        /// <para>该函数为内核函数 &lt;- [NtQueryInformationProcess 在 Windows 的未来版本中可能已更改或不可用。 应用程序应使用本主题中列出的备用函数。]</para>
        /// <a href="https://learn.microsoft.com/zh-cn/windows/win32/api/winternl/nf-winternl-ntqueryinformationprocess">MSDN页面</a>
        /// </summary>
        /// <param name="processHandle">目标进程的句柄</param>
        /// <param name="processInformationClass">要检索的进程信息的类型</param>
        /// <param name="processInformation">缓冲区指针</param>
        /// <param name="processInformationLength">缓冲区大小</param>
        /// <param name="returnLength">函数返回所请求信息的大小(指针)</param>
        /// <returns>返回NTSTATUS成功或错误代码</returns>
        [DllImport("ntdll.dll")]
        public static extern int NtQueryInformationProcess(
            IntPtr processHandle,
            int processInformationClass,
            ref PROCESS_BASIC_INFORMATION processInformation,
            uint processInformationLength,
            out uint returnLength);

        /// <summary>
        /// 声明win32结构PROCESS_BASIC_INFORMATION
        /// </summary>
        public struct PROCESS_BASIC_INFORMATION
        {
            /// <summary>
            /// 进程退出代码(ExitStatus)
            /// <para>为了清晰和安全起见，最好使用 GetExitCodeProcess</para>
            /// </summary>
            public IntPtr Reserved1;
            /// <summary>
            /// 指向PEB结构
            /// </summary>
            public IntPtr PebBaseAddress;
            /// <summary>
            /// _(AffinityMask)
            /// <para>可以强制转换为DWORD，并且包含GetProcessAffinityMask为lpProcessAffinityMask参数返回的相同值</para>
            /// </summary>
            public IntPtr Reserved2_0;
            /// <summary>
            /// 进程优先级(BasePriority)
            /// </summary>
            public IntPtr Reserved2_1;
            /// <summary>
            /// 查询过程的唯一标识符
            /// </summary>
            public IntPtr UniqueProcessId;
            /// <summary>
            /// 父进程的唯一标识符
            /// </summary>
            public IntPtr InheritedFromUniqueProcessId;
        }

        /// <summary>
        /// 指示标准输出流
        /// </summary>
        public const int STD_INPUT_HANDLE = -10;
        /// <summary>
        /// 指示标准输出流
        /// </summary>
        public const int STD_OUTPUT_HANDLE = -11;
        /// <summary>
        /// 指示标准错误流
        /// </summary>
        public const int STD_ERROR_HANDLE = -12;
        /// <summary>
        /// 指示无效句柄
        /// </summary>
        public static readonly IntPtr INVALID_HANDLE_VALUE = (IntPtr)(-1);

        /// <summary>
        /// 获取当前进程指定标准设备的句柄
        /// </summary>
        /// <param name="nStdHandle">标准设备的类型</param>
        /// <returns>返回当前进程指定标准设备的句柄</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetStdHandle(int nStdHandle);

        /// <summary>
        /// 设置当前进程指定标准设备的句柄
        /// </summary>
        /// <param name="nStdHandle">标准设备的类型</param>
        /// <param name="hHandle">流句柄</param>
        /// <returns>返回操作是否成功</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetStdHandle(int nStdHandle, IntPtr hHandle);

        /// <summary>
        /// 关闭句柄
        /// </summary>
        /// <param name="hObject">目标句柄</param>
        /// <returns>返回操作是否成功</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// 配置控制台代码页
        /// </summary>
        /// <param name="wCodePageID">代码页ID</param>
        /// <returns>返回操作是否成功</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleOutputCP(uint wCodePageID);

        /// <summary>
        /// 声明win32结构CONSOLE_FONT_INFO_EX
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct CONSOLE_FONT_INFO_EX
        {
            /// <summary>
            /// 结构大小
            /// </summary>
            public int cbSize;
            /// <summary>
            /// 系统控制台字体表中字体的索引
            /// </summary>
            public uint nFont;
            /// <summary>
            /// 字符宽度和高度信息
            /// </summary>
            public COORD dwFontSize;
            /// <summary>
            /// 字体间距和家族
            /// </summary>
            public int FontFamily;
            /// <summary>
            /// 字体粗细
            /// </summary>
            public int FontWeight;
            /// <summary>
            /// 字体名称
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string FaceName;
        }

        /// <summary>
        /// 设置控制台字体
        /// </summary>
        /// <param name="hConsoleOutput">控制台标准输出流句柄</param>
        /// <param name="bMaximumWindow">?是否设置最大窗口大小的字体信息</param>
        /// <param name="lpConsoleCurrentFont">包含字体信息的win32结构lpConsoleCurrentFont</param>
        /// <returns>返回操作是否成功</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetCurrentConsoleFontEx(
            IntPtr hConsoleOutput, bool bMaximumWindow, ref CONSOLE_FONT_INFO_EX lpConsoleCurrentFont);

        /// <summary>
        /// 声明win32结构COORD
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct COORD
        {
            /// <summary>
            /// X维度的值(视调用方不同存在不同含义)
            /// </summary>
            public short X;
            /// <summary>
            /// Y维度的值(视调用方不同存在不同含义)
            /// </summary>
            public short Y;
        }

        /// <summary>
        /// 声明win32结构SMALL_RECT
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct SMALL_RECT
        {
            /// <summary>
            /// 左边距
            /// </summary>
            public short Left;
            /// <summary>
            /// 上边距
            /// </summary>
            public short Top;
            /// <summary>
            /// 右边距
            /// </summary>
            public short Right;
            /// <summary>
            /// 下边距
            /// </summary>
            public short Bottom;
        }

        /// <summary>
        /// 设置控制台缓冲区大小
        /// </summary>
        /// <param name="hConsoleOutput">目标控制台的标准输出流句柄</param>
        /// <param name="dwSize">大小信息</param>
        /// <returns>返回操作是否成功</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleScreenBufferSize(
            IntPtr hConsoleOutput, COORD dwSize);

        /// <summary>
        /// 设置控制台显示区域大小
        /// </summary>
        /// <param name="hConsoleOutput">目标控制台的标准输出流句柄</param>
        /// <param name="bAbsolute">指示使用控制台参考系(true)还是屏幕参考系(false)</param>
        /// <param name="lpConsoleWindow">大小信息</param>
        /// <returns>返回操作是否成功</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleWindowInfo(
            IntPtr hConsoleOutput, bool bAbsolute, ref SMALL_RECT lpConsoleWindow);

        /// <summary>
        /// 指示 Ctrl+C 由系统处理，且不会放入输入缓冲区中
        /// </summary>
        public const uint ENABLE_PROCESSED_INPUT = 0x0001;
        /// <summary>
        /// 指示用户可通过此标志使用鼠标选择和编辑文本
        /// </summary>
        public const uint ENABLE_QUICK_EDIT_MODE = 0x0040;
        /// <summary>
        /// 指示允许控制台处理 ASCII 控制序列
        /// </summary>
        public const uint ENABLE_PROCESSED_OUTPUT = 0x0001;
        /// <summary>
        /// 指示启用虚拟终端的ANSI转义序列支持
        /// </summary>
        public const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

        /// <summary>
        /// 获取控制台模式
        /// </summary>
        /// <param name="hConsoleHandle">控制台标准设备句柄</param>
        /// <param name="dwMode">当前控制台的模式数据</param>
        /// <returns>返回操作是否成功</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint dwMode);

        /// <summary>
        /// 设置控制台模式
        /// </summary>
        /// <param name="hConsoleHandle">控制台标准设备句柄</param>
        /// <param name="dwMode">要设置的控制台的模式数据</param>
        /// <returns>返回操作是否成功</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        /// <summary>
        /// 表示Ctrl+C组合键
        /// </summary>
        public const int CTRL_C_EVENT = 0;
        /// <summary>
        /// 表示Ctrl+Break(Pause)组合键
        /// </summary>
        public const int CTRL_BREAK_EVENT = 1;

        /// <summary>
        /// 声明win32委托ConsoleCtrlDelegate
        /// </summary>
        /// <param name="ctrlType">控制台按键触发类型</param>
        /// <returns>返回操作是否由处理程序处理</returns>
        public delegate bool ConsoleCtrlDelegate(int ctrlType);

        /// <summary>
        /// 添加控制台事件处理函数
        /// </summary>
        /// <param name="handler">win32委托ConsoleCtrlDelegate实例</param>
        /// <param name="add">指示操作是否为添加(否则移除对应的处理函数)</param>
        /// <returns>返回操作是否成功</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate handler, bool add);

        /// <summary>
        /// 声明win32结构CONSOLE_SCREEN_BUFFER_INFOEX
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct CONSOLE_SCREEN_BUFFER_INFOEX
        {
            /// <summary>
            /// 结构在内存中的大小
            /// </summary>
            public uint cbSize;
            /// <summary>
            /// 控制台缓冲区大小
            /// </summary>
            public COORD dwSize;
            /// <summary>
            /// 控制台光标在缓冲区中的坐标
            /// </summary>
            public COORD dwCursorPosition;
            /// <summary>
            /// 控制台缓冲区的字符属性
            /// </summary>
            public ushort wAttributes;
            /// <summary>
            /// 控制台的显示范围
            /// </summary>
            public SMALL_RECT srWindow;
            /// <summary>
            /// 控制台窗口的最大大小
            /// </summary>
            public COORD dwMaximumWindowSize;
            /// <summary>
            /// 控制台弹出窗口的填充属性
            /// </summary>
            public ushort wPopupAttributes;
            /// <summary>
            /// 指示是否支持全屏模式
            /// </summary>
            public bool bFullscreenSupported;
            /// <summary>
            /// 控制台16色颜色列表
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public uint[] ColorTable;
        }

        /// <summary>
        /// 获取控制台标准设备的屏幕缓冲区信息
        /// </summary>
        /// <param name="hConsoleOutput">控制台标准设备句柄</param>
        /// <param name="lpConsoleScreenBufferInfoEx">win32结构CONSOLE_SCREEN_BUFFER_INFOEX实例</param>
        /// <returns>返回操作是否成功</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetConsoleScreenBufferInfoEx(IntPtr hConsoleOutput, ref CONSOLE_SCREEN_BUFFER_INFOEX lpConsoleScreenBufferInfoEx);

        /// <summary>
        /// 设置控制台标准设备的屏幕缓冲区信息
        /// </summary>
        /// <param name="hConsoleOutput">控制台标准设备句柄</param>
        /// <param name="lpConsoleScreenBufferInfoEx">win32结构CONSOLE_SCREEN_BUFFER_INFOEX实例</param>
        /// <returns>返回操作是否成功</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleScreenBufferInfoEx(IntPtr hConsoleOutput, ref CONSOLE_SCREEN_BUFFER_INFOEX lpConsoleScreenBufferInfoEx);

        #endregion

        #region 控制台相关互操作声明（补充）

        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint OPEN_EXISTING = 3;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        #endregion

        #region DWM互操作声明

        /// <summary>
        /// 获取DWM是否启用
        /// </summary>
        /// <returns>返回当前设备中DWM的启用情况</returns>
        [DllImport("Dwmapi.dll", ExactSpelling = true, PreserveSig = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DwmIsCompositionEnabled();

        /// <summary>
        /// 声明win32枚举DWMWINDOWATTRIBUTE
        /// </summary>
        public enum DWMWINDOWATTRIBUTE : uint
        {
            /// <summary>
            /// 获取当前DWM状态
            /// </summary>
            DWMWA_NCRENDERING_ENABLED = 1,
            /// <summary>
            /// 配置DWM状态
            /// </summary>
            DWMWA_NCRENDERING_POLICY = 2,
            /// <summary>
            /// 配置是否允许渲染工作区
            /// </summary>
            DWMWA_ALLOW_NCPAINT = 3,
            /// <summary>
            /// 配置是否同步系统暗色模式配置(17763~18985)
            /// </summary>
            DWMWA_USE_IMMERSIVE_DARK_MODE_20H1 = 19,
            /// <summary>
            /// 配置是否同步系统暗色模式配置
            /// </summary>
            DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
            /// <summary>
            /// 配置窗口圆角参数
            /// </summary>
            DWMWA_WINDOW_CORNER_PREFERENCE = 33,
            /// <summary>
            /// 配置窗口标题栏颜色
            /// </summary>
            DWMWA_CAPTION_COLOR = 35,
            /// <summary>
            /// 配置窗口标题文本颜色
            /// </summary>
            DWMWA_TEXT_COLOR = 36
        }

        /// <summary>
        /// 声明win32枚举DWMNCRENDERINGPOLICY
        /// </summary>
        public enum DWMNCRENDERINGPOLICY : uint
        {
            /// <summary>
            /// 使用系统DWM配置
            /// </summary>
            DWMNCRP_USEWINDOWSTYLE = 0,
            /// <summary>
            /// 禁用DWM
            /// </summary>
            DWMNCRP_DISABLED = 1,
            /// <summary>
            /// 启用DWM
            /// </summary>
            DWMNCRP_ENABLED = 2
        }

        /// <summary>
        /// 声明win32枚举DWM_WINDOW_CORNER_PREFERENCE
        /// </summary>
        public enum DWM_WINDOW_CORNER_PREFERENCE : uint
        {
            /// <summary>
            /// 默认圆角风格
            /// </summary>
            DWMWCP_DEFAULT = 0,
            /// <summary>
            /// 无圆角
            /// </summary>
            DWMWCP_SQUARE = 1,
            /// <summary>
            /// 标准圆角
            /// </summary>
            DWMWCP_ROUND = 2,
            /// <summary>
            /// 较小圆角
            /// </summary>
            DWMWCP_ROUNDSMALL = 3
        }

        /// <summary>
        /// 声明win32结构MARGINS
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct MARGINS
        {
            /// <summary>
            /// 左边距
            /// </summary>
            public int Left;
            /// <summary>
            /// 右边距
            /// </summary>
            public int Right;
            /// <summary>
            /// 上边距
            /// </summary>
            public int Top;
            /// <summary>
            /// 下边距
            /// </summary>
            public int Bottom;
        }

        /// <summary>
        /// 获取/设置指定窗口的DWM配置
        /// </summary>
        /// <param name="hwnd">目标窗口句柄</param>
        /// <param name="dwAttribute">要获取/设置的DWM属性枚举</param>
        /// <param name="pvAttribute">承载DWM属性值的特定数据类型实例</param>
        /// <param name="cbAttribute">属性值的内存大小</param>
        /// <returns>返回操作是否成功(S_OK为成功，否则失败)</returns>
        [DllImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute", PreserveSig = true)]
        public static extern int DwmGetWindowAttribute(
            IntPtr hwnd,
            DWMWINDOWATTRIBUTE dwAttribute,
            ref bool pvAttribute,
            uint cbAttribute);

        /// <summary>
        /// 获取/设置指定窗口的DWM配置
        /// </summary>
        /// <param name="hwnd">目标窗口句柄</param>
        /// <param name="dwAttribute">要获取/设置的DWM属性枚举</param>
        /// <param name="pvAttribute">承载DWM属性值的特定数据类型实例</param>
        /// <param name="cbAttribute">属性值的内存大小</param>
        /// <returns>返回操作是否成功(S_OK为成功，否则失败)</returns>
        [DllImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute", PreserveSig = true)]
        public static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            DWMWINDOWATTRIBUTE dwAttribute,
            ref bool pvAttribute,
            uint cbAttribute);

        /// <summary>
        /// 获取/设置指定窗口的DWM配置
        /// </summary>
        /// <param name="hwnd">目标窗口句柄</param>
        /// <param name="dwAttribute">要获取/设置的DWM属性枚举</param>
        /// <param name="pvAttribute">承载DWM属性值的特定数据类型实例</param>
        /// <param name="cbAttribute">属性值的内存大小</param>
        /// <returns>返回操作是否成功(S_OK为成功，否则失败)</returns>
        [DllImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute", PreserveSig = true)]
        public static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            DWMWINDOWATTRIBUTE dwAttribute,
            ref uint pvAttribute,
            uint cbAttribute);

        /// <summary>
        /// 获取/设置指定窗口的DWM配置
        /// </summary>
        /// <param name="hwnd">目标窗口句柄</param>
        /// <param name="dwAttribute">要获取/设置的DWM属性枚举</param>
        /// <param name="pvAttribute">承载DWM属性值的特定数据类型实例</param>
        /// <param name="cbAttribute">属性值的内存大小</param>
        /// <returns>返回操作是否成功(S_OK为成功，否则失败)</returns>
        [DllImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute", PreserveSig = true)]
        public static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            DWMWINDOWATTRIBUTE dwAttribute,
            ref int pvAttribute,
            uint cbAttribute);

        /// <summary>
        /// 设置窗口工作区包含的窗口边框
        /// </summary>
        /// <param name="hwnd">目标窗口句柄</param>
        /// <param name="margins">指示窗口边框的win32结构MARGINS</param>
        /// <returns>返回操作是否成功(S_OK为成功，否则失败)</returns>
        [DllImport("dwmapi.dll", EntryPoint = "DwmExtendFrameIntoClientArea", PreserveSig = true)]
        public static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

        /// <summary>
        /// 指示DWM函数操作成功的值
        /// </summary>
        public const int S_OK = 0;

        #endregion



        /// <summary>
        /// win32param::HWND_TOPMOST
        /// </summary>
        public static readonly IntPtr HWND_TOPMOST = (IntPtr)(-1);


        /// <summary>
        /// 阻止生成 WM_SYNCPAINT 消息
        /// </summary>
        public const int SWP_DEFERERASE = 0x2000;
        /// <summary>
        /// 丢弃工作区的整个内容
        /// </summary>
        public const int SWP_NOCOPYBITS = 0x0100;
        /// <summary>
        /// 指示不改变窗口Z序
        /// </summary>
        public const int SWP_NOZORDER = 0x0004;
        /// <summary>
        /// 指示不改变窗口位置
        /// </summary>
        public const int SWP_NOMOVE = 0x0002;
        /// <summary>
        /// 指示不改变窗口大小
        /// </summary>
        public const int SWP_NOSIZE = 0x0001;
        /// <summary>
        /// 指示不激活窗口
        /// </summary>
        public const int SWP_NOACTIVATE = 0x0010;
        /// <summary>
        /// 指示不进行重绘
        /// </summary>
        public const int SWP_NOREDRAW = 0x0008;
        /// <summary>
        /// 阻止窗口接收WM_WINDOWPOSCHANGING消息
        /// </summary>
        public const int SWP_NOSENDCHANGING = 0x0400;
        /// <summary>
        /// 指示进行异步操作
        /// </summary>
        public const int SWP_ASYNCWINDOWPOS = 0x4000;
        /// <summary>
        /// 指示重新计算窗口框架
        /// </summary>
        public const int SWP_FRAMECHANGED = 0x0020;
        /// <summary>
        /// 指示窗口需要设置为显示状态
        /// </summary>
        public const uint SWP_SHOWWINDOW = 0x0040;

        /// <summary>
        /// win32api::SetWindowPos
        /// </summary>
        /// <param name="hWnd">param#1</param>
        /// <param name="hWndInsertAfter">param#2</param>
        /// <param name="X">param#3</param>
        /// <param name="Y">param#4</param>
        /// <param name="cx">param#5</param>
        /// <param name="cy">param#6</param>
        /// <param name="uFlags">param#7</param>
        /// <returns>returns</returns>
        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags
        );



        /// <summary>
        /// 修复.NET框架中Console类问题的公开类
        /// </summary>
        public static class BugFix
        {
            /// <summary>
            /// 存储反射实例的内部字段
            /// </summary>
            private static FieldInfo handleField_out = null;
            /// <summary>
            /// 存储反射实例的内部字段
            /// </summary>
            private static FieldInfo handleField_in = null;

            /// <summary>
            /// 通过反射强制刷新内部ConsoleHandle值
            /// <para>
            /// 1.在通过AllocConsole函数绑定控制台后，必须执行Console.SetOut()/Console.SetIn()/Console.SetError()刷新标准设备句柄<br/>
            /// 2.默认Console类中，_consoleOutputHandle/_consoleInputHandle的值一经读取将会永不变化
            /// (由于其使用属性值承载，属性内部只包含get索引器，且get检测到相关值不为空时会直接输出)<br/>
            /// 3.调用FreeConsole释放控制台并通过AllocConsole重新分配新的控制台时，标准设备句柄已经变化，但默认Console类中根本不存在任何能够实现刷新内部缓存的方法<br/>
            /// 4.综上：只要初始控制台被释放，接下来所有的控制台调用Console类内部方法时都会抛出"无效句柄"异常<br/>
            /// 5.由于相关字段为私有字段，只能使用反射的方法实现手动刷新缓存数据
            /// </para>
            /// <para>注意：执行反射会消耗较长时间，代码应在单独线程中执行</para>
            /// </summary>
            /// <param name="reset">是否重置相关字段，一般情况下仅在调试时使用</param>
            public static void RefreshConsoleHandle(bool reset)
            {
                try
                {
                    // 获取Console类的Type对象
                    Type consoleType = typeof(Console);
                    // 获取私有静态字段
                    if (handleField_out == null) handleField_out =
                        consoleType.GetField("_consoleOutputHandle", BindingFlags.Static | BindingFlags.NonPublic);
                    if (handleField_in == null) handleField_in =
                        consoleType.GetField("_consoleInputHandle", BindingFlags.Static | BindingFlags.NonPublic);
                    // 配置私有静态字段
                    if (reset && is_Redirected)
                    {
                        // 获取标准设备句柄
                        IntPtr consoleOut = CreateFile("CONOUT$", GENERIC_READ | GENERIC_WRITE,
                            FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
                        IntPtr consoleIn = CreateFile("CONIN$", GENERIC_READ | GENERIC_WRITE,
                            FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
                        hwnds[0] = consoleOut;
                        hwnds[1] = consoleIn;
                        // 重定向win32流
                        SetStdHandle(STD_OUTPUT_HANDLE, consoleOut);
                        SetStdHandle(STD_ERROR_HANDLE, consoleOut);
                        SetStdHandle(STD_INPUT_HANDLE, consoleIn);
                        // 重新配置内部字段
                        handleField_out?.SetValue(null, consoleOut); // 需要移除JIT编译器的类型安全检查
                        handleField_in?.SetValue(null, consoleIn);
                        // 重定向.Net流
                        Console.SetOut(new StreamWriter(Console.OpenStandardOutput(), utf8) { AutoFlush = true });
                        Console.SetIn(new StreamReader(Console.OpenStandardInput(), utf8));
                        Console.SetError(new StreamWriter(Console.OpenStandardOutput(), utf8) { AutoFlush = true });
                    }
                    else
                    {
                        // 获取控制台数据流
                        IntPtr stdOutHandle = GetStdHandle(STD_OUTPUT_HANDLE);
                        IntPtr stdErrorHandle = GetStdHandle(STD_ERROR_HANDLE);
                        IntPtr stdInputHandle = GetStdHandle(STD_INPUT_HANDLE);
                        // 重定向win32流
                        SetStdHandle(STD_OUTPUT_HANDLE, stdOutHandle);
                        SetStdHandle(STD_ERROR_HANDLE, stdOutHandle);
                        SetStdHandle(STD_INPUT_HANDLE, stdInputHandle);
                        // 重新配置内部字段
                        handleField_out?.SetValue(null, stdOutHandle); // 需要移除JIT编译器的类型安全检查
                        handleField_in?.SetValue(null, stdInputHandle);
                        // 重定向.Net流
                        Console.SetOut(new StreamWriter(Console.OpenStandardOutput(), utf8) { AutoFlush = true });
                        Console.SetIn(new StreamReader(Console.OpenStandardInput(), utf8));
                        Console.SetError(new StreamWriter(Console.OpenStandardOutput(), utf8) { AutoFlush = true });
                    }
                }
                catch (Exception)
                {
                    FreeConsole();
                    throw;
                }
            }

            /// <summary>
            /// 初始化所有反射资源
            /// </summary>
            public static void Initialize()
            {
                Type consoleType = typeof(Console);
                handleField_out =
                    consoleType.GetField("_consoleOutputHandle", BindingFlags.Static | BindingFlags.NonPublic);
                handleField_in =
                    consoleType.GetField("_consoleInputHandle", BindingFlags.Static | BindingFlags.NonPublic);
            }

            /// <summary>
            /// 执行预操作（需在调用 win32api::AllocConsole 前进行）
            /// </summary>
            private static void Pre()
            {
                IntPtr stdOutHandle = GetStdHandle(STD_OUTPUT_HANDLE);
                IntPtr stdErrorHandle = GetStdHandle(STD_ERROR_HANDLE);
                IntPtr stdInputHandle = GetStdHandle(STD_INPUT_HANDLE);
                bool _ = true;
                _ &= (stdOutHandle == IntPtr.Zero || stdOutHandle == INVALID_HANDLE_VALUE);
                _ &= (stdErrorHandle == IntPtr.Zero || stdErrorHandle == INVALID_HANDLE_VALUE);
                _ &= (stdInputHandle == IntPtr.Zero || stdInputHandle == INVALID_HANDLE_VALUE);
                is_Redirected = !_;
            }

            /// <summary>
            /// 安全创建控制台窗口
            /// </summary>
            /// <returns>返回操作是否成功</returns>
            public static bool CreatConsole()
            {
                Pre();
                return AllocConsole();
            }

            /// <summary>
            /// 切换控制台编码方式
            /// </summary>
            /// <param name="codepage">目标代码页</param>
            public static void SetCodePage(uint codepage)
            {
                var coder = Encoding.GetEncoding((int)codepage);
                Console.OutputEncoding = coder;
                Console.InputEncoding = coder;
            }

            /// <summary>
            /// 缓存UTF-8编码器实例
            /// </summary>
            public static readonly Encoding utf8 = new UTF8Encoding(false);

            /// <summary>
            /// 标志位：指示句柄是否被重定向
            /// </summary>
            private static bool is_Redirected = false;

            /// <summary>
            /// 保存新句柄信息
            /// </summary>
            private static readonly IntPtr[] hwnds = new IntPtr[2];

            /// <summary>
            /// 安全关闭控制台窗口
            /// </summary>
            public static void CloseConsole()
            {
                FreeConsole();
                if (hwnds != null)
                {
                    foreach (var hwnd in hwnds)
                    {
                        if (hwnd != IntPtr.Zero && hwnd != INVALID_HANDLE_VALUE)
                        {
                            CloseHandle(hwnd);
                        }
                    }
                    hwnds[0] = IntPtr.Zero;
                    hwnds[1] = IntPtr.Zero;
                }
            }
        }



        /// <summary>
        /// 用于配置控制台窗口参数的公开类
        /// </summary>
        /// <param name="consolehwnd">目标控制台窗口</param>
        /// <exception cref="ArgumentNullException">控制台窗口句柄为空</exception>
        public static void SetConsole(IntPtr consolehwnd)
        {
            // 检查控制台窗口句柄
            if (consolehwnd == IntPtr.Zero)
            {
                throw new ArgumentNullException("未识别到正确的控制台窗口句柄。");
            }
            // 配置控制台窗口
            Console.Title = "...";
            IntPtr menuhwnd = GetSystemMenu(consolehwnd, false);
            if (menuhwnd != IntPtr.Zero)
            {
                RemoveMenu(menuhwnd, SC_SIZE, MF_BYCOMMAND);
                RemoveMenu(menuhwnd, SC_CLOSE, MF_BYCOMMAND);
                RemoveMenu(menuhwnd, SC_DOSA, MF_BYCOMMAND);
                RemoveMenu(menuhwnd, SC_DOSB, MF_BYCOMMAND);
            }
            long style = GetWindowLongPtr(consolehwnd, GWL_STYLE);
            if (style != 0L)
            {
                SetWindowLongPtr(consolehwnd, GWL_STYLE, style & ~WS_MAXIMIZEBOX & ~WS_THICKFRAME);
                SetWindowPos(consolehwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);
            }
            if (!Debugger.IsAttached)
            {
                try
                {
                    Console.Clear(); // 前置易引发异常的代码，预测后续代码是否能够成功执行，防止执行中意外中断
                    IntPtr hConsole = GetStdHandle(STD_OUTPUT_HANDLE);
                    if (hConsole != IntPtr.Zero && hConsole != INVALID_HANDLE_VALUE)
                    {
                        // 配置控制台代码页
                        SetConsoleOutputCP(65001U);
                        // 通过win32API修改控制台(及缓冲区)大小
                        // 使用Console类内置方法依然会抛出异常(句柄无效)，具体原因未知
                        // 经测试：Console类中打印字符串的方法不受影响
                        (short console_w, short console_h) = (40, 11);
                        COORD BSIZE = new()
                        {
                            X = short.MaxValue - 1,
                            Y = short.MaxValue - 1,
                        };
                        SMALL_RECT CRECT = new()
                        {
                            Left = 0,
                            Top = 0,
                            Right = (short)(console_w - 1),
                            Bottom = (short)(console_h - 1)
                        };
                        SetConsoleScreenBufferSize(hConsole, BSIZE);
                        SetConsoleWindowInfo(hConsole, true, ref CRECT);
                        BSIZE.X = console_w;
                        SetConsoleScreenBufferSize(hConsole, BSIZE);
                        Version sysver = Environment.OSVersion.Version;
                        // 根据版本决定是否进行额外操作（Windows Vista +）
                        if (sysver >= new Version(6, 0, 6000))
                        {
                            // 配置控制台字体
                            CONSOLE_FONT_INFO_EX info = new()
                            {
                                cbSize = Marshal.SizeOf(typeof(CONSOLE_FONT_INFO_EX)),
                                nFont = 0U,
                                dwFontSize = new COORD { X = 0, Y = 16 },
                                FontFamily = 0,
                                FontWeight = 500,
                                FaceName = "Consolas"
                            };
                            SetCurrentConsoleFontEx(hConsole, true, ref info);
                            SetCurrentConsoleFontEx(hConsole, false, ref info);
                            // 调整控制台模式，启用扩展模式
                            if (GetConsoleMode(hConsole, out uint mode))
                            {
                                mode |= ENABLE_PROCESSED_OUTPUT;
                                mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
                                SetConsoleMode(hConsole, mode);
                            }
                            // 修改控制台默认颜色（0=Black, 7=White）
                            CONSOLE_SCREEN_BUFFER_INFOEX csbi = new()
                            {
                                cbSize = (uint)Marshal.SizeOf(typeof(CONSOLE_SCREEN_BUFFER_INFOEX)),
                                ColorTable = new uint[16]
                            };
                            GetConsoleScreenBufferInfoEx(hConsole, ref csbi);
                            csbi.ColorTable[0] = 0x00202020;
                            csbi.ColorTable[7] = 0x00E9E9E9;
                            SetConsoleScreenBufferInfoEx(hConsole, ref csbi);
                        }
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }
                catch (Exception)
                {
                    // Do Nothing ...
                }
            }
        }




        /// <summary>
        /// 用于配置窗口DWM参数的公开类
        /// </summary>
        public static class DWMEX
        {
            /// <summary>
            /// 配置窗口DWM参数
            /// </summary>
            /// <param name="window">目标窗口句柄</param>
            /// <param name="C2NC">是否允许工作区绘制</param>
            /// <param name="DARK">是否允许同步暗色模式配置</param>
            /// <param name="B_COLORREF">标题栏颜色</param>
            /// <param name="T_COLORREF">标题文本颜色</param>
            /// <param name="CORNER">是否启用圆角（true：标准圆角，false：较小圆角，null：无圆角）</param>
            /// <param name="Topmost">是否置顶</param>
            /// <returns>返回操作是否成功执行</returns>
            public static bool SetDWM(IntPtr window, bool C2NC, bool DARK, int B_COLORREF, int T_COLORREF, bool? CORNER, bool Topmost)
            {
                if (Topmost)
                {
                    SetWindowPos(window, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE);
                }
                if (!DwmIsCompositionEnabled()) return false;
                // 系统版本检测：
                // Windows Vista -> 6.0.6000 (RTM)
                //                  6.0.6001 (SP1)
                //                  6.0.6002 (SP2)
                // Windows 7     -> 6.1.7600 (RTM)
                //                  6.1.7601 (SP1)
                // Windows 8     -> 6.2.9200
                // Windows 8.1   -> 6.3.9600
                // Windows 10    -> 10.0.10240
                // Windows 11    -> 10.0.22000
                Version sysver = Environment.OSVersion.Version;
                if (sysver >= new Version(6, 0, 6000))
                {
                    bool dwm_result = true;
                    bool isDWMenable = false;
                    DwmGetWindowAttribute(
                        window,
                        DWMWINDOWATTRIBUTE.DWMWA_NCRENDERING_ENABLED,
                        ref isDWMenable,
                        sizeof(int));
                    uint flag_0 = (uint)DWMNCRENDERINGPOLICY.DWMNCRP_ENABLED;
                    if (isDWMenable || DwmSetWindowAttribute(
                        window,
                        DWMWINDOWATTRIBUTE.DWMWA_NCRENDERING_POLICY,
                        ref flag_0,
                        sizeof(uint)) == S_OK)
                    {
                        bool flag_1 = true;
                        if (C2NC) DwmSetWindowAttribute(
                            window,
                            DWMWINDOWATTRIBUTE.DWMWA_ALLOW_NCPAINT,
                            ref flag_1,
                            sizeof(int));
                        if (sysver >= new Version(10, 0, 17763))
                        {
                            bool flag_2 = true;
                            DWMWINDOWATTRIBUTE ENUM = DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE_20H1;
                            if (sysver.Build >= 18985)
                            {
                                ENUM = DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE;
                            }
                            if (DARK) DwmSetWindowAttribute(
                                window,
                                ENUM,
                                ref flag_2,
                                sizeof(int));
                            if (sysver.Build >= 19041)
                            {
                                dwm_result &= DwmSetWindowAttribute(
                                    window,
                                    DWMWINDOWATTRIBUTE.DWMWA_CAPTION_COLOR,
                                    ref B_COLORREF,
                                    sizeof(uint)) == S_OK;
                                dwm_result &= DwmSetWindowAttribute(
                                    window,
                                    DWMWINDOWATTRIBUTE.DWMWA_TEXT_COLOR,
                                    ref T_COLORREF,
                                    sizeof(uint)) == S_OK;
                            }
                        }
                        if (sysver >= new Version(10, 0, 22000))
                        {
                            uint flag_3 = CORNER.HasValue
                                ? (CORNER.Value
                                    ? (uint)DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND
                                    : (uint)DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUNDSMALL)
                                : (uint)DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_SQUARE;
                            DwmSetWindowAttribute(
                                window,
                                DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE,
                                ref flag_3,
                                sizeof(uint));
                        }
                        MARGINS margins = new() { Left = 0, Top = 0, Right = 0, Bottom = 0 };
                        if (C2NC) DwmExtendFrameIntoClientArea(window, ref margins);
                        return dwm_result;
                    }
                }
                return false;
            }
        }

        public static class CoreAPI
        {
            /// <summary>
            /// win32enum::ProcessAccessFlags
            /// </summary>
            [Flags]
            public enum ProcessAccessFlags : uint
            {
                PROCESS_SET_INFORMATION = 0x00000200,
                PROCESS_QUERY_INFORMATION = 0x00000400,
                PROCESS_SET_QUOTA = 0x00000100,
                PROCESS_TERMINATE = 0x00000001
            }

            /// <summary>
            /// win32api::OpenProcess
            /// </summary>
            /// <param name="processAccess">param#1</param>
            /// <param name="bInheritHandle">param#2</param>
            /// <param name="processId">param#3</param>
            /// <returns>returns</returns>
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr OpenProcess(
                ProcessAccessFlags processAccess,
                bool bInheritHandle,
                int processId);

            /// <summary>
            /// win32api::QueryFullProcessImageName
            /// </summary>
            /// <param name="hProcess">param#1</param>
            /// <param name="dwFlags">param#2</param>
            /// <param name="lpExeName">param#3</param>
            /// <param name="lpdwSize">param#4</param>
            /// <returns>returns</returns>
            [DllImport("kernel32.dll")]
            public static extern bool QueryFullProcessImageName(
                [In] IntPtr hProcess,
                [In] int dwFlags,
                [Out] StringBuilder lpExeName,
                ref int lpdwSize);

            /// <summary>
            /// win32api::GetProcessAffinityMask
            /// </summary>
            /// <param name="hProcess">param#1</param>
            /// <param name="lpProcessAffinityMask">param#2</param>
            /// <param name="lpSystemAffinityMask">param#3</param>
            /// <returns></returns>
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool GetProcessAffinityMask(
                IntPtr hProcess,
                out UIntPtr lpProcessAffinityMask,
                out UIntPtr lpSystemAffinityMask);

            /// <summary>
            /// win32api::SetProcessAffinityMask
            /// </summary>
            /// <param name="hProcess">param#1</param>
            /// <param name="dwProcessAffinityMask">param#2</param>
            /// <returns>returns</returns>
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern ulong SetProcessAffinityMask(IntPtr hProcess, ulong dwProcessAffinityMask);

            /// <summary>
            /// win32api::CloseHandle
            /// </summary>
            /// <param name="hObject">param#1</param>
            /// <returns>returns</returns>
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool CloseHandle(IntPtr hObject);

            /// <summary>
            /// win32api::GetCurrentThread
            /// </summary>
            /// <returns>returns</returns>
            [DllImport("kernel32.dll")]
            public static extern IntPtr GetCurrentThread();

            /// <summary>
            /// win32api::SetThreadAffinityMask
            /// </summary>
            /// <param name="hThread">param#1</param>
            /// <param name="dwThreadAffinityMask">param#1</param>
            /// <returns>returns</returns>
            [DllImport("kernel32.dll")]
            public static extern ulong SetThreadAffinityMask(IntPtr hThread, ulong dwThreadAffinityMask);

            /// <summary>
            /// win32api::SwitchToThread
            /// </summary>
            /// <returns>returns</returns>
            [DllImport("kernel32.dll")]
            public static extern bool SwitchToThread();

            /// <summary>
            /// win32api::GetCurrentProcessorNumber
            /// </summary>
            /// <returns>returns</returns>
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern int GetCurrentProcessorNumber();
        }
    }
}
