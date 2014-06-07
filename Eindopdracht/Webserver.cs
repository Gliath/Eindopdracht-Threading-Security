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

                    Byte[] bByteFile = null;
                    String path = HandleURLRequest(rURL, out sStatus);
                    if (!String.IsNullOrWhiteSpace(path))
                    {
                        FileStream fStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                        BinaryReader bReader = new BinaryReader(fStream);

                        byte[] bytes = new byte[fStream.Length];
                        int read;
                        while ((read = bReader.Read(bytes, 0, bytes.Length)) != 0) { }

                        bByteFile = bytes;

                        bReader.Close();
                        fStream.Close();
                    }
                    else // This only occurs when DirectoryBrowsing is on and the Directory exists
                    {
                        try
                        {
                            rURL = rURL.EndsWith("/") ? rURL : rURL + "/";
                            String sHTML = "";
                            String templateHTML = "";
                            String templateList = "";
                            String templateListItem = "";

                            using (StreamReader sr = new StreamReader("Template\\DirectoryBrowsing.html"))
                                templateHTML = sr.ReadToEnd();

                            using (StreamReader sr = new StreamReader("Template\\DirBrowseList.html"))
                                templateListItem = sr.ReadToEnd();

                            foreach (String sItem in GetAllFilesFromDirectory(rURL))
                                templateList += templateListItem.Replace("{Item}", sItem);

                            sHTML = templateHTML.Replace("{List}", templateList).Replace("{URL}", rURL);
                            bByteFile = Encoding.ASCII.GetBytes(sHTML);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("The template could not be read. Message:");
                            Console.WriteLine(e.Message);
                        }
                    }

                    String sMime = GetMime(rURL.Substring(rURL.Contains('.') ? rURL.LastIndexOf('.') : 0));

                    SendHeader(rHTML, sStatus, sMime, bByteFile.Length, ref sClient);
                    SendData(bByteFile, ref sClient);

                    sClient.Close();
                }
            }
        }

        private String HandleURLRequest(String rURL, out String sStatusCode)
        {
            sStatusCode = "200";

            int iPos;
            String fileName = "";
            String path = GetLocalPath(rURL);

            // First check if there is something behind the /
            // Secondly check what after the / stands contains a .
            // Thirdly check if the length of the filename (that what stands behind the / ) is longer or equal than 3
            // Lastly check if the file has a name or extention which is atleast 1 character long, by checking if the . is within the appropriate bounds.
            // If so, it's a file (/dir/f.l or /directory/file.withextenstion)
            if ((iPos = rURL.LastIndexOf('/') + 1) < rURL.Length && (fileName = rURL.Substring(iPos)).Contains('.') && (fileName.LastIndexOf('.') >= 1 && fileName.LastIndexOf('.') < fileName.Length - 1))
            {
                if (File.Exists(path))
                    return path; // The file exists, return the path

                // The file does not exist, return a 404 page
                sStatusCode = "404";
                return "ErrorPages\\404.html";
            }
            else // If not it's a directory (/dir or /dir/
            {
                if (Directory.Exists(path))
                {
                    path = path.EndsWith("/") ? path : path + '/';
                    String file = GetTheDefaultFileName(path.EndsWith("/") ? path : path + '/'); // Makes sure that the last char is a /
                    if (!String.IsNullOrWhiteSpace(file))
                        return path + file; // The default file exists, return the path

                    // No defaultpage exists, check if DirectoryBrowsing aanstaat
                    if (Settings.DirectoryBrowsing)
                        // Needs special handling. (there's no default file, needs to be dynamic)
                        return "";
                }

                // The directory does not exist, return a 404 page
                sStatusCode = "404";
                return "ErrorPages\\404.html";
            }
        }

        private void SendHeader(String sHTML, String sStatus, String sMime, int iLength, ref Socket sClient)
        {
            String sBuffer = "";
            sBuffer += sHTML + " " + sStatus + "\r\n";
            sBuffer += "Content-Type: " + sMime + "\r\n";
            sBuffer += "Content-Length: " + iLength + "\r\n\r\n";

            Byte[] bSendData = Encoding.ASCII.GetBytes(sBuffer);
            SendData(bSendData, ref sClient);

            Console.WriteLine("Header Total Bytes: " + iLength.ToString());
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
                if (File.Exists(sLocalDirectory + Settings.DefaultPages[i]))
                {
                    sFile = Settings.DefaultPages[i];
                    break;
                }
            }

            return sFile;
        }

        private String GetLocalPath(String requestedDirectory)
        {
            return Settings.Root + (requestedDirectory.StartsWith("/") ? requestedDirectory.Substring(1) : requestedDirectory);
        }

        private String[] GetAllFilesFromDirectory(String requestedDirectory)
        {
            try
            {
                int iPos = 0;
                String fileName = "";
                String[] fAbsolute = Directory.GetFileSystemEntries(GetLocalPath(requestedDirectory), "*", SearchOption.TopDirectoryOnly);
                String[] fRelative = new String[fAbsolute.Length];
                for (int i = 0; i < fAbsolute.Length; i++)
                {
                    fRelative[i] = requestedDirectory + fAbsolute[i].Substring(fAbsolute[i].LastIndexOf('/') + 1);

                    if (!fAbsolute[i].EndsWith("/") && !((iPos = fAbsolute[i].LastIndexOf('/') + 1) < fAbsolute[i].Length && (fileName = fAbsolute[i].Substring(iPos)).Contains('.') && (fileName.LastIndexOf('.') >= 1 && fileName.LastIndexOf('.') < fileName.Length - 1))) // Is it a directory or not
                        fRelative[i] += "/";
                }

                return fRelative;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error occured when retrieving files from directory.\n{0}", e.ToString());
            }

            return new String[] { };
        }
    }
}