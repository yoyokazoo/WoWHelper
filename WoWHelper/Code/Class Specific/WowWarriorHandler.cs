using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using WoWHelper.Code;
using WoWHelper.Code.WorldState;

namespace WoWHelper
{
    public class WowWarriorHandler : WowClassHandler<WowWarriorClassState>
    {
        #region Input

        // Warrior keybinds/macros. Private so only this handler can press them -- shared
        // keys (food, attack, dynamite, etc.) stay on WowInput.
        // Using Shift Keys built into the macros instead of binding a bar to the shift keys,
        // as it screws up regular bars if we bind a bar to shift
        private static class Input
        {
            /*
             * 2 Pull
#showtooltip [mod:shift] Shoot; Charge
/cast [nomod] Charge
/cast [mod:shift] Shoot
            */
            public const Keys CHARGE = Keys.D2;
            public const Keys SHIFT_SHOOT = Keys.D2;

            /*
             * 3 H/C
#showtooltip [mod:shift] Cleave; Heroic Strike
/cast [nomod] Heroic Strike
/cast [mod:shift] Cleave
            */
            public const Keys HEROIC_STRIKE = Keys.D3;
            public const Keys SHIFT_CLEAVE = Keys.D3;

            /*
             * 4 Buff
#showtooltip [mod:shift] Berserker Rage; [mod:ctrl] Sweeping Strikes; Battle Shout
/cast [nomod] Battle Shout
/cast [mod:shift] Berserker Rage
/cast [mod:ctrl] Sweeping Strikes
            */
            public const Keys BATTLE_SHOUT = Keys.D4;
            // TODO: add stance dancing to macros??
            public const Keys SHIFT_BERSERKER_RAGE = Keys.D4;
            public const Keys CTRL_SWEEPING_STRIKES = Keys.D4;

            /*
             * 5 Dmg
#showtooltip [mod:shift] Sunder Armor; Mortal Strike
/cast [nomod] Mortal Strike
/cast [mod:shift] Sunder Armor
            */
            public const Keys MORTALSTRIKE_BLOODTHIRST = Keys.D5;
            public const Keys SHIFT_SUNDER_ARMOR = Keys.D5;

            /*
             * 6 Rend
#showtooltip [mod:shift] Execute; Mortal Strike
/cast [nomod] Mortal Strike
/cast [mod:shift] Execute
            */
            public const Keys REND = Keys.D6;
            public const Keys SHIFT_OVERPOWER = Keys.D6;

            /*
             * 7 shit
#showtooltip [mod:shift] Retaliation; Execute
/cast [nomod] Execute
/cast [mod:shift] Retaliation
            */
            public const Keys EXECUTE = Keys.D7;
            public const Keys SHIFT_RETALIATION = Keys.D7;
        }

        #endregion

        #region Constants

        // Warrior costs
        private const int HEROIC_STRIKE_RAGE_COST = 15;
        private const int BATTLE_SHOUT_RAGE_COST = 10;
        private const int REND_RAGE_COST = 10;
        private const int OVERPOWER_RAGE_COST = 5;
        private const int EXECUTE_RAGE_COST = 15;
        private const int SUNDER_ARMOR_RAGE_COST = 15;

        private const int SWEEPING_STRIKES_RAGE_COST = 30;
        private const int WHIRLWIND_RAGE_COST = 25;
        private const int CLEAVE_RAGE_COST = 20;

        private const int MORTAL_STRIKE_BLOODTHIRST_RAGE_COST = 30;

        private const int EXECUTE_HP_THRESHOLD = 20;
        private const int REND_HP_THRESHOLD = 60;
        private const int SUNDER_ARMOR_HP_THRESHOLD = 75;

        private const int BERSERKER_RAGE_COOLDOWN_MILLIS = 30 * 1000;    // 30 seconds
        private const int BATTLE_SHOUT_DURATION_MILLIS = 3 * 60 * 1000;  // 3 minutes

        #endregion

        #region State

        // When the bot last pressed Battle Shout -- see ShouldCastBattleShout.
        private long BattleShoutPressTime;
        private long BerserkerRageTime;
        // Retaliation is a once-per-fight emergency button; reset in OnEnterFight.
        private bool EmergencyCombatActionTaken;

        public WowWarriorHandler(WowPlayer player) : base(player) { }

        #endregion

        #region Tasks

        public override void OnEnterFight()
        {
            EmergencyCombatActionTaken = false;
        }

        public override async Task<bool> CombatLoopTask()
        {
            await SingleCombatTask();

            return true;
        }

