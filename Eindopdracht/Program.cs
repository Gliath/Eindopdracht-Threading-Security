using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Eindopdracht
{
    public class Program
    {
        private static SettingsReader Settings;
        private static Thread processLogs;
        public static void Main(String[] args)
        {
            Settings = new SettingsReader();

            Webserver webserver = new Webserver(Settings, false);
            Webserver adminserver = new Webserver(Settings, true);

            Thread tWeb = new Thread(m => webserver.StartListening());
            Thread tAdmin = new Thread(m => adminserver.StartListening());
            tWeb.Start();
            tAdmin.Start();
			
            Logger logger = Logger.getInstance();
            processLogs = new Thread(logger.processLogs);
            processLogs.Start();

            Console.WriteLine("Christiaan & Luke's webserver. Press ^C to quit.");
        }
    }
}