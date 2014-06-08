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
        public static string cerficicate_name = @"TempCert.cer";

        private SettingsReader Settings;
        private TcpListener listener;
        private X509Certificate certificate;

        public SecuredWebserver(SettingsReader Settings)
            : base(Settings)
        {
            this.Settings = Settings;
            listener = new TcpListener(IPAddress.Parse("127.0.0.1"), Settings.AdminPort);
            certificate = X509Certificate.CreateFromCertFile(cerficicate_name);
        }

        public override void StartListening()
        {
            listener.Start();
            Console.WriteLine("Secure server online, listening");

            while (true)
            {
                Socket sClient = listener.AcceptSocket();
                Console.WriteLine("Socket Type: " + sClient.SocketType);
                if (sClient.Connected)
                    Console.WriteLine("Client Connected\nClient IP: {0}", sClient.RemoteEndPoint);

                Stream stream = new NetworkStream(sClient, true);
                SslStream sslStream = new SslStream(stream, false);

                try
                {
                    sslStream.AuthenticateAsServer(certificate, false, SslProtocols.Tls, true);

                    byte[] buffer = new byte[2048];
                    StringBuilder message = new StringBuilder();
                    
                    int bit = -1;
                    do
                    {
                        bit = sslStream.Read(buffer, 0, buffer.Length);

                        Decoder decoder = Encoding.UTF8.GetDecoder();
                        char[] chars = new char[decoder.GetCharCount(buffer, 0, bit)];
                        decoder.GetChars(buffer, 0, bit, chars, 0);
                        message.Append(chars);

                        if (message.ToString().IndexOf("<EOF>") != -1)
                        {
                            break;
                        }
                    } while (bit != 0);

                    Console.WriteLine(message);
                }
                
                catch (AuthenticationException e)
                {
                    Console.WriteLine(e.Message);
                }
                
                finally
                {
                    sslStream.Close();
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
    }
}