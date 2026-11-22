using System;
using System.Threading.Tasks;

namespace WoWHelper
{
    // Stub -- no rotation or engage yet.
    public class WowDruidHandler : WowClassHandler<WowDruidClassState>
    {
        public WowDruidHandler(WowPlayer player) : base(player) { }

        public override Task<bool> ExecuteEngageTask()
        {
            throw new NotImplementedException($"{nameof(WowDruidHandler)}.{nameof(ExecuteEngageTask)}");
        }

        public override Task SingleCombatTask()
        {
            throw new NotImplementedException($"{nameof(WowDruidHandler)}.{nameof(SingleCombatTask)}");
        }

        public override bool CanEngageTarget()
        {
            return ClassState.CanSpellcastPullTarget;
        }
    }
}
