using System;
using System.Collections.Generic;

namespace nlogic_sim
{
    public class MemoryManagementUnit : MMIO, HardwareInterrupter
    {
        /// <summary>
        /// The base address in physical memory of the active process's page directory
        /// </summary>
        private uint active_page_directory_base_address;

        /// <summary>
        /// The base address in physical memory of the page directory to switch to when the
        /// MMU breakpoint is next hit
        /// </summary>
        private uint queued_page_directory_base_address;

        /// <summary>
        /// A virtual address which, when the MMU is next instructed to access, will exit kernel
        /// mode and swap to the queued page directory
        /// </summary>
        private uint mmu_breakpoint;

        /// <summary>
        /// The page table entry or page directory entry which cause the MMU to fault due to the state
        /// of its protection bits. Stored for reference by the page fault or syscall handlers
        /// </summary>
        private uint faulted_pte;

        /// <summary>
        /// The MMU receives a signal when the processor cycles in order to know
        /// when to clear this faulted state. Currently occurs in: SimulationEnvironment.run(),
        /// immediately before cycling
        /// </summary>
        private bool faulted = false;

        /// <summary>
        /// When in kernel mode, the MMU will not raise interrupts
        /// </summary>
        private bool kernel_mode = false;

        /// <summary>
        /// Physical memory of this MMU's environment (RAM)
        /// </summary>
        private byte[] environment_memory;

        /// <summary>
        /// Callback supplied by the environment to send an interrupt to the processor.
        /// Parameters:
        ///     (bool): is this a retry interrrupt
        ///     (bool): is this a kernel-only interrupt
        /// </summary>
        private Action<bool, bool> raise_interrupt;
        
        public MemoryManagementUnit(byte[] memory)
        {
            environment_memory = memory;
        }

        //simulation environment interface methods

        /// <summary>
        /// Translates a virtual address into a physical address.
        /// Raises and interrupt and returns false if translation
        /// resulted in a fault.
        /// Returns true if the translation was successful.
        /// Fills translation with the translated address.
        /// </summary>
        /// <param name="address">Virtual address to translate into a physical address</param>
        /// <param name="write">True if the planned memory operation is a write, false if it is a read</param>
        /// <param name="translation">Holds translated physical address if the translation is successful,
        /// 0 otherwise</param>
        /// <returns></returns>
        public bool translate_address(uint address, bool write, out uint translation)
        {
            translation = 0;

            //do not translate any addresses while faulted
            //no reads or writes should occur until the processor cycles and enters the trap
            if (this.faulted)
            {
                return false;
            }


            //get page directory entry from active page directory
            uint page_directory_entry_address = get_page_directory_entry_address(this.active_page_directory_base_address, address);
            uint page_directory_entry_data = read_environment_memory_byte(page_directory_entry_address);
            PageTableEntry PDE = new PageTableEntry(page_directory_entry_data);

            //check that the page table is safe to read
            ProtectionCheckResult protection = check_protection(PDE, false);
            if (protection != ProtectionCheckResult.SAFE)
            {
                //raise a kernel-handled interrupt and abort translation
                //set the RETRY flag if the protection requires a retry interrupt
                this.raise_interrupt(protection == ProtectionCheckResult.RETRY_INTERRUPT, true);
                return false;
            }

            //set the refernced bit on the PDE for this page table, since we just accessed it
            //TODO set the referenced bit

            //get page table entry from page table
            uint page_table_entry_address = get_page_table_entry_address(PDE.number, address);
            uint page_table_entry_data = read_environment_memory_byte(page_table_entry_address);
            PageTableEntry PTE = new PageTableEntry(page_table_entry_data);

            //check that the physical page allows the planned memory operation
            protection = check_protection(PTE, false);
            if (protection != ProtectionCheckResult.SAFE)
            {
                //raise a kernel-handled interrupt and abort translation
                //set the RETRY flag if the protection requires a retry interrupt
                this.raise_interrupt(protection == ProtectionCheckResult.RETRY_INTERRUPT, true);
                return false;
            }

            //set the refernced bit on the PTE for this page, since we're about to access it
            //TODO set the referenced bit

            //the page table is safe to access
            //get the final physical address
            translation = get_physical_address(PTE.number, address);
            return true;
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
            this.raise_interrupt = signal_callback;
        }
        
        
        
        //MMIO device interface methods

