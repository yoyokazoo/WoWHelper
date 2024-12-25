using System;
using System.Collections.Generic;

namespace WoWHelper.Code.Goap
{
    public class GoapPlanner
    {
        public List<GoapGoal> Goals;

        public GoapPlanner()
        {
            Goals = new List<GoapGoal>();
        }

        // Let's always have a goal, even if that goal is to idle.
        public GoapGoal PickGoal(WoWWorldState worldStates)
        {
            float highestPriority = float.MinValue;
            GoapGoal highestPriGoal = null;

            foreach(var goal in Goals)
            {
                if (goal.Priority >= highestPriority)
                {
                    highestPriority = goal.Priority;
                    highestPriGoal = goal;
                }
            }

            if (highestPriGoal == null)
            {
                throw new Exception("Planner could not find a valid goal.  This should never happen!  Add a goal with float.MinValue priority as the default goal, even if it is to just idle.");
            }

            return highestPriGoal;
        }

        public virtual void FindPath()
        {

        }
    }
}
