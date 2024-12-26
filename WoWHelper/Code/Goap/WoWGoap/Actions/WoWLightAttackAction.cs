using System;

namespace WoWHelper.Code.Goap.WoWGoap.Actions
{
    public class WoWLightAttackAction : GoapAction
    {
        public WoWLightAttackAction(Action actionToPerform) : base(actionToPerform)
        {
        }

        public override float GetCost(GoapWorldState worldState)
        {
            return 1;
        }

        public override float GetBenefit(GoapWorldState worldState)
        {
            return 1;
        }
    }
}