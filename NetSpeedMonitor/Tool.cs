using PacketDotNet;
using SharpPcap;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace USTC.Software.hanyizhao.NetSpeedMonitor
{
    class Tool
    {

        public static PacketAddress GetPacketAddressFromRowPacket(RawCapture rawCapture, ref int len, ref TCPUDP protocol)
        {
            try
            {
                len = rawCapture.Data.Length;
                Packet p = Packet.ParsePacket(rawCapture.LinkLayerType, rawCapture.Data);
                IpPacket ipPacket = (IpPacket)p.Extract(typeof(IpPacket));
                if (ipPacket != null)
                {
                    IPAddress sourceAddress, destinationAddress;
                    sourceAddress = ipPacket.SourceAddress;
                    destinationAddress = ipPacket.DestinationAddress;
                    if (sourceAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                        && destinationAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        IPProtocolType type = ipPacket.NextHeader;
                        if (type == IPProtocolType.TCP)
                        {
                            TcpPacket tcpPacket = (TcpPacket)ipPacket.Extract(typeof(TcpPacket));
                            if (tcpPacket != null)
                            {
                                protocol = TCPUDP.TCP;
                                return new PacketAddress(sourceAddress, tcpPacket.SourcePort, destinationAddress, tcpPacket.DestinationPort);
                            }
                        }
                        else if(type == IPProtocolType.UDP)
                        {
                            UdpPacket udpPacket = (UdpPacket)ipPacket.Extract(typeof(UdpPacket));
                            if (udpPacket != null)
                            {
                                protocol = TCPUDP.UDP;
                                return new PacketAddress(sourceAddress, udpPacket.SourcePort, destinationAddress, udpPacket.DestinationPort);
                            }
                        }
                    }
                }
                return null;
            }
            catch(Exception)
            {
                Console.WriteLine("Packet Error");
                //Console.WriteLine(e.Message + "\n" + e.StackTrace);
                return null;
            }
            
        }

        public static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        
        public static bool MoveWindowBackToWorkArea(Window window, Thickness padding)
        {
            Rect workArea = SystemParameters.WorkArea;
            Rect rect = new Rect(window.Left - padding.Left, window.Top - padding.Top, window.Width + padding.Left + padding.Right, window.Height + padding.Top + padding.Bottom);
            if(!workArea.Contains(rect))
            {
                double heightSpan =rect.Bottom - workArea.Bottom;
                if(heightSpan > 0)
                {
                    window.Top = window.Top - heightSpan;
                }
                else
                {
                    heightSpan = workArea.Top - rect.Top;
                    if(heightSpan > 0)
                    {
                        window.Top = window.Top + heightSpan;
                    }
                }
                double widthSpan = rect.Right - workArea.Right;
                if(widthSpan > 0)
                {
                    window.Left = window.Left - widthSpan;
                }
                else
                {
                    widthSpan = workArea.Left - rect.Left;
                    if(widthSpan > 0)
                    {
                        window.Left = window.Left + widthSpan;
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private static string DoubleLengthMax4(double d)
        {
            string c = d.ToString();
            if (c.Length > 4)
            {
                c = c.Substring(0, 4);
            }
            if (c.EndsWith("."))
            {
                c = c.Substring(0, c.Length - 1);
            }
            return c;
        }

        public static void WindowMissFromMission(Window window)
        {
            WindowInteropHelper helper = new WindowInteropHelper(window);
            long old = WinAPIWrapper.GetWindowLong(helper.Handle, WinAPIWrapper.GWL_EXSTYLE);
            old |= WinAPIWrapper.WS_EX_TOOLWINDOW;
            Console.WriteLine(WinAPIWrapper.SetWindowLong(helper.Handle, WinAPIWrapper.GWL_EXSTYLE, (IntPtr)old));
        }

        public static string GetNetSpeedString(long len, double timeSpan)
        {
            if (timeSpan <= 0)
            {
                timeSpan = 1;
            }

            double value = (double)len * 1000 / timeSpan;
            if (value < 1024 * 1024)
            {
                return DoubleLengthMax4(value / 1024) + "K/s";
            }
            if (value < 1024 * 1024 * 1024)
            {
                return DoubleLengthMax4(value / 1024 / 1024) + "M/s";
            }
            return DoubleLengthMax4(value / 1024 / 1024 / 1024) + "G/s";
        }
    }
}
