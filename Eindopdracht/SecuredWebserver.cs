using MySql.Data.MySqlClient;
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
using System.Threading;
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

        private Dictionary<string, int> activeIPs;
        private Connector connector;
        private SessionManager sessions;

        public SecuredWebserver(SettingsReader Settings)
            : base(Settings)
        {
            this.Settings = Settings;
            activeIPs = new Dictionary<string, int>();
            connector = Connector.getInstance();
            sessions = new SessionManager(connector);

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

                new Thread(m =>
                {
                    try
                    {
                        /* SSL Afhandeling
                        stream.AuthenticateAsServer(certificate, false, SslProtocols.Tls, true);
                        String message = readMessage(stream);
                        Console.WriteLine(message);
                        */

                        Byte[] byteMessage = new Byte[1024];
                        int i = sClient.Receive(byteMessage, byteMessage.Length, 0);
                        String request = Encoding.ASCII.GetString(byteMessage).ToLower();
                        Console.WriteLine(request.Replace("\0", ""));

                        int iFirstPos = request.IndexOf(' ');
                        int iLastPos = request.IndexOf(' ', iFirstPos + 1);

                        if (iFirstPos > 0 && iLastPos > 0)
                        {
                            IPEndPoint IP = sClient.RemoteEndPoint as IPEndPoint;
                            String type = request.Substring(0, iFirstPos);
                            String url = request.Substring(iFirstPos + 1, iLastPos - (iFirstPos + 1)).Replace("\\", "/");
                            String html = request.Substring(iLastPos + 1, 8);

                            String referer = "";
                            String[] getParams = url.Split('?');
                            if (getParams.Length > 1)
                                url = getParams[0];

                            int iPosReferer = request.IndexOf("Referer: ");
                            if (iPosReferer > 0)
                                referer = request.Substring(iPosReferer, request.IndexOf("\r\n", iPosReferer) - iPosReferer).Substring(9);

                            Byte[] byteFile = null;
                            String status = "";
                            switch (type)
                            {
                                case "get":
                                    byteFile = HandleGETRequest(url, IP.Address.ToString(), getParams, referer, out status);
                                    break;
                                case "post":
                                    String postParams = request.Substring(request.LastIndexOf("\r\n") + 2).Replace("\0", "");
                                    Console.WriteLine("Post parameters: {0}", postParams);
                                    byteFile = HandlePOSTRequest(url, IP.Address.ToString(), postParams.Split('&'), referer, out status);
                                    break;
                                default:
                                    Console.WriteLine("Unsupported request type encountered: {0}", type);
                                    byteFile = HandleError(status = "400");
                                    break;
                            }

                            if (byteFile != null)
                            {
                                //Socket socket = client.Client; Nodig voor SSL
                                SendHeader(html, status, "text/html", byteFile.Length, ref sClient);
                                SendData(byteFile, ref sClient);
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
                }).Start();
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

                if (messageData.ToString().Contains("\r\n"))
                {
                    break;
                }
            } while (bytes != 0);

            return messageData.ToString();
        }

        private Byte[] HandleGETRequest(String url, String ip, String[] getParams, String referer, out String status)
        {
            status = "200";

            String path = "";
            String message = "";
            Boolean isLoggedIn = CheckStateOfSession(ip);

            switch (url)
            {
                case "/":
                    if (isLoggedIn)
                        path = "cp_page.html";
                    else
                        path = "index.html";
                    break;
                case "/logout":
                    if (activeIPs.ContainsKey(ip))
                    {
                        sessions.removeSession(getHashcodeOfActiveIP(ip));
                        activeIPs.Remove(ip);
                        Console.WriteLine("User has logged out");
                    }
                    else
                        Console.WriteLine("User is not even logged in");

                    path = "index.html";
                    break;
                case "/log":
                    path = "log";
                    break;
                case "/users":
                    path = "usermanage_list.html";
                    break;
                case "/create":
                    path = "usermanage_create.html";
                    break;
                case "/edit":
                    path = "usermanage_edit.html";
                    break;
                case "/delete":
                    path = "usermanage_delete.html";
                    break;
                default:
                    return HandleError(status = "404");
            }

            if (!url.Equals("/") && !isLoggedIn) // If the user is not logged in
                return HandleError(status = "403");

            if (((url.Equals("/edit") || url.Equals("/delete")) && getParams.Length != 2 && !getParams[1].ToLower().StartsWith("id=")))
                return HandleError(status = "404");

            if (!path.Equals("log"))
                path = "SecuredPages\\" + path;

            try
            {
                String htmlPage = "";
                if (path.Equals("log"))
                {
                    //Get log, print it all (seperate lines)
                }

                using (StreamReader sr = new StreamReader(path))
                    htmlPage = sr.ReadToEnd();

                // If controlpage, get old settings
                if (path.Contains("cp_page.html"))
                {
                    String oldDefaultPages = "";
                    foreach (String defaultPage in Settings.DefaultPages)
                        oldDefaultPages += defaultPage + ";";

                    oldDefaultPages = oldDefaultPages.Substring(0, oldDefaultPages.Length - 1);

                    htmlPage = htmlPage.Replace("{oldPort}", Settings.Port.ToString())
                                    .Replace("{oldAdminPort}", Settings.AdminPort.ToString())
                                    .Replace("{oldRoot}", Settings.Root)
                                    .Replace("{oldDefaultPages}", oldDefaultPages)
                                    .Replace("{oldDirectoryBrowsing}", Settings.DirectoryBrowsing ? "checked" : "");
                }
                else if (url.Equals("/users")) // list of users
                {
                    List<User> users = new List<User>();
                    MySqlDataReader dr = connector.query("SELECT * FROM users");

                    if (dr.HasRows)
                    {
                        while (dr.Read())
                            users.Add(new User(int.Parse(dr.GetString("id")), dr.GetString("username"), dr.GetString("password"), dr.GetString("type")));

                        String listItem = "";
                        using (StreamReader sr = new StreamReader("SecuredPages\\usermanage_listitem.html"))
                            listItem = sr.ReadToEnd();

                        String listUsers = "";
                        foreach (User user in users)
                            listUsers += listItem.Replace("{username}", user.Username).Replace("{isAdmin}", user.Type.Equals("admin") ? "checked" : "").Replace("{id}", user.ID.ToString());

                        htmlPage = htmlPage.Replace("{ListUsers}", listUsers);
                    }
                    else
                    {
                        using (StreamReader sr = new StreamReader("SecuredPages\\redirect.html"))
                            htmlPage = sr.ReadToEnd();

                        // If it fails to load the users, redirect client to users overview
                        htmlPage.Replace("{url}", "/");
                    }

                    connector.CloseConnection();
                }
                else if (((url.StartsWith("/edit") || url.StartsWith("/delete")) && url.Contains('?')))
                {
                    MySqlDataReader dr = connector.query("SELECT * from users where id = '" + getParams[1].Substring(3) + "'");
                    if (dr.Read())
                    {
                        htmlPage = htmlPage.Replace("{user}", dr[1].ToString()).Replace("{id}", dr[0].ToString());
                        htmlPage = htmlPage.Replace("{oldUsername}", dr[1].ToString()).Replace("{oldIsAdmin", dr[3].ToString().Equals("admin") ? "checked" : "");
                        connector.CloseConnection();
                    }
                    else
                    {
                        using (StreamReader sr = new StreamReader("SecuredPages\\redirect.html"))
                            htmlPage = sr.ReadToEnd();

                        // If it fails to load the user, redirect client to users overview
                        htmlPage.Replace("{url}", "/users");
                    }
                }

                // Replace with something better
                htmlPage = htmlPage.Replace("{Message}", message);

                return Encoding.ASCII.GetBytes(htmlPage);
            }
            catch (Exception e)
            {
                Console.WriteLine("File could not be read. Message:");
                Console.WriteLine(e.Message);
            }

            return HandleError(status = "404");
        }

        private Byte[] HandlePOSTRequest(String url, String ip, String[] postParams, String referer, out String status)
        {
            status = "200";

            Boolean isLoggedIn = CheckStateOfSession(ip);

            String message = "";
            String path = "";
            String page = "";
            switch (url)
            {
                case "/":
                    if (isLoggedIn) // cp_page submit
                    {
                        String[] newSettings = new String[5];
                        for (int i = 0; i < postParams.Length - 1; i++) // Don't need the hidden input
                            newSettings[i] = Uri.UnescapeDataString(postParams[i].Split('=')[1]);

                        Settings.SaveNewSettings(int.Parse(newSettings[0]), int.Parse(newSettings[1]), newSettings[2], newSettings[3].Split(';'), newSettings[4].Equals("on"));
                        message = "Successfully saved settings.";

                        ;

                        path = "SecuredPages\\cp_page.html";
                    }
                    else // index submit
                    {
                        if (activeIPs.ContainsKey(ip))
                        {
                            Console.Write("The user is already logged in.");
                            break;
                        }

                        SessionManager.Warning warning;
                        int hashcode = HandleLoginAttempt(postParams, ip, out warning);

                        switch (warning)
                        {
                            case SessionManager.Warning.WRONG_COMBINATION:
                                path = "SecuredPages\\index.html";
                                Console.WriteLine(message = "The user has entered a wrong combination.");
                                break;
                            case SessionManager.Warning.USER_ALREADY_LOGGED_IN:
                                path = "SecuredPages\\cp_page.html";
                                Console.WriteLine(message = "The user is already logged in.");
                                break;
                            case SessionManager.Warning.SESSION_EXPIRED:
                                path = "SecuredPages\\index.html";
                                Console.WriteLine(message = "The session has expired.");
                                break;
                            case SessionManager.Warning.BLOCKED_IP:
                                path = "SecuredPages\\index.html";
                                Console.WriteLine(message = "{0} is blocked.", ip);
                                break;
                            case SessionManager.Warning.NONE:
                                activeIPs.Add(ip, hashcode);
                                path = "SecuredPages\\cp_page.html";
                                Console.WriteLine(message = "The user has logged in successfully.");
                                break;
                        }
                    }
                    break;

                case "/create":

                    break;

                case "/edit":

                    break;

                case "/delete":

                    break;
            }

            try
            {
                using (StreamReader sr = new StreamReader(path))
                    page = sr.ReadToEnd();

                if(path.Contains("cp_page"))
                {
                    String oldDefaultPages = "";
                    foreach (String defaultPage in Settings.DefaultPages)
                        oldDefaultPages += defaultPage + ";";

                    oldDefaultPages = oldDefaultPages.Substring(0, oldDefaultPages.Length - 1);

                    page = page.Replace("{oldPort}", Settings.Port.ToString())
                                .Replace("{oldAdminPort}", Settings.AdminPort.ToString())
                                .Replace("{oldRoot}", Settings.Root)
                                .Replace("{oldDefaultPages}", oldDefaultPages)
                                .Replace("{oldDirectoryBrowsing}", Settings.DirectoryBrowsing ? "checked" : "");
                }


                return Encoding.ASCII.GetBytes(page.Replace("{Message}", message));
            }
            catch (Exception e)
            {
                Console.WriteLine("File could not be read. Message:");
                Console.WriteLine(e.Message);
            }

            return HandleError(status = "404");
        }

        private Byte[] HandleError(String errorCode)
        {
            if (!errorCode.Equals("400") || !errorCode.Equals("403") || !errorCode.Equals("404"))
            {
                Console.WriteLine("Unknown error occured. Error code: {0}", errorCode);
                errorCode = "404";
            }

            string file = "";

            using (StreamReader sr = new StreamReader("ErrorPages\\" + errorCode + ".html"))
                file = sr.ReadToEnd();

            return Encoding.ASCII.GetBytes(file);
        }

        private int HandleLoginAttempt(String[] loginParams, String IP, out SessionManager.Warning warning)
        {
            string username = loginParams[0].Split('=')[1];
            string password = loginParams[1].Split('=')[1];

            return sessions.Login(username, password, IP, out warning);
        }

        private int getHashcodeOfActiveIP(String IP)
        {
            return activeIPs[IP];
        }

        private bool CheckStateOfSession(String IP)
        {
            if (activeIPs.ContainsKey(IP))
            {
                SessionManager.Warning warning = sessions.checkSession(getHashcodeOfActiveIP(IP));

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