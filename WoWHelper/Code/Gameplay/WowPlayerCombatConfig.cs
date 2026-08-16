using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using WoWHelper.Code.WorldState;

namespace WoWHelper
{
    public partial class WowPlayer
    {
        // Each dispatch below casts ClassState to the class-specific type the
        // target method expects. Since ClassState is built once from
        // FarmingConfig.CombatConfiguration (see WowPlayer's constructor) and
        // never changes afterward, this cast should always succeed in
        // practice -- if it doesn't, that means ClassState and
        // CombatConfiguration have gone out of sync, and throwing
        // immediately here is the right outcome (silently reading the wrong
        // class's state would be worse).
        public async Task<bool> StartBattleReadyTask()
        {
            switch (FarmingConfig.CombatConfiguration)
            {
                case Code.Gameplay.WowCombatConfiguration.Warrior: return await WarriorStartBattleReadyRecoverTask((WowWarriorClassState)ClassState);
                case Code.Gameplay.WowCombatConfiguration.Mage: return await MageStartBattleReadyRecoverTask((WowMageClassState)ClassState);
                case Code.Gameplay.WowCombatConfiguration.Shaman: return await ShamanStartBattleReadyRecoverTask((WowShamanClassState)ClassState);
                default: throw new System.NotImplementedException();
            }
        }

        public async Task<bool> WaitUntilBattleReadyTask()
        {
            switch (FarmingConfig.CombatConfiguration)
            {
                case Code.Gameplay.WowCombatConfiguration.Warrior: return await WarriorWaitUntilBattleReadyTask((WowWarriorClassState)ClassState);
                case Code.Gameplay.WowCombatConfiguration.Mage: return await MageWaitUntilBattleReadyTask((WowMageClassState)ClassState);
                case Code.Gameplay.WowCombatConfiguration.Shaman: return await ShamanWaitUntilBattleReadyTask((WowShamanClassState)ClassState);
                default: throw new System.NotImplementedException();
            }
        }

        public async Task<bool> StartEngageTask()
        {
            switch (FarmingConfig.CombatConfiguration)
            {
                case Code.Gameplay.WowCombatConfiguration.Warrior: return await WarriorKickOffEngageTask((WowWarriorClassState)ClassState);
                case Code.Gameplay.WowCombatConfiguration.Mage: return await MageKickOffEngageTask((WowMageClassState)ClassState);
                case Code.Gameplay.WowCombatConfiguration.Shaman: return await ShamanKickOffEngageTask((WowShamanClassState)ClassState);
                default: throw new System.NotImplementedException();
            }
        }

        public async Task<bool> WaitUntilEngageTask()
        {
            switch (FarmingConfig.CombatConfiguration)
            {
                case Code.Gameplay.WowCombatConfiguration.Warrior: return await WarriorFaceCorrectDirectionToEngageTask((WowWarriorClassState)ClassState);
                case Code.Gameplay.WowCombatConfiguration.Mage: return await MageFaceCorrectDirectionToEngageTask((WowMageClassState)ClassState);
                case Code.Gameplay.WowCombatConfiguration.Shaman: return await ShamanFaceCorrectDirectionToEngageTask((WowShamanClassState)ClassState);
                default: throw new System.NotImplementedException();
            }
        }

        public async Task<bool> CombatLoopTask()
        {
            switch (FarmingConfig.CombatConfiguration)
            {
                case Code.Gameplay.WowCombatConfiguration.Warrior: return await WarriorCombatLoopTask((WowWarriorClassState)ClassState);
                case Code.Gameplay.WowCombatConfiguration.Mage: return await MageCombatLoopTask((WowMageClassState)ClassState);
                case Code.Gameplay.WowCombatConfiguration.Shaman: return await ShamanCombatLoopTask((WowShamanClassState)ClassState);
                default: throw new System.NotImplementedException();
            }
        }

        // The real per-class logic lives in WarriorCanEngageTarget/MageCanEngageTarget/
        // ShamanCanEngageTarget (each in its own Wow*Tasks.cs, called there with
        // that class's own typed ClassState directly -- no dispatch needed since
        // the caller already knows its class). This thin wrapper exists only for
        // genuinely class-agnostic callers, like WowMovementTasks.cs's
        // PathfindingLoopTask, which runs identically for every class and can't
        // know which typed ClassState to use at compile time.
        public bool CanEngageTarget()
        {
            switch (FarmingConfig.CombatConfiguration)
            {
                case Code.Gameplay.WowCombatConfiguration.Warrior: return WarriorCanEngageTarget((WowWarriorClassState)ClassState);
                case Code.Gameplay.WowCombatConfiguration.Mage: return MageCanEngageTarget((WowMageClassState)ClassState);
                case Code.Gameplay.WowCombatConfiguration.Shaman: return ShamanCanEngageTarget((WowShamanClassState)ClassState);
                default: throw new System.NotImplementedException();
            }
        }
    }
}