using System;
using System.Runtime.InteropServices;
using System.Threading;

public static class KeyPoller
{
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private const int VK_ESCAPE = 0x1B;

    private static volatile bool _running = false;
    public static event Action EscPressed;

    public static void Start()
    {
        if (_running) return;
        _running = true;

        Thread t = new Thread(() =>
        {
            bool lastDown = false;

            while (_running)
            {
                // Highest bit = key currently down
                bool down = (GetAsyncKeyState(VK_ESCAPE) & 0x8000) != 0;

                // Fire event on key-down transition
                if (down && !lastDown)
                {
                    EscPressed?.Invoke();
                }

                lastDown = down;

                Thread.Sleep(100); // polling interval
            }
        });

        t.IsBackground = true;
        t.Start();
    }

    public static void Stop()
    {
        _running = false;
    }
}
