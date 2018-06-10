using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using USTC.Software.hanyizhao.NetSpeedMonitor.Properties;

namespace USTC.Software.hanyizhao.NetSpeedMonitor
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window, ICanMoveDetailWindowToRightPlace
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeWindowMenu();
            detailWindow = new DetailWindow(this);
            detailWindow.IsVisibleChanged += DetailWindow_IsVisibleChanged;
        }
        
        private void DetailWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if(detailWindow.Visibility == Visibility.Hidden)
            {
                TryToEdgeHide();
            }
        }

        public void NewData(UDStatistic statistics)
        {
            UploadLabel.Content = Tool.GetNetSpeedString(statistics.upload, statistics.timeSpan);
            DownloadLabel.Content = Tool.GetNetSpeedString(statistics.download, statistics.timeSpan);
            if (detailWindow.Visibility == Visibility.Visible)
            {
                detailWindow.NewData(statistics.items, statistics.timeSpan);
            }
        }
        
        private void InitializeWindowMenu()
        {
            WindowMenuStartOnBoot.IsChecked = Settings.Default.startOnBoot;
            WindowMenuEdgeHide.IsChecked = Settings.Default.edgeHide;
            WindowMenuShowTrayIcon.IsChecked = Settings.Default.ShowTrayIcon;
            //Language Settings
            List<OneLanguage> languages = Languages.GetLanguages();
            String nowLanguageFile = Settings.Default.language;
            foreach(OneLanguage i in languages)
            {
                MenuItem menuItem = new MenuItem()
                {
                    Header = i.ShowName,
                    IsCheckable = true,
                    IsChecked = i.FileName == nowLanguageFile,
                    Tag = i.FileName
                };
                menuItem.Click += MenuItem_ChangeLanguageClick;
                WindowMenuLanguage.Items.Add(menuItem);
            }
            if(nowLanguageFile == null || nowLanguageFile == "")
            {
                WindowMenuUserDefault.IsChecked = true;
            }
            WindowMenuUserDefault.Tag = "";
            WindowMenuAutoUpdate.IsChecked = Settings.Default.AutoUpdate;
            //Transparency Settings
            for (int i = 100; i >= 10; i -= 10)
            {
                MenuItem menuItem = new MenuItem()
                {
                    Header = i + "%",
                    IsCheckable = true,
                    Tag = i
                };
                menuItem.Click += MenuItem_ChangeTransparency;
                WindowMenuTransparency.Items.Add(menuItem);
            }
            Callback_TransparencyDoChange(Settings.Default.Transparency);
        }

        public void Callback_TransparencyDoChange(int transparency)
        {
            if(transparency <= 100 && transparency >= 10 && transparency % 10 == 0)
            {
                foreach (MenuItem i in WindowMenuTransparency.Items)
                {
                    if(i.Tag is int myTag)
                    {
                        i.IsChecked = myTag == transparency;
                    }
                }
                Opacity = (double)transparency / 100;
            }
        }

        private void MenuItem_ChangeTransparency(object sender, RoutedEventArgs e)
        {
            if(sender is MenuItem menuItem)
            {
                if (!menuItem.IsChecked)
                {
                    menuItem.IsChecked = true;
                }
                else
                {
                    if(Application.Current is App app && menuItem.Tag is int transparency)
                    {
                        app.TryToSetTransparency(transparency);
                    }
                }
            }
        }

        private void MenuItem_ChangeLanguageClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                if(!menuItem.IsChecked)
                {
                    menuItem.IsChecked = true;
                }
                else
                {
                    if(Application.Current is App app && menuItem.Tag is String path)
                    {
                        app.TryToSetLanguage(path);
                    }
                }
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current is App app)
            {
                if (sender == WindowMenuExit)
                {
                    app.TryToExit();
                }
                else if (sender == WindowMenuStartOnBoot)
                {
                    bool startOnBoot = WindowMenuStartOnBoot.IsChecked;
                    app.menuStartOnBoot.Checked = startOnBoot;
                    app.TryToSetStartOnBoot(startOnBoot);
                }
                else if (sender == WindowMenuEdgeHide)
                {
                    bool edgeHide = WindowMenuEdgeHide.IsChecked;
                    app.menuEdgeHide.Checked = edgeHide;
                    app.TryToSetEdgeHide(edgeHide);
                }
                else if(sender == WindowMenuShowTrayIcon)
                {
                    bool showTrayIcon = WindowMenuShowTrayIcon.IsChecked;
                    app.menuShowTrayIcon.Checked = showTrayIcon;
                    app.TryToSetShowTrayIcon(showTrayIcon);
                }
                else if (sender == WindowMenuAutoUpdate)
                {
                    bool autoUpdate = WindowMenuAutoUpdate.IsChecked;
                    app.menuAutoUpdate.Checked = autoUpdate;
                    app.TryToSetAutoUpdate(autoUpdate);
                }
                else if(sender == WindowMenuCheckUpdate)
                {
                    app.TryToCheckUpdate();
                }
                else if(sender == WindowMenuAbout)
                {
                    app.TryToShowAboutWindow();
                }
            }
            
        }
        
        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            detailWindow.OthersWantShow(false);
            TryToEdgeShow();
        }

        private void SaveLeftAndTopToSettings()
        {
            Settings.Default.MainWindowLeft = Left;
            Settings.Default.MainWindowTop = Top;
            Settings.Default.Save();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                detailWindow.OthersWantHide(true);
                leftPressTime = DateTime.Now;
                oldLeft = Left;
                oldTop = Top;
                DragMove();
                SaveLeftAndTopToSettings();
            }
            catch(Exception)
            {

            }
            
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
            Tool.WindowMissFromMission(this, false);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr handle = (new WindowInteropHelper(this)).Handle;
            HwndSource.FromHwnd(handle).AddHook(new HwndSourceHook(WindowProc));
            taskBarCreatedMsg = WinAPIWrapper.RegisterWindowMessage("TaskbarCreated");
            uCallBackMsg = WinAPIWrapper.RegisterWindowMessage("APPBARMSG_CSDN_HELPER_USTC.Software.hanyizhao.NetSpeedMonitor");
            RegisterAppBar(true);
        }
        
        /// <summary>
        /// Call this method in main thread.
        /// </summary>
        private bool CheckHasFullScreenApp()
        {
            bool result = false;
            IntPtr foreWindow = WinAPIWrapper.GetForegroundWindow();
            WinAPIWrapper.GetWindowThreadProcessId(foreWindow, out uint processid);
            String foreGroundWindowName = "";
            try
            {
                foreGroundWindowName = Process.GetProcessById((int)processid).ProcessName;
            }
            catch (Exception)
            {
            }
            if (foreGroundWindowName != "explorer" && !foreWindow.Equals(new WindowInteropHelper(this).Handle))
            {
                IntPtr deskWindow = WinAPIWrapper.GetDesktopWindow();
                if (!foreWindow.Equals(deskWindow) && !foreWindow.Equals(WinAPIWrapper.GetShellWindow()))
                {
                    WinAPIWrapper.GetWindowRect(foreWindow, out RECT foreWindowRECT);
                    WinAPIWrapper.GetWindowRect(deskWindow, out RECT deskWindowRECT);
                    result = foreWindowRECT.left <= deskWindowRECT.left
                        && foreWindowRECT.top <= deskWindowRECT.top
                        && foreWindowRECT.right >= deskWindowRECT.right
                        && foreWindowRECT.bottom >= deskWindowRECT.bottom;
                }
            }
            return result;
        }

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if(msg == taskBarCreatedMsg)
            {
                Console.WriteLine("Receive Message: TaskbarCreated");
                RegisterAppBar(false);
                RegisterAppBar(true);
            }
            if (msg == uCallBackMsg)
            {
                if (wParam.ToInt32() == (int)ABNotify.ABN_FULLSCREENAPP)
                {
                    bool hasFull = false;
                    if (lParam.ToInt32() == 1)
                    {
                        hasFull = CheckHasFullScreenApp();
                    }
                    HideAllView(hasFull);
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


        public void TryToEdgeShow()
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
                SaveLeftAndTopToSettings();
            }
            
        }

        public void TryToEdgeHide()
        {
            if (Application.Current is App app)
            {
                if (!app.screenLengthMaxOne)
                {
                    if (Settings.Default.edgeHide)
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
                                SaveLeftAndTopToSettings();
                            }
                        }
                    }

                }
            }
        }

        
        public void RegisterAppBar(bool register)
        {
            APPBARDATA abd = new APPBARDATA();
            abd.cbSize = Marshal.SizeOf(abd);
            abd.hWnd = new WindowInteropHelper(this).Handle;
            
            if (register)
            {
                //register
                abd.uCallbackMessage = uCallBackMsg;
                uint ret = WinAPIWrapper.SHAppBarMessage((int)ABMsg.ABM_NEW, ref abd);
                // Check whether there is a full screen app now.
                HideAllView(CheckHasFullScreenApp());
            }
            else
            {
                WinAPIWrapper.SHAppBarMessage((int)ABMsg.ABM_REMOVE, ref abd);
            }
        }
       
        private int uCallBackMsg, taskBarCreatedMsg;

        private DetailWindow detailWindow;
        
        public bool isEdgeHide = false;
        private double edgeHideSpace = 4;

        private double oldLeft, oldTop;
        private DateTime leftPressTime = DateTime.Now;

        public readonly Thickness windowPadding = new Thickness(-3, 0, -3, -3);

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
        }

        void ICanMoveDetailWindowToRightPlace.MoveDetailWindowToRightPlace(DetailWindow dw)
        {
            Thickness pa = windowMargin;
            Rect mainRect = new Rect(Left - pa.Left, Top - pa.Top,
                Width + pa.Left + pa.Right, Height + pa.Top + pa.Bottom);
            Rect workArea = SystemParameters.WorkArea;
            if (workArea.Bottom - mainRect.Bottom >= dw.Height)//bellow
            {
                dw.Top = mainRect.Bottom;
                if (mainRect.Left + dw.Width <= workArea.Right)
                {
                    dw.Left = mainRect.Left;
                }
                else
                {
                    dw.Left = mainRect.Right - dw.Width;
                }

            }
            else if (mainRect.Top - workArea.Top >= dw.Height)//top
            {
                dw.Top = mainRect.Top - dw.Height;
                if (mainRect.Left + dw.Width <= workArea.Right)
                {
                    dw.Left = mainRect.Left;
                }
                else
                {
                    dw.Left = mainRect.Right - dw.Width;
                }
            }
            else//left or right
            {
                if (mainRect.Right + dw.Width <= workArea.Right)//right
                {
                    dw.Left = mainRect.Right;
                }
                else
                {
                    dw.Left = mainRect.Left - dw.Width;//left
                }
                if (mainRect.Top + dw.Height <= workArea.Bottom)
                {
                    dw.Top = mainRect.Top;
                }
                else
                {
                    dw.Top = workArea.Bottom - dw.Height;
                }
            }
        }
        
        public readonly Thickness windowMargin = new Thickness(-3, 3, -3, 0);
    }
}
