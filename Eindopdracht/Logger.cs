using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Eindopdracht
{
    public class Logger
    {
        private static int SIZE = 20;
        private static String LogFilePath = "Data\\Log.txt";
        private static Logger instance;

        private LoggerQueue queue;
        private Semaphore producers, consumers;

        private Logger()
        {
            queue = new LoggerQueue(SIZE);
            producers = new Semaphore(SIZE, SIZE); // Many producers
            consumers = new Semaphore(0, SIZE); // Only one consumer
        }

        // There can be only one.
        public static Logger getInstance()
        {
            if (instance == null)
                instance = new Logger();

            return instance;
        }

        // This method is used by a thread to keep processing logs while it's alive.
        public void processLogs()
        {
            while (true)
            {
                String entry = pop();

                // Write in a file
                if (File.Exists(LogFilePath))
                {
                    String logContents;

                    using (StreamReader sr = new StreamReader(LogFilePath))
                        logContents = sr.ReadToEnd() + "\r\n" + entry;

                    File.WriteAllText(LogFilePath, logContents);
                }
                else
                    File.WriteAllText(LogFilePath, entry);
            }
        }

        // This method puts the entry at the rear of the queue.
        public bool put(String entry)
        {
            if (String.IsNullOrWhiteSpace(entry))
                return false;

            // This is used by multiple producers.
            lock (this)
            {
                producers.WaitOne(); // If queue is full, producers are going to wait here

                if (!queue.add(entry))
                    return false; // Something went wrong, invalid entry or queue full?

                consumers.Release();
                return true;
            }
        }

        // This method returns the front element and removes it afterwards.
        private String pop()
        {
            consumers.WaitOne(); // If queue is empty, consumer is going to wait here
            String entry = queue.pop();

            if (String.IsNullOrWhiteSpace(entry))
                return "Error occured, queue empty";

            producers.Release();

            return entry;
        }
    }
}