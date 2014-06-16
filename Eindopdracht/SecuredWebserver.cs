using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Eindopdracht
{
    public class SecuredWebserver : AbstractWebserver
    {
        public static string cerficicate_name = @"TempCert.pfx";

        private SettingsReader Settings;
        private TcpListener listener;
        private X509Certificate certificate;

        public SecuredWebserver(SettingsReader Settings)
            : base(Settings)
        {
            this.Settings = Settings;
            listener = new TcpListener(IPAddress.Parse("127.0.0.1"), Settings.AdminPort);
            certificate = new X509Certificate2(cerficicate_name, "ChrisLuke");
            // X509Certificate.CreateFromCertFile(cerficicate_name);
        }

        public override void StartListening()
        {
            listener.Start();
            Console.WriteLine("Secure server online, listening");

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                SslStream stream = new SslStream(client.GetStream(), false);

                //stream.ReadTimeout = 5000;
                //stream.WriteTimeout = 5000;

                Console.WriteLine("Socket Type: " + client.Client.SocketType);
                if (client.Connected)
                    Console.WriteLine("Client Connected\nClient IP: {0}", client.Client.RemoteEndPoint);

                try
                {
                    stream.AuthenticateAsServer(certificate, false, SslProtocols.Tls, true);
                    String message = readMessage(stream);
                    Console.WriteLine(message);

                    int iFirstPos = message.IndexOf(' ');
                    int iLastPos = message.IndexOf(' ', iFirstPos + 1);

                    if (iFirstPos > 0 && iLastPos > 0)
                    {
                        String rType = message.Substring(0, iFirstPos);
                        String rURL = message.Substring(iFirstPos + 1, iLastPos - (iFirstPos + 1)).Replace("\\", "/");
                        String rHTML = message.Substring(iLastPos + 1, 8);

                        if (!rType.Equals("GET") && !rType.Equals("POST"))
                        {
                            Console.WriteLine("Unsupported request type encountered: {0}", rType);
                            // Send Error 400
                            client.Close();
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

                        Socket socket = client.Client;
                        SendHeader(rHTML, sStatus, sMime, bByteFile.Length, ref socket);
                        SendData(bByteFile, ref socket);
                    }
                }

                catch (AuthenticationException e)
                {
                    Console.WriteLine(e.Message);
                    stream.Close();
                    client.Close();
                    return;
                }

                catch (IOException e)
                {
                    Console.WriteLine(e.Message);
                    stream.Close();
                    client.Close();
                    return;
                }
                
                finally
                {
                    stream.Close();
                    client.Close();
                }
            }
        }

        private String readMessage(SslStream stream)
        {
            byte[] buffer = new byte[2048];
            StringBuilder messageData = new StringBuilder();
            int bytes = -1;
            do
            {
                bytes = stream.Read(buffer, 0, buffer.Length);

                Decoder decoder = Encoding.UTF8.GetDecoder();
                char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
                decoder.GetChars(buffer, 0, bytes, chars, 0);
                messageData.Append(chars);

                if (messageData.ToString().Contains("\r\n")) {
                    break;
                }
            } while (bytes != 0);

            return messageData.ToString();
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
    }
}