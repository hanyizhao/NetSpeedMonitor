using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace USTC.Software.hanyizhao.NetSpeedMonitor
{
    /// <summary>
    /// DetailWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DetailWindow : Window
    {
        public DetailWindow(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            asynShow = new Run(AsynShow);
            asynHide = new Run(AsynHide);
            InitializeComponent();
            InitializeContent();
            IsVisibleChanged += DetailWindow_IsVisibleChanged;
        }

        private void DetailWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if(Visibility == Visibility.Hidden)
            {
                idMap.Clear();
            }
        }

        private void InitializeContent()
        {
            int rows = ContentGrid.RowDefinitions.Count;
            icons = new Image[rows];
            names = new TextBlock[rows];
            downs = new TextBlock[rows];
            ups = new TextBlock[rows];
            for (int i = 0; i < rows; i++)
            {
                Image icon = new Image();
                Grid.SetColumn(icon, 0);
                Grid.SetRow(icon, i);
                ContentGrid.Children.Add(icon);
                icons[i] = icon;

                TextBlock name = new TextBlock();
                Grid.SetColumn(name, 1);
                Grid.SetRow(name, i);
                name.VerticalAlignment = VerticalAlignment.Center;
                ContentGrid.Children.Add(name);
                names[i] = name;

                TextBlock down = new TextBlock();
                Grid.SetColumn(down, 2);
                Grid.SetRow(down, i);
                down.HorizontalAlignment = HorizontalAlignment.Right;
                down.VerticalAlignment = VerticalAlignment.Center;
                ContentGrid.Children.Add(down);
                downs[i] = down;

                TextBlock up = new TextBlock();
                Grid.SetColumn(up, 3);
                Grid.SetRow(up, i);
                up.HorizontalAlignment = HorizontalAlignment.Right;
                up.VerticalAlignment = VerticalAlignment.Center;
                ContentGrid.Children.Add(up);
                ups[i] = up;
            }
        }

        private void ClearViewContent()
        {
            for (int i = 0; i < ContentGrid.RowDefinitions.Count; i++)
            {
                icons[i].Source = null;
                names[i].Text = null;
                downs[i].Text = null;
                ups[i].Text = null;
            }
        }

        public void NewData(List<UDOneItem> items, double timeSpan)
        {
            if (items.Count == 0)
            {
                for(int i = 0;i < ContentGrid.RowDefinitions.Count;i++)
                {
                    if(names[i].Text == null|| names[i].Text == "")
                    {
                        break;
                    }
                    else
                    {
                        ups[i].Text = "0K/s";
                        downs[i].Text = "0K/s";
                    }
                }
            }
            else
            {
                ClearViewContent();
                for(int i =0;i < ContentGrid.RowDefinitions.Count && i < items.Count;i++)
                {
                    UDOneItem item = items[i];
                    downs[i].Text = Tool.GetNetSpeedString(item.download, timeSpan);
                    ups[i].Text = Tool.GetNetSpeedString(item.upload, timeSpan);
                    if(item.processID == -1)
                    {
                        names[i].Text = "bridge";
                    }
                    else
                    {
                        if (!idMap.TryGetValue(item.processID, out ProcessView view))
                        {
                            view = new ProcessView();
                            Process process = null;
                            try
                            {
                                process = Process.GetProcessById(item.processID);
                            }
                            catch (Exception)
                            {
                                view.name = "Process ID: " + item.processID;
                            }
                            if (process != null)
                            {
                                try
                                {
                                    view.name = process.ProcessName;
                                }
                                catch (Exception)
                                {
                                    view.name = "Process ID: " + item.processID;
                                }
                                try
                                {
                                    view.filePath = process.MainModule.FileName;
                                    view.image = Imaging.CreateBitmapSourceFromHIcon(System.Drawing.Icon.ExtractAssociatedIcon(view.filePath).Handle,
                                        Int32Rect.Empty, BitmapSizeOptions.FromRotation(Rotation.Rotate0));
                                }
                                catch (Exception)
                                {
                                    
                                }
                            }
                            idMap[item.processID] = view;
                        }
                        
                        names[i].Text = view.name;
                        if (view.image != null)
                        {
                            icons[i].Source = view.image;
                        }
                    }
                }
            }
        }

        public void OthersWantShow(bool now)
        {
            runManager.RemoveMission(asynHide);
            if (Visibility == Visibility.Visible)
            {
                runManager.RemoveMission(asynShow);
            }
            else
            {
                if (now)
                {
                    MyShow();
                    runManager.RemoveMission(asynShow);
                }
                else
                {
                    runManager.RunAfter(asynShow, 1000);
                }
            }
        }

        public void OthersWantHide(bool now)
        {
            runManager.RemoveMission(asynShow);
            if (Visibility == Visibility.Hidden)
            {
                runManager.RemoveMission(asynHide);
            }
            else
            {
                if (now)
                {
                    Hide();
                    runManager.RemoveMission(asynHide);
                }
                else
                {
                    runManager.RunAfter(asynHide, 1000);
                }
            }
        }

        private void MoveToSafePlace()
        {
            Thickness pa = mainWindow.windowMargin;
            Rect mainRect = new Rect(mainWindow.Left - pa.Left, mainWindow.Top - pa.Top,
                mainWindow.Width + pa.Left + pa.Right, mainWindow.Height + pa.Top + pa.Bottom);
            Rect workArea = SystemParameters.WorkArea;
            if (workArea.Bottom - mainRect.Bottom >= Height)//bellow
            {
                Top = mainRect.Bottom;
                if (mainRect.Left + Width <= workArea.Right)
                {
                    Left = mainRect.Left;
                }
                else
                {
                    Left = mainRect.Right - Width;
                }

            }
            else if (mainRect.Top - workArea.Top >= Height)//top
            {
                Top = mainRect.Top - Height;
                if (mainRect.Left + Width <= workArea.Right)
                {
                    Left = mainRect.Left;
                }
                else
                {
                    Left = mainRect.Right - Width;
                }
            }
            else//left or right
            {
                if (mainRect.Right + Width <= workArea.Right)//right
                {
                    Left = mainRect.Right;
                }
                else
                {
                    Left = mainRect.Left - Width;//left
                }
                if (mainRect.Top + Height <= workArea.Bottom)
                {
                    Top = mainRect.Top;
                }
                else
                {
                    Top = workArea.Bottom - Height;
                }
            }


        }

        private void MyShow()
        {
            MoveToSafePlace();
            Show();
        }

        private void AsynShow()
        {
            Dispatcher.InvokeAsync(new Action(() =>
            {
                MyShow();
            }));
        }

        private void AsynHide()
        {
            Dispatcher.InvokeAsync(new Action(() =>
            {
                Hide();
            }));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Tool.WindowMissFromMission(this);
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            OthersWantShow(false);
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            OthersWantHide(false);
        }



        private MainWindow mainWindow;

        private Run asynShow, asynHide;

        private void Image_MouseEnter(object sender, MouseEventArgs e)
        {
            CloseImage.Opacity = 1;
        }

        private void Image_MouseLeave(object sender, MouseEventArgs e)
        {
            CloseImage.Opacity = 0.3;
        }

        private void CloseImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CloseImage.Opacity = 0.3;
        }

        private void CloseImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            OthersWantHide(true);
        }

        private DelayRunManager runManager = new DelayRunManager();
        private Dictionary<int, ProcessView> idMap = new Dictionary<int, ProcessView>();

        private Image[] icons;
        private TextBlock[] names;
        private TextBlock[] ups;
        private TextBlock[] downs;

        private class ProcessView
        {
            public string name;
            public string filePath;
            public ImageSource image;
        }
    }

    public class ContentListViewItem
    {
        public string NAME { get; set; }
        public string DOWNLOAD { get; set; }
        public string UPLOAD { get; set; }
    }
}
