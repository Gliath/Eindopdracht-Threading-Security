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
            
            Eindopdracht.SessionManager.Warning warning;
            int hashcode = sm.Login("chris", "password", "127.0.0.1", out warning);

            Assert.AreEqual(warning, Eindopdracht.SessionManager.Warning.NONE);
        }
    }
}
