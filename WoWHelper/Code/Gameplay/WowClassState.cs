using System.Drawing;

namespace WoWHelper
{
    // Holds class-specific state so we don't need to look up and pass
    // class state we don't need
    public abstract class WowClassState
    {
        public bool Initialized { get; protected set; }

        public abstract void UpdateFromBitmap(Bitmap bmp);
    }
}
