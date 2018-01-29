using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace USTC.Software.hanyizhao.NetSpeedMonitor
{
    /// <summary>
    /// WelcomeWindow.xaml 的交互逻辑
    /// </summary>
    public partial class WelcomeWindow : Window
    {
        public WelcomeWindow()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty WindowHeightAnimationProperty = DependencyProperty.Register("WindowHeightAnimation", typeof(double), typeof(WelcomeWindow), new PropertyMetadata(OnWindowHeightAnimationChanged));

        private static void OnWindowHeightAnimationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                if (d is Window window)
                {
                    IntPtr handle = new WindowInteropHelper(window).Handle;
                    if (WinAPIWrapper.GetWindowRect(handle, out RECT rect))
                    {
                        if (PresentationSource.FromVisual(window) != null)
                        {
                            int height = (int)Math.Round(window.PointToScreen(new Point(0, (double)e.NewValue)).Y - rect.top);
                            WinAPIWrapper.SetWindowPos(handle, new IntPtr((int)SpecialWindowHandles.TopMost), rect.left, rect.top, rect.right - rect.left, height, SetWindowPosFlags.SHOWWINDOW);
                        }
                    }
                }
            }
            catch (Exception)
            {

            }

        }

        public void ReduceAndClose(Point center)
        {
            Point leftTop = PointToScreen(new Point(0, 0));
            Point rightBottom = PointToScreen(new Point(ActualWidth, ActualHeight));
            float dpiX, dpiY;
            using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
            {
                dpiX = graphics.DpiX;
                dpiY = graphics.DpiY;
            }
            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap((int)(rightBottom.X - leftTop.X), (int)(rightBottom.Y - leftTop.Y), dpiX, dpiY, PixelFormats.Pbgra32);
            renderTargetBitmap.Render(this);
            ForeImage.Source = renderTargetBitmap;
            ForeImage.Visibility = Visibility.Visible;

            double time = 400;
            Storyboard storyboard = new Storyboard();
            DoubleAnimation leftA = new DoubleAnimation(Left, center.X, TimeSpan.FromMilliseconds(time), FillBehavior.Stop);
            Storyboard.SetTarget(leftA, this);
            Storyboard.SetTargetProperty(leftA, new PropertyPath(Window.LeftProperty));
            DoubleAnimation topA = new DoubleAnimation(Top, center.Y, TimeSpan.FromMilliseconds(time), FillBehavior.Stop);
            Storyboard.SetTarget(topA, this);
            Storyboard.SetTargetProperty(topA, new PropertyPath(Window.TopProperty));
            DoubleAnimation widthA = new DoubleAnimation(Width, 0, TimeSpan.FromMilliseconds(time), FillBehavior.Stop);
            Storyboard.SetTarget(widthA, this);
            Storyboard.SetTargetProperty(widthA, new PropertyPath(Window.WidthProperty));
            DoubleAnimation heightA = new DoubleAnimation(Height, 0, TimeSpan.FromMilliseconds(time), FillBehavior.Stop);
            Storyboard.SetTarget(heightA, this);
            Storyboard.SetTargetProperty(heightA, new PropertyPath(WelcomeWindow.WindowHeightAnimationProperty));
            storyboard.Children.Add(leftA);
            storyboard.Children.Add(topA);
            storyboard.Children.Add(widthA);
            storyboard.Children.Add(heightA);
            storyboard.Completed += Storyboard_Completed;
            storyboard.Begin();
        }

        private void Storyboard_Completed(object sender, EventArgs e)
        {
            Hide();
            Dispatcher.InvokeAsync(new Action(() =>
            {
                Close();
            }));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Tool.WindowMissFromMission(this);
        }
    }
}