        public override async Task SingleCombatTask()
        {
            // First do our "Make sure we're not standing around doing nothing" checks
            if (await Player.MeleeMakeSureWeAreAttackingEnemyTask())
            {
                return;
            }

            // Next, check if we need to pop any big cooldowns
            if (await EmergencyActionTask())
            {
                return;
            }

            if (await Player.ThrowDynamiteTask())
            {
                return;
            }

            if (await Player.UseHealingPotionTask())
            {
                return;
            }

            if (ShouldOpenWithBerserkerRage())
            {
                await StartOfCombatBerserkerRage();
                BerserkerRageTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            }

            if (ShouldCastBattleShout())
            {
                if (WorldState.ResourcePercent >= BATTLE_SHOUT_RAGE_COST)
                {
                    await WowInput.PressKey(Input.BATTLE_SHOUT);
                    BattleShoutPressTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                }
            }
            else if (ShouldCastSweepingStrikes())
            {
                if (WorldState.ResourcePercent >= SWEEPING_STRIKES_RAGE_COST)
                {
                    await WowInput.PressKeyWithControl(Input.CTRL_SWEEPING_STRIKES);
                }
            }
            else if (ShouldCastOverpower())
            {
                if (WorldState.ResourcePercent >= OVERPOWER_RAGE_COST)
                {
                    await WowInput.PressKeyWithShift(Input.SHIFT_OVERPOWER);
                }
            }
            else if (ShouldCastExecute())
            {
                if (WorldState.ResourcePercent >= EXECUTE_RAGE_COST)
                {
                    await WowInput.PressKey(Input.EXECUTE);
                }
            }
            else if (ShouldCastSunderArmor())
            {
                if (WorldState.ResourcePercent >= SUNDER_ARMOR_RAGE_COST)
                {
                    await WowInput.PressKeyWithShift(Input.SHIFT_SUNDER_ARMOR);
                }
            }
            else if (ShouldCastRend())
            {
                if (WorldState.ResourcePercent >= REND_RAGE_COST)
                {
                    await WowInput.PressKey(Input.REND);
                }
            }
            else if (ShouldCastMortalStrikeOrBloodthirst())
            {
                if (WorldState.ResourcePercent >= MORTAL_STRIKE_BLOODTHIRST_RAGE_COST)
                {
                    await WowInput.PressKey(Input.MORTALSTRIKE_BLOODTHIRST);
                }
            }
            else if (WorldState.AttackerCount > 1)
            {
                if (ShouldCastCleave())
                {
                    if (WorldState.ResourcePercent >= CleaveRageRequired())
                    {
                        await WowInput.PressKeyWithShift(Input.SHIFT_CLEAVE);
                    }
                }
            }
            else // TODO: 0 attackers can happen if I forget to turn enemy nameplates on
            {
                if (ShouldCastHeroicStrike())
                {
                    if (WorldState.ResourcePercent >= HeroicStrikeRageRequired())
                    {
                        await WowInput.PressKey(Input.HEROIC_STRIKE);
                    }
                }
                // TODO: Actually split out Heroic Strike and cast if we have really surplus rage
            }
        }

        // Fear-casters are worth opening on preemptively, rather than reacting once feared.
        private bool ShouldOpenWithBerserkerRage()
        {
            return WorldState.IsTargetFearCaster && !GeneralHelpers.CurrentTimeInsideDuration(BerserkerRageTime, BERSERKER_RAGE_COOLDOWN_MILLIS);
        }

        // None of the ShouldCastX checks below look at rage -- they only decide
        // whether the ability is the right thing to be doing given the game state. Rage is
        // checked afterwards, inside the chosen branch of the rotation chain.

        private bool ShouldCastBattleShout()
        {
            // rather than pass a custom bool for a spell that will be trained 5m into the character's life, let's just hack this one
            bool knowsBattleShout = WorldState.PlayerLevel >= 3 || WorldState.AllSkillsKnownForThisLevel;
            // On Midnight-rules clients the addon can't read auras in combat at all, so in
            // combat BattleShoutActive only knows about a shout from before the pull (the
            // addon remembers its expiration). A shout cast mid-fight is invisible to it, so
            // trust our own press time instead. Out of combat the read is real, so the press
            // time is ignored (a press that didn't land gets retried after the fight).
            bool recentlyPressedInCombat = WorldState.IsInCombat &&
                GeneralHelpers.CurrentTimeInsideDuration(BattleShoutPressTime, BATTLE_SHOUT_DURATION_MILLIS);
            return !ClassState.BattleShoutActive && !recentlyPressedInCombat && knowsBattleShout;
        }

        // Only below Battle Shout in priority. Sweeping Strikes makes the next
        // few melee swings cleave to a second target, so it's only worth popping
        // against multiple attackers, and only once it's actually trained and off
        // cooldown -- both folded into ClassState.CanCastSweepingStrikes itself
        // (see its property comment on WowWarriorClassState).
        private bool ShouldCastSweepingStrikes()
        {
            return ClassState.CanCastSweepingStrikes &&
                WorldState.AttackerCount > 1;
        }

