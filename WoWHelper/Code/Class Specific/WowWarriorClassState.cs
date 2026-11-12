using System.Drawing;

namespace WoWHelper
{
    // Warrior's slice of the Class pixel rows. Box numbers here MUST match
    // WARRIOR_CLASS_BOOL_ONE_FIELDS in WarriorFunctions.lua (ClassBoolTwo is
    // unused for Warrior so far).
    public class WowWarriorClassState : WowClassState
    {
        public bool BattleShoutActive { get; private set; }
        public bool TargetHasRend { get; private set; }
        public bool CanChargeTarget { get; private set; }
        public bool CanShootTarget { get; private set; }
        public bool WaitingToShoot { get; private set; }
        // Heroic Strike OR Cleave is currently queued for the next swing (Lua's
        // IsAnyNextSwingSpellQueued -- only one on-next-swing ability can be queued at a
        // time, so one bit covers both). Gates WarriorShouldCastHeroicStrike/Cleave so the
        // rotation doesn't keep re-pressing an already-queued next-swing ability every tick
        // until the swing actually lands.
        public bool NextSwingSpellQueued { get; private set; }
        public bool OverpowerUsable { get; private set; }
        public bool KnowsRend { get; private set; }

        public bool KnowsExecute { get; private set; }
        public bool MortalStrikeOrBloodThirstCooledDown { get; private set; }
        public bool KnowsMortalStrikeOrBloodthirst { get; private set; }
        public bool KnowsCharge { get; private set; }
        public bool TargetHasSunderArmor { get; private set; }
        public bool KnowsSunderArmor { get; private set; }
        // Whether Sweeping Strikes is both trained AND off cooldown -- the trained check
        // is folded into the Lua side's CanCastSweepingStrikes() itself (WarriorFunctions.lua)
        // rather than exposed as its own bit the way KnowsRend/KnowsSunderArmor/etc. are,
        // since nothing here ever needs "trained" on its own, only paired with "off
        // cooldown" (see that function's comment for why that's safe for this one but not
        // for KnowsCharge/KnowsMortalStrikeOrBloodthirst, which are each read from more
        // than one place).
        public bool CanCastSweepingStrikes { get; private set; }

        public override void UpdateFromBitmap(Bitmap bmp)
        {
            Initialized = true;

            PixelRowBits bits = WowPixelRows.ReadBits(bmp, PixelRow.ClassBoolOne);

            BattleShoutActive = bits[1];
            TargetHasRend = bits[2];
            CanChargeTarget = bits[3];
            CanShootTarget = bits[4];
            WaitingToShoot = bits[5];
            NextSwingSpellQueued = bits[6];
            OverpowerUsable = bits[7];
            // Whether Rend is actually trained yet -- used instead of a hardcoded
            // "PlayerLevel >= 4" check in WowWarriorHandler.ShouldCastRend.
            KnowsRend = bits[8];

            // Whether Execute is actually trained yet -- used instead of a hardcoded
            // "PlayerLevel >= 24" check in WowWarriorHandler.ShouldCastExecute.
            KnowsExecute = bits[9];
            MortalStrikeOrBloodThirstCooledDown = bits[10];
            // Whether Mortal Strike/Bloodthirst is actually trained yet -- used instead of a
            // hardcoded "PlayerLevel >= 40" check in WowWarriorHandler's
            // ShouldCastMortalStrikeOrBloodthirst/HeroicStrikeRageRequired.
            KnowsMortalStrikeOrBloodthirst = bits[11];
            // Whether Charge is actually trained yet -- used instead of a hardcoded
            // "PlayerLevel >= 4" check in
            // WowWarriorHandler.ExecuteEngageTask/CanEngageTarget.
            KnowsCharge = bits[12];
            // Any stack of Sunder Armor on the target at all (anyone's), not a stack count --
            // WowWarriorHandler.ShouldCastSunderArmor only wants one application on
            // the target, not a full 5-stack.
            TargetHasSunderArmor = bits[13];
            KnowsSunderArmor = bits[14];
            // 15 reserved
            CanCastSweepingStrikes = bits[16];
        }
    }
}
