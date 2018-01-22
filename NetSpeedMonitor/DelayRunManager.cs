using System;
using System.Collections.Generic;
using System.Timers;

namespace USTC.Software.hanyizhao.NetSpeedMonitor
{
    /// <summary>
    /// timer没有关闭！！！！这是个问题啊
    /// </summary>
    class DelayRunManager
    {

        public bool HasMission(Run run)
        {
            lock(lockMap)
            {
                return map.ContainsKey(run);
            }
        }

        public void RemoveMission(Run run)
        {
            lock(lockMap)
            {
                if(map.TryGetValue(run, out Timer timer))
                {
                    map.Remove(run);
                    reverseMap.Remove(timer);
                }
            }
        }

        public void RunAfter(Run run, long miliseconds)
        {
            lock(lockMap)
            {
                if(!map.ContainsKey(run))
                {
                    Timer timer = new Timer
                    {
                        AutoReset = false,
                        Interval = miliseconds
                    };
                    timer.Elapsed += Timer_Elapsed;
                    map.Add(run, timer);
                    reverseMap.Add(timer, run);
                    timer.Enabled = true;
                }
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Timer timer = sender as Timer;
            Run run = null;
            lock (lockMap)
            {
                if (timer != null && reverseMap.ContainsKey(timer))
                {
                    run = reverseMap[timer];
                    reverseMap.Remove(timer);
                    map.Remove(run);
                }
            }
            run?.Invoke();
        }

        private readonly object lockMap = new object();
        private Dictionary<Run, Timer> map = new Dictionary<Run, Timer>();
        private Dictionary<Timer, Run> reverseMap = new Dictionary<Timer, Run>();


    }

    public delegate void Run();
}
