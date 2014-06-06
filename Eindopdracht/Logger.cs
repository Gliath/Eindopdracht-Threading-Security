using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Eindopdracht
{
    public class Logger
    {
        private static Logger instance;
        private bool canReadQueue;
        private volatile bool canProcessLogs;

        private String[] elements;
        private static int SIZE = 20;
        private int n;
        private int front;
        private int rear;

        private Logger()
        {
            canReadQueue = false;
            canProcessLogs = true;

            elements = new String[SIZE];
            
            n = 0;
            front = 0;
            rear = 0;
        }

        // There can be only one consumer (Singleton pattern).
        public Logger getInstance() {
            if(instance == null) {
                instance = new Logger();
            }

            return instance;
        }

        // This method is used by a thread to keep processing logs while it's alive.
        public void processLogs()
        {
            while (canProcessLogs)
            {
                String entry = get();
                // Log it.
            }
        }

        public void interruptProcessing()
        {
            canProcessLogs = false;
        }

        public void resumeProcessing()
        {
            canProcessLogs = true;
        }

        // This method puts the entry at the rear of the queue.
        public bool add(String entry) {
            // This is used by multiple producers.
            lock (this)
            {
                // Validate argument before it's put in queue.
                if (String.IsNullOrEmpty(entry))
                {
                    return false;
                }

                while (canReadQueue && !isFull())
                {
                    Monitor.Wait(this);
                }

                if (rear != (size() - 1)) // wraparound
                    rear = -1;

                elements[++rear] = entry;
                n++;

                canReadQueue = true;
                Monitor.Pulse(this);

                return true;
            }
        }

        // This method returns the front element and removes it afterwards.
        private String get()
        {
            // This is used by one consumer.
            lock (this)
            {
                while (!canReadQueue && !isEmpty())
                {
                    Monitor.Wait(this);
                }

                String entry = elements[front++];

                if (front == SIZE) // wraparound
                    front = 0;

                n--;

                canReadQueue = false;
                Monitor.Pulse(this);

                return entry;
            }
        }

        public bool isEmpty()
        {
            return n == 0;
        }

        public bool isFull()
        {
            return n == SIZE;
        }

        public int size()
        {
            return n;
        }
    }
}