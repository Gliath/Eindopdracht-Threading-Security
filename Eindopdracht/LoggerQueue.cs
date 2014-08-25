using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eindopdracht
{
    public class LoggerQueue
    {
        private string[] elements;
        private int size;
        private int entries;
        private int getpos, putpos;

        public LoggerQueue(int size)
        {
            if(size < 1) {
                throw new ArgumentException("The LoggerQueue must be able to contain atleast one element.");
            }

            this.elements = new string[size];
            this.size = size;
            
            this.entries = 0;
            this.getpos = 0;
            this.putpos = 0;
        }

        public String pop()
        {
            if (isEmpty())
                return "";

            String entry = elements[getpos];
            getpos = (getpos + 1) % size;
            entries--;

            return entry;
        }

        public bool add(String entry)
        {
            if (String.IsNullOrWhiteSpace(entry) || isFull())
                return false;

            elements[putpos] = entry;
            putpos = (putpos + 1) % size;
            entries++;

            return true;
        }

        public int Size
        {
            get { return size; }
        }

        public bool isEmpty()
        {
            return entries == 0;
        }

        public bool isFull()
        {
            return entries == size;
        }
    }
}