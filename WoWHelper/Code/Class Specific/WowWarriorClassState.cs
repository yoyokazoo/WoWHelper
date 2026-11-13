using System.Drawing;

namespace WoWHelper
{
    public class WowWarriorClassState : WowClassState
    {
        public bool BattleShoutActive { get; private set; }
        public bool TargetHasRend { get; private set; }
        public bool CanChargeTarget { get; private set; }
        public bool CanShootTarget { get; private set; }
        public bool WaitingToShoot { get; private set; }
        public bool NextSwingSpellQueued { get; private set; }
        public bool OverpowerUsable { get; private set; }
        public bool KnowsRend { get; private set; }

        public bool KnowsExecute { get; private set; }
        public bool MortalStrikeOrBloodThirstCooledDown { get; private set; }
        public bool KnowsMortalStrikeOrBloodthirst { get; private set; }
        public bool KnowsCharge { get; private set; }
        public bool TargetHasSunderArmor { get; private set; }
        public bool KnowsSunderArmor { get; private set; }
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
            KnowsRend = bits[8];

            KnowsExecute = bits[9];
            MortalStrikeOrBloodThirstCooledDown = bits[10];
            KnowsMortalStrikeOrBloodthirst = bits[11];
            KnowsCharge = bits[12];
            TargetHasSunderArmor = bits[13];
            KnowsSunderArmor = bits[14];
            // 15 reserved
            CanCastSweepingStrikes = bits[16];
        }
    }
}
