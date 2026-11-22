using System;
using System.Threading.Tasks;

namespace WoWHelper
{
    // Stub -- no rotation or engage yet.
    public class WowMageHandler : WowClassHandler<WowMageClassState>
    {
        public WowMageHandler(WowPlayer player) : base(player) { }

        public override Task<bool> ExecuteEngageTask()
        {
            throw new NotImplementedException($"{nameof(WowMageHandler)}.{nameof(ExecuteEngageTask)}");
        }

        public override Task SingleCombatTask()
        {
            throw new NotImplementedException($"{nameof(WowMageHandler)}.{nameof(SingleCombatTask)}");
        }

        public override bool CanEngageTarget()
        {
            return ClassState.CanSpellcastPullTarget;
        }
    }
}
