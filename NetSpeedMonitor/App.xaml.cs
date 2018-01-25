using SharpPcap;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
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

        public void FreeMutex()
        {
            if(mutex != null)
            {
                try
                {
                    mutex.Dispose();
                }
                catch(Exception)
                {

                }
            }
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length > 0)
            {
                if (e.Args[0] == "-startup")
                {
                    if (!Settings.Default.startOnBoot)
                    {
                        Shutdown();
                        return;
                    }
                }
                else if (e.Args.Length == 2 && e.Args[0] == "-processid")
                {
                    if (Int32.TryParse(e.Args[1], out int id))
                    {
                        ProcessDetailWindow w = new ProcessDetailWindow(id, null);
                        Dispatcher.InvokeAsync(new Action(() => { w.Show(); }));
                    }
                }
            }
            
            mutex = new Mutex(true, "USTC.Software.hanyizhao.NetSpeedMonitor", out bool createNew);
            
            if (createNew)
            {
                try
                {
                    CaptureDeviceList devices = CaptureDeviceList.Instance;
                    MainWindow window = new MainWindow(devices);
                    if(Settings.Default.MainWindowLeft > -200000 && Settings.Default.MainWindowTop > -200000)
                    {
                        window.Left = Settings.Default.MainWindowLeft;
                        window.Top = Settings.Default.MainWindowTop;
                        Dispatcher.InvokeAsync(new Action(()=> {
                            window.isEdgeHide = true;
                            window.TryToEdgeShow();
                            window.TryToEdgeHide();
                        }));
                    }
                    else
                    {
                        Dispatcher.InvokeAsync(new Action(() => {
                            window.isEdgeHide = true;
                            window.TryToEdgeShow();
                            window.TryToEdgeHide();
                        }));
                    }

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
