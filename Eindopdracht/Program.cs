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
        private static Logger logger;
        private static Thread processLogs;
        public static void Main(String[] args)
        {
            Settings = new SettingsReader();

            AbstractWebserver webserver = new UnsecuredWebserver(Settings);
            AbstractWebserver adminserver = new SecuredWebserver(Settings);

            Thread tWeb = new Thread(m => webserver.StartListening());
            Thread tAdmin = new Thread(m => adminserver.StartListening());
            tWeb.Start();
            tAdmin.Start();

            logger = Logger.getInstance();
            processLogs = new Thread(logger.processLogs);
            processLogs.Start();

            Thread generateLogs1 = new Thread(m => Program.generateLogs());
            //generateLogs1.Start();

            Thread generateLogs2 = new Thread(m => Program.generateLogs());
            //generateLogs2.Start();

            Console.WriteLine("Christiaan & Luke's webserver. Press ^C to quit.");
        }

        public static void generateLogs() {
            int count = 0;

            while(true) {
                logger.add(String.Format("Test {0}", ++count));
            }
        }
    }
}