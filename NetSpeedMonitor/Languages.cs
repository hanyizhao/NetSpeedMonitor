using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace USTC.Software.hanyizhao.NetSpeedMonitor
{
    class Languages
    {
        public static List<OneLanguage> getLanguages()
        {
            List<OneLanguage> result = new List<OneLanguage>
            {
                new OneLanguage("StringResource.xaml", "English (US)"),
                new OneLanguage("StringResource.zh-CN.xaml", "简体中文")
            };
            return result;
        }

        public static ResourceDictionary GetFromDefaultLanguage()
        {
            ResourceDictionary result = null;
            String language = CultureInfo.CurrentUICulture.Name;
            try
            {
                result = new ResourceDictionary
                {
                    Source = new Uri("pack://application:,,,/Resources/StringResource." + language + ".xaml", UriKind.RelativeOrAbsolute)
                };
            }
            catch (Exception)
            {
                result = null;
            }
            return result;
        }

        public static ResourceDictionary GetResourceDictionary(String fileName)
        {
            ResourceDictionary result = null;
            if(fileName != null)
            {
                try
                {
                    result = new ResourceDictionary
                    {
                        Source = new Uri("pack://application:,,,/Resources/" + fileName, UriKind.RelativeOrAbsolute)
                    };
                }
                catch (Exception)
                {
                    result = null;
                }
            }
            return result;
        }
    }

    class OneLanguage
    {

        public String FileName { get { return fileName; } }
        public String ShowName { get { return showName; } }

        public OneLanguage(String fileName, String showName)
        {
            this.fileName = fileName;
            this.showName = showName;
        }

        private String fileName;
        private String showName;
    }
}
