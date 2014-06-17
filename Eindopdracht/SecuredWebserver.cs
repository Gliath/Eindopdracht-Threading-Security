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
        /* Voor SSL
        public static string cerficicate_name = @"TempCert.pfx";
        private X509Certificate certificate;
        */

        private SettingsReader Settings;
        private TcpListener listener;

        public SecuredWebserver(SettingsReader Settings)
            : base(Settings)
        {
            this.Settings = Settings;
            listener = new TcpListener(IPAddress.Parse("127.0.0.1"), Settings.AdminPort);
            //certificate = new X509Certificate2(cerficicate_name, "ChrisLuke");
        }

        public override void StartListening()
        {
            listener.Start();
            Console.WriteLine("'Secure' server online, listening");

            while (true)
            {
                /* Voor SSL
                TcpClient client = listener.AcceptTcpClient();

                //stream.ReadTimeout = 5000;
                //stream.WriteTimeout = 5000;
                */

                // Debug, SSL te ontwijken
                Socket sClient = listener.AcceptSocket();
                Console.WriteLine("Socket Type: " + sClient.SocketType);
                if (sClient.Connected)
                    Console.WriteLine("Client Connected\nClient IP: {0}", sClient.RemoteEndPoint);

                /* SSL afhandeling
                Console.WriteLine("Socket Type: " + client.Client.SocketType);
                if (client.Connected)
                    Console.WriteLine("Client Connected\nClient IP: {0}", client.Client.RemoteEndPoint);
                */

                try
                {
                    /* SSL Afhandeling
                    stream.AuthenticateAsServer(certificate, false, SslProtocols.Tls, true);
                    String message = readMessage(stream);
                    Console.WriteLine(message);
                    */

                    Byte[] byteMessage = new Byte[1024];
                    int i = sClient.Receive(byteMessage, byteMessage.Length, 0);
                    String message = Encoding.ASCII.GetString(byteMessage);
                    Console.WriteLine(message);

                    int iFirstPos = message.IndexOf(' ');
                    int iLastPos = message.IndexOf(' ', iFirstPos + 1);

                    if (iFirstPos > 0 && iLastPos > 0)
                    {
                        String type = message.Substring(0, iFirstPos);
                        String url = message.Substring(iFirstPos + 1, iLastPos - (iFirstPos + 1)).Replace("\\", "/");
                        String html = message.Substring(iLastPos + 1, 8);

                        if (!type.Equals("GET") && !type.Equals("POST"))
                        {
                            Console.WriteLine("Unsupported request type encountered: {0}", type);
                            // Send Error 400
                            //client.Close(); Nodig voor SSL
                            continue;
                        }

                        Console.WriteLine("Request type: {0}\nRequest URL:  {1}\nRequest HTML: {2}", type, url, html);

                        if (type.Equals("GET"))
                        {
                            String status = "";
                            // Will be a path to a 40x page or the requested file

                            Byte[] byteFile = null;
                            String path = HandleGETRequest(url, out status);

                            try
                            {
                                String htmlPage = "";

                                using (StreamReader sr = new StreamReader(path))
                                    htmlPage = sr.ReadToEnd();

                                htmlPage = htmlPage.Replace("{LoginError}", "");

                                byteFile = Encoding.ASCII.GetBytes(htmlPage);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("File could not be read. Message:");
                                Console.WriteLine(e.Message);
                            }

                            //Socket socket = client.Client; Nodig voor SSL
                            SendHeader(html, status, "text/html", byteFile.Length, ref sClient);
                            SendData(byteFile, ref sClient);
                        }
                        else // PUT
                        {
                            String postParams = message.Substring(message.LastIndexOf("\r\n"));
                            Console.WriteLine("Post parameters: {0}", postParams);
                        }

                    }
                }

                catch (AuthenticationException e)
                {
                    Console.WriteLine(e.Message);
                    /*
                    stream.Close();
                    */
                    sClient.Close();

                    return;
                }

                catch (IOException e)
                {
                    Console.WriteLine(e.Message);
                    /*
                    stream.Close();
                    */
                    sClient.Close();

                    return;
                }
                
                finally
                {
                    /*
                    stream.Close();
                    */

                    sClient.Close();
                }
            }
        }
        /// <summary>
        /// Uses an SslStream to read the message.
        /// </summary>
        /// <param name="stream">The SslStream</param>
        /// <returns></returns>
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

        private String HandleGETRequest(String url, out String statusCode)
        {
            statusCode = "200";
            String path = "";

            switch (url)
            {
                case "/":
                    path = "index.html";
                    break;
                case "/log":
                    break;
                case "/users":
                    break;
                case "/create":
                    break;
                case "/edit":
                    break;
                case "/delete":
                    break;
                case "/deleteUser":
                    break;
                default:
                    break;
            }

            if (String.IsNullOrWhiteSpace(path))
            {
                // The file does not exist, return a 404 page
                statusCode = "404";
                return "ErrorPages\\404.html";
            }
            else
            {
                path = "SecuredPages\\" + path;
                return path;
            }
        }
    }
}