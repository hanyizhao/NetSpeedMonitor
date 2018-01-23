using SharpPcap;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using USTC.Software.hanyizhao.NetSpeedMonitor.Properties;

namespace USTC.Software.hanyizhao.NetSpeedMonitor
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {

        private Mutex mutex;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if(e.Args.Length > 0 && e.Args[0] == "-startup")
            {
                if(!Settings.Default.startOnBoot)
                {
                    Shutdown();
                    return;
                }
            }
            mutex = new Mutex(true, "USTC.Software.hanyizhao.NetSpeedMonitor", out bool createNew);
            if (createNew)
            {
                try
                {
                    CaptureDeviceList devices = CaptureDeviceList.Instance;
                    Window window = new MainWindow(devices);
                    window.Show();
                }
                catch (Exception ee)
                {
                    MessageBox.Show("WinPcap is one dependency of NetSpeedMonitor.\nYou can visit https://www.winpcap.org/ to install this software.\nAnd make sure WinPcap is properly installed on the local machine. \n\n[NetSpeedMonitor]\nERROR:" + ee.Message);
                    Process.Start("https://www.winpcap.org/");
                    Shutdown();
                }
            }
            else
            {
                Shutdown();
            }

        }
    }
}
