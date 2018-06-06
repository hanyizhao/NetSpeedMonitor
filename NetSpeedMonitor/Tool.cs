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
        /// <summary>
        /// Get the packet Information from <see cref="RawCapture"/>
        /// </summary>
        /// <param name="rawCapture">The raw captured packet</param>
        /// <param name="len">Get the length of bytes of the packet</param>
        /// <param name="protocol">Get the tansport protocol of the packet</param>
        /// <returns>The Addresses of the packet. Null if the packet has error, or it's not IP packet, or It's IPV6.</returns>
        public static PacketAddress GetPacketAddressFromRowPacket(RawCapture rawCapture, ref int len, ref TCPUDP protocol)
        {
            try
            {
                Packet p = Packet.ParsePacket(rawCapture.LinkLayerType, rawCapture.Data);
                IpPacket ipPacket = (IpPacket)p.Extract(typeof(IpPacket));
                if (ipPacket != null)
                {
                    len = ipPacket.PayloadLength;
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

        /// <summary>
        /// Get string resource
        /// </summary>
        /// <param name="key">key</param>
        /// <returns>the value or NULL</returns>
        public static String GetStringResource(String key)
        {
            object val = Application.Current.TryFindResource(key);
            if(val == null)
            {
                return "NULL";
            }
            else
            {
                return val.ToString();
            }
        }

        /// <summary>
        /// Get version of the setup program.
        /// Strictly, the version of assembly is different from setup version.
        /// So we transfer from 1.0.1.1 to 1.0.11.
        /// </summary>
        /// <returns></returns>
        public static String GetVersion()
        {
            String nowVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            int lastDot = nowVersion.LastIndexOf('.');
            return nowVersion.Substring(0, lastDot) + nowVersion.Substring(lastDot + 1);
        }

        /// <summary>
        /// Determines whether the current principal belongs to the Windows user group Administrator.
        /// </summary>
        /// <returns></returns>
        public static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        
        /// <summary>
        /// Make sure the window is in the work area. (Make sure the window is in the screen and doesn't be covered by taskbar.)
        /// </summary>
        /// <param name="window">The window smaller than work area.</param>
        /// <param name="padding">Padding of the window</param>
        /// <returns>False if work area doesn't contain the window </returns>
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

        /// <summary>
        /// Make sure the length of double is short than 4.
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
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

        public static int CompareTwoVersionString(String s1, String s2)
        {
            String[] s1List = s1.Split(new char[]{ '.'});
            String[] s2List = s2.Split(new char[] { '.' });
            for(int i = 0;i < s1List.Length && i < s2List.Length;i++)
            {
                if(int.TryParse(s1List[i], out int i1) && int.TryParse(s2List[i], out int i2))
                {
                    if(i1 < i2)
                    {
                        return -1;
                    }
                    else if(i1 > i2)
                    {
                        return 1;
                    }
                }
            }
            if(s1List.Length < s2List.Length)
            {
                return -1;
            }
            else if(s1List.Length > s2List.Length)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Remove the window from "Alt + TAB list".
        /// </summary>
        /// <param name="window">The window</param>
        /// <param name="NoActivate">No activate (get focus)</param>
        public static void WindowMissFromMission(Window window, bool NoActivate)
        {
            WindowInteropHelper helper = new WindowInteropHelper(window);
            long old = WinAPIWrapper.GetWindowLong(helper.Handle, WinAPIWrapper.GWL_EXSTYLE);
            old |= WinAPIWrapper.WS_EX_TOOLWINDOW;
            if(NoActivate)
            {
                old |= WinAPIWrapper.WS_EX_NOACTIVATE;
            }
            Console.WriteLine("Remove the window from Alt+TAB list" + WinAPIWrapper.SetWindowLong(helper.Handle, WinAPIWrapper.GWL_EXSTYLE, (IntPtr)old));
        }

        /// <summary>
        /// Get the network speed.
        /// </summary>
        /// <param name="len">The length of bytes of packets</param>
        /// <param name="timeSpan">Time span in milliseconds</param>
        /// <returns>The formatted string of network speed.</returns>
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
