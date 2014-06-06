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
        private Boolean Secured;

        private Dictionary<String, String> MimeDictionary;
        private SettingsReader Settings;

        public Webserver(SettingsReader Settings, Boolean Secured)
        {
            this.Settings = Settings;
            this.Secured = Secured;

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

            listener = new TcpListener(IPAddress.Parse("127.0.0.1"), Secured ? Settings.AdminPort : Settings.Port);
        }

        public void StartListening()
        {
            listener.Start();
            Console.WriteLine((Secured ? "Admin" : "Normal") + " server online, listening");

            while (true)
            {
                Socket sClient = listener.AcceptSocket();
                Console.WriteLine("Socket Type: " + sClient.SocketType);
                if (sClient.Connected)
                    Console.WriteLine("Client Connected\nClient IP: {0}", sClient.RemoteEndPoint);

                Byte[] bReceived = new Byte[1024];
                int i = sClient.Receive(bReceived, bReceived.Length, 0);
                String sRequest = Encoding.ASCII.GetString(bReceived);
                //Console.WriteLine(sRequest);

                int iFirstPos = sRequest.IndexOf(' ');
                int iLastPos = sRequest.IndexOf(' ', iFirstPos + 1);

                if (iFirstPos > 0 && iLastPos > 0)
                {
                    String rType = sRequest.Substring(0, iFirstPos);
                    String rURL = sRequest.Substring(iFirstPos + 1, iLastPos - (iFirstPos + 1)).Replace("\\", "/");
                    String rHTML = sRequest.Substring(iLastPos + 1, 8);

                    if (!rType.Equals("GET") && !rType.Equals("POST"))
                    {
                        Console.WriteLine("Unsupported request type encountered: {0}", rType);
                        // Send Error 400
                        sClient.Close();
                        continue;
                    }

                    Console.WriteLine("Request type: {0}\nRequest URL: {1}\nRequest HTML: {2}", rType, rURL, rHTML);

                    String sStatus = "";
                    // Will be a path to a 40x page if something went wrong or default file or the requested file
                    /*

                    FileStream fStream = new FileStream(HandleURLRequest(rURL, out sStatus), FileMode.Open, FileAccess.Read, FileShare.Read);
                    BinaryReader bReader = new BinaryReader(fStream);

                    int iFileLength = 0;
                    String sByteFile = "";
                    byte[] bytes = new byte[fStream.Length];
                    int read;
                    while ((read = bReader.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        sByteFile += Encoding.ASCII.GetString(bytes, 0, read);
                        iFileLength += read;
                    }

                    bReader.Close();
                    fStream.Close();

                    SendHeader(rHTML, sStatus, GetMime(rURL.Substring(rURL.LastIndexOf('.'))), iFileLength, ref sClient);
                    SendData(Encoding.ASCII.GetBytes(sByteFile), ref sClient);
                    */
                    sClient.Close();
                }
            }
        }

        private String HandleURLRequest(String rURL, out String sStatusCode)
        {
            sStatusCode = "200";

            return "/";
        }

        private void SendHeader(String rHTML, String sStatus, String p, int iFileLength, ref Socket sClient)
        {

        }

        private void SendData(byte[] bData, ref Socket sClient)
        {
            int iBytes = 0;
            try
            {
                if (sClient.Connected)
                    if ((iBytes = sClient.Send(bData, bData.Length, 0)) == -1)
                        Console.WriteLine("Socket Error.\nCould not send data");
                    else
                        Console.WriteLine("Bytes sent: {0}", iBytes);
                else
                    Console.WriteLine("Connection has dropped");
            }
            catch (Exception e)
            {
                Console.WriteLine("An error appeared: {0}", e.ToString());
            }
        }

        private String GetMime(String ext)
        {
            if (MimeDictionary.ContainsKey(ext))
                return MimeDictionary[ext];

            return "text/html";
        }

        private String GetTheDefaultFileName(String sLocalDirectory)
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
            try
            {
                return Directory.GetFiles(requestedDirectory, "*", SearchOption.AllDirectories);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error occured when retrieving files from directory.\n{0}", e.ToString());
            }

            return new String[] { };
        }
    }
}