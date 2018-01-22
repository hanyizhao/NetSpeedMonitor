using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace USTC.Software.hanyizhao.NetSpeedMonitor
{
    class PortProcessMap
    {
        private static PortProcessMap single;

        public static PortProcessMap GetInstance()
        {
            if(single == null)
            {
                single = new PortProcessMap();
            }
            return single;
        }
        
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
            timer.Elapsed += Timer_Elapsed;
            map = new Dictionary<Port, int>();
        }

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
                foreach (TcpProcessRecord i in tcps)
                {
                    Port p = new Port(i.LocalAddress, i.LocalPort, TCPUDP.TCP);
                    map[p] = i.ProcessId;
                }
                foreach(UdpProcessRecord i in udps)
                {
                    Port p = new Port(i.LocalAddress, i.LocalPort, TCPUDP.UDP);
                    map[p] = i.ProcessId;
                }
            }
            
        }

        private readonly object timerLock = new object();
        private Timer timer;
        private Dictionary<Port, int> map;
        private readonly object mapLock = new object();
    }

    
    
}
