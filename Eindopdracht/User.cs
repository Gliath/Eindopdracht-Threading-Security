using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eindopdracht
{
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
