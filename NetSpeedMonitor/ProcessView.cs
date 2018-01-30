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
    /// <summary>
    /// Some information of a Process, including PID, name, file path and icon.
    /// </summary>
    public class ProcessView
    {
        /// <summary>
        /// PID, not null.
        /// </summary>
        public int ID { get { return id; } }

        /// <summary>
        /// Name of the process. Check <see cref="SuccessGetInfo"/> before use.
        /// </summary>
        public string Name { get { return name; } }

        /// <summary>
        /// File path of the process. Because of authority problem, Maybe It's null even <see cref="SuccessGetInfo"/> is True.
        /// </summary>
        public string FilePath { get { return filePath; } }

        /// <summary>
        /// Icon of the process. Because of authority problem, Maybe It's null even <see cref="SuccessGetInfo"/> is True. Some system process doesn't have icon.
        /// </summary>
        public ImageSource Image { get { return image; } }

        /// <summary>
        /// Check If getting information of the process successfully. Maybe it's False when the process ID expired.
        /// </summary>
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
