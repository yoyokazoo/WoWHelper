using System;
using System.Threading.Tasks;

namespace WoWHelper
{
    // Stub -- no rotation or engage yet.
    public class WowPaladinHandler : WowClassHandler<WowPaladinClassState>
    {
        public WowPaladinHandler(WowPlayer player) : base(player) { }

        public override Task<bool> ExecuteEngageTask()
        {
            throw new NotImplementedException($"{nameof(WowPaladinHandler)}.{nameof(ExecuteEngageTask)}");
        }

        public override Task SingleCombatTask()
        {
            throw new NotImplementedException($"{nameof(WowPaladinHandler)}.{nameof(SingleCombatTask)}");
        }

        public override bool CanEngageTarget()
        {
            return false;
        }
    }
}
