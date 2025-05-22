using System;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Threading;

namespace Win32
{
//    internal class TMSTimer
    public class TMSTimer
    {
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long lpFrequency);

        private long tstart;
        private long freq;

        public TMSTimer()
        {
            tstart = 0;
            if (QueryPerformanceFrequency(out freq) == false)
            {
                // high-performance counter not supported
                throw new Win32Exception("QueryPerformanceFrequency()  not supported");
            }
        }

        public void start()
        {
            // lets do the waiting threads there work
            Thread.Sleep(0);
            QueryPerformanceCounter(out tstart);
        }

        public ulong getms()
        {          
          long tnow;
          QueryPerformanceCounter(out tnow);
          return (ulong)((double)(tnow - tstart) * 1000.0 / (double) freq);
        }
    }
}