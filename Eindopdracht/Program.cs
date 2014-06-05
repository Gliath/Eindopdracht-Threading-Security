using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Eindopdracht
{
    public class Program
    {
        private static SettingsReader settings;
        public static void Main(string[] args)
        {
            settings = new SettingsReader();

            Webserver ws = new Webserver(SendResponse, "http://localhost:" + settings.Port + "/");
            Webserver aw = new Webserver(SendAdminResponse, "http://localhost:" + settings.AdminPort + "/");
            ws.Run();
            aw.Run();

            Console.WriteLine("A simple webserver. Press a key to quit.");
            Console.ReadKey();
            ws.Stop();
            aw.Stop();
        }

        public static string SendResponse(HttpListenerRequest request)
        {
            return string.Format("<html><body>My web page.<br>{0}</body></html>", DateTime.Now);
        }

        public static string SendAdminResponse(HttpListenerRequest request)
        {
            return string.Format("<html><body>My admin web page.<br>{0}</body></html>", DateTime.Now);
        }
    }
}