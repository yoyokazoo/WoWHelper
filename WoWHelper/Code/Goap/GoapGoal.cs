namespace WoWHelper.Code.Goap
{
    public abstract class GoapGoal
    {
        public float Priority { get; set; }

        // Priority should probably be dynamic instead
        public GoapGoal(float priority)
        {
            Priority = priority;
        }

        public virtual bool IsValid(WoWWorldState worldStates)
        {
            return false;
        }
    }
}
