using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Eindopdracht
{
    public class SessionManager
    {
        private Connector connector;
        private Dictionary<int, Session> sessions;
        private List<int> loggedInUsers;
        private Dictionary<string, int> loginAttemps;
        private Dictionary<string, DateTime> blockedIPs;

        public static int NUMBER_OF_LOGIN_ATTEMPTS = 5;
        public static int NUMBER_OF_HOURS_BLOCKED = 1;

        public enum Warning {
            WRONG_COMBINATION,
            BLOCKED_IP,
            USER_ALREADY_LOGGED_IN,
            SESSION_DOES_NOT_EXIST,
            SESSION_EXPIRED,
            NONE
        }

        public SessionManager(Connector connector)
        {
            this.connector = connector;
            this.sessions = new Dictionary<int, Session>();
            this.loggedInUsers = new List<int>();
            this.loginAttemps = new Dictionary<string, int>();
            this.blockedIPs = new Dictionary<string, DateTime>();
        }

        public int Login(string username, string password, string ip, out Warning warning) {
            User user = null;

            // Encrypt password with MD5 hashing
            password = encryptPassword(password);
            
            string query = "SELECT * from users where username = '" + username + "' and password = '" + password + "'";
            MySqlDataReader dr = connector.query(query);

            // If the user has x number of login attemps, then block his IP for x number of hours.
            if (loginAttemps.ContainsKey(ip) && loginAttemps[ip] == NUMBER_OF_LOGIN_ATTEMPTS)
            {
                blockedIPs.Add(ip, new DateTime().AddHours(NUMBER_OF_HOURS_BLOCKED));
                loginAttemps.Remove(ip);
            }

            // If the IP is blocked and the x number of hours hasn't passed, then return warning BLOCKED_IP.
            if (blockedIPs.ContainsKey(ip))
            {
                if (blockedIPs[ip].CompareTo(new DateTime()) < 0)
                {
                    warning = Warning.BLOCKED_IP;
                    return -1;
                }
                else
                {
                    blockedIPs.Remove(ip);
                }
            }

            // If the database returns a row then return warning NONE or else keep track of login attemps of this IP.
            if (dr.Read())
            {
                user = new User(int.Parse(dr[0].ToString()), dr[1].ToString(), dr[2].ToString(), dr[3].ToString());
                connector.CloseConnection();

                int hashcode = addSession(ip, user, out warning);

                if (warning == Warning.NONE)
                {
                    // returns hascode associated with session
                    return hashcode;
                }

                return -1;
            }
            else
            {
                if(loginAttemps.ContainsKey(ip)) {
                    int value = loginAttemps[ip];
                    loginAttemps[ip] = value++;
                } else {
                    loginAttemps.Add(ip, 1);
                }
            }

            connector.CloseConnection();
            warning = Warning.WRONG_COMBINATION;
            return -1;
        }

        public Session getSession(int hashcode)
        {
            switch (checkSession(hashcode))
            {
                case Warning.SESSION_EXPIRED:
                    Console.WriteLine("Session is expired.");
                    break;
                case Warning.SESSION_DOES_NOT_EXIST:
                    Console.WriteLine("Session does not exists.");
                    break;
            }

            if (checkSession(hashcode) == Warning.NONE)
            {
                return sessions[hashcode];
            }

            return null;
        }

        private int addSession(String IP, User user, out Warning warning) 
        {
            // If the user was already logged in, then don't let another IP log in.
            if (loggedInUsers.Contains(user.ID))
            {
                warning = Warning.USER_ALREADY_LOGGED_IN;
                return -1;
            }
            else
            {
                loggedInUsers.Add(user.ID);
            }

            Session session = new Session(IP, user);
            int hashcode = session.GetHashCode();
            sessions.Add(hashcode, session);

            warning = Warning.NONE;
            return hashcode;
        }

        private Warning checkSession(int hashcode)
        {
            // If there is a session associated with the hashcode, then check if it's expired and if so remove it.
            if (sessions.ContainsKey(hashcode))
            {
                Session session = sessions[hashcode];

                if (session.Expires.CompareTo(new DateTime()) < 0)
                {
                    return Warning.NONE;
                }
                else
                {
                    removeSession(hashcode);
                    return Warning.SESSION_EXPIRED;
                }
            }

            return Warning.SESSION_DOES_NOT_EXIST;
        }

        private void removeSession(int hashcode)
        {
            Session session = sessions[hashcode];
            loggedInUsers.Remove(session.ID);
            sessions.Remove(hashcode);
        }

        private string encryptPassword(string password)
        {
            MD5 md5 = MD5.Create();
            byte[] data = md5.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
            {
                sb.Append(data[i].ToString("x2"));
            }

            return sb.ToString();
        }
    }

    public class Session
    {
        private readonly string ip;
        private readonly int id;
        private readonly DateTime expires;
        private readonly User user;

        public Session(string ip, User user)
        {
            this.ip = ip;
            this.id = user.ID;
            this.user = user;

            DateTime expires = DateTime.Now;
            this.expires = expires.AddHours(1);
        }

        public string IP
        {
            get { return this.ip; }
        }

        public int ID
        {
            get { return this.id; }
        }

        public DateTime Expires
        {
            get { return this.expires; }
        }
    }

    public class User
    {
        private readonly int id;
        private readonly string username;
        private readonly string password;
        private readonly string type;

        public User(int id, string username, string password, string type)
        {
            this.id = id;
            this.username = username;
            this.password = password;
            this.type = type;
        }

        public int ID
        {
            get { return this.id; }
        }

        public string Username
        {
            get { return this.username; }
        }

        public string Password
        {
            get { return this.password; }
        }

        public string Type
        {
            get { return this.type; }
        }
    }
}