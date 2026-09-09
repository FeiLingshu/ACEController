using ACEController.Experimental;
using ACEController.Win32API;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using static ACEController.Win32API.API;
using static ACEController.Win32API.API.BugFix;
using static ACEController.Win32API.API.CoreAPI;
using static ACEController.Win32API.API.DWMEX;
using static ACEController.Win32API.E;

namespace ACEController
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0042:析构变量声明", Justification = "<挂起>")]
    public class Program
    {
        public const string GUID = "78798E28-2236-41DC-A3AF-D415C5ED397C";
        public static EventWaitHandle ProgramStarted;
        [STAThread]
        public static void Main(string[] _)
        {
            ProgramStarted = new EventWaitHandle(false, EventResetMode.AutoReset, GUID, out bool createNew);
            if (!createNew)
            {
                ProgramStarted.Set();
                return;
            }
            ThreadPool.RegisterWaitForSingleObject(ProgramStarted, (state, timeout) => { }, null, -1, false);
            using (Process selfproc = Process.GetCurrentProcess())
            {
                SetProcessEcoQoS(selfproc.Id, true);
            }
            do
            {
                WaitReg();
                var cores = GetCore();
                Worker(cores.Flag, cores.Mask);
            } while (Thread.CurrentThread.IsAlive);
        }
        private static void WaitReg()
        {
            AutoResetEvent watchertimer = new(false);
            MEWatcher watcher = new(watchertimer);
            object regdata = Registry.GetValue(
                    "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\AntiCheatExpert Service", "ImagePath", string.Empty);
            if (regdata is not string || string.IsNullOrEmpty((string)regdata))
            {
                watcher.StartWatcher();
                watcher.runcount = true;
                watchertimer.WaitOne();
                watcher.CloseWatcher();
                GC.KeepAlive(watcher);
                GC.Collect();
            }
        }
        private static (bool Flag, ulong Mask) GetCore()
        {
            if (Environment.ProcessorCount > 64)
            {
                return (false, ulong.MaxValue);
            }
            string bin = string.Empty;
            using (Process selfproc = Process.GetCurrentProcess())
            {
                bin = $"{new FileInfo(selfproc.MainModule.FileName).DirectoryName}\\CPU.bin";
            }
            bool coreflag = false;
            ulong processormask = 0UL;
            void Auto()
            {
                try
                {
                    processormask = Core.GetCoreMap();
                    coreflag = true;
                }
                catch (Exception) { }
            }
            if (File.Exists(bin))
            {
                byte[] data = File.ReadAllBytes(bin);
                if (data.Length == Environment.ProcessorCount)
                {
                    List<int> indices = data
                        .Select((value, index) => new { value, index })
                        .Where(x => x.value == 1)
                        .Select(x => x.index)
                        .ToList();
                    if (indices.Count == 0)
                    {
                        Auto();
                    }
                    else
                    {
                        indices.ForEach(i =>
                        {
                            processormask |= (1U << i);
                        });
                        coreflag = true;
                    }
                }
                else
                {
                    Auto();
                }
            }
            else
            {
                Auto();
            }
            return (coreflag, processormask);
        }
        private struct ProcessInfo
        {
            internal Process process1;
            internal Process process2;
            internal bool corestate;
            internal ulong processormask;
            internal string __path;
            internal ProcessInfo(Process process1, Process process2, bool corestate, ulong processormask, string __path)
            {
                this.process1 = process1;
                this.process2 = process2;
                this.corestate = corestate;
                this.processormask = processormask;
                this.__path = __path;
            }
        }
        private static void Worker(bool corestate, ulong processormask)
        {
            ServiceController sc = new("AntiCheatExpert Service");
            ProcessInfo pi = new();
            (Thread main, Thread back) ts = (null, null);
            Thread scan = null;
            PerformanceCounter[][] tools = new PerformanceCounter[2][] { new PerformanceCounter[5], new PerformanceCounter[5] };
            try
            {
                sc.WaitForStatus(ServiceControllerStatus.Running);
                string reg = (string)Registry.GetValue("HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\AntiCheatExpert Service", "ImagePath", string.Empty);
                string path = Regex.Replace(reg, @" -autorun\Z", string.Empty).Trim('"');
                if (new FileInfo(path).Name != "SGuardSvc64.exe")
                {
                    throw new FileNotFoundException("Service Verify Faild");
                }
                pi = new(null, null, corestate, processormask, path);
                if (File.Exists(path) && File.Exists($"{new FileInfo(path).Directory}\\SGuardSvc64.exe"))
                {
                    bool check = false;
                    AutoResetEvent timer = new(false);
                    do
                    {
                        Process[] processes = Process.GetProcessesByName("SGuardSvc64");
                        foreach (Process process in processes)
                        {
                            if (GetProcessFilename(process) == $"{new FileInfo(path).Directory}\\SGuardSvc64.exe")
                            {
                                pi.process2 = Process.GetProcessById(process.Id);
                                Set(process, corestate, processormask, false);
                                check = true;
                                break;
                            }
                        }
                        foreach (Process process in processes)
                        {
                            process.Dispose();
                        }
                        timer.WaitOne(100, false);
                    } while (sc.Status == ServiceControllerStatus.Running && !check);
                }
                if (File.Exists(path) && File.Exists($"{new FileInfo(path).Directory}\\SGuard64.exe"))
                {
                    bool check = false;
                    AutoResetEvent timer = new(false);
                    do
                    {
                        Process[] processes = Process.GetProcessesByName("SGuard64");
                        foreach (Process process in processes)
                        {
                            if (GetProcessFilename(process) == $"{new FileInfo(path).Directory}\\SGuard64.exe")
                            {
                                pi.process1 = Process.GetProcessById(process.Id);
                                Set(process, corestate, processormask, true);
                                check = true;
                                break;
                            }
                        }
                        foreach (Process process in processes)
                        {
                            process.Dispose();
                        }
                        timer.WaitOne(100, false);
                    } while (sc.Status == ServiceControllerStatus.Running && !check);
                }
                ts = CheckLoop(pi, sc);
                scan = RunScan(pi, Core.CountEnabledCores(processormask, corestate), tools);
            }
            catch (Exception) { }
            sc.WaitForStatus(ServiceControllerStatus.Stopped);
            sc.Dispose();
            (bool _1, bool _2) T_Flag = (false, false);
            if (scan != null && scan.IsAlive)
            {
                scan.Abort();
                T_Flag._2 = true;
            }
            if (ts.main != null && ts.main.IsAlive) ts.main.Abort();
            if (ts.back != null && ts.back.IsAlive)
            {
                ts.back.Abort();
                T_Flag._1 = true;
            }
            if (T_Flag._1) ts.back.Join(5000);
            if (T_Flag._2) scan.Join(5000);
            CloseConsole();
            foreach (var tool in tools)
            {
                foreach (var item in tool)
                {
                    item?.Dispose();
                }
            }
            pi.process1?.Dispose();
            pi.process2?.Dispose();
        }
        private static void Set(Process process, bool corestate, ulong processormask, bool unsafemode)
        {
            process.PriorityClass = ProcessPriorityClass.Idle;
            if (corestate) Core.SetProcess(process.Id, processormask);
            SetProcessEcoQoS(process.Id, true);
            SetJobObject($"0x{"SBTX&SBACE&FKU".GetHashCode():X8}", process.Id, unsafemode);
        }
        private static string GetProcessFilename(Process p)
        {
            int capacity = 32767;
            StringBuilder builder = new(capacity);
            IntPtr ptr = OpenProcess(ProcessAccessFlags.PROCESS_QUERY_INFORMATION, false, p.Id);
            if (ptr == IntPtr.Zero) return string.Empty;
            try
            {
                if (!QueryFullProcessImageName(ptr, 0, builder, ref capacity))
                {
                    return string.Empty;
                }
                return builder.ToString();
            }
            finally
            {
                CoreAPI.CloseHandle(ptr);
            }
        }
        private static (Thread main, Thread back) CheckLoop(ProcessInfo processinfo, ServiceController service)
        {
            Thread thread = new(() =>
            {
                AutoResetEvent timer = new(false);
                do
                {
                    timer.WaitOne(new TimeSpan(0, 1, 0), false);
                    if (!Thread.CurrentThread.IsAlive) break;
                    try
                    {
                        Process p1 = Interlocked.CompareExchange(ref processinfo.process1, null, null);
                        Process p2 = Interlocked.CompareExchange(ref processinfo.process2, null, null);
                        if (p1 != null)
                        {
                            Set(p1, processinfo.corestate, processinfo.processormask, true);
                        }
                        if (p2 != null)
                        {
                            Set(p2, processinfo.corestate, processinfo.processormask, false);
                        }
                    }
                    catch (Exception) { continue; }
                } while (Thread.CurrentThread.IsAlive);
            })
            { IsBackground = true };
            Thread backupthread = new(() =>
            {
                AutoResetEvent timer = new(false);
                do
                {
                    try
                    {
                        Process p = Interlocked.CompareExchange(ref processinfo.process1, null, null);
                        p?.WaitForExit();
                        p?.Dispose();
                        timer.WaitOne(1000, false);
                        if (service.Status != ServiceControllerStatus.Running) continue;
                        Interlocked.Exchange(ref processinfo.process1, null);
                        do
                        {
                            Process[] processes = Process.GetProcessesByName("SGuard64");
                            foreach (Process process in processes)
                            {
                                if (GetProcessFilename(process) == $"{new FileInfo(processinfo.__path).Directory}\\SGuard64.exe")
                                {
                                    Process _process = Process.GetProcessById(process.Id);
                                    Interlocked.Exchange(ref processinfo.process1, _process);
                                    Set(_process, processinfo.corestate, processinfo.processormask, true);
                                    break;
                                }
                            }
                            foreach (Process process in processes)
                            {
                                process.Dispose();
                            }
                            timer.WaitOne(1000, false);
                        } while (Thread.CurrentThread.IsAlive);
                    }
                    catch (Exception)
                    {
                        timer.WaitOne(1000, false);
                        continue;
                    }
                } while (Thread.CurrentThread.IsAlive);
            })
            { IsBackground = true };
            thread.Start();
            if (Interlocked.CompareExchange(ref processinfo.process1, null, null) != null)
            {
                backupthread.Start();
            }
            return (thread, backupthread);
        }
        private static Thread RunScan(ProcessInfo PI, int corecount, PerformanceCounter[][] tools)
        {
            Initialize();
            if (CreatConsole())
            {
                RefreshConsoleHandle(Debugger.IsAttached);
                SetConsole(GetConsoleWindow());
                SetDWM(GetConsoleWindow(), false, true, 0x00202020, 0x00E9E9E9, true, true);
            }
            Console.CursorVisible = false;
            (int x, int y) line_pid = (0, Console.CursorTop);
            string std_1 = $" SGuardSvc64.exe -> PID = {PI.process2.Id}";
            string std_2 = $" SGuard64.exe -> PID = {PI.process1.Id}";
            Console.WriteLine(std_1.PadRight(Console.WindowWidth - 1));
            Console.WriteLine(std_2.PadRight(Console.WindowWidth - 1));
            if (!PerformanceCounterCategory.Exists("Process V2"))
            {
                Console.WriteLine(" [ Error ]");
                Console.WriteLine("  - Didn't Find a Performance Group Named \"Process V2\"");
                Console.WriteLine("  - That Means Your System is Not Supported");
                Console.WriteLine(" ==========");
                Console.WriteLine(" Copyright 2026 By FeiLingshu");
                return null;
            }
            Console.Write(" Core Usage".PadRight(Console.WindowWidth - 5));
            (int x, int y) line_pt = (Console.CursorLeft, Console.CursorTop);
            Console.WriteLine("Idle");
            Console.Write(" ||");
            (int x, int y) line_pt_line = (2, Console.CursorTop);
            Console.WriteLine();
            Console.Write(" IO Read Speed".PadRight(Console.WindowWidth - 12));
            (int x, int y) line_iorb = (Console.CursorLeft, Console.CursorTop);
            Console.WriteLine("       Idle");
            Console.Write(" File Load Speed (Est)".PadRight(Console.WindowWidth - 12));
            (int x, int y) line_fls = (Console.CursorLeft, Console.CursorTop);
            Console.WriteLine("       Idle");
            Console.Write(" IO Write Speed".PadRight(Console.WindowWidth - 12));
            (int x, int y) line_iowb = (Console.CursorLeft, Console.CursorTop);
            Console.WriteLine("       Idle");
            Console.WriteLine(" ==========");
            Console.WriteLine(" Copyright 2026 By FeiLingshu");
            if (PI.process1.Id == 0 && PI.process2.Id == 0)
            {
                return null;
            }
            else
            {
                int PID_1 = 0;
                int PID_2 = 0;
                PerformanceCounter[] pcs_1 = tools[0];
                PerformanceCounter[] pcs_2 = tools[1];
                void SetPCS(int P1, int P2)
                {
                    if (P1 != 0)
                    {
                        try
                        {
                            pcs_1[0] = new PerformanceCounter(
                                "Process V2",
                                "% Processor Time",
                                $"SGuard64:{P1}",
                                true);
                            _ = pcs_1[0].NextValue();
                        }
                        catch (Exception) { }
                        try
                        {
                            pcs_1[1] = new PerformanceCounter(
                                "Process V2",
                                "IO Read Bytes/sec",
                                $"SGuard64:{P1}",
                                true);
                            _ = pcs_1[1].NextValue();
                        }
                        catch (Exception) { }
                        try
                        {
                            pcs_1[2] = new PerformanceCounter(
                                "Process V2",
                                "IO Write Bytes/sec",
                                $"SGuard64:{P1}",
                                true);
                            _ = pcs_1[2].NextValue();
                        }
                        catch (Exception) { }
                        try
                        {
                            pcs_1[3] = new PerformanceCounter(
                                "Process V2",
                                "Page Faults/sec",
                                $"SGuard64:{P1}",
                                true);
                            _ = pcs_1[3].NextValue();
                        }
                        catch (Exception) { }
                        try
                        {
                            pcs_1[4] = new PerformanceCounter(
                                "Process V2",
                                "Working Set",
                                $"SGuard64:{P1}",
                                true);
                            _ = pcs_1[4].NextValue();
                        }
                        catch (Exception) { }
                    }
                    if (P2 != 0)
                    {
                        try
                        {
                            pcs_2[0] = new PerformanceCounter(
                                "Process V2",
                                "% Processor Time",
                                $"SGuardSvc64:{P2}",
                                true);
                            _ = pcs_2[0].NextValue();
                        }
                        catch (Exception) { }
                        try
                        {
                            pcs_2[1] = new PerformanceCounter(
                                "Process V2",
                                "IO Read Bytes/sec",
                                $"SGuardSvc64:{P2}",
                                true);
                            _ = pcs_2[1].NextValue();
                        }
                        catch (Exception) { }
                        try
                        {
                            pcs_2[2] = new PerformanceCounter(
                                "Process V2",
                                "IO Write Bytes/sec",
                                $"SGuardSvc64:{P2}",
                                true);
                            _ = pcs_2[2].NextValue();
                        }
                        catch (Exception) { }
                        try
                        {
                            pcs_2[3] = new PerformanceCounter(
                                "Process V2",
                                "Page Faults/sec",
                                $"SGuardSvc64:{P2}",
                                true);
                            _ = pcs_2[3].NextValue();
                        }
                        catch (Exception) { }
                        try
                        {
                            pcs_2[4] = new PerformanceCounter(
                                "Process V2",
                                "Working Set",
                                $"SGuardSvc64:{P2}",
                                true);
                            _ = pcs_2[4].NextValue();
                        }
                        catch (Exception) { }
                    }
                }
                PID_1 = PI.process1.Id;
                PID_2 = PI.process2.Id;
                SetPCS(PID_1, PID_2);
                Thread thread = new(() =>
                {
                    AutoResetEvent timer = new(false);
                    do
                    {
                        try
                        {
                            int pid_1 = 0;
                            int pid_2 = 0;
                            Process p1 = Interlocked.CompareExchange(ref PI.process1, null, null);
                            Process p2 = Interlocked.CompareExchange(ref PI.process2, null, null);
                            if (p1 != null)
                            {
                                pid_1 = p1.Id;
                            }
                            if (p2 != null)
                            {
                                pid_2 = p2.Id;
                            }
                            if (!(PID_1 == pid_1 && PID_2 == pid_2))
                            {
                                Console.CursorLeft = line_pid.x;
                                Console.CursorTop = line_pid.y;
                                string std_1 = $" SGuardSvc64.exe -> PID = {pid_2}";
                                string std_2 = $" SGuard64.exe -> PID = {pid_1}";
                                Console.WriteLine(std_1.PadRight(Console.WindowWidth - 1));
                                Console.WriteLine(std_2.PadRight(Console.WindowWidth - 1));
                                foreach (var item_1 in pcs_1)
                                {
                                    item_1?.Dispose();
                                }
                                foreach (var item_2 in pcs_2)
                                {
                                    item_2?.Dispose();
                                }
                                SetPCS(pid_1, pid_2);
                            }
                            float PT = 0F;
                            if (pcs_1[0] != null)
                            {
                                PT += pcs_1[0].NextValue();
                            }
                            if (pcs_2[0] != null)
                            {
                                PT += pcs_2[0].NextValue();
                            }
                            PT /= corecount;
                            if (PT < 0F) PT = 0F;
                            if (PT > 100F) PT = 100F;
                            float IORB_1 = 0F;
                            float IORB_2 = 0F;
                            if (pcs_1[1] != null)
                            {
                                IORB_1 = pcs_1[1].NextValue();
                            }
                            if (pcs_2[1] != null)
                            {
                                IORB_2 = pcs_2[1].NextValue();
                            }
                            float IOWB_1 = 0F;
                            float IOWB_2 = 0F;
                            if (pcs_1[2] != null)
                            {
                                IOWB_1 = pcs_1[2].NextValue();
                            }
                            if (pcs_2[2] != null)
                            {
                                IOWB_2 = pcs_2[2].NextValue();
                            }
                            double MM_1 = 0D;
                            double MM_2 = 0D;
                            if (pcs_1[3] != null && pcs_1[4] != null)
                            {
                                MM_1 = Ex.RunMath(pcs_1[3], pcs_1[4], 1);
                            }
                            if (pcs_2[3] != null && pcs_2[4] != null)
                            {
                                MM_2 = Ex.RunMath(pcs_2[3], pcs_2[4], 2);
                            }
                            Console.CursorLeft = line_pt.x;
                            Console.CursorTop = line_pt.y;
                            int PTINT = (int)Math.Round(PT, 0, MidpointRounding.AwayFromZero);
                            Console.Write($"{PTINT,3}%");
                            Console.CursorLeft = line_pt_line.x;
                            Console.CursorTop = line_pt_line.y;
                            int L = 0;
                            if (PTINT >= 1)
                            {
                                L = 1 + (int)Math.Round((Console.WindowWidth - 5) * (PTINT - 1) / 99D, 0, MidpointRounding.AwayFromZero);
                            }
                            var f = Console.ForegroundColor;
                            var b = Console.BackgroundColor;
                            Console.ForegroundColor = b;
                            Console.BackgroundColor = f;
                            Console.Write(string.Empty.PadLeft(L));
                            Console.ForegroundColor = f;
                            Console.BackgroundColor = b;
                            Console.Write($"|{string.Empty.PadRight(Console.WindowWidth - 4 - L)}");
                            Console.CursorLeft = line_iorb.x;
                            Console.CursorTop = line_iorb.y;
                            Console.Write(Ex.GetSpeed((double)IORB_1, (double)IORB_2));
                            Console.CursorLeft = line_fls.x;
                            Console.CursorTop = line_fls.y;
                            Console.Write(Ex.GetSpeed(MM_1, MM_2));
                            Console.CursorLeft = line_iowb.x;
                            Console.CursorTop = line_iowb.y;
                            Console.Write(Ex.GetSpeed((double)IOWB_1, (double)IOWB_2));
                        }
                        catch (Exception) { continue; }
                        timer.WaitOne(1000, false);
                    } while (Thread.CurrentThread.IsAlive);
                })
                { IsBackground = true };
                thread.Start();
                return thread;
            }
        }
    }
    public class MEWatcher
    {
        private readonly AutoResetEvent timer = null;
        public MEWatcher(AutoResetEvent timer)
        {
            this.timer = timer;
        }
        public bool runcount = true;
        private ManagementEventWatcher eventWatcher = null;
        private EventArrivedEventHandler handle = null;
        public void StartWatcher()
        {
            try
            {
                string selecrtext = $@"SELECT * FROM RegistryValueChangeEvent WHERE Hive='HKEY_LOCAL_MACHINE' AND KeyPath='SYSTEM\\CurrentControlSet\\Services\\AntiCheatExpert Service' AND ValueName='ImagePath'";
                WqlEventQuery regQuery = new(selecrtext);
                eventWatcher = new ManagementEventWatcher(regQuery);
                handle = new EventArrivedEventHandler(HandleEvent);
                eventWatcher.EventArrived += handle;
                eventWatcher.Start();
            }
            catch (Exception) { }
        }
        public void CloseWatcher()
        {
            try
            {
                if (eventWatcher != null)
                {
                    eventWatcher.EventArrived -= handle;
                    handle = null;
                    eventWatcher.Stop();
                    eventWatcher.Dispose();
                    eventWatcher = null;
                }
            }
            catch (Exception)
            {
                eventWatcher = null;
            }
        }
        public void HandleEvent(object sender, EventArrivedEventArgs e)
        {
            string reg = (string)Registry.GetValue("HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\AntiCheatExpert Service", "ImagePath", string.Empty);
            string path = Regex.Replace(reg, @" -autorun\Z", string.Empty).Trim('"');
            if (File.Exists(path) && File.Exists($"{new FileInfo(path).Directory}\\SGuard64.exe") && File.Exists($"{new FileInfo(path).Directory}\\SGuardSvc64.exe"))
            {
                new AutoResetEvent(false).WaitOne(100, false);
                if (runcount)
                {
                    timer.Set();
                    runcount = false;
                }
            }
        }
    }
}
