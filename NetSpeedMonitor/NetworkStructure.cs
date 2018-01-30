using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace USTC.Software.hanyizhao.NetSpeedMonitor
{
    /// <summary>
    /// This class stores the structure of network, including IPv4 and subnet mask of each network card.
    /// </summary>
    public class NetworkStructure
    {
        /// <summary>
        /// Initialize the network structure.
        /// </summary>
        /// <param name="networks">The list of information of network cards.</param>
        public NetworkStructure(List<Network> networks)
        {
            Init(networks);
        }

        /// <summary>
        /// Refresh the network structure.
        /// </summary>
        /// <param name="networks"></param>
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

        /// <summary>
        /// Get the data flow direction of a packet.
        /// </summary>
        /// <param name="address">Addresses of a packet</param>
        /// <returns>Data flow direction</returns>
        public PacketFlow GetPacketFlow(PacketAddress address)
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
        private Dictionary<PacketAddress, PacketFlow> cache = new Dictionary<PacketAddress, PacketFlow>();
    }

    /// <summary>
    /// IPv4 and subnet mask of a network card.
    /// </summary>
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


    /// <summary>
    /// Source address and destion address of a packet.
    /// </summary>
    public class PacketAddress
    {
        public uint source;
        public uint destination;
        public ushort sourcePort;
        public ushort destinationPort;

        public PacketAddress(IPAddress source, ushort sourcePort, IPAddress destination, ushort destinationPort)
        {
            this.source = BitConverter.ToUInt32(source.GetAddressBytes(), 0);
            this.destination = BitConverter.ToUInt32(destination.GetAddressBytes(), 0);
            this.sourcePort = sourcePort;
            this.destinationPort = destinationPort;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (obj is PacketAddress y)
            {
                return source == y.source && destination == y.destination
                    && sourcePort == y.sourcePort && destinationPort == y.destinationPort;
            }
            return false;
        }

        public override int GetHashCode() => ((((int)source * 31 + sourcePort) * 31) + (int)destination) * 31 + destinationPort;
    }

    /// <summary>
    /// The necessary information of a packet.
    /// </summary>
    public class PacketFlow
    {
        /// <summary>
        /// The direction of the data flow.
        /// </summary>
        public FlowType type;

        /// <summary>
        /// The IP of host. (Maybe there are more than one network card. So the ip of host is dynamic.)
        /// </summary>
        public uint myIP;

        /// <summary>
        /// Port of transport protocol.
        /// </summary>
        public ushort port;

        /// <summary>
        /// Transport protocol.
        /// </summary>
        public TCPUDP protocol;

        /// <summary>
        /// We may capture the packet which souce IP and destination IP don't belong to our host.
        /// </summary>
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

        /// <summary>
        /// The direction of a packet. Upload or Download or The packet we should discard.
        /// </summary>
        public enum FlowType
        {
            UPLOAD, DOWNLOAD, DROP
        }
    }
}
