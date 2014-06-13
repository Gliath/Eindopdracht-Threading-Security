using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Eindopdracht
{
    public class Storage
    {
        private List<int> loggedInUsers;
        private Dictionary<int, Session> sessions;

        public enum Warning {
            USER_ALREADY_LOGGED_IN,
            SESSION_DOES_NOT_EXIST,
            SESSION_EXPIRED,
            NONE
        }

        public Storage()
        {
            this.sessions = new Dictionary<int, Session>();
            this.loggedInUsers = new List<int>();
        }

        public Warning addSession(String IP, User user) {
            if (String.IsNullOrEmpty(IP))
            {
                throw new ArgumentException("IP cannot be null or empty.");
            }

            if (user == null)
            {
                throw new ArgumentException("User object cannot be null.");
            }

            if (loggedInUsers.Contains(user.ID))
            {
                return Warning.USER_ALREADY_LOGGED_IN;
            }
            else
            {
                loggedInUsers.Add(user.ID);
            }

            Session session = new Session(IP, user.ID);
            int hashcode = session.GetHashCode();
            sessions.Add(hashcode, session);

            return Warning.NONE;
        }

        public Warning checkSession(int hashcode)
        {
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

        private class Session
        {
            private readonly string ip;
            private readonly int id;
            private readonly DateTime expires;

            public Session(string ip, int id)
            {
                this.ip = ip;
                this.id = id;
                
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
    }
}