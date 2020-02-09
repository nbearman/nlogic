using System;
using System.Collections.Generic;

namespace nlogic_sim
{
    public class MemoryManagementUnit : MMIO, HardwareInterrupter
    {
        /// <summary>
        /// The page table 
        /// </summary>
        private Dictionary<uint, uint> active_page_table;
        
        public MemoryManagementUnit()
        {
        }

        //simulation environment interface methods
        public uint translate_address(uint address, out bool success)
        {
            //if fault
            //  raise interrupt
            //  abort translation

            throw new NotImplementedException();
            return 0;
        }

        //hardware interrupter device interface methods
        void HardwareInterrupter.register_signal_callback(Action signal_callback)
        {
            throw new NotImplementedException();
        }
        
        
        
        //MMIO device interface methods

        uint MMIO.get_size()
        {
            throw new NotImplementedException();
            return 0;
        }

        void MMIO.write_memory(uint address, byte[] data)
        {
            throw new NotImplementedException();
        }

        byte[] MMIO.read_memory(uint address, uint length)
        {
            throw new NotImplementedException();
            return null;
        }


    }
}