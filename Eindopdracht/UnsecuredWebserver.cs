using Newtonsoft.Json.Linq;
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
        private Dictionary<string, int> activeIPs;
        private SessionManager sessionManager;
        private Logger logger;

        public UnsecuredWebserver(SettingsReader Settings, Connector connector, Logger logger)
            : base(Settings)
        {
            this.Settings = Settings;
            listener = new TcpListener(IPAddress.Parse("127.0.0.1"), Settings.Port);
            activeIPs = new Dictionary<string, int>();
            sessionManager = new SessionManager(connector);
            this.logger = logger;
        }

        public override void StartListening()
        {
            listener.Start();
            Console.WriteLine("Server online, listening");
           
            while (true)
            {
                Socket sClient = listener.AcceptSocket();
                Console.WriteLine("Socket Type: " + sClient.SocketType);
                if (sClient.Connected)
                    Console.WriteLine("Client Connected\nClient IP: {0}", sClient.RemoteEndPoint);

                new Thread(m =>
                {
                    Byte[] bReceived = new Byte[1024];
                    int i = sClient.Receive(bReceived, bReceived.Length, 0);
                    String sRequest = Encoding.ASCII.GetString(bReceived);
                    Console.WriteLine(sRequest);

                    int iFirstPos = sRequest.IndexOf(' ');
                    int iLastPos = sRequest.IndexOf(' ', iFirstPos + 1);

                    if (iFirstPos > 0 && iLastPos > 0)
                    {
                        IPEndPoint IP = sClient.RemoteEndPoint as IPEndPoint;
                        String rType = sRequest.Substring(0, iFirstPos);
                        String rURL = sRequest.Substring(iFirstPos + 1, iLastPos - (iFirstPos + 1)).Replace("\\", "/");
                        String rHTML = sRequest.Substring(iLastPos + 1, 8);
                        
                        switch(rType) {
                            case "GET":
                                HandleGetWithURL(sRequest, rURL, IP.Address.ToString());
                                break;
                            case "POST":
                                HandlePostWithURL(sRequest, rURL, IP.Address.ToString());
                                break;
                            default:
                                Console.WriteLine("Unsupported request type encountered: {0}", rType);
                                sClient.Close(); // Send Error 400
                                break;
                        }

                        Console.WriteLine("Request type: {0}\nRequest URL: {1}\nRequest HTML: {2}", rType, rURL, rHTML);

                        /*String sStatus = "";
                        // Will be a path to a 40x page if something went wrong or default file or the requested file

                        Byte[] bByteFile = null;
                        String path = HandleURLRequest(rURL, out sStatus);

                        if (!String.IsNullOrWhiteSpace(path))
                        {
                            FileStream fStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                            BinaryReader bReader = new BinaryReader(fStream);

                            if (rType.Equals("POST"))
                            {
                                Dictionary<string, string> post = HandlePostRequest(sRequest);

                                switch (rURL)
                                {
                                    case "/login":
                                        SessionManager.Warning warning;
                                        HandleLoginAttempt(post["body"], sClient.RemoteEndPoint.ToString(), out warning);
                                        break;
                                }
                            }

                            
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
                    }*/

                    sClient.Close();
                    }
                }).Start();
            }
        }

        private int getHashcodeOfActiveIP(String IP)
        {
            return activeIPs[IP];
        }

        private Session getSessionOfActiveIP(String IP)
        {
            return sessionManager.getSession(getHashcodeOfActiveIP(IP));
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

        private void HandleGetWithURL(String request, String url, String IP)
        {
            bool isLoggedIn = CheckStateOfSession(IP);
            User user = isLoggedIn ? getSessionOfActiveIP(IP).User : null;

            switch (url)
            {
                case "/logs":
                    if (isLoggedIn == false)
                    {
                        Console.WriteLine("No access to this feature.");
                        return;
                    }

                    switch (user.Type)
                    {
                        case User.USER_TYPE.ADMIN:
                            Console.WriteLine("Admin part of logs");
                            break;
                        case User.USER_TYPE.SUPPORTER:
                            Console.WriteLine("Supporter part of logs");
                            break;
                    }

                    break;
                case "/settings":
                    if (isLoggedIn == false)
                    {
                        Console.WriteLine("No access to this feature.");
                        return;
                    }

                    switch (user.Type)
                    {
                        case User.USER_TYPE.ADMIN:
                            Console.WriteLine("Admin part of settings");
                            break;
                        case User.USER_TYPE.SUPPORTER:
                            Console.WriteLine("Supporter part of settings");
                            break;
                    }

                    break;
            }
        }

        private void HandlePostWithURL(String request, String url, String IP)
        {
            Dictionary<string, string> post = HandlePostRequest(request);

            switch (url)
            {
                case "/login":
                    if (activeIPs.ContainsKey(IP))
                    {
                        Console.Write("The user is already logged in.");
                        break;
                    }

                    SessionManager.Warning warning;
                    int hashcode = HandleLoginAttempt(post["body"], IP, out warning);

                    switch (warning)
                    {
                        case SessionManager.Warning.WRONG_COMBINATION:
                            Console.WriteLine("The user has entered a wrong combination.");
                            break;
                        case SessionManager.Warning.USER_ALREADY_LOGGED_IN:
                            Console.WriteLine("The user is already logged in.");
                            break;
                        case SessionManager.Warning.SESSION_EXPIRED:
                            Console.WriteLine("The session is expired.");
                            break;
                        case SessionManager.Warning.BLOCKED_IP:
                            Console.WriteLine("{0} is blocked.", IP);
                            break;
                        case SessionManager.Warning.NONE:
                            activeIPs.Add(IP, hashcode);
                            Console.WriteLine("The user has logged in.");
                            break;
                    }

                    break;

                case "/logout":
                    if (activeIPs.ContainsKey(IP))
                    {      
                        sessionManager.removeSession(getHashcodeOfActiveIP(IP));
                        activeIPs.Remove(IP);
                        Console.WriteLine("user has logged out");
                    }

                    Console.WriteLine("no user logged in");

                    break;
            }
        }

        private Dictionary<string, string> HandlePostRequest(String request)
        {
            Dictionary<string, string> post = new Dictionary<string, string>();

            Match match = Regex.Match(request, @"{([^}]*)");
            if (match.Success)
            {
                post.Add("body", "{" + match.Groups[1].Value + "}");
            }

            return post;
        }

        private int HandleLoginAttempt(String jsonPackage, String IP, out SessionManager.Warning warning)
        {
            JObject package = JObject.Parse(jsonPackage);
            string username = (String)package["username"];
            string password = (String)package["password"];

            return sessionManager.Login(username, password, IP, out warning);
        }

        private bool CheckStateOfSession(String IP)
        {
            if (activeIPs.ContainsKey(IP))
            {
                SessionManager.Warning warning = sessionManager.checkSession(getHashcodeOfActiveIP(IP));

                switch (warning)
                {
                    case SessionManager.Warning.SESSION_EXPIRED:
                        Console.WriteLine("The session for {0} has expired as {1} hours have passed.", IP, Session.SESSION_LENGTH_IN_HOURS);
                        return false;
                    case SessionManager.Warning.SESSION_DOES_NOT_EXIST:
                        Console.WriteLine("The user has not the necessary privileges to access this feature.");
                        return false;
                    case SessionManager.Warning.NONE:
                        return true;
                }
            }

            return false;
        }
    }
}