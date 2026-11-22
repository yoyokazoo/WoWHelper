using System;
using System.Threading.Tasks;

namespace WoWHelper
{
    // Stub -- no rotation or engage yet.
    public class WowRogueHandler : WowClassHandler<WowRogueClassState>
    {
        public WowRogueHandler(WowPlayer player) : base(player) { }

        public override bool ResourceIsMana => false;

        public override Task<bool> ExecuteEngageTask()
        {
            throw new NotImplementedException($"{nameof(WowRogueHandler)}.{nameof(ExecuteEngageTask)}");
        }

        public override Task SingleCombatTask()
        {
            throw new NotImplementedException($"{nameof(WowRogueHandler)}.{nameof(SingleCombatTask)}");
        }

        public override bool CanEngageTarget()
        {
            return false;
        }
    }
}
