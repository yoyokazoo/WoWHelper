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

        public virtual bool IsValid(GoapWorldState worldState)
        {
            return false;
        }

        public virtual float GetCost(GoapWorldState worldState)
        {
            return float.MaxValue;
        }

        // This will need to be updated.  Needs to take into account world state, current path, etc.
        public virtual float GetBenefit(GoapWorldState worldState)
        {
            return 0.0f;
        }
    }
}
