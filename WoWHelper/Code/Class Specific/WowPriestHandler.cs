using System;
using System.Threading.Tasks;

namespace WoWHelper
{
    // Stub -- no rotation or engage yet.
    public class WowPriestHandler : WowClassHandler<WowPriestClassState>
    {
        public WowPriestHandler(WowPlayer player) : base(player) { }

        public override Task<bool> ExecuteEngageTask()
        {
            throw new NotImplementedException($"{nameof(WowPriestHandler)}.{nameof(ExecuteEngageTask)}");
        }

        public override Task SingleCombatTask()
        {
            throw new NotImplementedException($"{nameof(WowPriestHandler)}.{nameof(SingleCombatTask)}");
        }

        public override bool CanEngageTarget()
        {
            return ClassState.CanSpellcastPullTarget;
        }
    }
}
