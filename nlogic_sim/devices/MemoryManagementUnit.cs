using System;
using System.Collections.Generic;

namespace nlogic_sim
{
    public class MemoryManagementUnit : MMIO, HardwareInterrupter
    {
        /// <summary>
        /// The page table 
        /// </summary>
        //TODO the page table will live in memory, but the MMU needs to store the address of the active page table
        //probably change this to a uint address in memory
        private Dictionary<uint, uint> active_page_table;

        //TODO implement memory breakpoint
        //when the address with the breakpoint is accessed, send an interrupt

        //TODO the MMU needs to receive a signal when the processor cycles in order
        //to know when to clear it's faulted state
        private bool faulted = false;

        
        public MemoryManagementUnit()
        {
        }

        //simulation environment interface methods

        /// <summary>
        /// Returns true if the translation was successful
        /// Fills transation with the translated address
        /// </summary>
        /// <param name="address"></param>
        /// <param name="translation"></param>
        /// <returns></returns>
        public bool translate_address(uint address, out uint translation)
        {
            //if fault
            //  raise interrupt
            //  abort translation

            translation = address;
            return true;

            throw new NotImplementedException();
            return false;
        }

        /// <summary>
        /// Clear the fault status of the MMU
        /// </summary>
        public void clear_fault()
        {
            this.faulted = false;
        }

        //hardware interrupter device interface methods
        void HardwareInterrupter.register_signal_callback(Action<bool, bool> signal_callback)
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