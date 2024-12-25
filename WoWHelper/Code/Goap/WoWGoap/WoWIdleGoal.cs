using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WoWHelper.Code.Goap.WoWGoap
{
    public class WoWIdleGoal : GoapGoal
    {
        public WoWIdleGoal() : base(0) { }

        public override bool IsValid()
        {
            return true;
        }
    }
}
