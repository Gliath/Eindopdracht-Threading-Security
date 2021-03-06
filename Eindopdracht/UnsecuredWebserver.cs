﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Eindopdracht
{
    public class UnsecuredWebserver : AbstractWebserver
    {
        private TcpListener listener;
        private SettingsReader Settings;
        private Logger logger;

        public UnsecuredWebserver(SettingsReader Settings)
            : base(Settings)
        {
            this.Settings = Settings;
            listener = new TcpListener(IPAddress.Parse("127.0.0.1"), Settings.Port);
            logger = Logger.getInstance();
        }

        public override void StartListening()
        {
            listener.Start();
            Console.WriteLine("Server online, listening");

            while (true)
            {
                Console.WriteLine("");
                Socket sClient = listener.AcceptSocket();

                new Thread(m =>
                {
                    Byte[] bReceived = new Byte[1024];
                    int i = sClient.Receive(bReceived, bReceived.Length, 0);
                    String sRequest = Encoding.ASCII.GetString(bReceived);

                    int iFirstPos = sRequest.IndexOf(' ');
                    int iLastPos = sRequest.IndexOf(' ', iFirstPos + 1);

                    if (iFirstPos > 0 && iLastPos > 0)
                    {
                        IPEndPoint IP = sClient.RemoteEndPoint as IPEndPoint;
                        String rType = sRequest.Substring(0, iFirstPos);
                        String rURL = sRequest.Substring(iFirstPos + 1, iLastPos - (iFirstPos + 1)).Replace("\\", "/");
                        String rHTML = sRequest.Substring(iLastPos + 1, 8);

                        Console.WriteLine("Request type: {0}\nRequest URL: {1}\nRequest HTML: {2}\n", rType, rURL, rHTML);

                        String sStatus = "";
                        Byte[] bByteFile = null;
                        String path;

                        if (rType.Equals("GET"))
                            path = HandleURLRequest(rURL, out sStatus);
                        else
                        {
                            Console.WriteLine("Unsupported request type encountered: {0}", rType);
                            path = "ErrorPages\\400.html";
                        }

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
                        else // This only occurs when DirectoryBrowsing is turned on and the Directory exists
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

                        logger.put(String.Format("[{0}], {1}, ({2}): Requested URL: {3}", IP.Address.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "unknown ms", rURL));

                        sClient.Close();

                        sClient.Close();
                    }
                }).Start();
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

                    // No defaultpage exists, check if DirectoryBrowsing is true
                    if (Settings.DirectoryBrowsing)
                        // Needs special handling. (there's no default file, needs to be dynamic)
                        return "";
                }

                // The directory does not exist, return a 404 page
                sStatusCode = "404";

                return "ErrorPages\\404.html";
            }
        }
    }
}