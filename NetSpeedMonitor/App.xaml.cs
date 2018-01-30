using SharpPcap;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Media.Animation;
using USTC.Software.hanyizhao.NetSpeedMonitor.Properties;

namespace USTC.Software.hanyizhao.NetSpeedMonitor
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    { 

        /// <summary>
        /// Make sure there is only one instance.
        /// </summary>
        private Mutex mutex;

        /// <summary>
        /// Release the Mutex. It is not necessary to invoke this function when this program is closing.
        /// It is used when restart. Thus the new process can own the Mutex before the old process release the Mutex.
        /// </summary>
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
                // This arg is used when start on boot. When user set false on "start on boot", the program still starts on boot but then stops immediately.
                if (e.Args[0] == "-startup")
                {
                    if (!Settings.Default.startOnBoot)
                    {
                        Shutdown();
                        return;
                    }
                }
                // These arguments is used when program restart. Will firstly show the window of detail of specific process.
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

            // There is no instance until now.
            if (createNew)
            {
                captureManager = new CaptureManager(udMap);
                welcomeWindow.Show();
                Thread t = new Thread(new ThreadStart(()=> {
                    //如果用户按的足够快，先按了exit，那么会先执行Exit，后执行captureManager.InitAndStart() !!! This is a bug, but it will not trigger unless user is really really fast !!!.
                    if (!captureManager.InitAndStart())
                    {
                        Dispatcher.InvokeAsync(new Action(()=> {
                            MessageBox.Show("WinPcap is one dependency of NetSpeedMonitor.\nYou can visit https://www.winpcap.org/ to install this software.\nAnd make sure WinPcap is properly installed on the local machine. \n\n[NetSpeedMonitor]");
                            Process.Start("https://www.winpcap.org/");
                            Shutdown();
                        }));
                    }
                    else
                    {
                        Dispatcher.InvokeAsync(new Action(() => {
                            InitViewAndNeedCloseResourcees();
                            welcomeWindow.ReduceAndClose(new Point(mainWindow.Left + mainWindow.Width / 2, mainWindow.Top + mainWindow.Height / 2));
                        }));
                    }
                }));
                t.Start();
            }
            else
            {
                Shutdown();
            }
        }
        
        
        private void InitViewAndNeedCloseResourcees()
        {
            mainWindow = new MainWindow();
            detailWindow = new DetailWindow(mainWindow);
            mainWindow.SetDetailWindow(detailWindow);
            detailWindow.IsVisibleChanged += DetailWindow_IsVisibleChanged;
            if (Settings.Default.MainWindowLeft > -200000 && Settings.Default.MainWindowTop > -200000)
            {
                mainWindow.Left = Settings.Default.MainWindowLeft;
                mainWindow.Top = Settings.Default.MainWindowTop;
                Dispatcher.InvokeAsync(new Action(() =>
                {
                    mainWindow.isEdgeHide = true;
                    mainWindow.TryToEdgeShow();
                    mainWindow.TryToEdgeHide();
                }));
            }
            else
            {
                Dispatcher.InvokeAsync(new Action(() =>
                {
                    mainWindow.isEdgeHide = true;
                    mainWindow.TryToEdgeShow();
                    mainWindow.TryToEdgeHide();
                }));
            }
            InitializeTray();
            mainWindow.Show();
            timer.Elapsed += Timer_Elapsed;
            timer.Enabled = true;
        }

        private void DetailWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            portProcessMap.Enabled = detailWindow.IsVisible;
            if (!detailWindow.IsVisible)
            {
                mainWindow.TryToEdgeHide();
            }
        }

        private void InitializeTray()
        {
            menuExit = new System.Windows.Forms.MenuItem("Exit", TrayMenu_Click);

            menuEdgeHide = new System.Windows.Forms.MenuItem("Hide when close to edge", TrayMenu_Click)
            {
                Checked = Settings.Default.edgeHide
            };
            menuStartOnBoot = new System.Windows.Forms.MenuItem("Start on boot", TrayMenu_Click)
            {
                Checked = Settings.Default.startOnBoot
            };
            System.Windows.Forms.MenuItem menuSetting = new System.Windows.Forms.MenuItem("Settings", new System.Windows.Forms.MenuItem[] { menuStartOnBoot, menuEdgeHide });
            System.Windows.Forms.ContextMenu menu = new System.Windows.Forms.ContextMenu(new System.Windows.Forms.MenuItem[] { menuSetting, menuExit });

            notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = new System.Drawing.Icon(Application.GetResourceStream(new Uri("pack://application:,,,/icon.ico", UriKind.RelativeOrAbsolute)).Stream),
                ContextMenu = menu,
                Visible = true
            };
        }

        public void TryToSetStartOnBoot(bool startOnBoot)
        {
            Settings.Default.startOnBoot = startOnBoot;
            Settings.Default.Save();
        }

        public void TryToExit()
        {
            portProcessMap.Enabled = false;
            mainWindow.RegisterAppBar(false);
            timer.Enabled = false;
            notifyIcon.Dispose();
            FreeMutex();
            Shutdown();
            Thread t = new Thread(captureManager.Stop);
            t.Start();
        }

        public void TryToSetEdgeHide(bool edgeHide)
        {
            Settings.Default.edgeHide = edgeHide;
            Settings.Default.Save();
            if (edgeHide)
            {
                mainWindow.TryToEdgeHide();
            }
            else
            {
                mainWindow.TryToEdgeShow();
            }
        }

        private void TrayMenu_Click(object sender, EventArgs e)
        {
            if (sender == menuExit)
            {
                TryToExit();
            }
            else if (sender == menuStartOnBoot)
            {
                menuStartOnBoot.Checked = !menuStartOnBoot.Checked;
                mainWindow.WindowMenuStartOnBoot.IsChecked = menuStartOnBoot.Checked;
                TryToSetStartOnBoot(menuStartOnBoot.Checked);
            }
            else if (sender == menuEdgeHide)
            {
                menuEdgeHide.Checked = !menuEdgeHide.Checked;
                mainWindow.WindowMenuEdgeHide.IsChecked = menuEdgeHide.Checked;
                TryToSetEdgeHide(menuEdgeHide.Checked);
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            UDStatistic statistics = udMap.NextStatistic(10, portProcessMap);
            Dispatcher.Invoke(new Action(() =>
            {
                mainWindow.UploadLabel.Content = Tool.GetNetSpeedString(statistics.upload, statistics.timeSpan);
                mainWindow.DownloadLabel.Content = Tool.GetNetSpeedString(statistics.download, statistics.timeSpan);
                if (detailWindow.Visibility == Visibility.Visible)
                {
                    detailWindow.NewData(statistics.items, statistics.timeSpan);
                }
            }));
        }

        public System.Windows.Forms.MenuItem menuExit, menuEdgeHide, menuStartOnBoot;

        private System.Windows.Forms.NotifyIcon notifyIcon;
        private MainWindow mainWindow;
        private DetailWindow detailWindow;
        private WelcomeWindow welcomeWindow = new WelcomeWindow();
        private CaptureManager captureManager;
        private UDMap udMap = new UDMap();
        private PortProcessMap portProcessMap = PortProcessMap.GetInstance();
        
        private System.Timers.Timer timer = new System.Timers.Timer
        {
            Interval = 1000,
            AutoReset = true
        };
    }
}
