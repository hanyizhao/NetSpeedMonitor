using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace USTC.Software.hanyizhao.NetSpeedMonitor
{
    /// <summary>
    /// This class stores necessary information of every packets, including <see cref="PacketFlow"/> and length of the packet.
    /// </summary>
    public class UDMap
    {
        public UDMap()
        {
            Init();
            lastTime = DateTime.Now;
        }

        /// <summary>
        /// Get the statistic result. This method analyses the network traffic before this method is called to the last time this method called.
        /// So invoking this method twice will get different statistic result.
        /// </summary>
        /// <param name="topMax">The top N processes.</param>
        /// <param name="portProcessMap">The relationships of Process and Socket Address</param>
        /// <returns></returns>
        public UDStatistic NextStatistic(int topMax, PortProcessMap portProcessMap)
        {
            lock(this)
            {
                UDStatistic statistic = new UDStatistic
                {
                    upload = uploadStatistic,
                    download = downloadStatistic
                };
                DateTime now = DateTime.Now;
                statistic.timeSpan = now.Subtract(lastTime).TotalMilliseconds;
                lastTime = now;
                Init();
                if(portProcessMap.Enabled)
                {
                    Dictionary<int, ProcessStatistic> nowProMap = new Dictionary<int, ProcessStatistic>();
                    long unKnownUp = 0;
                    long unKnownDown = 0;
                    foreach (var i in portMap.Values)
                    {
                        int id = portProcessMap.GetIPPortProcesId(i.port);
                        if (id == -1)
                        {
                            unKnownDown += i.downLen;
                            unKnownUp += i.upLen;
                        }
                        else
                        {
                            if (!nowProMap.TryGetValue(id, out ProcessStatistic sta))
                            {
                                sta = new ProcessStatistic
                                {
                                    processId = id
                                };
                                nowProMap[id] = sta;
                            }
                            sta.downLen += i.downLen;
                            sta.upLen += i.upLen;
                        }
                    }
                    var values = nowProMap.Values.ToList();
                    foreach (ProcessStatistic i in values)
                    {
                        i.sum = i.downLen + i.upLen;
                    }
                    values.Sort();
                    HashSet<int> processes = new HashSet<int>();
                    int k = 0;
                    while (k < topMax && k < values.Count)
                    {
                        ProcessStatistic ps = values[k];
                        if (ps.sum == 0)
                        {
                            break;
                        }
                        UDOneItem item = new UDOneItem(ps.processId, values[k].upLen, values[k].downLen);
                        statistic.items.Add(item);
                        processes.Add(ps.processId);
                        k++;
                    }
                    k = 0;
                    while(statistic.items.Count < topMax && k < lastTimeUDItems.Count)
                    {
                        UDOneItem item = lastTimeUDItems[k];
                        
                        if(item.ProcessID >= 0)
                        {
                            if (!processes.Contains(item.ProcessID) && portProcessMap.IsProcessHasConnect(item.ProcessID))
                            {
                                statistic.items.Add(new UDOneItem(item.ProcessID, 0, 0));
                                processes.Add(item.ProcessID);
                            }
                        }
                        k++;
                    }
                    
                    if (unKnownDown != 0 || unKnownUp != 0)
                    {
                        //statistic.items.Add(new UDOneItem() { name = "miss", download = unKnownDown, upload = unKnownUp});
                    }
                    if (noPortStatistic.downLen != 0 || noPortStatistic.upLen != 0 || (lastTimeUDItems.Count > 0 && lastTimeUDItems[lastTimeUDItems.Count - 1].ProcessID == -1))
                    {
                        statistic.items.Add(new UDOneItem(-1, noPortStatistic.upLen, noPortStatistic.downLen));
                    }
                    lastTimeUDItems = new List<UDOneItem>(statistic.items);
                }
                noPortStatistic = new PortStatistic();
                portMap.Clear();
                return statistic;
            }
        }
       
        /// <summary>
        /// When one packet is captured by threads, we store the information in the manager.
        /// </summary>
        /// <param name="packet">The information of one packet</param>
        /// <param name="len">The length of bytes of one packet</param>
        public void AddPacket(PacketFlow packet, int len)
        {
            lock(this)
            {
                if(packet.type != PacketFlow.FlowType.DROP)
                {
                    if(packet.hasIPAndPort)
                    {
                        Port p = new Port(packet.myIP, packet.port, packet.protocol);
                        if(!portMap.TryGetValue(p, out PortStatistic statistic))
                        {
                            statistic = new PortStatistic
                            {
                                port = p
                            };
                            portMap[p] = statistic;
                        }
                        if(packet.type == PacketFlow.FlowType.DOWNLOAD)
                        {
                            statistic.downLen += len;
                            downloadStatistic += len;
                        }
                        else
                        {
                            statistic.upLen += len;
                            uploadStatistic += len;
                        }

                    }
                    else
                    {
                        if(packet.type == PacketFlow.FlowType.UPLOAD)
                        {
                            noPortStatistic.upLen += len;
                            uploadStatistic += len;
                        }
                        else
                        {
                            noPortStatistic.downLen += len;
                            downloadStatistic += len;
                        }
                    }
                }
            }
        }
        
        private void Init()
        {
            uploadStatistic = 0;
            downloadStatistic = 0;
        }

        private class ProcessStatistic : IComparable<ProcessStatistic>
        {
            public int processId;
            public long upLen;
            public long downLen;
            public long sum;

            public int CompareTo(ProcessStatistic other)
            {
                return other.sum.CompareTo(sum);
            }
        }

        /// <summary>
        /// The statistic information of one socket address. (There are lots of packets from the same socket address or to the same address.)
        /// </summary>
        private class PortStatistic
        {
            public Port port;
            public long upLen;
            public long downLen;
        }
        
        private DateTime lastTime;
        private long uploadStatistic, downloadStatistic;
        private PortStatistic noPortStatistic = new PortStatistic();
        private Dictionary<Port, PortStatistic> portMap = new Dictionary<Port, PortStatistic>();
        private List<UDOneItem> lastTimeUDItems = new List<UDOneItem>();
    }

    /// <summary>
    /// The network traffic statistic information of one process.
    /// </summary>
    public class UDOneItem
    {
        /// <summary>
        /// Get the process ID. -1 means maybe there is a NAT. Bacause we can not locate any socket address that belongs to our network cards.
        /// </summary>
        public int ProcessID { get { return processID; } }

        /// <summary>
        /// Get the length of the bytes of upload.
        /// </summary>
        public long Upload { get { return upload; } }

        /// <summary>
        /// Get the length of the bytes of download.
        /// </summary>
        public long Download { get { return download; } }

        private int processID;
        private long upload;
        private long download;

        public UDOneItem(int processID, long upload, long download)
        {
            this.processID = processID;
            this.upload = upload;
            this.download = download;
        }
    }

    /// <summary>
    /// The statistic result.
    /// </summary>
    public class UDStatistic
    {
        /// <summary>
        /// The totoal length of upload bytes.
        /// </summary>
        public long upload;

        /// <summary>
        /// The total length of download bytes.
        /// </summary>
        public long download;

        /// <summary>
        /// The timespan this statistic considers
        /// </summary>
        public double timeSpan;

        /// <summary>
        /// The detail of each process.
        /// </summary>
        public List<UDOneItem> items = new List<UDOneItem>();
    }

    /// <summary>
    /// Transport protocol.
    /// </summary>
    public enum TCPUDP
    {
        TCP, UDP
    }

    /// <summary>
    /// One socket address, including IP, port and the transport protocol.
    /// </summary>
    public class Port : ICloneable
    {
        public uint ip;
        public ushort port;
        public TCPUDP protocol;

        public Port(uint ip, ushort port, TCPUDP protocol)
        {
            this.ip = ip;
            this.port = port;
            this.protocol = protocol;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public override bool Equals(object obj)
        {
            if(this == obj)
            {
                return true;
            }
            if(obj is Port y)
            {
                return port == y.port && ip == y.ip &&  protocol == y.protocol;
            }
            return false;
        }

        public override int GetHashCode() => (port * 31 + (int)ip) * 31 + (int)protocol;
    }
}
