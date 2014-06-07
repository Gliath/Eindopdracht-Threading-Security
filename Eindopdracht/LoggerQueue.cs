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

        public LoggerQueue(int size)
        {
            if(size < 1) {
                throw new ArgumentException("The LoggerQueue must contain atleast contain one element.");
            }

            this.elements = new string[size];
            this.size = size;
            this.entries = -1;
        }

        public String pop()
        {
            String entry = elements[0];
            elements[0] = null;
            entries--;

            if(!isEmpty() && entries > 0) {
                for (int i = 0; i < entries; i++)
                {
                    elements[i] = elements[i + 1];
                }
            }

            return entry;
        }

        public bool add(String entry)
        {
            if (String.IsNullOrWhiteSpace(entry))
                return false;

            if (isFull())
                return false;

            elements[++entries] = entry;
            return true;
        }

        public int Size
        {
            get { return size; }
        }

        public bool isEmpty()
        {
            return entries == -1;
        }

        public bool isFull()
        {
            return entries == (size - 1);
        }
    }
}
