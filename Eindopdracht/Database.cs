using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eindopdracht
{
    public class Database
    {
        private MySqlConnection connection;
        private static string SERVER = "localhost";
        private static string DATABASE = "threading";
        private static string USERNAME = "root";
        private static string PASSWORD = ""; // TODO: change to something more difficult!

        public Database()
        {
            string connectionString = "SERVER=" + SERVER + ";" + "DATABASE=" + DATABASE + ";" + "USERNAME=" + USERNAME + ";" + "PASSWORD=" + PASSWORD + ";";
            connection = new MySqlConnection(connectionString);
        }

        public User Login(String username, String password) {
            // TODO: Add MD5 and stored procedure

            /*MySqlCommand cmd = new MySqlCommand();
            cmd.CommandText = "threading_login";
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            
            cmd.Parameters.AddWithValue("username", username);
            cmd.Parameters["username"].Direction = System.Data.ParameterDirection.Input;

            cmd.Parameters.AddWithValue("password", password);
            cmd.Parameters["password"].Direction = System.Data.ParameterDirection.Input;
            */

            try
            {
                string query = "SELECT * from users where username = '" + username + "' and password = '" + password + "'";

                if (this.OpenConnection() == true)
                {
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    MySqlDataReader dr = cmd.ExecuteReader();

                    if (dr.Read())
                    {
                        User user = new User(int.Parse(dr[0].ToString()), dr[1].ToString(), dr[2].ToString(), dr[3].ToString());
                        this.CloseConnection();
                        return user;
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return null;
        }

        private bool OpenConnection()
        {
            try
            {
                connection.Open();
                return true;
            }
            catch (MySqlException ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        private bool CloseConnection()
        {
            try
            {
                connection.Close();
                return true;
            }
            catch (MySqlException ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
    }
}
