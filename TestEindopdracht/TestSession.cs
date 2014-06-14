using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Eindopdracht;

namespace TestEindopdracht
{
    [TestClass]
    public class TestSession
    {
        [TestMethod]
        public void create_session()
        {
            Connector connection = Connector.getInstance();
            SessionManager sm = new SessionManager(connection);
            Assert.AreEqual(Eindopdracht.SessionManager.Warning.NONE, sm.Login("chris", "password", "127.0.0.1"));
        }
    }
}
