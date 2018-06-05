using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace USTC.Software.hanyizhao.NetSpeedMonitor
{
    /// <summary>
    /// AboutWindow.xaml 的交互逻辑
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            TextBlockVersion.Text = Tool.GetStringResource("Version") + " " + Tool.GetVersion();
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Hyperlink link)
            {
                Process.Start(new ProcessStartInfo(link.NavigateUri.AbsoluteUri));
            }
        }
    }
}
