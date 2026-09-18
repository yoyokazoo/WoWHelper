using System.Drawing;

namespace WoWHelper
{
    // Warrior's slice of ClassBoolOne/Two/ClassIntOne. Bit layout here MUST
    // match WarriorFunctions.lua's GetWarriorClassBoolOne/Two/GetWarriorClassIntOne.
    public class WowWarriorClassState : WowClassState
    {
        public bool BattleShoutActive { get; private set; }
        public bool TargetHasRend { get; private set; }
        public bool CanChargeTarget { get; private set; }
        public bool CanShootTarget { get; private set; }
        public bool WaitingToShoot { get; private set; }
        public bool HeroicStrikeQueued { get; private set; }
        public bool OverpowerUsable { get; private set; }
        public bool KnowsRend { get; private set; }

        public bool KnowsExecute { get; private set; }
        public bool MortalStrikeOrBloodThirstCooledDown { get; private set; }
        public bool KnowsMortalStrikeOrBloodthirst { get; private set; }
        public bool KnowsCharge { get; private set; }
        public bool TargetHasSunderArmor { get; private set; }
        public bool KnowsSunderArmor { get; private set; }

        public override void UpdateFromBitmap(Bitmap bmp, WowScreenConfiguration screenConfig)
        {
            Initialized = true;

            Color color = bmp.GetPixel(screenConfig.ClassBoolOnePosition.X, screenConfig.ClassBoolOnePosition.Y);
            WowWorldState.DecodeByte(color.R, out var r1, out var r2, out var r3, out var r4, out var r5, out var r6, out var r7, out var r8);
            WowWorldState.DecodeByte(color.G, out var g1, out var g2, out var g3, out var g4, out var g5, out var g6, out var g7, out var g8);

            BattleShoutActive = r1;
            TargetHasRend = r2;
            CanChargeTarget = r3;
            CanShootTarget = r4;
            WaitingToShoot = r5;
            HeroicStrikeQueued = r6;
            OverpowerUsable = r7;
            // Whether Rend is actually trained yet -- replaces a hardcoded
            // "PlayerLevel >= 4" check in WowWarriorTasks.WarriorShouldCastRend.
            KnowsRend = r8;

            // Whether Execute is actually trained yet -- replaces a hardcoded
            // "PlayerLevel >= 24" check in WowWarriorTasks.WarriorShouldCastExecute.
            KnowsExecute = g1;
            MortalStrikeOrBloodThirstCooledDown = g2;
            // Whether Mortal Strike/Bloodthirst is actually trained yet -- replaces a
            // hardcoded "PlayerLevel >= 40" check in WowWarriorTasks.cs's
            // WarriorShouldCastMortalStrikeOrBloodthirst/WarriorShouldCastHeroicStrike.
            KnowsMortalStrikeOrBloodthirst = g3;
            // Whether Charge is actually trained yet -- replaces a hardcoded
            // "PlayerLevel >= 4" check in WowWarriorTasks.cs's
            // WarriorFaceCorrectDirectionToEngageTask/WarriorCanEngageTarget.
            KnowsCharge = g4;
            // Any stack of Sunder Armor on the target at all (anyone's), not a stack count --
            // WowWarriorTasks.WarriorShouldCastSunderArmor only wants one application on
            // the target, not a full 5-stack.
            TargetHasSunderArmor = g5;
            KnowsSunderArmor = g6;
            // g7-g8, ClassBoolTwo, and ClassIntOne currently reserved/unused for Warrior.
        }
    }
}
