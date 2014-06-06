using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Eindopdracht
{
    public class Webserver
    {
        private TcpListener listener;

        private Dictionary<String, String> MimeDictionary;
        private SettingsReader Settings;

        public Webserver(SettingsReader Settings)
        {
            this.Settings = Settings;

            // Setup MimeDictionary
            MimeDictionary = new Dictionary<string, string>();
            MimeDictionary.Add(".html", "text/html");
            MimeDictionary.Add(".htm", "text/html");
            MimeDictionary.Add(".xml", "text/xml");
            MimeDictionary.Add(".jpg", "image/jpeg");
            MimeDictionary.Add(".png", "image/png");
            MimeDictionary.Add(".bmp", "image/bmp");
            MimeDictionary.Add(".gif", "image/gif");
            MimeDictionary.Add(".ico", "image/x-icon");

            listener = new TcpListener(IPAddress.Parse("127.0.0.1"), Settings.Port);
        }

        private String GetMime(String ext)
        {
            if (MimeDictionary.ContainsKey(ext))
                return MimeDictionary[ext];

            return "text/html";
        }

        private string GetTheDefaultFileName(string sLocalDirectory)
        {
            String sFile = "";
            for (int i = 0; i < Settings.DefaultPages.Length; i++)
            {
                if (File.Exists(sLocalDirectory + Settings.DefaultPages[i]) == true)
                {
                    sFile = Settings.DefaultPages[i];
                    break;
                }
            }

            return sFile;
        }

        private String GetLocalDirectory(String requestedDirectory)
        {
            return Settings.Root + requestedDirectory;
        }

        private String[] GetAllFilesFromDirectory(String requestedDirectory)
        {
            try {
                return Directory.GetFiles(requestedDirectory, "*", SearchOption.AllDirectories);
            } catch (Exception e) {
                Console.WriteLine("Error occured when retrieving files from directory.\n{0}", e.ToString());
            }

            return new String[] { };
        }
    }
}