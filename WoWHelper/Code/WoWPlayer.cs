using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WoWHelper
{
    public class WoWPlayer
    {
        public WoWWorldState WorldState { get; private set; }

        public WoWPlayer()
        {
            WorldState = new WoWWorldState();
        }

        public void UpdateFromBitmap(Bitmap bmp)
        {
            WorldState.UpdateFromBitmap(bmp);
        }
    }
}
