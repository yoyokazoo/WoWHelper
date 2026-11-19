using System.Threading.Tasks;

namespace WoWHelper
{
    public partial class WowPlayer
    {
        // Thin forwarders into ClassHandler (see WowClassHandler.cs), the one object
        // holding everything class-specific. ClassHandler is built once the player's class
        // is detected (see UpdateClassState); calling any of these before then means
        // CombatConfiguration is still Unknown, which throws rather than guessing.
        private WowClassHandler RequireClassHandler(string callerName)
        {
            if (ClassHandler == null)
            {
                throw new System.NotImplementedException(
                    $"{callerName}: no class handler for CombatConfiguration \"{CombatConfiguration}\" (class not detected yet?).");
            }

            return ClassHandler;
        }

        public Task<bool> StartBattleReadyTask() =>
            RequireClassHandler(nameof(StartBattleReadyTask)).StartBattleReadyRecoverTask();

        public Task<bool> PostBattleRecoveryTask() =>
            RequireClassHandler(nameof(PostBattleRecoveryTask)).PostBattleRecoveryTask();

        public Task<bool> PreBattleBuffingTask() =>
            RequireClassHandler(nameof(PreBattleBuffingTask)).PreBattleBuffingTask();

        public bool PlayerIsBattleReady() =>
            RequireClassHandler(nameof(PlayerIsBattleReady)).IsBattleReady();

        public async Task<bool> StartEngageTask()
        {
            EngageAttempts = 1;
            await EndWalkForwardTask();
            await TurnToFaceTargetMarkerTask();
            return true;
        }

        public Task<bool> ExecuteEngageTask()
        {
            var handler = RequireClassHandler(nameof(ExecuteEngageTask));
            EngageAttempts++;
            return handler.ExecuteEngageTask();
        }

        public Task PlayerSingleCombatTask() =>
            RequireClassHandler(nameof(PlayerSingleCombatTask)).SingleCombatTask();

        // For class-agnostic callers, like WowMovementTasks.cs's PathfindingLoopTask,
        // which runs identically for every class.
        public bool CanEngageTarget() =>
            RequireClassHandler(nameof(CanEngageTarget)).CanEngageTarget();
    }
}
