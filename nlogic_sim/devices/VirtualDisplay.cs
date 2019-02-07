using System;
using System.Threading;

namespace nlogic_sim
{
    public class VirtualDisplay : MMIO
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

        //holds data to be displayed on the display
        public byte[] display_buffer;

        private uint base_address;

        public VirtualDisplay()
        {
            display_buffer = new byte[2000];
        }


        public void power_on(byte[] memory, uint start_address)
        {
            ThreadStart s = new ThreadStart(display_thread);
            Thread p = new Thread(cycle);
        }

        private void display_thread()
        {
            while (true)
            {
                Thread.Sleep(100);

            }
        }

        private void cycle()
        {
        }

        uint MMIO.get_size()
        {
            return (uint)display_buffer.Length;
        }

        void MMIO.set_base_address(uint address)
        {
            this.base_address = address;
        }

        void MMIO.write_memory(uint address, byte[] data)
        {
            throw new NotImplementedException();
        }

        byte[] MMIO.read_memory(uint address, uint length)
        {
            throw new NotImplementedException();
        }
    }
}