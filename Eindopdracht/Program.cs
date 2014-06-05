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
        private static SettingsReader Settings;
        public static void Main(string[] args)
        {
            Settings = new SettingsReader();

            Webserver ws = new Webserver(Settings);
            Webserver aw = new Webserver(Settings);

            Console.WriteLine("A simple webserver. Press a key to quit.");
            Console.ReadKey();
        }
    }
}