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
            if (Visibility == Visibility.Hidden)
            {
                idMap.Clear();
            }
        }

        private void InitializeContent()
        {
            int rows = ContentGrid.RowDefinitions.Count;
            canvases = new Canvas[rows];
            icons = new Image[rows];
            names = new TextBlock[rows];
            downs = new TextBlock[rows];
            ups = new TextBlock[rows];
            labels = new Label[rows];
            for (int i = 0; i < rows; i++)
            {
                Canvas canvas = new Canvas();
                Grid.SetRow(canvas, i);
                Grid.SetColumnSpan(canvas, 6);
                ContentGrid.Children.Add(canvas);
                canvases[i] = canvas;

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

                Label label = new Label();
                Grid.SetColumn(label, 2);
                Grid.SetRow(label, i);
                ContentGrid.Children.Add(label);
                labels[i] = label;
                label.MouseDown += DetailLabel_MouseDown;

                TextBlock down = new TextBlock();
                Grid.SetColumn(down, 3);
                Grid.SetRow(down, i);
                down.HorizontalAlignment = HorizontalAlignment.Right;
                down.VerticalAlignment = VerticalAlignment.Center;
                ContentGrid.Children.Add(down);
                downs[i] = down;

                TextBlock up = new TextBlock();
                Grid.SetColumn(up, 4);
                Grid.SetRow(up, i);
                up.HorizontalAlignment = HorizontalAlignment.Right;
                up.VerticalAlignment = VerticalAlignment.Center;
                ContentGrid.Children.Add(up);
                ups[i] = up;
            }
        }

        private void DetailLabel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(sender is Label label)
            {
                int row = Grid.GetRow(label);
                if(row >= 0 && row < localItems.Count)
                {
                    int id = localItems[row].ProcessID;
                    if (idMap.TryGetValue(id, out ProcessView process))
                    {
                        ProcessDetailWindow win = new ProcessDetailWindow(process, mainWindow);
                        win.Show();
                    }
                    else
                    {
                        ProcessDetailWindow win = new ProcessDetailWindow(id, mainWindow);
                        win.Show();
                    }
                }
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
                for (int i = 0; i < ContentGrid.RowDefinitions.Count; i++)
                {
                    if (names[i].Text == null || names[i].Text == "")
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
                localItems.Clear();
                localItems.AddRange(items);
                ClearViewContent();
                for (int i = 0; i < ContentGrid.RowDefinitions.Count && i < items.Count; i++)
                {
                    UDOneItem item = items[i];
                    downs[i].Text = Tool.GetNetSpeedString(item.Download, timeSpan);
                    ups[i].Text = Tool.GetNetSpeedString(item.Upload, timeSpan);
                    if (item.ProcessID == -1)
                    {
                        names[i].Text = "bridge";
                    }
                    else
                    {
                        if (!idMap.TryGetValue(item.ProcessID, out ProcessView view))
                        {
                            view = new ProcessView(item.ProcessID);
                            idMap[item.ProcessID] = view;
                        }
                        names[i].Text = view.Name ?? "Process ID: " + view.ID;
                        if (view.Image != null)
                        {
                            icons[i].Source = view.Image;
                        }
                    }
                }
            }
            RefreshDetailButton(Mouse.GetPosition(ContentGrid));
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
            RefreshDetailButton(e.GetPosition(ContentGrid));
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            OthersWantHide(false);
            RefreshDetailButton(e.GetPosition(ContentGrid));
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            RefreshDetailButton(e.GetPosition(ContentGrid));
        }

        private void RefreshDetailButton(Point p)
        {
            foreach (var i in labels)
            {
                i.Visibility = Visibility.Hidden;
            }
            foreach (var i in canvases)
            {
                i.Background = null;
            }
            if (p.X < 0 || p.Y < 0 || p.X > ContentGrid.ActualHeight || p.Y > ContentGrid.ActualWidth)
            {
                return;
            }

            int row = (int)(p.Y / 20);
            if (row >= 0 && row < ContentGrid.RowDefinitions.Count && ups[row].Text != null && ups[row].Text != "")
            {
                labels[row].Visibility = Visibility.Visible;
                canvases[row].Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f4f4f4"));
            }


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
        private Label[] labels;
        private Canvas[] canvases;

        private List<UDOneItem> localItems = new List<UDOneItem>();


    }
    
    public class ContentListViewItem
    {
        public string NAME { get; set; }
        public string DOWNLOAD { get; set; }
        public string UPLOAD { get; set; }
    }
}
