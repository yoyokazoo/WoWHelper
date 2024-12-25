using System;

namespace WoWHelper.Code.Goap
{
    public abstract class GoapAction
    {
        public Action ActionToPerform { get; set; }

        public GoapAction(Action actionToPerform)
        {
            ActionToPerform = actionToPerform;
        }

        public virtual bool IsValid(WoWWorldState worldState)
        {
            return false;
        }

        public virtual float GetCost(WoWWorldState worldState)
        {
            return float.MaxValue;
        }

        // This will need to be updated.  Needs to take into account world state, current path, etc.
        public virtual float GetBenefit(WoWWorldState worldState)
        {
            return 0.0f;
        }
    }
}
