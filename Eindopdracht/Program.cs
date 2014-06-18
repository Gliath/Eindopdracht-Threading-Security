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
        private static Connector connector;
        private static Logger logger;
        public static void Main(String[] args)
        {
            Settings = new SettingsReader();
            connector = Connector.getInstance();
            logger = Logger.getInstance();

            AbstractWebserver webserver = new UnsecuredWebserver(Settings, connector, logger);
            AbstractWebserver adminserver = new SecuredWebserver(Settings);

            Thread tWeb = new Thread(m => webserver.StartListening());
            Thread tAdmin = new Thread(m => adminserver.StartListening());
            
            Thread tLogger = new Thread(logger.processLogs);

            tLogger.Start();
            tWeb.Start();
            tAdmin.Start();
            
            Console.WriteLine("Christiaan & Luke's webserver. Press ^C to quit.");
        }
    }
}