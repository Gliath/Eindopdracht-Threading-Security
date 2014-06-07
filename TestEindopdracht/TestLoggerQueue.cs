using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Eindopdracht;

namespace TestEindopdracht
{
    [TestClass]
    public class TestLoggerQueue
    {
        [TestMethod]
        public void add_single_entry()
        {
            LoggerQueue queue = new LoggerQueue(20);
            
            Assert.IsTrue(queue.add("Testing testing testing..."));
            Assert.IsFalse(queue.add(null));
            Assert.IsFalse(queue.add(""));
        }

        [TestMethod]
        public void fill_queue()
        {
            LoggerQueue queue = new LoggerQueue(20);

            for (int i = 0; i < queue.Size; i++)
            {
                queue.add("Testing testing testing...");
            }

            Assert.IsTrue(queue.isFull());
            Assert.IsFalse(queue.add("Just one more..."));
        }

        [TestMethod]
        public void pop_single_entry()
        {
            LoggerQueue queue = new LoggerQueue(1);
            
            queue.add("test");
            Assert.IsTrue(queue.isFull());

            Console.WriteLine(queue.pop());
            Assert.IsTrue(queue.isEmpty());
        }

        [TestMethod]
        public void pop_multiple_entries()
        {
            LoggerQueue queue = new LoggerQueue(3);

            queue.add("test1");
            queue.add("test2");
            queue.add("test3");

            Assert.AreEqual(queue.pop(), "test1");
            Assert.AreEqual(queue.pop(), "test2");
            Assert.AreEqual(queue.pop(), "test3");
        }
    }
}
