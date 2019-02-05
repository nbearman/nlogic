using System;
using System.Threading;

namespace nlogic_sim
{
    public class VirtualDisplay
    {
        /// <summary>
        /// The virtual display uses the system memory to communicate with the processor;
        /// it reserves a block of memory to which the processor should write to send data
        /// to the display. Specific addresses are also used to send signals to the display
        /// to clear the display, refresh the display, and perform other various actions.
        /// The virtual display runs its own internal clock, which is cycled by calling cycle().
        /// The signal bytes are checked when cycling, and this is when the display takes actions.
        /// </summary>

        public char[][] display_data;
        public byte[] display_buffer;

        public static void power_on(byte[] memory, uint start_address)
        {
            ThreadStart s = new ThreadStart(display_thread);
            Thread p = new Thread(cycle);
        }

        static void display_thread()
        {
            while (true)
            {
                Thread.Sleep(100);

            }
        }

        static void cycle()
        {
        }
    }
}