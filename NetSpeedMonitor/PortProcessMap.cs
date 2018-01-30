using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace USTC.Software.hanyizhao.NetSpeedMonitor
{
    /// <summary>
    /// This class stores the relationships between process ID and socket address.
    /// These relationships are not real-time and not correct 100%. 
    /// Because the process sends or receives the packet first, but we capture the packet after. So the process may not be recorded any more by the OS when we call Windows API.
    /// </summary>
    public class PortProcessMap
    {
        private static PortProcessMap single;

        /// <summary>
        /// Single Instance
        /// </summary>
        /// <returns></returns>
        public static PortProcessMap GetInstance()
        {
            if(single == null)
            {
                single = new PortProcessMap();
            }
            return single;
        }
        
        /// <summary>
        /// Start or Stop mapping. (Reduce the cost of CPU)
        /// </summary>
        public bool Enabled
        {
            get
            {
                lock(timerLock)
                {
                    return timer.Enabled;
                }
            }
            set
            {
                lock(timerLock)
                {
                    timer.Enabled = value;
                }
            }
        }
        
        private PortProcessMap()
        {
            timer = new Timer
            {
                AutoReset = true,
                Interval = 1,
                Enabled = false
            };
            map = new Dictionary<Port, int>();
            activeProcessId = new HashSet<int>();
            timer.Elapsed += Timer_Elapsed;
        }

        /// <summary>
        /// Check whether the process has TCP connection or UDP connection.
        /// </summary>
        /// <param name="processId">Process ID</param>
        /// <returns></returns>
        public bool IsProcessHasConnect(int processId)
        {
            lock(mapLock)
            {
                return activeProcessId.Contains(processId);
            }
        }

        /// <summary>
        /// Get the process ID according to the specific socket address.
        /// </summary>
        /// <param name="p">Socket Address</param>
        /// <returns>Process ID. return -1 when we can't find the process ID. Maybe because the process release the TCP or UDP connection immediately.</returns>
        public int GetIPPortProcesId(Port p)
        {
            lock(mapLock)
            {
                if (map.TryGetValue(p, out int id))
                {
                    return id;
                }
                if(p.Clone() is Port p2)
                {
                    p2.ip = 0;
                    if (map.TryGetValue(p2, out int id2))
                    {
                        return id2;
                    }
                }
                return -1;
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            List<TcpProcessRecord> tcps = PortProcessMapWrapper.GetAllTcpConnections();
            List<UdpProcessRecord> udps = PortProcessMapWrapper.GetAllUdpConnections();
            lock(mapLock)
            {
                activeProcessId.Clear();
                foreach (TcpProcessRecord i in tcps)
                {
                    Port p = new Port(i.LocalAddress, i.LocalPort, TCPUDP.TCP);
                    map[p] = i.ProcessId;
                    activeProcessId.Add(i.ProcessId);
                }
                foreach(UdpProcessRecord i in udps)
                {
                    Port p = new Port(i.LocalAddress, i.LocalPort, TCPUDP.UDP);
                    map[p] = i.ProcessId;
                    activeProcessId.Add(i.ProcessId);
                }
            }
            
        }

        private readonly object timerLock = new object();
        private Timer timer;
        private Dictionary<Port, int> map;
        private readonly object mapLock = new object();
        private HashSet<int> activeProcessId;
    }

    
    
}
