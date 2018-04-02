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
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using USTC.Software.hanyizhao.NetSpeedMonitor.Properties;

namespace USTC.Software.hanyizhao.NetSpeedMonitor
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeWindowMenu();
        }
        
        public void SetDetailWindow(DetailWindow detail)
        {
            detailWindow = detail;
        }
        
        
        private void InitializeWindowMenu()
        {
            WindowMenuStartOnBoot.IsChecked = Settings.Default.startOnBoot;
            WindowMenuEdgeHide.IsChecked = Settings.Default.edgeHide;
            List<OneLanguage> languages = Languages.getLanguages();
            String nowLanguageFile = Settings.Default.language;
            foreach(OneLanguage i in languages)
            {
                MenuItem menuItem = new MenuItem()
                {
                    Header = i.ShowName,
                    IsCheckable = true,
                    IsChecked = i.FileName == nowLanguageFile,
                    Tag = i.FileName,
                };
                menuItem.Click += MenuItem_ChangeLanguageClick;
                WindowMenuLanguage.Items.Add(menuItem);
            }
            if(nowLanguageFile == null || nowLanguageFile == "")
            {
                WindowMenuUserDefault.IsChecked = true;
            }
            WindowMenuUserDefault.Tag = "";
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
            if (sender == WindowMenuExit)
            {
                if (Application.Current is App app)
                {
                    app.TryToExit();
                }
            }
            else if (sender == WindowMenuStartOnBoot)
            {
                bool startOnBoot = WindowMenuStartOnBoot.IsChecked;
                if (Application.Current is App app)
                {
                    app.menuStartOnBoot.Checked = startOnBoot;
                    app.TryToSetStartOnBoot(startOnBoot);
                }
            }
            else if (sender == WindowMenuEdgeHide)
            {
                bool edgeHide = WindowMenuEdgeHide.IsChecked;
                if (Application.Current is App app)
                {
                    app.menuEdgeHide.Checked = edgeHide;
                    app.TryToSetEdgeHide(edgeHide);
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

        private IntPtr desktopHandle;
        private IntPtr shellHandle;
        private int uCallBackMsg;

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

        public readonly Thickness windowMargin = new Thickness(-3, 3, -3, 0);
    }
}
