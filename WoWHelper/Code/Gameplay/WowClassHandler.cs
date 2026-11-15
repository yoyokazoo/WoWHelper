using System.Drawing;
using System.Threading.Tasks;
using WoWHelper.Code;
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

        public virtual bool ResourceIsMana => true;

        public virtual async Task<bool> StartBattleReadyRecoverTask()
        {
            if (WorldState.PlayerHpPercent < WowConstants.EAT_FOOD_HP_THRESHOLD)
            {
                await WowInput.PressKey(WowInput.EAT_FOOD);
                await Task.Delay(200);
            }

            if (ResourceIsMana && WorldState.ResourcePercent < WowConstants.DRINK_WATER_MP_THRESHOLD)
            {
                await WowInput.PressKeyWithShift(WowInput.SHIFT_DRINK_WATER);
                await Task.Delay(200);
            }

            await WowInput.PressKeyWithControl(WowInput.EAT_FOOD);

            return true;
        }

        // Immediate post battle actions like curing poison, using surplus rage, etc.
        public virtual async Task<bool> PostBattleRecoveryTask()
        {
            await Task.Delay(0);
            return true;
        }

        // Pre battle actions we want to wait as long as possible to do so we don't waste duration AKA buffing
        public virtual async Task<bool> PreBattleBuffingTask()
        {
            await Task.Delay(0);
            return true;
        }

        public virtual bool IsBattleReady()
        {
            bool hpRecovered = WorldState.PlayerHpPercent >= WowConstants.STOP_RESTING_HP_THRESHOLD;
            bool mpRecovered = !ResourceIsMana || WorldState.ResourcePercent >= WowConstants.STOP_RESTING_MP_THRESHOLD;
            bool potionIsCooledDown = !GeneralHelpers.CurrentTimeInsideDuration(Player.HealthPotionTime, WowConstants.POTION_COOLDOWN_MILLIS);
            bool battleReady = hpRecovered && mpRecovered && potionIsCooledDown;
            return battleReady;
        }

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
