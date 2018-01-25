using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace USTC.Software.hanyizhao.NetSpeedMonitor
{
    public class ProcessView
    {
        public int ID { get { return id; } }
        public string Name { get { return name; } }
        public string FilePath { get { return filePath; } }
        public ImageSource Image { get { return image; } }
        public bool SuccessGetInfo { get { return successGetInfo; } }

        private int id;
        private string name;
        private string filePath;
        private ImageSource image;
        private bool successGetInfo;

        public ProcessView(int id)
        {
            this.id = id;
            Process process = null;
            try
            {
                process = Process.GetProcessById(id);
            }
            catch (Exception)
            {

            }
            if (process != null)
            {
                successGetInfo = true;
                try
                {
                    name = process.ProcessName;
                }
                catch (Exception)
                {

                }
                try
                {
                    filePath = process.MainModule.FileName;
                    image = Imaging.CreateBitmapSourceFromHIcon(System.Drawing.Icon.ExtractAssociatedIcon(filePath).Handle,
                        Int32Rect.Empty, BitmapSizeOptions.FromRotation(Rotation.Rotate0));
                }
                catch (Exception)
                {

                }

            }
        }
    }
}
