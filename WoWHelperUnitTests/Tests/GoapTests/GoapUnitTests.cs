using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using WoWHelper;
using WoWHelper.Code.Goap;
using WoWHelper.Code.Goap.WoWGoap;

namespace WoWHelperUnitTests.Tests.GoapTests
{
    [TestClass]
    public class GoapUnitTests : UnitTestBase
    {
        [TestMethod]
        public void TestPickGoal_NoGoals_ThrowsException()
        {
            Assert.ThrowsException<System.Exception>(() => Planner.PickGoal(WorldState));
        }

        [TestMethod]
        public void TestPickGoal_IdleGoal_FindsIdleGoal()
        {
            var idleGoal = new WoWIdleGoal();
            Planner.Goals.Add(idleGoal);

            var goal = Planner.PickGoal(WorldState);
            Assert.AreEqual(idleGoal, goal);
        }

        [TestMethod]
        public void TestPickGoal_MultipleGoals_FindsHighestPriority()
        {
            var idleGoal = new WoWIdleGoal();
            var killGoal = new WoWKillEnemyGoal();
            Planner.Goals.Add(idleGoal);
            Planner.Goals.Add(killGoal);

            var goal = Planner.PickGoal(WorldState);
            Assert.AreEqual(killGoal, goal);
        }

        // make sure PickGoal only picks from valid goals


    }
}
