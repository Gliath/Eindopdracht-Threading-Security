using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eindopdracht
{
    public class Connector
    {
        private static Connector instance = null;
        private MySqlConnection connection;
        private static string SERVER = "localhost";
        private static string DATABASE = "threading";
        private static string PORT = "3307";
        private static string USERNAME = "root";
        private static string PASSWORD = "root"; // TODO: change to something more difficult!

        private Connector()
        {
            string connectionString = "SERVER=" + SERVER + ";" + "PORT=" + PORT + ";" + "DATABASE=" + DATABASE + ";" + "USERNAME=" + USERNAME + ";" + "PASSWORD=" + PASSWORD + ";";
            connection = new MySqlConnection(connectionString);
        }

        public static Connector getInstance()
        {
            if (instance == null)
                instance = new Connector();

            return instance;
        }

        public MySqlDataReader selectUserQuery(String username, String password)
        {
            // TODO: Add MD5 and stored procedure

            /*MySqlCommand cmd = new MySqlCommand();
            cmd.CommandText = "threading_login";
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            
            cmd.Parameters.AddWithValue("username", username);
            cmd.Parameters["username"].Direction = System.Data.ParameterDirection.Input;

            cmd.Parameters.AddWithValue("password", password);
            cmd.Parameters["password"].Direction = System.Data.ParameterDirection.Input;
            */

            //MySqlCommand cmd = new MySqlCommand(query, connection);
            MySqlCommand cmd = new MySqlCommand("SELECT * FROM users WHERE username = ?username AND password = ?password", connection);
            cmd.Parameters.AddWithValue("?username", username);
            cmd.Parameters.AddWithValue("?password", password);
            return executeQuery(cmd);
        }

        public MySqlDataReader selectUserByIDQuery(String id)
        {
            MySqlCommand cmd = new MySqlCommand("SELECT * FROM users WHERE id = ?id", connection);
            cmd.Parameters.AddWithValue("?id", id);
            return executeQuery(cmd);
        }

        public MySqlDataReader selectUsersQuery()
        {
            MySqlCommand cmd = new MySqlCommand("SELECT * FROM users", connection);
            return executeQuery(cmd);
        }

        public int createUserQuery(String username, String password, String type)
        {
            MySqlCommand cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO users (username, password, type) VALUES (?username, ?password, ?type)";
            cmd.Parameters.AddWithValue("?username", username);
            cmd.Parameters.AddWithValue("?password", password);
            cmd.Parameters.AddWithValue("?type", type);
            return executeNonQuery(cmd);
        }

        public int editUserQuery(int id, String username, String type)
        {
            MySqlCommand cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE users SET username = ?username, type = ?type WHERE ID = ?id";
            cmd.Parameters.AddWithValue("?id", id);
            cmd.Parameters.AddWithValue("?username", username);
            cmd.Parameters.AddWithValue("?type", type);
            return executeNonQuery(cmd);
        }

        public int deleteUserQuery(int id)
        {
            MySqlCommand cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM users WHERE ID = ?id";
            cmd.Parameters.AddWithValue("?id", id);
            return executeNonQuery(cmd);
        }

        private MySqlDataReader executeQuery(MySqlCommand command)
        {
            try
            {
                if (this.OpenConnection() == true)
                {
                    MySqlDataReader dr = command.ExecuteReader();
                    return dr;
                }
            }
            catch (MySqlException ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return null;
        }

        private int executeNonQuery(MySqlCommand command)
        {
            try
            {
                if (this.OpenConnection() == true)
                    return command.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return 0;
        }

        public bool IsOpen()
        {
            return connection.State == System.Data.ConnectionState.Open;
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

        public bool CloseConnection()
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