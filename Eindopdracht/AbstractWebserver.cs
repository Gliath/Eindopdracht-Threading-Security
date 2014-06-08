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
    public abstract class AbstractWebserver
    {
        private Dictionary<String, String> MimeDictionary;
        private SettingsReader Settings;

        public AbstractWebserver(SettingsReader Settings)
        {
            this.Settings = Settings;

            // Setup MimeDictionary
            MimeDictionary = new Dictionary<String, String>();
            MimeDictionary.Add(".html", "text/html");
            MimeDictionary.Add(".htm", "text/html");
            MimeDictionary.Add(".xml", "text/xml");
            MimeDictionary.Add(".jpg", "image/jpeg");
            MimeDictionary.Add(".png", "image/png");
            MimeDictionary.Add(".bmp", "image/bmp");
            MimeDictionary.Add(".gif", "image/gif");
            MimeDictionary.Add(".ico", "image/x-icon");
        }

        public abstract void StartListening();

        protected void SendHeader(String sHTML, String sStatus, String sMime, int iLength, ref Socket sClient)
        {
            String sBuffer = "";
            sBuffer += sHTML + " " + sStatus + "\r\n";
            sBuffer += "Content-Type: " + sMime + "\r\n";
            sBuffer += "Content-Length: " + iLength + "\r\n\r\n";

            Byte[] bSendData = Encoding.ASCII.GetBytes(sBuffer);
            SendData(bSendData, ref sClient);

            Console.WriteLine("Header Total Bytes: " + iLength.ToString());
        }

        protected void SendData(byte[] bData, ref Socket sClient)
        {
            int iBytes = 0;
            try
            {
                if (sClient.Connected)
                    if ((iBytes = sClient.Send(bData, bData.Length, 0)) == -1)
                        Console.WriteLine("Socket Error.\nData could not be send");
                    else
                        Console.WriteLine("Bytes sent: {0}", iBytes);
                else
                    Console.WriteLine("Connection has dropped");
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occured: {0}", e.ToString());
            }
        }

        protected String GetMime(String ext)
        {
            if (MimeDictionary.ContainsKey(ext))
                return MimeDictionary[ext];

            return "text/html";
        }

        protected String GetTheDefaultFileName(String sLocalDirectory)
        {
            String sFile = "";
            for (int i = 0; i < Settings.DefaultPages.Length; i++)
            {
                if (File.Exists(sLocalDirectory + Settings.DefaultPages[i]))
                {
                    sFile = Settings.DefaultPages[i];
                    break;
                }
            }

            return sFile;
        }

        protected String GetLocalPath(String requestedDirectory)
        {
            return Settings.Root + (requestedDirectory.StartsWith("/") ? requestedDirectory.Substring(1) : requestedDirectory);
        }

        protected String[] GetAllFilesFromDirectory(String requestedDirectory)
        {
            try
            {
                String[] fAbsolute = Directory.GetFileSystemEntries(GetLocalPath(requestedDirectory), "*", SearchOption.TopDirectoryOnly);
                String[] fRelative = new String[fAbsolute.Length];
                for (int i = 0; i < fAbsolute.Length; i++)
                {
                    fRelative[i] = requestedDirectory + fAbsolute[i].Substring(fAbsolute[i].LastIndexOf('/') + 1);

                    int iPos;
                    String fileName;
                    if (!fAbsolute[i].EndsWith("/") && !((iPos = fAbsolute[i].LastIndexOf('/') + 1) < fAbsolute[i].Length && (fileName = fAbsolute[i].Substring(iPos)).Contains('.') && (fileName.LastIndexOf('.') >= 1 && fileName.LastIndexOf('.') < fileName.Length - 1))) // Is it a directory or not
                        fRelative[i] += "/";
                }

                return fRelative;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error occured when retrieving files from the directory.\n{0}", e.ToString());
            }

            return new String[] { };
        }
    }
}