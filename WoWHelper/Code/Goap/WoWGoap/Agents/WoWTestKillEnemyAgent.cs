using System;
using WoWHelper.Code.Goap.WoWGoap.Actions;

namespace WoWHelper.Code.Goap.WoWGoap
{
    public class WoWTestKillEnemyAgent : GoapAgent
    {
        public WoWTestKillEnemyAgent()
        {
            var lightAttackActionToPerform = new Action(() => Console.WriteLine("Light attack!"));
            var lightAttackAction = new WoWLightAttackAction(lightAttackActionToPerform);
            
            Actions.Add(lightAttackAction);
        }
    }
}