using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace USTC.Software.hanyizhao.NetSpeedMonitor
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            bool result = InitializeCapture();
            if (!result)
            {
                Application.Current.Shutdown();
                return;
            }
            InitializeComponent();
            InitializeTray();
            InitializeReadSpeedData();
            detailWindow = new DetailWindow(this);
            detailWindow.IsVisibleChanged += DetailWindow_IsVisibleChanged;
        }


        private void InitializeReadSpeedData()
        {
            timer.Elapsed += Timer_Elapsed;
            timer.Enabled = true;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            UDStatistic statistics = uploadDownloadMap.NextStatistic(10, portProcessMap);
            Dispatcher.Invoke(new Action(() =>
            {
                UploadLabel.Content = Tool.GetNetSpeedString(statistics.upload, statistics.timeSpan);
                DownloadLabel.Content = Tool.GetNetSpeedString(statistics.download, statistics.timeSpan);
                if(detailWindow.Visibility == Visibility.Visible)
                {
                    detailWindow.NewData(statistics.items, statistics.timeSpan);
                }
            }));
        }

        private void DetailWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            portProcessMap.Enabled = detailWindow.IsVisible;
            if(!detailWindow.IsVisible)
            {
                TryToEdgeHide();
            }
        }

        private void InitializeTray()
        {
            System.Windows.Forms.MenuItem menuExit = new System.Windows.Forms.MenuItem("Exit");
            System.Windows.Forms.ContextMenu menu = new System.Windows.Forms.ContextMenu(new System.Windows.Forms.MenuItem[] { menuExit });
            menuExit.Click += MenuExit_Click;
            notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = new System.Drawing.Icon(Application.GetResourceStream(new Uri("pack://application:,,,/icon.ico", UriKind.RelativeOrAbsolute)).Stream),
                ContextMenu = menu,
                Visible = true
            };
        }

        private void MenuExit_Click(object sender, EventArgs e)
        {
            RegisterAppBar(false);
            Hide();
            detailWindow.OthersWantHide(true);
            timer.Enabled = false;
            notifyIcon.Dispose();
            lock (lockDevices)
            {
                if(devices != null)
                {
                    foreach (ICaptureDevice i in devices)
                    {
                        StopDevice(i);
                    }
                }
            }
            Application.Current.Shutdown();
        }

        private bool InitializeCapture()
        {
            try
            {
                devices = CaptureDeviceList.Instance;
            }
            catch (Exception)
            {
                MessageBox.Show("Make sure WinPcap is properly installed on the local machine.[NetSpeedMonitor]");
                return false;
            }

            lock (lockDevices)
            {
                if (devices != null)
                {
                    NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
                    foreach (ICaptureDevice i in devices)
                    {
                        if (i is LibPcapLiveDevice d)
                        {
                            StartDevice(d);
                        }
                    }
                    RefreshNetworkInformation();
                    return true;
                }
            }
            return false;
        }

        private void RefreshNetworkInformation()
        {
            List<NetworkStructure.Network> networks = new List<NetworkStructure.Network>();
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in networkInterfaces)
            {
                if (adapter.Supports(NetworkInterfaceComponent.IPv4))
                {
                    IPInterfaceProperties properties = adapter.GetIPProperties();
                    if (properties.UnicastAddresses.Count > 0)
                    {
                        foreach (UnicastIPAddressInformation oneAddress in properties.UnicastAddresses)
                        {
                            if (oneAddress.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                if (oneAddress.IPv4Mask != null)
                                {
                                    networks.Add(new NetworkStructure.Network(oneAddress.Address, oneAddress.IPv4Mask));
                                    Console.WriteLine(adapter.Name);
                                    Console.WriteLine(oneAddress.Address);

                                    uint d = BitConverter.ToUInt32(oneAddress.IPv4Mask.GetAddressBytes(), 0);
                                    Console.WriteLine(oneAddress.IPv4Mask + " " + d + " " + BitConverter.ToString(BitConverter.GetBytes(d)) + "\n");
                                }
                            }
                        }
                    }
                }
            }
            networkStructure.RefreshNetworkStructure(networks);
        }

        private void StopDevice(ICaptureDevice device)
        {
            if (device.Started)
            {
                device.OnPacketArrival -= I_OnPacketArrival;
                device.StopCapture();
                device.Close();
            }
        }

        private void StartDevice(ICaptureDevice device)
        {
            if (!device.Started)
            {
                device.OnPacketArrival += I_OnPacketArrival;
                device.Open(DeviceMode.Normal);
                device.StartCapture();
            }
            else
            {
                StopDevice(device);
                device.OnPacketArrival += I_OnPacketArrival;
                device.Open(DeviceMode.Normal);
                device.StartCapture();
            }
        }

        private void I_OnPacketArrival(object sender, CaptureEventArgs e)
        {
            int len = e.Packet.Data.Length;
            Packet p = Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);
            var ipPacket = (IpPacket)p.Extract(typeof(IpPacket));
            if (ipPacket != null)
            {
                IPAddress sourceAddress, destinationAddress;
                sourceAddress = ipPacket.SourceAddress;
                destinationAddress = ipPacket.DestinationAddress;
                if (sourceAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                    && destinationAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    ushort sourcePort = 0, destinationPort = 0;
                    TCPUDP protocol = TCPUDP.UDP;
                    var tcpPacket = (TcpPacket)p.Extract(typeof(TcpPacket));
                    if (tcpPacket != null)
                    {
                        protocol = TCPUDP.TCP;
                        sourcePort = tcpPacket.SourcePort;
                        destinationPort = tcpPacket.DestinationPort;
                    }
                    else
                    {
                        var udpPacket = (UdpPacket)p.Extract(typeof(UdpPacket));
                        if (udpPacket != null)
                        {
                            sourcePort = udpPacket.SourcePort;
                            destinationPort = udpPacket.DestinationPort;
                        }
                    }
                    PacketFlow packetFlow = networkStructure.GetPacketFlow(new NetworkStructure.PackageAddress(sourceAddress, sourcePort, destinationAddress, destinationPort));
                    packetFlow.protocol = protocol;
                    uploadDownloadMap.AddPacket(packetFlow, len);
                    //Console.WriteLine(e.Device.Statistics + "\t\t\t" + sourceAddress + ":" + sourcePort + "------>" + destinationAddress + ":" + destinationPort + " " + packetFlow);
                }
                //Console.WriteLine(e.Device.Statistics + "\t\t\t" + e.Packet.Data.Length + "\t" + ipPacket.SourceAddress + " " + ipPacket.DestinationAddress + " Thread ID: " + Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void NetworkChange_NetworkAddressChanged(object sender, EventArgs e)
        {
            Console.WriteLine("NetworkChange Time:" + DateTime.Now.ToLongTimeString());
            delayRunManager.RunAfter(RefreshDeviceList, 5000);
        }

        private void RefreshDeviceList()
        {
            lock (lockDevices)
            {
                DateTime start = DateTime.Now;
                devices.Refresh();
                DateTime end = DateTime.Now;
                Console.WriteLine("Refresh Time:" + end.Subtract(start).TotalMilliseconds + "ms");
                foreach (ICaptureDevice i in devices)
                {
                    if (i is LibPcapLiveDevice d)
                    {
                        StartDevice(d);
                    }
                }
                RefreshNetworkInformation();
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            detailWindow.OthersWantShow(false);
            TryToEdgeShow();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            detailWindow.OthersWantHide(true);
            leftPressTime = DateTime.Now;
            oldLeft = Left;
            oldTop = Top;
            DragMove();
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            detailWindow.OthersWantHide(false);
            TryToEdgeHide();
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (oldLeft == Left && oldTop == Top)
            {
                if (DateTime.Now.Subtract(leftPressTime).TotalMilliseconds < 500)
                {
                    TryToEdgeShow();
                    detailWindow.OthersWantShow(true);
                }
            }
            else
            {
                detailWindow.OthersWantShow(false);
                Tool.MoveWindowBackToWorkArea(this, windowPadding);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Tool.WindowMissFromMission(this);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr handle = (new WindowInteropHelper(this)).Handle;
            HwndSource.FromHwnd(handle).AddHook(new HwndSourceHook(WindowProc));
            RegisterAppBar(true);
        }

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == uCallBackMsg)
            {
                if (wParam.ToInt32() == (int)ABNotify.ABN_FULLSCREENAPP)
                {
                    IntPtr win = WinAPIWrapper.GetForegroundWindow();
                    if (!win.Equals(desktopHandle) && !win.Equals(shellHandle) && lParam.ToInt32() == 1)
                    {
                        HideAllView(true);
                    }
                    else
                    {
                        HideAllView(false);
                    }
                }
            }
            return IntPtr.Zero;
        }

        private void HideAllView(bool hide)
        {
            if (hide)
            {
                Hide();
                detailWindow.OthersWantHide(true);
            }
            else
            {
                Show();
            }
        }


        private void TryToEdgeShow()
        {
            if(isEdgeHide)
            {
                if (Top + windowPadding.Top < 0)
                {
                    Top = windowPadding.Top;
                }
                if (Top + Height + windowPadding.Bottom > SystemParameters.PrimaryScreenHeight)
                {
                    Top = SystemParameters.PrimaryScreenHeight - Height - windowPadding.Bottom;
                }
                if (Left + windowPadding.Left < 0)
                {
                    Left = windowPadding.Left;
                }
                if (Left + Width + windowPadding.Right > SystemParameters.PrimaryScreenWidth)
                {
                    Left = SystemParameters.PrimaryScreenWidth - Width - windowPadding.Right;
                }
                isEdgeHide = false;
            }
            
        }

        private void TryToEdgeHide()
        {
            if (!isEdgeHide)
            {
                if (!detailWindow.IsVisible)
                {
                    if (Top - windowPadding.Top <= 2)
                    {
                        Top = -windowPadding.Bottom - Height + edgeHideSpace;
                        isEdgeHide = true;
                    }
                    else if (SystemParameters.PrimaryScreenHeight - (Top + Height + windowPadding.Bottom) <= 2)
                    {
                        Top = SystemParameters.PrimaryScreenHeight + windowPadding.Top - edgeHideSpace;
                        isEdgeHide = true;
                    }
                    else if (Left - windowPadding.Left <= 2)
                    {
                        Left = -windowPadding.Right - Width + edgeHideSpace;
                        isEdgeHide = true;
                    }
                    else if (SystemParameters.PrimaryScreenWidth - (Left + Width + windowPadding.Right) <= 2)
                    {
                        Left = SystemParameters.PrimaryScreenWidth + windowPadding.Left - edgeHideSpace;
                        isEdgeHide = true;
                    }
                }

            }
            
        }

        
        private void RegisterAppBar(bool register)
        {
            APPBARDATA abd = new APPBARDATA();
            abd.cbSize = Marshal.SizeOf(abd);
            WindowInteropHelper helper = new WindowInteropHelper(this);
            abd.hWnd = helper.Handle;

            desktopHandle = WinAPIWrapper.GetDesktopWindow();
            shellHandle = WinAPIWrapper.GetShellWindow();
            if (register)
            {
                //register
                uCallBackMsg = WinAPIWrapper.RegisterWindowMessage("APPBARMSG_CSDN_HELPER");
                abd.uCallbackMessage = uCallBackMsg;
                uint ret = WinAPIWrapper.SHAppBarMessage((int)ABMsg.ABM_NEW, ref abd);
            }
            else
            {
                WinAPIWrapper.SHAppBarMessage((int)ABMsg.ABM_REMOVE, ref abd);
            }
        }

        private System.Windows.Forms.NotifyIcon notifyIcon;
        private CaptureDeviceList devices;
        private readonly object lockDevices = new object();
        private DelayRunManager delayRunManager = new DelayRunManager();
        private NetworkStructure networkStructure = new NetworkStructure(new List<NetworkStructure.Network>());
        private UDMap uploadDownloadMap = new UDMap();
        System.Timers.Timer timer = new System.Timers.Timer
        {
            Interval = 1000,
            AutoReset = true
        };

        private IntPtr desktopHandle;
        private IntPtr shellHandle;
        private int uCallBackMsg;

        private DetailWindow detailWindow;
        
        private bool isEdgeHide = false;
        private double edgeHideSpace = 4;

        private double oldLeft, oldTop;
        private DateTime leftPressTime = DateTime.Now;

        public readonly Thickness windowPadding = new Thickness(-3, 0, -3, -3);
        public readonly Thickness windowMargin = new Thickness(-3, 3, -3, 0);

        private PortProcessMap portProcessMap = PortProcessMap.GetInstance();
        

    }
}
