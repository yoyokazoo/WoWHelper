using InputManager;
using System;
using System.Threading.Tasks;
using WindowsGameAutomationTools.Slack;
using WoWHelper.Code;
using WoWHelper.Code.WorldState;

namespace WoWHelper
{
    public partial class WowPlayer
    {
        // ensureValidState updates state before checking, set to false only if you're sure we haven't clicked a button between the last world state update
        public async Task<bool> WaitForGlobalCooldownTask(bool ensureValidState = true)
        {
            if (ensureValidState)
            {
                await UpdateWorldStateAsync();
            }

            while (!WorldState.GCDCooledDown)
            {
                await UpdateWorldStateAsync();
            }

            return true;
        }

        // Waits up to totalMillis, but exits early if you get into combat
        public async Task<bool> WaitUnlessInCombatTask(long totalMillis, bool ensureValidState = true)
        {
            if (ensureValidState)
            {
                await UpdateWorldStateAsync();
            }

            long deadline = DateTimeOffset.Now.ToUnixTimeMilliseconds() + totalMillis;

            while (!WorldState.IsInCombat && DateTimeOffset.Now.ToUnixTimeMilliseconds() < deadline)
            {
                await UpdateWorldStateAsync();
            }

            return !WorldState.IsInCombat;
        }

        public async Task<bool> ThrowDynamiteTask(bool forceThrow = false)
        {
            bool shouldThrowDynamite = forceThrow || (WorldState.AttackerCount > 1 && WorldState.PlayerLevel >= WowConstants.DYNAMITE_LEVEL);
            bool canThrowDynamite = !GeneralHelpers.CurrentTimeInsideDuration(DynamiteTime, WowConstants.DYNAMITE_COOLDOWN_MILLIS);

            if (shouldThrowDynamite && canThrowDynamite)
            {
                Mouse.Move(ScreenConfiguration.DynamiteAndDummyX, ScreenConfiguration.DynamiteAndDummyY);
                await Task.Delay(50);
                await WowInput.PressKey(WowInput.THROW_DYNAMITE);
                DynamiteTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                await Task.Delay(1000);
                return true;
            }

            return false;
        }

        public async Task<bool> UseHealingPotionTask()
        {
            bool shouldUseHealingPotion = WorldState.PlayerHpPercent <= WowConstants.HEALING_POTION_HP_THRESHOLD;
            bool canUseHealingPotion = !GeneralHelpers.CurrentTimeInsideDuration(HealthPotionTime, WowConstants.POTION_COOLDOWN_MILLIS);

            if (shouldUseHealingPotion && canUseHealingPotion)
            {
                SlackHelper.SendMessageToChannel("Potion used!");
                await Task.Delay(200); // there's a brief, non-gcd limiter that prevents clicking everything simultaneously
                await WowInput.PressKey(WowInput.HEALING_POTION);
                HealthPotionTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                return true;
            }

            return false;
        }

        public async Task<bool> MeleeMakeSureWeAreAttackingEnemyTask()
        {
            bool attackerJustDied = PreviousWorldState.AttackerCount > WorldState.AttackerCount && WorldState.AttackerCount > 0;
            bool inCombatButNotAutoAttacking = WorldState.IsInCombat && !WorldState.IsAutoAttacking;
            bool facingWrongWay = WorldState.FacingWrongWay;
            bool targetNeedsToBeInFront = WorldState.TargetNeedsToBeInFront;
            bool targetIsEvading = WorldState.TargetRecentlyEvaded;
            bool invalidTarget = WorldState.InvalidTarget;
            bool outOfRange = WorldState.OutOfRange;
            bool notInLineOfSight = WorldState.NotInLineOfSight;
            bool noPathAvailable = WorldState.NoPathAvailable;
            bool targetUnreachable = WorldState.TargetUnreachable;
            bool notAttackingEnemy = attackerJustDied || inCombatButNotAutoAttacking || facingWrongWay || targetNeedsToBeInFront || targetIsEvading || invalidTarget || outOfRange || notInLineOfSight || noPathAvailable || targetUnreachable;

            Console.WriteLine($"attackerJustDied {attackerJustDied}, inCombatButNotAutoAttacking {inCombatButNotAutoAttacking}, facingWrongWay {facingWrongWay}, targetNeedsToBeInFront {targetNeedsToBeInFront}, invalidTarget {invalidTarget}, outOfRange {outOfRange}, notInLineOfSight {notInLineOfSight}, noPathAvailable {noPathAvailable}, targetIsEvading {targetIsEvading}");

            if (facingWrongWay || targetNeedsToBeInFront || targetIsEvading)
            {
                // We don't show the pink square on mobs that other players tagged.
                // We could get into a scenario where someone else tags but we grab threat
                // before that registers, so just in case, scoot back
                if (!await TurnToFaceTargetMarkerTask())
                {
                    await ScootBackwardsTask();
                }
            }

            // we may have targeted something in the distance then got aggroed by something else, clear target so we pick them up
            if (invalidTarget || outOfRange)
            {
                // attacker count now updates immediately, so only clear target if the mob isn't
                // actually in combat with us OR it is and there are multiple attackers (worth
                // re-prioritizing) -- don't spam clear if we don't have to
                if (!WorldState.CurrentTargetInCombatWithUs || (WorldState.CurrentTargetInCombatWithUs && WorldState.AttackerCount > 1))
                {
                    Console.WriteLine($"tooFarAway || invalidTarget || outOfRange || notInLineOfSight, WorldState.CurrentTargetInCombatWithUs {WorldState.CurrentTargetInCombatWithUs}, WorldState.AttackerCount {WorldState.AttackerCount}");
                    await WowInput.PressKeyWithShift(WowInput.SHIFT_CLEAR_TARGET);

                    // if it's actually too far away, waiting a bit won't matter
                    // if we have mistargeted something far away, give the attacker a bit of time to hit us before /startattack,
                    // otherwise /startattack auto targets in front of us, sometimes not the right mob and we get stuck in a loop
                    await Task.Delay(300);
                }
            }

            if (targetUnreachable)
            {
                await WowInput.PressKeyWithShift(WowInput.SHIFT_CLEAR_TARGET);
            }

            if (attackerJustDied || inCombatButNotAutoAttacking)
            {
                await StartAttackTask();
            }

            // Right now we skip the rest of the combat tasks if we're doing any of these. Is this necessary? Maybe only on clear target?
            return notAttackingEnemy;
        }

        // Clear target and suppress targeting for a while if we detected that we can't engage our current target
        public async Task<bool> AbandonUnreachableEngageTarget()
        {
            if (!WorldState.TargetUnreachable)
            {
                return false;
            }

            Console.WriteLine($"Target unreachable while trying to engage (notInLineOfSight {WorldState.NotInLineOfSight}, noPathAvailable {WorldState.NoPathAvailable}) -- clearing target and suppressing retarget for a bit");
            await WowInput.PressKeyWithShift(WowInput.SHIFT_CLEAR_TARGET);
            LastLineOfSightBailoutTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            return true;
        }

        public async Task<bool> StartAttackTask()
        {
            await WowInput.PressKey(WowInput.START_ATTACK);

            return true;
        }
    }
}