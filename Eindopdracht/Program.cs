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
        public static void Main(string[] args)
        {
            Settings = new SettingsReader();

            Webserver ws = new Webserver(Settings);
            Webserver aw = new Webserver(Settings);

            Logger logger = Logger.getInstance();
            processLogs = new Thread(logger.processLogs);
            processLogs.Start();
            
            Console.WriteLine("A simple webserver. Press a key to quit.");
            Console.ReadKey();
        }
    }
}