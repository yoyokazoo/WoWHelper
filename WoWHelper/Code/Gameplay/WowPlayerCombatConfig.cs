using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using WoWHelper.Code.WorldState;

namespace WoWHelper
{
    public partial class WowPlayer
    {
        public async Task<bool> StartBattleReadyTask()
        {
            switch (FarmingConfig.CombatConfiguration)
            {
                case Code.Gameplay.WowCombatConfiguration.Warrior: return await WarriorStartBattleReadyRecoverTask();
                case Code.Gameplay.WowCombatConfiguration.Mage: return await WarriorStartBattleReadyRecoverTask();
                default: throw new System.NotImplementedException();
            }
        }

        public async Task<bool> WaitUntilBattleReadyTask()
        {
            switch (FarmingConfig.CombatConfiguration)
            {
                case Code.Gameplay.WowCombatConfiguration.Warrior: return await WarriorWaitUntilBattleReadyTask();
                case Code.Gameplay.WowCombatConfiguration.Mage: return await WarriorWaitUntilBattleReadyTask();
                default: throw new System.NotImplementedException();
            }
        }

        public async Task<bool> StartEngageTask()
        {
            switch (FarmingConfig.CombatConfiguration)
            {
                case Code.Gameplay.WowCombatConfiguration.Warrior: return await WarriorKickOffEngageTask();
                case Code.Gameplay.WowCombatConfiguration.Mage: return await WarriorKickOffEngageTask();
                default: throw new System.NotImplementedException();
            }
        }

        public async Task<bool> WaitUntilEngageTask()
        {
            switch (FarmingConfig.CombatConfiguration)
            {
                case Code.Gameplay.WowCombatConfiguration.Warrior: return await WarriorFaceCorrectDirectionToEngageTask();
                case Code.Gameplay.WowCombatConfiguration.Mage: return await WarriorFaceCorrectDirectionToEngageTask();
                default: throw new System.NotImplementedException();
            }
        }

        public async Task<bool> CombatLoopTask()
        {
            switch (FarmingConfig.CombatConfiguration)
            {
                case Code.Gameplay.WowCombatConfiguration.Warrior: return await WarriorCombatLoopTask();
                case Code.Gameplay.WowCombatConfiguration.Mage: return await WarriorCombatLoopTask();
                default: throw new System.NotImplementedException();
            }
        }

        public bool CanEngageTarget()
        {
            switch (FarmingConfig.EngageMethod)
            {
                case WowLocationConfiguration.EngagementMethod.Charge: return WorldState.CanChargeTarget;
                case WowLocationConfiguration.EngagementMethod.Shoot: return WorldState.CanShootTarget;
                case WowLocationConfiguration.EngagementMethod.Frostbolt: return WorldState.CanShootTarget;
                default: throw new System.NotImplementedException();
            }
        }
    }
}