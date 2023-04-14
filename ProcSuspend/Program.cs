using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace ProcSuspend
{
    internal class Program
    {
        static int ProcId;
        static void Main(string[] args)
        {
            string File;
            int delay;
            string arguments;
            Console.Title = "Process Suspender";
            if (args.Length != 0)
            {
                Console.WriteLine($"File: {args[0]}");
                File = args[0];
            }
            else
            {
                Console.WriteLine("Enter File Path: ");
                File = Console.ReadLine();
            }
            Console.WriteLine("Enter SR delay (ms): ");
            delay = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("Enter arguments (space for none): ");
            arguments = Console.ReadLine();
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = File,
                Arguments = arguments
            };
            Process proc = Process.Start(startInfo);
            ProcId = proc.Id;
            SuspendProcess();
            Console.WriteLine($"Process is started with ID {proc.Id}");
            Console.WriteLine($"Suspended, press any key to resume process for {delay} milliseconds");
            while (true)
            {
                Console.ReadLine();
                ResumeProcess();
                Thread.Sleep(delay);
                SuspendProcess();
                Console.WriteLine($"Continued to process for {delay} ms.");
            }
        }
        [Flags]
        public enum ThreadAccess : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        static extern int ResumeThread(IntPtr hThread);
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool CloseHandle(IntPtr handle);


        private static void SuspendProcess()
        {
            var process = Process.GetProcessById(ProcId); // throws exception if process does not exist

            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }

                SuspendThread(pOpenThread);

                CloseHandle(pOpenThread);
            }
        }

        public static void ResumeProcess()
        {
            var process = Process.GetProcessById(ProcId);

            if (process.ProcessName == string.Empty)
                return;

            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }

                var suspendCount = 0;
                do
                {
                    suspendCount = ResumeThread(pOpenThread);
                } while (suspendCount > 0);

                CloseHandle(pOpenThread);
            }
        }
    }
}
