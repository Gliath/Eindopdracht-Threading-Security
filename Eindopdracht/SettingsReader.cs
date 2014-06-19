using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Eindopdracht
{
    public class SettingsReader
    {
        public int Port { get; private set; }
        public int AdminPort { get; private set; }
        public String Root { get; private set; }
        public String[] DefaultPages { get; private set; }
        public Boolean DirectoryBrowsing { get; private set; }

        /// <summary>
        /// Provides default settings and provides them.
        /// </summary>
        public SettingsReader()
        {
            ReadSettingsFile();
        }

        private void ReadSettingsFile()
        {
            if (!File.Exists("data\\Settings.xml")) // If there are no settings
            { // Setup default settings
                SaveNewSettings(8080, 8081, "C:\\Webserver\\Root", new String[] {"index.html", "index.htm"}, false); 
                return;
            }

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load("data\\Settings.xml");

                for (int i = 0; i < doc.FirstChild.ChildNodes.Count; i++)
                {
                    switch (doc.FirstChild.ChildNodes[i].Name)
                    {
                        case "Port":
                            Port = int.Parse(doc.FirstChild.ChildNodes[i].InnerText);
                            break;
                        case "AdminPort":
                            AdminPort = int.Parse(doc.FirstChild.ChildNodes[i].InnerText);
                            break;
                        case "Root":
                            Root = doc.FirstChild.ChildNodes[i].InnerText.Replace('\\', '/');
                            break;
                        case "DefaultPages":
                            DefaultPages = doc.FirstChild.ChildNodes[i].InnerText.Split(';');
                            break;
                        case "DirectoryBrowsing":
                            DirectoryBrowsing = Boolean.Parse(doc.FirstChild.ChildNodes[i].InnerText);
                            break;
                        default:
                            Console.WriteLine("Unknown Setting found: {0} with value: {1}", doc.FirstChild.ChildNodes[i].Name, doc.FirstChild.ChildNodes[i].InnerText);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An Exception Occurred : " + e.ToString());
            }
        }

        public void SaveNewSettings(int Port, int AdminPort, String Root, String[] DefaultPages, Boolean DirectoryBrowsing)
        {
            if (Port == 0 || AdminPort == 0 || String.IsNullOrWhiteSpace(Root))
                return;

            String sDefaultPages = "";
            for (int i = 0; i < DefaultPages.Length; i++)
                if (!String.IsNullOrWhiteSpace(DefaultPages[i]))
                    sDefaultPages += DefaultPages[i] + ";";
                else
                    return;

            sDefaultPages = sDefaultPages.Substring(0, sDefaultPages.Length - 1); // Remove the last ;

            XElement xE = new XElement("Settings",
                            new XElement("Port", Port),
                            new XElement("AdminPort", AdminPort),
                            new XElement("Root", Root),
                            new XElement("DefaultPages", sDefaultPages),
                            new XElement("DirectoryBrowsing", DirectoryBrowsing));

            if (!Directory.Exists("data"))
                Directory.CreateDirectory("data");

            File.WriteAllText("data\\Settings.xml", xE.ToString());

            ReadSettingsFile();
        }
    }
}