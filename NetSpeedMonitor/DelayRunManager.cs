using System;
using System.Collections.Generic;
using System.Timers;

namespace USTC.Software.hanyizhao.NetSpeedMonitor
{
    /// <summary>
    /// Run function after some time.
    /// When program is closed, tasks maybe will still actived. 
    /// This class use <see cref="Timer"/> to handle the time. 
    /// It is recommended to remove all missions when closing.
    /// </summary>
    class DelayRunManager
    {
        /// <summary>
        /// Check whether this function is in the task list.
        /// </summary>
        /// <param name="run">The function</param>
        /// <returns></returns>
        public bool HasMission(Run run)
        {
            lock(lockMap)
            {
                return map.ContainsKey(run);
            }
        }

        /// <summary>
        /// Remove the function from the task list. It is safe if the funcion is not in the list.
        /// </summary>
        /// <param name="run">The function</param>
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

        /// <summary>
        /// Run <see cref="Run"/> after some time.
        /// If <see cref="Run"/> is already in the task list, this method does nothing.
        /// </summary>
        /// <param name="run">The task</param>
        /// <param name="miliseconds">Time span</param>
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
