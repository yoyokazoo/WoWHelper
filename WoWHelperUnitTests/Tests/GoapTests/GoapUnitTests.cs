using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using WoWHelper.Code.Goap;
using WoWHelper.Code.Goap.WoWGoap;

namespace WoWHelperUnitTests.Tests.GoapTests
{
    [TestClass]
    public class GoapUnitTests
    {
        [TestMethod]
        public void TestPickGoal_NoGoals_ThrowsException()
        {
            GoapPlanner planner = new GoapPlanner();
            Assert.ThrowsException<System.Exception>(() => planner.PickGoal());
        }

        [TestMethod]
        public void TestPickGoal_IdleGoal_FindsIdleGoal()
        {
            GoapPlanner planner = new GoapPlanner();
            var idleGoal = new WoWIdleGoal();
            planner.Goals.Add(idleGoal);

            var goal = planner.PickGoal();
            Assert.AreEqual(idleGoal, goal);
        }

        [TestMethod]
        public void TestPickGoal_MultipleGoals_FindsHighestPriority()
        {
            GoapPlanner planner = new GoapPlanner();
            var idleGoal = new WoWIdleGoal();
            var killGoal = new WoWKillEnemyGoal();
            planner.Goals.Add(idleGoal);
            planner.Goals.Add(killGoal);

            var goal = planner.PickGoal();
            Assert.AreEqual(killGoal, goal);
        }

        // make sure PickGoal only picks from valid goals


    }
}