        uint MMIO.get_size()
        {
            //16 bytes for 4 uint registers:
            //  active page directory base address
            //  queued page directory base address
            //  virtual address mmu breakpoint
            //  faulted PTE
            return 16;
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

        /// <summary>
        /// Check if the target page associated with a page table (or page directory)
        /// entry is protected and should therefore fault during the given operation (reading or writing).
        /// </summary>
        /// <param name="pte">Page table entry to check the protection bits on</param>
        /// <param name="write">True if the memory operation to be performed on the target page is a write,
        /// false if the operation is a read.
        /// </param>
        /// <returns>The type of interrupt the MMU should raise if the page is protected, or a safe status if
        /// the planned operation is permitted.</returns>
        private ProtectionCheckResult check_protection(PageTableEntry pte, bool write)
        {
            if (pte.readable)
            {
                //page is safe to read
                //page fault only the page is not writable and we are writing
                if (!pte.writable && write)
                {
                    //readable but not writable
                    //either a shared physical page or a clean page
                    //page needs to be split or marked as dirty
                    return ProtectionCheckResult.RETRY_INTERRUPT;
                }

                //readable and not writing or
                //readable, writable, and writing
                return ProtectionCheckResult.SAFE;
            }

            //if a page is not "readable," both reads and writes are illegal
            //the writable bit now indicates if the page is paged out or not allocated
            else
            {
                if (pte.writable)
                {
                    //not readable and "writable" indicates the page is mapped but paged out
                    //page is evicted, page fault
                    return ProtectionCheckResult.RETRY_INTERRUPT;
                }
                else
                {
                    //not readable, not writable indicates the page is not mapped at all
                    //the handler will also decide if this was intended to be a system call
                    //access violation, non-retry kernel table interrupt
                    return ProtectionCheckResult.NON_RETRY_INTERRUPT;
                }
            }
        }

        private uint read_environment_memory_byte(uint address)
        {
            byte[] result = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                result[i] = this.environment_memory[address + i];
            }
            return Utility.uint32_from_byte_array(result);
        }

        private static uint get_page_directory_entry_address(uint page_directory_base_address, uint virtual_address)
        {
            //virtual address mask:
            //[directory #] [page #]      [offset]
            //[1111 1111 11][00 0000 0000][0000 0000 0000]
            uint directory_number = (virtual_address & 0xFFC00000) >> 22;

            //page directory entry address:
            //[directory base address]  [directory #] [00]
            //[0000 0000 0000 0000 0000][00 0000 0000][00]
            return page_directory_base_address | (directory_number << 2);
        }

        private static uint get_page_table_entry_address(uint page_table_physical_page_number, uint virtual_address)
        {
            //virtual address mask:
            //[directory #] [page #]      [offset]
            //[0000 0000 00][11 1111 1111][0000 0000 0000]
            uint page_number = (virtual_address & 0x003FF000) >> 12;

            //page table entry address:
            //[table phsyical page #]   [page #]      [00]
            //[0000 0000 0000 0000 0000][00 0000 0000][00]
            return (page_table_physical_page_number << 12) | (page_number << 2);
        }

        private static uint get_physical_address(uint physical_page_number, uint virtual_address)
        {
            //virtual address mask:
            //[directory #] [page #]      [offset]
            //[0000 0000 00][00 0000 0000][1111 1111 1111]
            uint offset_into_page = virtual_address & 0x00000FFF;

            //physical address:
            //[physical page #]         [offset]
            //[0000 0000 0000 0000 0000][0000 0000 0000]
            return (physical_page_number << 12) | offset_into_page;
        }

        private struct PageTableEntry
        {
            //PTE protection bits
            public bool readable;
            public bool writable;
            public bool referenced;
            public bool dirty;

            //number represented by the lower 20 bits of the PTE
            //either a directory number or a physical page number
            public uint number;

            //original uint used to create this PTE
            public uint full_pte_data;

            public PageTableEntry(uint data)
            {
                this.readable = Utility.get_bit(data, (uint)PageTableEntryProtectionBits.READABLE);
                this.writable = Utility.get_bit(data, (uint)PageTableEntryProtectionBits.WRITABLE);
                this.referenced = Utility.get_bit(data, (uint)PageTableEntryProtectionBits.REFERENCED);
                this.dirty = Utility.get_bit(data, (uint)PageTableEntryProtectionBits.DIRTY);

                //the number (page table number or physical page number) is held in the bottom 20 bits
                this.number = data & 0x000FFFFF;

                //store the original PTE as it was read from; this will allow the MMU to 
                //fill a register with the faulty PTE for the processor to retrieve later
                this.full_pte_data = data;
            }
        }

        private enum PageTableEntryProtectionBits
        {
            READABLE = 0,
            WRITABLE = 1,
            REFERENCED = 2,
            DIRTY = 3,
        }

        private enum ProtectionCheckResult
        {
            SAFE = 0,
            RETRY_INTERRUPT = 1,
            NON_RETRY_INTERRUPT = 2,
        }
    }
}