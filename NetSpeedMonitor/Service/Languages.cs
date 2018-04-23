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
    /// <summary>
    /// Multi-language resources
    /// </summary>
    class Languages
    {
        /// <summary>
        /// Get language list.
        /// </summary>
        /// <returns>Lnguage list</returns>
        public static List<OneLanguage> GetLanguages()
        {
            List<OneLanguage> result = new List<OneLanguage>
            {
                new OneLanguage("StringResource.xaml", "English (US)"),
                new OneLanguage("StringResource.zh-CN.xaml", "简体中文")
            };
            return result;
        }

        /// <summary>
        /// Get default language resource according to language of PC.
        /// </summary>
        /// <returns>Language resource according to language of PC. Or null if don't support the language of PC.</returns>
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

        /// <summary>
        /// Get language resource.
        /// </summary>
        /// <param name="fileName">The name of language resource file.</param>
        /// <returns>Language resource or null</returns>
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

    /// <summary>
    /// One supported language
    /// </summary>
    class OneLanguage
    {
        /// <summary>
        /// The name of language resource file.
        /// </summary>
        public String FileName { get { return fileName; } }

        /// <summary>
        /// The name used to display to user.
        /// </summary>
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
