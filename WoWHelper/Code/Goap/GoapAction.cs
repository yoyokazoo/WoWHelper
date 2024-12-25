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

        public virtual bool IsValid()
        {
            return false;
        }

        public virtual float GetCost()
        {
            return float.MaxValue;
        }

        // This will need to be updated.  Needs to take into account world state, current path, etc.
        public virtual float GetBenefit()
        {
            return 0.0f;
        }
    }
}
