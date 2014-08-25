using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eindopdracht
{
    public class UserHandler
    {
        public static Boolean createUser(String username, String password, String type)
        {
            if (String.IsNullOrWhiteSpace(username) || String.IsNullOrWhiteSpace(password) || String.IsNullOrWhiteSpace(type))
                return false;

            Connector con = Connector.getInstance();
            con.createUserQuery(username, password, type);
            con.CloseConnection();

            return true;
        }

        public static Boolean editUser(int ID, String username, String type)
        {
            if (String.IsNullOrWhiteSpace(username) || String.IsNullOrWhiteSpace(type))
                return false;

            Connector con = Connector.getInstance();
            con.editUserQuery(ID, username, type);
            con.CloseConnection();

            return true;
        }

        public static Boolean deleteUser(int ID)
        {
            if (ID <= 0)
                return false;

            Connector con = Connector.getInstance();
            con.deleteUserQuery(ID);
            con.CloseConnection();

            return true;
        }
    }

    public class User
    {
        private readonly int id;
        private readonly string username;
        private readonly string password;
        private readonly USER_TYPE type;

        public enum USER_TYPE
        {
            ADMIN, SUPPORTER
        }

        public User(int id, string username, string password, string type)
        {
            this.id = id;
            this.username = username;
            this.password = password;

            switch (type)
            {
                case "admin":
                    this.type = USER_TYPE.ADMIN;
                    break;
                case "supporter":
                    this.type = USER_TYPE.SUPPORTER;
                    break;
                default:
                    throw new ArgumentException("The type of this user is undefined.");
            }
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

        public USER_TYPE Type
        {
            get { return this.type; }
        }
    }
}