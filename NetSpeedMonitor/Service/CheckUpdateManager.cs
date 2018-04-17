using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace USTC.Software.hanyizhao.NetSpeedMonitor
{
    class CheckUpdateManager
    {
        public void CheckForUpdates(bool showNoUpdateWindow)
        {
            lock(lockCheck)
            {
                if(!isChecking)
                {
                    isChecking = true;
                    this.showNoUpdateWindow = showNoUpdateWindow;
                    Thread thread = new Thread(DoCheckUpdate);
                    thread.Start();
                }
                else
                {
                    if(!this.showNoUpdateWindow)
                    {
                        this.showNoUpdateWindow = showNoUpdateWindow;
                    }
                }
            }
        }
        
        private void DoCheckUpdate()
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create("https://api.github.com/repos/hanyizhao/NetSpeedMonitor/releases/latest");
            request.UserAgent = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 5.1; Trident/4.0; .NET CLR 2.0.50727; InfoPath.2; AskTbPTV/5.17.0.25589; Alexa Toolbar)";
            try
            {
                if (request.GetResponse() is HttpWebResponse response)
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        {
                            String value = reader.ReadToEnd();
                            JObject jo = (JObject)JsonConvert.DeserializeObject(value);
                            String tag_name = jo["tag_name"].ToString();
                            String appName = Tool.GetStringResource("AppName");
                            String currentVersion = Tool.GetVersion();
                            if (Tool.CompareTwoVersionString(tag_name, currentVersion) > 0)
                            {
                                MessageBoxResult result = MessageBox.Show(Tool.GetStringResource("FindNewVersion") + tag_name
                                    + "\n" + Tool.GetStringResource("CurrentVersion") + currentVersion
                                    + "\n" + Tool.GetStringResource("UpgradeToNewVersion"),
                                    appName, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                                if (result == MessageBoxResult.OK)
                                {
                                    System.Diagnostics.Process.Start("https://github.com/hanyizhao/NetSpeedMonitor/releases/latest");
                                }
                            }
                            else
                            {
                                if (showNoUpdateWindow)
                                {
                                    MessageBox.Show(appName + " " + Tool.GetStringResource("IsUpToDate"), appName);
                                }
                            }
                        }
                    }
                    response.Close();
                }
            }
            catch (Exception e)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    MessageBox.Show(e.ToString(), Tool.GetStringResource("CheckForUpdates"), MessageBoxButton.OK, MessageBoxImage.Error);
                }));
            }
            lock (lockCheck)
            {
                isChecking = false;
                showNoUpdateWindow = false;
            }


        }

        private readonly object lockCheck = new object();
        private bool isChecking = false;
        private bool showNoUpdateWindow = false;
    }
}