        private bool ShouldCastOverpower()
        {
            return ClassState.OverpowerUsable;
        }

        private bool ShouldCastExecute()
        {
            return ClassState.KnowsExecute &&
                WorldState.TargetHpPercent <= EXECUTE_HP_THRESHOLD;
        }

        // One Sunder Armor per target, and only while it's still worth it: a single stack's
        // armor reduction pays for its rage over the rest of a full-HP fight, but not on a mob
        // that's already mostly dead. TargetHasSunderArmor is "any stack at all", so this
        // deliberately never builds past the first one -- that rage is better spent on
        // Mortal Strike/Bloodthirst/Heroic Strike further down the priority list.
        private bool ShouldCastSunderArmor()
        {
            return ClassState.KnowsSunderArmor &&
                !ClassState.TargetHasSunderArmor &&
                WorldState.TargetHpPercent >= SUNDER_ARMOR_HP_THRESHOLD;
        }

        // Bleed-immune targets never get Rend, regardless of anything else. Otherwise, Rend
        // once it's actually trained and the target isn't already bled, but only if it's
        // worth the rage: either high enough HP that Rend's DoT will have time to tick, or a
        // runner mob -- those flee at low HP, so Rend's damage-over-time keeps ticking (and
        // helps finish it off) even after it breaks line of sight/melee range.
        private bool ShouldCastRend()
        {
            if (WorldState.IsTargetBleedImmune)
            {
                return false;
            }

            return ClassState.KnowsRend &&
                !ClassState.TargetHasRend &&
                (WorldState.TargetHpPercent > REND_HP_THRESHOLD || WorldState.IsTargetRunnerMob);
        }

        // ClassState.MortalStrikeOrBloodThirstCooledDown alone isn't a safe usability check --
        // it's decoded from a Lua cooldown query that reads ready for a spell the player hasn't
        // even trained yet (see KnowsMortalStrikeOrBloodthirst()'s comment in
        // WarriorFunctions.lua) -- so KnowsMortalStrikeOrBloodthirst has to gate it too.
        // Sits above the attacker-count split in the rotation since it's the same call
        // either way (the two branches used to duplicate it).
        private bool ShouldCastMortalStrikeOrBloodthirst()
        {
            return ClassState.KnowsMortalStrikeOrBloodthirst &&
                ClassState.MortalStrikeOrBloodThirstCooledDown;
        }

        // Cleave only if nothing's already queued for the next swing (pressing it again
        // would just be a wasted keypress until the swing lands).
        private bool ShouldCastCleave()
        {
            return !ClassState.NextSwingSpellQueued;
        }

        // Heroic only if nothing's already queued for the next swing (see Cleave above).
        private bool ShouldCastHeroicStrike()
        {
            return !ClassState.NextSwingSpellQueued;
        }

        // Cleave/Heroic Strike are fillers for when Mortal Strike/Bloodthirst is on cooldown,
        // so their rage requirement is their own cost PLUS a reserve for the next Mortal
        // Strike/Bloodthirst -- otherwise the filler eats the rage MS/BT needs the moment it
        // comes back up. No reserve if we haven't trained MS/BT yet and can't cast it at all.
        private int CleaveRageRequired()
        {
            return CLEAVE_RAGE_COST + MortalStrikeOrBloodthirstRageReserve();
        }

        private int HeroicStrikeRageRequired()
        {
            return HEROIC_STRIKE_RAGE_COST + MortalStrikeOrBloodthirstRageReserve();
        }

        private int MortalStrikeOrBloodthirstRageReserve()
        {
            return ClassState.KnowsMortalStrikeOrBloodthirst ? MORTAL_STRIKE_BLOODTHIRST_RAGE_COST : 0;
        }

        public override async Task<bool> StartBattleReadyRecoverTask()
        {
            // clams
            //if (WorldState.PlayerHpPercent < WowConstants.EAT_FOOD_HP_THRESHOLD)
            //{
                await WowInput.PressKey(WowInput.EAT_FOOD);
            //}

            return true;
        }

        public override async Task<bool> WaitUntilBattleReadyTask()
        {
            // For now, I don't care if dynamite is cooled down.  If we dynamited and didn't have to potion, we're probably safe enough to keep going
            // especially since the dynamite cooldown is so short it'll probably be up by the time we need it again.

            bool battleReady = IsBattleReady();

            if (battleReady)
            {
                await Player.ScootForwardsTask();
            }

            return battleReady;
        }

