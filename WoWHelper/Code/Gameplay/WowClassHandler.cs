using System.Drawing;
using System.Threading.Tasks;
using WoWHelper.Code.Gameplay;

namespace WoWHelper
{
    // Everything class-specific the bot does: rotation, engage, rest/buff, plus the
    // class's own keybinds (a private nested Input class in each subtype), tuning
    // constants and timers. These used to be partial-class methods on WowPlayer, which
    // meant every class's inputs/helpers were visible to every other class's code; as
    // separate types, a Warrior keybind or helper is simply unreachable from Shaman code.
    //
    // WowPlayer builds exactly one of these (see WowPlayer.UpdateClassState) once the
    // player's class is detected, and calls into it through the thin wrappers in
    // WowPlayerCombatConfig.cs. Each handler also owns its class's typed WowClassState
    // (see WowClassHandler<TState>) -- nothing outside the handler can read it.
    public abstract class WowClassHandler
    {
        protected WowPlayer Player { get; }

        // Shorthand so handler code reads like it did as a WowPlayer partial. Re-read on
        // every access, since WowPlayer swaps in a new WowWorldState each update.
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
