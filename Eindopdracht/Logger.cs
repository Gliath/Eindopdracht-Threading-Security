using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Eindopdracht
{
    public class Logger
    {
        private LoggerQueue queue;
        private static int SIZE = 20;

        private static Logger instance;
        private bool canReadQueue;
        private volatile bool canProcessLogs;

        private Logger()
        {
            queue = new LoggerQueue(SIZE);
            canReadQueue = false;
            canProcessLogs = true;
        }

        // There can be only one consumer (Singleton pattern).
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
                while (canProcessLogs)
                    Console.WriteLine(String.Format("Popped: {0}", pop()));

                Monitor.Wait(canProcessLogs);
            }
        }

        public void testAddLogs()
        {
            while (true)
            {
                add(DateTime.Now.ToShortTimeString());
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }

        public void interruptProcessing()
        {
            canProcessLogs = false;
        }

        public void resumeProcessing()
        {
            canProcessLogs = true;
            Monitor.Pulse(canProcessLogs);
        }

        // This method puts the entry at the rear of the queue.
        public bool add(String entry)
        {
            // This is used by multiple producers.
            lock (this)
            {
                while (canReadQueue && queue.isFull())
                    Monitor.Wait(this);

                if (!queue.add(entry))
                    return false;

                canReadQueue = true;
                Monitor.Pulse(this);

                return true;
            }
        }

        // This method returns the front element and removes it afterwards.
        private String pop()
        {
            // This is used by one consumer.
            lock (this)
            {
                while (!canReadQueue && queue.isEmpty())
                    Monitor.Wait(this);

                String entry = queue.pop();

                canReadQueue = false;
                Monitor.Pulse(this);

                return entry;
            }
        }
    }
}