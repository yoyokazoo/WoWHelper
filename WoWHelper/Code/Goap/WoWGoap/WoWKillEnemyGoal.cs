namespace WoWHelper.Code.Goap.WoWGoap
{
    public class WoWKillEnemyGoal : GoapGoal
    {
        public WoWKillEnemyGoal() : base (100) { }

        public override bool IsValid()
        {
            // WorldState has an enemy nearby
            return false;
        }
    }
}
