using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eindopdracht
{
    public class SettingsReader
    {
        public int Port { get; private set; }
        public int AdminPort { get; private set; }
        public String Root { get; private set; }
        public String DefaultPage { get; private set; }
        public Boolean DirectoryBrowsing { get; private set; }

        /// <summary>
        /// Provides default settings and provides them.
        /// </summary>
        public SettingsReader()
        {
            ReadSettingsFile();
        }

        public void Reload()
        {
            ReadSettingsFile();
        }

        private void ReadSettingsFile()
        {
            StreamReader sr;
            String sLine = "";

            try
            {
                sr = new StreamReader("data\\Settings.Dat");

                while ((sLine = sr.ReadLine()) != null)
                {
                    sLine.Trim();

                    if (sLine.Length > 0)
                    {
                        int iPosType = sLine.IndexOf("=");

                        String sType = sLine.Substring(0, iPosType);
                        String sValue = sLine.Substring(iPosType + 1);

                        switch (sType)
                        {
                            case "Port":
                                Port = int.Parse(sValue);
                                break;
                            case "AdminPort":
                                AdminPort = int.Parse(sValue);
                                break;
                            case "Root":
                                Root = sValue;
                                break;
                            case "DefaultPage":
                                DefaultPage = sValue;
                                break;
                            case "DirectoryBrowsing":
                                DirectoryBrowsing = Boolean.Parse(sValue);
                                break;
                            default:
                                Console.WriteLine("Unknown Setting found: {0}", sType);
                                break;
                        }
                    }
                }

                sr.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("An Exception Occurred : " + e.ToString());
            }
        }
    }
}