using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
    /// ProcessDetail.xaml 的交互逻辑
    /// </summary>
    public partial class ProcessDetailWindow : Window
    {
        public ProcessDetailWindow(int id, MainWindow mainWindow) : this(new ProcessView(id), mainWindow)
        {

        }

        public ProcessDetailWindow(ProcessView tempP, MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            this.process = tempP;
            InitializeComponent();
            ProcessID.Text = process.ID + "";
            if (process.SuccessGetInfo)
            {
                ProcessName.Text = process.Name ?? Application.Current.FindResource("Unknown").ToString();
                ProcessIcon.Source = process.Image;
                if (process.FilePath == null && !Tool.IsAdministrator())
                {
                    OpenButtonImage.Source = Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Shield.Handle,
                        Int32Rect.Empty, BitmapSizeOptions.FromRotation(Rotation.Rotate0));
                    OpenButtonText.Text = Application.Current.FindResource("RunAsAdministratorToGetMoreInformation").ToString();
                    OpenButton.Click += OpenButton_RunAsAdmin_Click;
                }
                else
                {
                    if (process.FilePath == null)
                    {
                        OpenButton.IsEnabled = false;
                    }
                    else
                    {
                        ProcessPath.Text = process.FilePath;
                        OpenButton.Click += OpenButton_OpenPath_Click;
                    }
                }
            }
            else
            {
                ContentGrid.IsEnabled = false;
            }
        }

        private void OpenButton_OpenPath_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", "/select,\"" + process.FilePath + "\"");
        }

        private void OpenButton_RunAsAdmin_Click(object sender, RoutedEventArgs e)
        {
            if(Application.Current is App app)
            {
                app.FreeMutex();
                string exe = GetType().Assembly.Location;
                Process p = new Process
                {
                    StartInfo = new ProcessStartInfo(exe, "-processid " + process.ID)
                    {
                        Verb = "runas",
                    },
                };
                try
                {
                    bool b = p.Start();
                    app.TryToExit();
                }
                catch(Exception)
                {

                }
            }
        }

        private ProcessView process;
        private MainWindow mainWindow;

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            if(!process.SuccessGetInfo)
            {
                Dispatcher.InvokeAsync(new Action(() =>
                {
                    MessageBox.Show(Application.Current.FindResource("CantGetInformationOfThisProcessMaybeItsNotRunningNow_").ToString(),
                        Application.Current.FindResource("ERROR").ToString());
                }));
            }
        }
    }
}
