using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace USTC.Software.hanyizhao.NetSpeedMonitor
{
    partial class NetworkStructure
    {
        public class Network
        {
            public IPAddress ipv4;
            public IPAddress mask;

            public Network(IPAddress ipv4, IPAddress mask)
            {
                this.ipv4 = ipv4;
                this.mask = mask;
            }
        }

        public class PackageAddress
        {
            public uint source;
            public uint destination;
            public ushort sourcePort;
            public ushort destinationPort;

            public PackageAddress(IPAddress source, ushort sourcePort, IPAddress destination, ushort destinationPort)
            {
                this.source = BitConverter.ToUInt32(source.GetAddressBytes(), 0);
                this.destination = BitConverter.ToUInt32(destination.GetAddressBytes(), 0);
                this.sourcePort = sourcePort;
                this.destinationPort = destinationPort;
            }

            public override bool Equals(object obj)
            {
                if(this == obj)
                {
                    return true;
                }
                if (obj is PackageAddress y)
                {
                    return source == y.source && destination == y.destination
                        && sourcePort == y.sourcePort && destinationPort == y.destinationPort;
                }
                return false;
            }

            public override int GetHashCode()
            {
                return sourcePort * 65536 + destinationPort;
            }
        }

       

        public NetworkStructure(List<Network> networks)
        {
            Init(networks);
        }

        public void RefreshNetworkStructure(List<Network> networks)
        {
            lock (this)
            {
                myIPSet.Clear();
                myNetworks.Clear();
                cache.Clear();
                Init(networks);
            }
        }

        private void Init(List<Network> networks)
        {
            foreach (Network i in networks)
            {
                uint ip = BitConverter.ToUInt32(i.ipv4.GetAddressBytes(), 0);
                uint mask = BitConverter.ToUInt32(i.mask.GetAddressBytes(), 0);
                uint net = ip & mask;
                myNetworks.Add(new MyNetwork(net, mask, ip));
                myIPSet.Add(ip);
            }
        }

        public PacketFlow GetPacketFlow(PackageAddress address)
        {
            lock(this)
            {
                if (!cache.TryGetValue(address, out PacketFlow result))
                {
                    result = new PacketFlow();
                    if (myIPSet.Contains(address.source))
                    {
                        if (myIPSet.Contains(address.destination))
                        {
                            result.type = PacketFlow.FlowType.DROP;
                        }
                        else
                        {
                            result.type = PacketFlow.FlowType.UPLOAD;
                            result.hasIPAndPort = true;
                            result.myIP = address.source;
                            result.port = address.sourcePort;
                        }
                    }
                    else
                    {
                        if (myIPSet.Contains(address.destination))
                        {
                            result.type = PacketFlow.FlowType.DOWNLOAD;
                            result.hasIPAndPort = true;
                            result.myIP = address.destination;
                            result.port = address.destinationPort;
                        }
                        else
                        {
                            if (IsAddressInNetwork(address.source))
                            {
                                if (IsAddressInNetwork(address.destination))
                                {
                                    result.type = PacketFlow.FlowType.DROP;
                                }
                                else
                                {
                                    result.type = PacketFlow.FlowType.DOWNLOAD;
                                    result.hasIPAndPort = false;
                                }
                            }
                            else
                            {
                                if (IsAddressInNetwork(address.destination))
                                {
                                    result.type = PacketFlow.FlowType.UPLOAD;
                                    result.hasIPAndPort = false;
                                }
                                else
                                {
                                    result.type = PacketFlow.FlowType.DROP;
                                }
                            }
                        }
                    }
                    cache[address] = result;
                }
                return result;
            }
        }

        private bool IsAddressInNetwork(uint address)
        {
            bool result = false;
            foreach(MyNetwork i in myNetworks)
            {
                if((address & i.mask) == i.net)
                {
                    result = true;
                    break;
                }
            }
            return result;
        }
        
        private class MyNetwork
        {
            public uint net;
            public uint mask;
            public uint myIP;

            public MyNetwork(uint net, uint mask, uint myIP)
            {
                this.net = net;
                this.mask = mask;
                this.myIP = myIP;
            }

        }

        private HashSet<uint> myIPSet = new HashSet<uint>();
        private List<MyNetwork> myNetworks = new List<MyNetwork>();
        private Dictionary<PackageAddress, PacketFlow> cache = new Dictionary<PackageAddress, PacketFlow>();
    }

    public class PacketFlow
    {
        public FlowType type;
        public uint myIP;
        public ushort port;
        public TCPUDP protocol;
        public bool hasIPAndPort;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("[");
            sb.Append(type);
            if (hasIPAndPort)
            {
                sb.Append(", ");
                sb.Append(new IPAddress(BitConverter.GetBytes(myIP)));
                sb.Append(":").Append(port);
            }
            sb.Append("]");
            return sb.ToString();
        }

        public enum FlowType
        {
            UPLOAD, DOWNLOAD, DROP
        }
    }
}
