using System.Drawing;
using System.Threading.Tasks;
using WoWHelper.Code.Gameplay;

namespace WoWHelper
{
    public abstract class WowClassHandler
    {
        protected WowPlayer Player { get; }
        protected WowWorldState WorldState => Player.WorldState;

        protected WowClassHandler(WowPlayer player)
        {
            Player = player;
        }

        public abstract void UpdateFromBitmap(Bitmap bmp);

        public abstract Task<bool> StartBattleReadyRecoverTask();
        public abstract Task<bool> WaitUntilBattleReadyTask();
        public abstract bool IsBattleReady();
        public abstract Task<bool> KickOffEngageTask();
        public abstract Task<bool> ExecuteEngageTask();
        public abstract Task<bool> CombatLoopTask();
        public abstract Task SingleCombatTask();
        public abstract bool CanEngageTarget();

        // Called when the player enters PlayerGoal.FIGHT, for per-fight state resets.
        public virtual void OnEnterFight() { }

        public static WowClassHandler Create(WowCombatConfiguration combatConfiguration, WowPlayer player)
        {
            switch (combatConfiguration)
            {
                case WowCombatConfiguration.Warrior: return new WowWarriorHandler(player);
                case WowCombatConfiguration.Shaman: return new WowShamanHandler(player);
                case WowCombatConfiguration.Warlock: return new WowWarlockHandler(player);
                default: throw new System.NotImplementedException(
                    $"{nameof(WowClassHandler)}.{nameof(Create)}: no handler implemented for CombatConfiguration \"{combatConfiguration}\" -- " +
                    $"this should only be called with a resolved (non-Unknown) CombatConfiguration.");
            }
        }
    }

    // Typed half of the base: holds this class's own WowClassState subtype, built once
    // and updated in place every tick off the same bitmap as WorldState.
    public abstract class WowClassHandler<TState> : WowClassHandler where TState : WowClassState, new()
    {
        protected TState ClassState { get; } = new TState();

        protected WowClassHandler(WowPlayer player) : base(player) { }

        public override void UpdateFromBitmap(Bitmap bmp)
        {
            ClassState.UpdateFromBitmap(bmp);
        }
    }
}
