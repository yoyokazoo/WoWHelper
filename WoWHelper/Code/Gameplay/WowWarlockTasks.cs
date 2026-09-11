using System.Threading.Tasks;

namespace WoWHelper
{
    public partial class WowPlayer
    {
        // Warlock rotation -- not implemented yet. Mirrors the six dispatch
        // entry points WowWarriorTasks/WowMageTasks/WowShamanTasks each
        // provide (see WowPlayerCombatConfig.cs's six switches), stubbed out
        // so WowCombatConfiguration.Warlock dispatches cleanly to here
        // instead of hitting the "no dispatch implemented" NotImplementedException
        // in WowPlayerCombatConfig.cs. Each throws its own NotImplementedException
        // for now -- fill these in the same way WowShamanTasks.cs does for
        // Shaman (that file is the most recently-added class and the closest
        // model to follow).
        public async Task<bool> WarlockStartBattleReadyRecoverTask(WowWarlockClassState classState)
        {
            await Task.Delay(0);
            throw new System.NotImplementedException($"{nameof(WarlockStartBattleReadyRecoverTask)} not implemented yet.");
        }

        public async Task<bool> WarlockWaitUntilBattleReadyTask(WowWarlockClassState classState)
        {
            await Task.Delay(0);
            throw new System.NotImplementedException($"{nameof(WarlockWaitUntilBattleReadyTask)} not implemented yet.");
        }

        public async Task<bool> WarlockKickOffEngageTask(WowWarlockClassState classState)
        {
            await Task.Delay(0);
            throw new System.NotImplementedException($"{nameof(WarlockKickOffEngageTask)} not implemented yet.");
        }

        public async Task<bool> WarlockFaceCorrectDirectionToEngageTask(WowWarlockClassState classState)
        {
            await Task.Delay(0);
            throw new System.NotImplementedException($"{nameof(WarlockFaceCorrectDirectionToEngageTask)} not implemented yet.");
        }

        public async Task<bool> WarlockCombatLoopTask(WowWarlockClassState classState)
        {
            await Task.Delay(0);
            throw new System.NotImplementedException($"{nameof(WarlockCombatLoopTask)} not implemented yet.");
        }

        // Real per-class logic goes directly here (not a dispatch) -- see the
        // comment above WowPlayerCombatConfig.CanEngageTarget for why each
        // class gets its own thin CanEngageTarget wrapper instead of sharing
        // one method.
        public bool WarlockCanEngageTarget(WowWarlockClassState classState)
        {
            throw new System.NotImplementedException($"{nameof(WarlockCanEngageTarget)} not implemented yet.");
        }
    }
}
