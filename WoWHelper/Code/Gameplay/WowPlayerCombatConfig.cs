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

        public Task<bool> WaitUntilBattleReadyTask() =>
            RequireClassHandler(nameof(WaitUntilBattleReadyTask)).WaitUntilBattleReadyTask();

        public bool PlayerIsBattleReady() =>
            RequireClassHandler(nameof(PlayerIsBattleReady)).IsBattleReady();

        public Task<bool> StartEngageTask()
        {
            var handler = RequireClassHandler(nameof(StartEngageTask));
            EngageAttempts = 1;
            return handler.KickOffEngageTask();
        }

        public Task<bool> ExecuteEngageTask()
        {
            var handler = RequireClassHandler(nameof(ExecuteEngageTask));
            EngageAttempts++;
            return handler.ExecuteEngageTask();
        }

        public Task<bool> CombatLoopTask() =>
            RequireClassHandler(nameof(CombatLoopTask)).CombatLoopTask();

        public Task PlayerSingleCombatTask() =>
            RequireClassHandler(nameof(PlayerSingleCombatTask)).SingleCombatTask();

        // For class-agnostic callers, like WowMovementTasks.cs's PathfindingLoopTask,
        // which runs identically for every class.
        public bool CanEngageTarget() =>
            RequireClassHandler(nameof(CanEngageTarget)).CanEngageTarget();
    }
}
