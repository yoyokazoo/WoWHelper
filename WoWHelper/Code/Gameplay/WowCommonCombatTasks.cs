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
        // ensureValidState updates state before checking
        // Set to false only if you're sure we haven't clicked a button between the last world state update
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

        // Waits up to totalMillis, but polls WorldState at the normal
        // UpdateWorldStateAsync/TIME_BETWEEN_WORLDSTATE_UPDATES cadence instead of blocking
        // blind through Task.Delay, so a fight starting mid-wait is noticed instead of missed.
        // Returns true if the full wait elapsed without ever being in combat; returns false
        // (immediately, no waiting at all) if already in combat, or as soon as combat starts
        // during the wait.
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
            bool shouldThrowDynamite = forceThrow || (WorldState.AttackerCount > 1 && WorldState.PlayerLevel >= WowGameplayConstants.DYNAMITE_LEVEL);

            if (shouldThrowDynamite)
            {
                Mouse.Move(FarmingConfig.ScreenConfiguration.DynamiteAndDummyX, FarmingConfig.ScreenConfiguration.DynamiteAndDummyY);
                await Task.Delay(50);
                Keyboard.KeyPress(WowInput.THROW_DYNAMITE);
                await Task.Delay(1000);
            }

            return shouldThrowDynamite;
        }

        public async Task<bool> UseHealingPotionTask()
        {
            bool shouldUseHealingPotion = WorldState.PlayerHpPercent <= WowGameplayConstants.HEALING_POTION_HP_THRESHOLD;

            if (shouldUseHealingPotion)
            {
                if (FarmingConfig.AlertOnPotionUsed)
                {
                    SlackHelper.SendMessageToChannel("Potion used!");
                }
                await Task.Delay(200); // there's a brief, non-gcd limiter that prevents clicking everything simultaneously
                await WowInput.PressKeyWithShift(WowInput.SHIFT_HEALING_POTION);
            }

            return shouldUseHealingPotion;
        }

        public async Task<bool> MeleeMakeSureWeAreAttackingEnemyTask()
        {
            bool attackerJustDied = PreviousWorldState.AttackerCount > WorldState.AttackerCount && WorldState.AttackerCount > 0;
            bool inCombatButNotAutoAttacking = WorldState.IsInCombat && !WorldState.IsAutoAttacking;
            bool tooFarAway = WorldState.TooFarAway;
            bool facingWrongWay = WorldState.FacingWrongWay; // potentially need to turn in case we're webbed and backing up wont work
            bool targetNeedsToBeInFront = WorldState.TargetNeedsToBeInFront;
            bool targetIsEvading = WorldState.TargetRecentlyEvaded;
            bool invalidTarget = WorldState.InvalidTarget;
            bool outOfRange = WorldState.OutOfRange;
            bool notInLineOfSight = WorldState.NotInLineOfSight;

            Console.WriteLine($"attackerJustDied {attackerJustDied}, inCombatButNotAutoAttacking {inCombatButNotAutoAttacking}, tooFarAway {tooFarAway}, facingWrongWay {facingWrongWay}, targetNeedsToBeInFront {targetNeedsToBeInFront}, invalidTarget {invalidTarget}, outOfRange {outOfRange}, notInLineOfSight {notInLineOfSight}, targetIsEvading {targetIsEvading}");

            if (facingWrongWay || targetNeedsToBeInFront || targetIsEvading)
            {
                // scoot back to make sure the mob is in front of you
                await ScootBackwardsTask();
            }

            // we may have targeted something in the distance then got aggroed by something else, clear target so we pick them up
            if (tooFarAway || invalidTarget || outOfRange)
            {
                // attacker count now updates immediately, so only clear target if the mob isn't
                // actually in combat with us OR it is and there are multiple attackers (worth
                // re-prioritizing) -- don't spam clear if we don't have to
                if (!WorldState.CurrentTargetInCombatWithUs || (WorldState.CurrentTargetInCombatWithUs && WorldState.AttackerCount > 1))
                {
                    Console.WriteLine($"tooFarAway || invalidTarget || outOfRange || notInLineOfSight, WorldState.CurrentTargetInCombatWithUs {WorldState.CurrentTargetInCombatWithUs}, WorldState.AttackerCount {WorldState.AttackerCount}");
                    Keyboard.KeyPress(WowInput.CLEAR_TARGET_MACRO);

                    // if it's actually too far away, waiting a bit won't matter
                    // if we have mistargeted something far away, give the attacker a bit of time to hit us before /startattack,
                    // otherwise /startattack auto targets in front of us, sometimes not the right mob and we get stuck in a loop
                    await Task.Delay(300);
                }
            }

            if (notInLineOfSight)
            {
                Keyboard.KeyPress(WowInput.CLEAR_TARGET_MACRO);
            }

            if (attackerJustDied || inCombatButNotAutoAttacking || tooFarAway)
            {
                // /startattack
                Keyboard.KeyPress(WowInput.START_ATTACK);
            }

            return attackerJustDied || inCombatButNotAutoAttacking || tooFarAway || facingWrongWay || targetNeedsToBeInFront || targetIsEvading || invalidTarget || outOfRange || notInLineOfSight;
        }

        // Called from each class's *FaceCorrectDirectionToEngageTask (the CONTINUE_TO_TRY_TO_ENGAGE
        // loop body, i.e. before we've ever landed a hit) right before attempting to pull. If the
        // target we tab/macro-picked isn't actually reachable (WorldState.NotInLineOfSight -- e.g.
        // it's leashed on the far side of terrain), that flag alone can stay true indefinitely, so
        // the class-specific CanEngageTarget() readiness check (which only reflects cooldown/GCD,
        // not reachability) would otherwise keep returning true forever and the engage loop would
        // never fall through to CHECK_FOR_LOGOUT to re-evaluate anything (see EngageAttempts /
        // ENGAGE_ROTATION_ATTEMPTS in WowManagementTasks.cs, which never gets checked while that
        // loop keeps "succeeding"). Clears the target so PathfindingLoopTask picks a fresh one, and
        // stamps LastLineOfSightBailoutTime so that pathfinding loop doesn't just immediately
        // re-press find-target and pick the same unreachable mob right back up -- see
        // LINE_OF_SIGHT_RETARGET_SUPPRESS_MILLIS.
        public bool AbandonUnreachableEngageTarget()
        {
            if (!WorldState.NotInLineOfSight)
            {
                return false;
            }

            Console.WriteLine("Target not in line of sight while trying to engage -- clearing target and suppressing retarget for a bit");
            Keyboard.KeyPress(WowInput.CLEAR_TARGET_MACRO);
            LastLineOfSightBailoutTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            return true;
        }

        public async Task<bool> StartAttackTask()
        {
            await Task.Delay(0);

            // always kick things off with /startattack
            Keyboard.KeyPress(WowInput.START_ATTACK);

            return true;
        }
    }
}