        public override bool IsBattleReady()
        {
            bool hpRecovered = WorldState.PlayerHpPercent >= WowConstants.STOP_RESTING_HP_THRESHOLD;
            bool potionIsCooledDown = !GeneralHelpers.CurrentTimeInsideDuration(Player.HealthPotionTime, WowConstants.POTION_COOLDOWN_MILLIS);
            bool battleReady = hpRecovered && potionIsCooledDown;
            return battleReady;
        }

        public override async Task<bool> KickOffEngageTask()
        {
            await Player.EndWalkForwardTask();
            await Player.TurnToFaceTargetMarkerTask();
            return true;
        }

        public override async Task<bool> ExecuteEngageTask()
        {
            if (await Player.AbandonUnreachableEngageTarget())
            {
                return false;
            }

            var location = Player.FarmingLocationConfiguration;

            if (location.EngageMethod == EngagementMethod.Charge && !ClassState.KnowsCharge) // no charge yet
            {
                await WowInput.PressKey(WowInput.START_ATTACK);
                // Losing Charge range mid-walk means the target's no longer engageable
                // (e.g. it moved/evaded), so stop walking rather than keep chasing it.
                await Player.WalkIntoMeleeRangeTask(giveUpCondition: () => !ClassState.CanChargeTarget);
                //await WowInput.PressKey(WowInput.START_ATTACK);
            }
            else if (location.EngageMethod == EngagementMethod.Charge)
            {
                await FaceAndChargeTarget();
            }
            else if (location.EngageMethod == EngagementMethod.Pull)
            {
                if (!ClassState.WaitingToShoot)
                {
                    await Player.TurnToFaceTargetMarkerTask();
                    await WowInput.PressKeyWithShift(Input.SHIFT_SHOOT);
                }
            }

            return CanEngageTarget();
        }

        private async Task<bool> FaceAndChargeTarget()
        {
            await Player.TurnToFaceTargetMarkerTask();
            await WowInput.PressKey(Input.CHARGE);
            return true;
        }

        public override bool CanEngageTarget()
        {
            var location = Player.FarmingLocationConfiguration;
            if (location == null)
            {
                return false;
            }

            switch (location.EngageMethod)
            {
                case EngagementMethod.Charge:
                    {
                        if (!ClassState.KnowsCharge && ClassState.CanChargeTarget)
                        {
                            return WowScreenCapture.FindTargetMarkerOnScreen(Player.ScreenConfiguration) != null;
                        }
                        return ClassState.CanChargeTarget;
                    }
                case EngagementMethod.Pull: return ClassState.CanShootTarget;
                default: throw new System.NotImplementedException(
                    $"{nameof(WowWarriorHandler)}.{nameof(CanEngageTarget)}: EngageMethod \"{location.EngageMethod}\" (from location " +
                    $"\"{Player.CurrentRoute?.Name}\") isn't supported for Warrior -- only Charge/Pull " +
                    $"are. This route's EngageMethod likely wasn't set up for this class.");
            }
        }

        private async Task<bool> EmergencyActionTask()
        {
            bool tooManyAttackers = WorldState.AttackerCount >= WowConstants.TOO_MANY_ATTACKERS_THRESHOLD;
            bool emergencyHpThreshold = WorldState.PlayerHpPercent <= WowConstants.EMERGENCY_HP_THRESHOLD;
            bool takeEmergencyAction = !EmergencyCombatActionTaken && (tooManyAttackers || emergencyHpThreshold);

            if (takeEmergencyAction)
            {
                EmergencyCombatActionTaken = true;
                string emergencyMessage = tooManyAttackers ? $"TOO MANY ATTACKERS HELP" : $"EMERGENCY HP THRESHOLD HIT {WorldState.PlayerHpPercent}";
                Player.SetLogoutVariablesAndSlack($"Got into a Retaliation situation ({emergencyMessage}), logging off for safety");

                // cast retaliation once GCD is cooled down
                await Player.WaitForGlobalCooldownTask();
                await WowInput.PressKeyWithShift(Input.SHIFT_RETALIATION);
            }

            return takeEmergencyAction;
        }

        private async Task<bool> StartOfCombatBerserkerRage()
        {
            await WowInput.PressKeyWithShift(Input.SHIFT_BERSERKER_RAGE);
            await Task.Delay(150);
            await WowInput.PressKey(Input.MORTALSTRIKE_BLOODTHIRST);
            await Task.Delay(150);
            await WowInput.PressKey(Input.MORTALSTRIKE_BLOODTHIRST);
            await Task.Delay(150);
            await WowInput.PressKey(Input.MORTALSTRIKE_BLOODTHIRST);
            await Task.Delay(150);
            await WowInput.PressKey(Input.MORTALSTRIKE_BLOODTHIRST);

            return true;
        }

        #endregion
    }
}
