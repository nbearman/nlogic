CONST physical_memory_pages 10
CONST mmio_disk_base_address 1024

CONST gpa_dump 00
CONST gpb_dump 04

//=========================================================================
// kernel entry point
//=========================================================================

//========
// DMEM region (0x00 - 0x3F)
//========
// IMPORTANT: instructions here will be overwritten when DMEM is written to (like dumping registers in interrupt handler)

// PC is at 0x08 when MMU is enabled, so these won't be executed
00 00
00 00
00 00

//kernel entry point; MMU enabled, we're in VA now

//set the MMU VA break point
IADF WBASE
SKIP PC
00 00 10 00 //MMIO base address (in VA)
08 WOFST //breakpoint register
00 WMEM //breakpoint at 0

//enable the breakpoint
14 WOFST //breakpoint enabled register
01 WMEM //non-zero -> enabled

00 RBASE
00 ROFST

//jump to 0 in user space
00 PC
// end kernel entry code

//TODO unused space 0x40 - 0x100

FILL100 //data should start after DMEM accessible region (0x00 - 0x3F) so it can't be overwritten by DMEM instructions
//=========================================================================
// kernel data structures
//=========================================================================
@@physical_page_map
//physical page map

//TODO this struct can be made smaller by packing some variables into a single uint
//  process id, phsyical page, and number of references will never be close to 4 bytes in size
//=========================
// physical page mapping entry
//==========
// 0x00 |   uint    |   process id
// 0x04 |   uint    |   directory physical page
// 0x08 |   uint    |   virtual page / table number
// 0x0C |   uint    |   number of references
// 0x10 |   uint    |   disk block number
// 0x14 |   uint    |   LRU referenced?
// 0x18 |   uint    |   dirty
//=========================
CONST ppage_map_entry_size 1C

//boot sequence (no owner)
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00

//kernel page directory
00 00 00 01 //kernel process ID == 1
00 00 00 01 //owning page directory is this directory, kernel's page directory
00 00 00 00 //this is a directory
00 00 00 01 //kernel process references this physical page
00 00 00 00 //no disk block number, can never be evicted
00 00 00 00
00 00 00 00

//kernel page table 0
00 00 00 01 //kernel process ID == 1
00 00 00 01 //owning page directory is kernel directory
00 00 00 00 //virtual table 0
00 00 00 01 //kernel process references this physical page
00 00 00 00 //no disk block number, can never be evicted
00 00 00 00
00 00 00 00

//kernel virtual page 0
00 00 00 01 //kernel process ID == 1
00 00 00 01 //owning page directory is kernel directory
00 00 00 00 //virtual page 0
00 00 00 01 //kernel process references this physical page
00 00 00 00 //no disk block number, can never be evicted
00 00 00 00
00 00 00 00

//empty (no owner)
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00

//user page directory
00 00 00 02 //user process ID == 2
00 00 00 05 //owning page directory is this directory, user's page directory
00 00 00 00 //this is a directory
00 00 00 01 //user process references this physical page
00 00 00 00 //no disk block number yet
00 00 00 00
00 00 00 00

//user page table 0
00 00 00 02 //user process ID == 2
00 00 00 05 //owning page directory is user page directory
00 00 00 00 //virtual table 0
00 00 00 01 //user process references this physical page
00 00 00 00 //no disk block number yet
00 00 00 00
00 00 00 00

//user virtual page 0
00 00 00 02 //user process ID == 2
00 00 00 05 //owning page directory is user page directory
00 00 00 00 //virtual page 0
00 00 00 01 //user process references this physical page
00 00 00 64 //loaded from disk block 100
00 00 00 00
00 00 00 00



//empty (no owner)
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
//empty (no owner)
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
//empty (no owner)
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
//empty (no owner)
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
//empty (no owner)
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
//empty (no owner)
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
//empty (no owner)
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
//empty (no owner)
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00
00 00 00 00

//end physical page map (32 page mappings)(TODO only 16 shown; space reserved for the rest with FILL)
FILL480

@@process_map
//process map

//=========================
// process map entry
//==========
// 0x00 |   uint    |   process id
// 0x04 |   uint    |   number of mapped virtual pages
// 0x08 |   uint    |   number of pages resident in memory
// 0x0C |   uint    |   disk block of process page directory
//=========================
CONST process_map_entry_size 10
CONST process_map_length 10

//kernel process descriptor
00 00 00 01 //kernel process ID == 1
00 00 00 03 //3 pages are mapped: 2 pages of memory and 1 mapped to the MMU
00 00 00 03 //page directory, page table, 1 page of memory
00 00 00 00 //no disk block number, kernel page directory can never be evicted

//user process descriptor
00 00 00 02 //user process ID == 2
00 00 00 03 //2 pages are mapped: 2 pages of memory
00 00 00 03 //page directory, page table, 1 page of memory
00 00 00 00 //TODO figure out if we're supposed to load process page directory from disk...
            //(it should probably be built dynamically from some kind of description file
            // that the kernel can read to determine how many pages of the program are
            // mapped out of the box [length of program data])

//end process map (16 process descriptors)
FILL580

//TODO unused space between 0x480 and 0x500
//  interrupt handler could be moved to 0x480


//=========================================================================
// interrupt handler
//=========================================================================
FILL600
//Fill to place the interrupt handler code at the correct location
    //TODO the address of the interrupt handler is currently hardcoded in the simulation environment
    //interrupt_handler_address in Processor class

//==============================
// DMEM region layout
// Data dumped automatically from last instruction cache
//=====================
// 0x00 |   uint    |   WBASE temporary dump
// 0x04 |   uint    |   WOFST temporary dump

// 0x08 - 0x24      |   (unused)

// 0x28 |   uint    |   FLAG
// 0x2C |   uint    |   GPA
// 0x30 |   uint    |   PC to return to
// 0x34 |   uint    |   EXE to return to
// 0x38 |   uint    |   destination register contents after last cycle (if RETRY)
// 0x3C |   uint    |   destination register location on stack (if RETRY)
//                          i.e., where to write contents (DMEM10) to
//==============================

WBASE DMEM00 //we have FLAG, safe to overwrite //TODO remove from last instruction cache?
WOFST DMEM04

//dump registers to kernel's stack
//TODO need to dump LINK as well (and update mapping in Processor3.cs)
IADF WBASE
SKIP PC
::KERNEL_STACK
00 WOFST
GPA WMEM
04 WOFST
GPB WMEM
08 WOFST
GPC WMEM
0C WOFST
GPD WMEM
10 WOFST
GPE WMEM
14 WOFST
GPF WMEM
18 WOFST
GPG WMEM
1C WOFST
GPH WMEM

20 WOFST
COMPA WMEM
24 WOFST
COMPB WMEM
28 WOFST
RBASE WMEM
2C WOFST
ROFST WMEM
30 WOFST
ALUM WMEM
34 WOFST
ALUA WMEM
38 WOFST
ALUB WMEM
3C WOFST
FPUM WMEM
40 WOFST
FPUA WMEM
44 WOFST
FPUB WMEM
48 WOFST
DMEM00 WMEM
4C WOFST
DMEM04 WMEM
50 WOFST

FRAME_START
//=========================
// interrupt handler stack layout / local variables
//==========

    STACK pc_val 04                 // PC contents
    STACK flag_val 04               // FLAG contents
    STACK faulted_pte 04            // faulted PTE
    STACK faulted_va 04             // faulted virtual address
    STACK faulted_operation 04      // faulted operation (0 if read, 1 if write)
    STACK faulted_pte_block 04      // disk block from faulted PTE
    STACK faulted_va_vpage_num 04   // virtual page number from faulted virtual address
    STACK faulted_va_table_num 04   // virtual table number from faulted virtual address
    STACK active_process_id 04      // process ID of the active process
    STACK active_process_page_directory_ppage 04    // physical page of the active process's page directory
    STACK active_process_map_entry_index 04         // index of active process in the process map
    STACK target_ppage 04           // physical page new page is moved into (TODO: unused)
    STACK fetched_pde 04            // possibly different than faulted PTE if the leaf page faulted
    STACK fetched_pte 04            // retrieved by following the PDE if leaf page faulted
                                    //   should be the same as faulted PTE in that case
    STACK fetched_pde_kpa 04        // VA in kernel space of fetched PDE's physical address
    STACK fetched_pte_kpa 04        // VA in kernel space of fetched PTE's physical address

    //lower 20 bits of fetched PDE and PTE; if the page it points
    //to is resident, it's a physical page number, else it's a disk
    //block number where the page was evicted to
    STACK fetched_pde_number 04
    STACK fetched_pte_number 04

    STACK updated_pde 04            // PDE data after updating tables
    STACK updated_pte 04            // PTE data after updating tables
    STACK virtual_table_ppage 04    // physical page number of the VA's page table
    STACK virtual_page_ppage 04     // physical page number of the VA's page

//=========================

//add stack frame; registers are dumped to bottom of the stack, so we don't need to save a frame pointer
//  (specifically because handler runs at the very bottom of the kernel's stack)
//(just set WBASE to 00 when it's time to retrieve them)
01 ALUM //add
WBASE ALUA
WOFST ALUB
ALUR WBASE //WBASE = FP == start of first frame on the stack
    //before this is the dumped contents of the processor before the interrupt
ISIZE_FRAME WOFST //WOFST = SP == frame size

//store FLAG in local variable
    WBASE RBASE
    ISTACK_flag_val ROFST
    FLAG RMEM
//store PC from last instruction cache in local variable
    ISTACK_pc_val ROFST
    DMEM30 RMEM

//if RETRY interrupt, we need to restore the destination's contents from the last instruction cache
//since a faulted read may have wrongfully clobbered that register
    //determine if this is a RETRY interrupt
        FLAG ALUA //FLAG to ALU
        IADF ALUB //RETRY bit mask to ALU
        SKIP PC
        10 00 00 00 //RETRY bit mask
        08 ALUM //AND mode
        ALUR ALUA //RETRY bit from FLAG (unshifted) to ALU
        1C ALUB //shift 28 bits right (RETRY bit to LSB position)
        06 ALUM //right shift mode
        ALUR COMPA //RETRY bit is 1? to comparator
        01 COMPB //0b1 to comparator
        COMPR PC
        :retry_interrupt
        :non_retry_interrupt

    @retry_interrupt
    //this is a RETRY interrupt, we need to restore the clobbered register
    //the last instruction cache contains the offset (from kernel stack base) where we stored the destination's contents
    //  (the location we need to repair depends on the instruction that faulted)

    //in some RETRY interrupts, we still don't need to restore the clobbered register
    //  (ex.: if a page fault occurred during instruction fetch, no register was written)
    //first, check for that case
        DMEM3C COMPA //dump location of clobbered register to comparator
        IADF COMPB
        SKIP PC
        FF FF FF FF //sentinel value set by trap to indicate we shouldn't restore
        //if the sentinel value is set, skip the restoring
        COMPR PC
        :retry_interrupt_skip_restore
        :retry_interrupt_do_restore

    @retry_interrupt_do_restore
    //we do need to restore the destination's old contents
    //overwrite our dump of that register with the last instruction cache's dump
        //TODO this needs to be tested manually
        IADF RBASE //base of kernel stack
        SKIP PC
        ::KERNEL_STACK
        DMEM3C ROFST //dump location of clobbered register is at DMEM3C
        DMEM38 RMEM //last instruction cache's dump of the register is DMEM38

@non_retry_interrupt
@retry_interrupt_skip_restore
//don't need to restore any register, continue executing

//determine the cause of the interrupt
    //mask flag to check if interrupt was raised on MMU channel
    FLAG ALUA
    IADF ALUB
    SKIP PC
    00 00 00 01 //mask for the first channel (MMU)
    AAND ALUM //AND mode
    ALUR COMPA
    00 COMPB //if flag channel is 1, jump to mmu interrupt handler
    COMPR PC
    :non_mmu_interrupt
    :mmu_interrupt

@non_mmu_interrupt
//if interrupt is not from MMU
    // determine if the interrupt is a system call by checking that all channels are 0
    FLAG ALUA
    IADF ALUB
    SKIP PC
    01 FF FF FF  //mask for all channels (first 7 bits are reserved for flags)
    AAND ALUM
    ALUR COMPA
    00 COMPB
    break
    COMPR PC
    :system_call
    :non_system_call
    @non_system_call
    7F FLAG // TODO not implemented

@mmu_interrupt
//else interrupt is from MMU
//retrieve the faulted PTE from the MMU
    IADF RBASE
    SKIP PC
    00 00 10 00 //MMU VA
    0C ROFST //0x0C == faulted PTE register offset in MMU address range
    RMEM GPA

//retrieve the faulted address from the MMU
    10 ROFST //0x10 == faulted VA register offset in MMU address range
    RMEM GPB

//retrieve the faulted operation from the MMU
    20 ROFST //0x20 == faulted operation register offset in MMU address range
    RMEM GPC

//store faulted PTE and faulted virtual address in local variables
    WBASE RBASE
    ISTACK_faulted_pte ROFST
    GPA RMEM
    ISTACK_faulted_va ROFST
    GPB RMEM
    ISTACK_faulted_operation ROFST
    GPC RMEM

//break the VA into its parts and store in local variables
    GPB ALUA //faulted VA to ALU
    IADF ALUB
    SKIP PC
    FF C0 00 00 //table number mask
    AAND ALUM
    ALUR ALUA
    16 ALUB
    ARSFT ALUM
    ISTACK_faulted_va_table_num ROFST
    ALUR RMEM

    GPB ALUA
    IADF ALUB
    SKIP PC
    00 3F F0 00 //virtual page number mask
    AAND ALUM
    ALUR ALUA
    0C ALUB
    ARSFT ALUM
    ISTACK_faulted_va_vpage_num ROFST
    ALUR RMEM


//get the active process ID and page directory physical page from kernel memory
    00 ROFST
    IADF RBASE
    SKIP PC
    ::ACTIVE_PROCESS_ID
    RMEM GPC
    IADF RBASE
    SKIP PC
    ::ACTIVE_PROCESS_PAGE_DIRECTORY_PHYSICAL_PAGE
    RMEM GPD

//store active process ID and page directory physical page in local variables
    WBASE RBASE
    ISTACK_active_process_id ROFST
    GPC RMEM
    ISTACK_active_process_page_directory_ppage ROFST
    GPD RMEM

//store active process entry index in local variable
    GPC GPH //active process ID to GPH
    RTRN LINK
    IADN PC
    :lite_find_process_map_entry_index_by_id //[GPH] -> [GPG][GPH]

    GPG COMPA
    00 COMPB
    COMPR PC
    :active_process_entry_not_found
    :active_process_entry_is_found
    @active_process_entry_not_found
    7F FLAG //no process entry found for active process; halt
    @active_process_entry_is_found
    WBASE RBASE
    ISTACK_active_process_map_entry_index ROFST
    GPH RMEM //store the process map entry ID in local variable

////////////////////////////////////
////////////////// New new handler
////////////////////////////////////

////////////////////////////////////////////
// fetch PDE
//
// check table is mapped
//     halt if not
// check table is resident
//     retrieve from disk if not
// mark table as referenced
// update page directory
//     set PDE R to 1
//
// fetch PTE
//
// check page is mapped
//     halt if not
// check page is resident
//     retrieve from disk if not
// mark page as referenced
// if write
//     check if page clean
//         check if page shared
//             split if so
//     mark page as dirty
// update page table
//     if page table is write protected
//         check if table clean
//             check if table shared
//                 split if so
//         make table as dirty
//         update page directory
//             set PDE W to 0
//     set PTE R to 1 (read allowed)
//     set PTE W to == !clean && !shared
////////////////////////////////////////////

//get PDE
    GPD GPH
    ISTACK_faulted_va_table_num ROFST
    RMEM GPG
    RTRN LINK
    IADN PC
    :lite_get_pte_kpa //[GPG][GPH] -> [GPH]
    //GPH holds kernel VA of PDE physical address

    //save kernel's VA of the PDE in local variable; we'll need to write the updated PDE back later
    WBASE RBASE
    ISTACK_fetched_pde_kpa ROFST
    GPH RMEM

    //set RBASE to point to the PDE for the faulted address (may or may not be the PDE/PTE that faulted)
    GPH RBASE
    00 ROFST
    RMEM GPE //PDE to GPE

    //save the PDE in local variable
    WBASE RBASE
    ISTACK_fetched_pde ROFST
    GPE RMEM
    ISTACK_updated_pde ROFST
    GPE RMEM

    //save the ppage/disk block number from the PDE in a local variable
    GPE ALUA
    IADF ALUB
    SKIP PC
    00 0F FF FF //number mask
    AAND ALUM
    ISTACK_fetched_pde_number ROFST
    ALUR RMEM

//check PDE protection bits to see if page table is mapped
    GPE ALUA //PDE to ALU
    IADF ALUB //RW bit mask to ALU
    SKIP PC
    C0 00 00 00 //RW bit mask
    AAND ALUM //AND mode

    ALUR ALUA //masked RW bits to ALU
    1E ALUB //30 bits right
    ARSFT ALUM //right shift mode
        //TODO don't need to shift to compare to 0

    //if RW bits are 00, page table is not mapped
    ALUR COMPA //RW bits to COMP
    00 COMPB //0b00
    COMPR PC
    :pde_not_mapped //RW == 00
    :pde_is_mapped

    @pde_not_mapped
    //RW == 00; page table is not mapped; access error
    7F FLAG //halt here (TODO: terminate user process)

    @pde_is_mapped
    //page table is mapped, continue

//check page table is resident
WBASE RBASE
ISTACK_fetched_pde ROFST
RMEM GPH
RTRN LINK
IADN PC
:lite_number_from_pte //[GPH] -> [GPH]
//GPH holds number (ppage or disk block, not known yet)
//store in local variable
    //if it is the ppage, it's correct; if it's not the ppage,
    //it's the disk block, but we'll update this variable when
    //loading the table into memory
ISTACK_virtual_table_ppage ROFST
GPH RMEM

//check if the table is resident
ISTACK_faulted_va_table_num ROFST
RMEM GPG
ISTACK_active_process_id ROFST
RMEM GPF
RTRN LINK
IADN PC
:lite_check_ppage_matches_vpage //[GPF][GPG][GPH] -> [GPH]
//GPH holds 1 if table is resident
GPH COMPA
01 COMPB
COMPR PC
:page_table_is_resident
:page_table_not_resident

@page_table_not_resident
    //the page table is not resident, which means the number in the PDE
    //actually represents the disk block where the table was evicted to
    break //TODO not tested
    RTRN LINK
    IADN PC
    :lite_get_open_ppage //() -> [GPH]
    //GPH holds open ppage
    //table will be stored in that ppage; store in local variable
    ISTACK_virtual_table_ppage ROFST
    GPH RMEM

    //load variables needed for loading the page from disk
    ISTACK_active_process_map_entry_index ROFST
    RMEM GPC
    ISTACK_faulted_va_table_num ROFST
    RMEM GPD
    ISTACK_active_process_page_directory_ppage ROFST
    RMEM GPE
    ISTACK_active_process_id ROFST
    RMEM GPF
    ISTACK_fetched_pde ROFST
    RMEM GPG
    RTRN LINK
    IADN PC
    :lite_load_pte_to_ppage //[GPC][GPD][GPE][GPF][GPG][GPH] -> [GPG]
    //store modified PDE in local variable
    ISTACK_updated_pde ROFST
    GPG RMEM

@page_table_is_resident
    //update physical page map to show page is referenced
        //because we're reading this table now
    WBASE RBASE
    ISTACK_virtual_table_ppage ROFST
    RMEM GPH
    14 GPG //offset to referenced field
    01 GPF //referenced = true
    RTRN LINK
    IADN PC
    :lite_set_ppage_field //[[GPF][GPG][GPH] -> ()

    //set R to 1 on PDE since the table is loaded and referenced now
    ISTACK_updated_pde ROFST
    RMEM GPH
    RTRN LINK
    IADN PC
    :lite_set_pte_readable //[GPH] -> [GPH]

    ISTACK_updated_pde ROFST
    //store newly updated PDE back into local variable
    GPH RMEM
    //also store updated PDE back into page directory
    ISTACK_fetched_pde_kpa ROFST //load PDE physical address
    RMEM RBASE //set RMEM to point to PDE physical address
    00 ROFST
    GPH RMEM //store PDE into page directory

    //mark the directory ppage as dirty, since we updated the PDE
    WBASE RBASE
    ISTACK_active_process_page_directory_ppage ROFST
    RMEM GPH
    18 GPG //offset to dirty field
    01 GPF //dirty = true
    RTRN LINK
    IADN PC
    :lite_set_ppage_field //[GPF][GPG][GPH] -> ()

//get PTE
    //calculate PTE physical address
    ISTACK_virtual_table_ppage ROFST
    RMEM GPH
    ISTACK_faulted_va_vpage_num ROFST
    RMEM GPG
    RTRN LINK
    IADN PC
    :lite_get_pte_kpa //[GPG][GPH] -> [GPH]
    //GPH holds VA of PTE physical address
    //store into local variable
    ISTACK_fetched_pte_kpa ROFST
    GPH RMEM

    //read the PTE
    GPH RBASE
    00 ROFST
    RMEM ALUA //hold in ALUA to extract the number later

    //store PTE into local variable
    WBASE RBASE
    ISTACK_fetched_pte ROFST
    ALUA RMEM
    ISTACK_updated_pte ROFST
    ALUA RMEM
    ALUA GPA //also store in GPA; PTE will be processed next

    //extract the ppage / disk block number (not known which yet)
    IADF ALUB
    SKIP PC
    00 0F FF FF //PTE number mask
    AAND ALUM
    //store number in local variable
    ISTACK_fetched_pte_number ROFST
    ALUR RMEM
    //also store as the ppage number
        //we don't yet know if it's a ppage or disk block,
        //but if it's not a ppage, this variable will be updated
        //when pulling the page into memory
    ISTACK_virtual_page_ppage ROFST
    ALUR RMEM

//check PTE protection bits to see if page is mapped
    GPA ALUA //PTE to ALU
    IADF ALUB
    SKIP PC
    C0 00 00 00 //RW bit mask
    AAND ALUM
    //if RW bits are 00, page is not mapped
    ALUR COMPA
    00 COMPB
    COMPR PC
    :pte_not_mapped
    :pte_is_mapped
    @pte_not_mapped
    //RW == 00; page is not mapped; access error
    7F FLAG //halt here (TODO: terminate user process)
    @pte_is_mapped
    //page is mapped, continue

//check page is resident
GPA GPH //move PTE to GPH for lite func arg
WBASE RBASE
ISTACK_faulted_va_vpage_num ROFST
RMEM GPG
ISTACK_active_process_id ROFST
RMEM GPF
RTRN LINK
IADN PC
:lite_check_ppage_matches_vpage //[GPF][GPG][GPH] -> [GPH]
//GPH holds 1 if table is resident
GPH COMPA
01 COMPB
COMPR PC
:page_is_resident
:page_not_resident

@page_not_resident
    //page is not resident, which means the number in the PTE
    //actually represents the disk block where the page was evicted to
    RTRN LINK
    IADN PC
    :lite_get_open_ppage //() -> [GPH]
    //GPH holds open ppage
    //vpage will be stored in that ppage; store in local variable
    WBASE RBASE
    ISTACK_virtual_page_ppage ROFST
    GPH RMEM

    //load variables needed for loading the page from disk
    ISTACK_active_process_map_entry_index ROFST
    RMEM GPC
    ISTACK_faulted_va_vpage_num ROFST
    RMEM GPD
    ISTACK_active_process_page_directory_ppage ROFST
    RMEM GPE
    ISTACK_active_process_id ROFST
    RMEM GPF
    ISTACK_fetched_pte ROFST
    RMEM GPG
    RTRN LINK
    IADN PC
    :lite_load_pte_to_ppage //[GPC][GPD][GPE][GPF][GPG][GPH] -> [GPG]
    //store modified PTE in local variable
    ISTACK_updated_pte ROFST
    GPG RMEM

@page_is_resident
    //update physical page map to show page is referenced
        //because we're accessing this page now
    WBASE RBASE
    ISTACK_virtual_page_ppage ROFST
    RMEM GPH
    14 GPG //offset to referenced field
    01 GPF //referenced = true
    RTRN LINK
    IADN PC
    :lite_set_ppage_field //[GPF][GPG][GPH] -> ()

    //set R to 1 on PTE since the page is loaded and referenced
    ISTACK_updated_pte ROFST
    RMEM GPH
    RTRN LINK
    IADN PC
    :lite_set_pte_readable //[GPH] -> [GPH]
    ISTACK_updated_pte ROFST
    //store newly updated PTE back into local variable
    GPH RMEM

//if this was a write operation, make sure the page is writable
WBASE RBASE
ISTACK_faulted_operation ROFST
RMEM COMPA
00 COMPB
COMPR PC //read operation == 0
:faulted_read
:faulted_write
    @faulted_write
    //the operation was a write operation, so make the page writable
    WBASE RBASE
    ISTACK_updated_pte ROFST
    RMEM ALUA
    IADF ALUB
    SKIP PC
    40 00 00 00 //W bit mask
    AAND ALUM
    ALUR COMPA
    00 COMPB
    COMPR PC
    :page_not_write_protected
    :page_is_write_protected
    @page_is_write_protected
    ISTACK_virtual_page_ppage ROFST
    RMEM GPH
    RTRN LINK
    IADN PC
    :lite_make_ppage_writable //[GPH] -> [GPH]
    //GPH holds physical page for target page
        //it may have changed because of page split

    //set the PTE page number in case it changed
    GPH GPG
    ISTACK_updated_pte ROFST
    RMEM GPH
    RTRN LINK
    IADN PC
    :lite_set_pte_number //[GPG][GPH] -> [GPH]
    //GPH holds updated PTE

    //the virtual page is no longer write protected
    RTRN LINK
    IADN PC
    :lite_set_pte_writable //[GPH] -> [GPH]
    //GPH holds updated PTE

    //store updated PTE back into local variable
        //it will be written back into the page table later
    ISTACK_updated_pte ROFST
    GPH RMEM

//possible conflicts from write protection have been resolved
//continue
@page_not_write_protected
@faulted_read


//check if the PTE needs to be written back
    //(a read fault where the table was not LRU referenced, but
    //  the virtual page was resident and LRU referenced would
    //  not require writing to the page table, so avoid that if
    //  we possibly can)
    ISTACK_fetched_pte ROFST
    RMEM COMPA
    ISTACK_updated_pte ROFST
    RMEM COMPB
    COMPR PC
    :pte_not_dirty
    :pte_is_dirty

@pte_is_dirty
//PTE has been changed, so it will need to be written back
    //the page table is definitely mapped, resident, and
    //referenced, since we loaded and read it to get to
    //this point; i.e. R on the PDE has been set to 1

    //check if W is 0 or 1; if it's 1, the page table
    //is clean, shared, or both
    ISTACK_updated_pde ROFST
    RMEM ALUA
    IADF ALUB
    SKIP PC
    40 00 00 00 //W bit mask
    AAND ALUM
    ALUR COMPA
    00 COMPB
    COMPR PC
    :page_table_not_write_protected
    :page_table_write_protected
    @page_table_write_protected
    ISTACK_virtual_table_ppage ROFST
    RMEM GPH
    RTRN LINK
    IADN PC
    :lite_make_ppage_writable // [GPH] -> [GPH]
    //GPH holds physical page for page table
        //it may have changed because of page split

    //update the virtual table ppage variable
    ISTACK_virtual_table_ppage ROFST
    GPH RMEM

    //set updated PDE's number to new ppage
    GPH GPG //move new number to GPG for func
    ISTACK_updated_pde ROFST
    RMEM GPH //pde to GPH
    RTRN LINK
    IADN PC
    :lite_set_pte_number //[GPG][GPH] -> [GPH]
    //GPH holds new PDE with changed number

    //set PDE as not write protected
    RTRN LINK
    IADN PC
    :lite_set_pte_writable //[GPH] -> [GPH]
    //GPH holds updated PDE

    //write PDE back to page directory
    ISTACK_fetched_pde_kpa ROFST //get PDE KPA (never changes in page fault)
    RMEM RBASE
    00 ROFST
    GPH RMEM //write PDE back into page directory

    //recalculate PTE KPA
    WBASE RBASE
    ISTACK_faulted_va_vpage_num ROFST
    RMEM GPG
    RTRN LINK
    IADN PC
    :lite_get_pte_kpa //[GPG][GPH] -> [GPH]
    //GPH holds KPA of PTE using new page table
    //store back into local variable
    ISTACK_fetched_pte_kpa ROFST
    GPH RMEM

    @page_table_not_write_protected
    //store updated PTE into page table
    WBASE RBASE
    ISTACK_updated_pte ROFST //load updated PTE
    RMEM GPA

    ISTACK_fetched_pte_kpa ROFST //load PTE physical address
    RMEM RBASE //set RMEM to point to PTE physical address
    00 ROFST
    GPA RMEM //store updated PTE into page table

@pte_not_dirty



@interrupt_return
//return from interrupt
//TODO restore processor state and return from interrupt handler
//TODO to ensure the MMU breakpoint isn't triggered by any reads from DMEM,
//  need to add delay to MMU breakpoint (2024: pretty sure this is done)

//load the user PC to return to
    WBASE RBASE
    ISTACK_pc_val ROFST
    RMEM GPB

//set the queued page directory to the active process
    ISTACK_active_process_page_directory_ppage ROFST
    RMEM GPA

    IADF RBASE
    SKIP PC
    00 00 10 00 //MMU base address in kernel VA
    04 ROFST //MMU queued page directory base address register
    GPA RMEM

//set the MMU breakpoint
    08 ROFST //MMU breakpoint address register
    GPB RMEM //PC from last instruction cache
        // breakpoint will be triggered when the instruction at <user PC> is fetched

    //set the breakpoint delay so it doesn't activate until the first cycle in user space
    //this will avoid unintentionally reading the breakpoint address if it happens to
    //coincide with one of the instructions between here and jumping back to <user PC>
    1C ROFST
    31 RMEM // 49 cycles until we want the breakpoint to become active

    //enable the breakpoint
    14 ROFST //break point enabled register
    01 RMEM //non-zero -> enabled

//restore processor state
    //prepare the new FLAG
    IADF DMEM08 //store the FLAG in DMEM so it can be set without using other registers immediately before returning
    SKIP PC
    //TODO for now, just set: !unlocked, !disabled, delay, !retry, !kernel, !user disabled, !user delay, no signal bits
    //  need to decide which other bits of the existing value need to be
    20 00 00 00

    //TODO need to restore LINK as well
    IADF WBASE
    SKIP PC
    ::KERNEL_STACK

    //we need to restore WMEM registers from DMEM, since we need WMEM to restore all other registers first
    48 WOFST
    WMEM DMEM00 //DMEM00 = dumped WBASE
    4C WOFST
    WMEM DMEM04 //DMEM04 = dumped WOFST

    //restore the rest of the registers
    00 WOFST
    WMEM GPA
    04 WOFST
    WMEM GPB
    08 WOFST
    WMEM GPC
    0C WOFST
    WMEM GPD
    10 WOFST
    WMEM GPE
    14 WOFST
    WMEM GPF
    18 WOFST
    WMEM GPG
    1C WOFST
    WMEM GPH
    20 WOFST
    WMEM COMPA
    24 WOFST
    WMEM COMPB
    28 WOFST
    WMEM RBASE
    2C WOFST
    WMEM ROFST
    30 WOFST
    WMEM ALUM
    34 WOFST
    WMEM ALUA
    38 WOFST
    WMEM ALUB
    3C WOFST
    WMEM FPUM
    40 WOFST
    WMEM FPUA
    44 WOFST
    WMEM FPUB

    //restore the dumped WMEM registers from DMEM
    DMEM00 WBASE
    DMEM04 WOFST
    //re-enable interrupts with a delay, lock the FLAG register
    DMEM08 FLAG

//jump to user space PC to resume execution
//MMU breakpoint will trigger, and the MMU will swap in the queued page directory (interrupted process)
    DMEM30 PC


//nothing left (this should never run)
02 FPUM
11 FPUA
22 FPUB
FPUR GPC
BREAK
7F FLAG
// end interrupt handler

//=========================
// interrupt handler stack frame end
//==========
FRAME_END
//=========================


//=========================
// Interrupt handler lite functions
//=============
// These subroutines do not use the stack and can be called from different frames.
// Lite functions:
//  1. Must restore WBASE and RBASE before returning
//  2. May overwrite all registers
//  3. May not access local variables, since they are defined outside any frame
//=========================

//=========================
@lite_number_from_pte
// Input
//  [GPH]: PTE (or PDE)
// Returns: number in GPH
//=========================
GPH ALUA
IADF ALUB
SKIP PC
00 0F FF FF //page/disk block number mask
AAND ALUM
ALUR GPH
LINK PC
//=========================
// End lite_number_from_pte
//=========================

//=========================
@lite_set_pte_readable
// Input
//  [GPH]: PTE (or PDE)
// Returns: updated PTE in GPH
//=========================
GPH ALUA
IADF ALUB
SKIP PC
80 00 00 00 //R bit mask
AOR ALUM
ALUR GPH
LINK PC
//=========================
// End lite_set_pte_readable
//=========================

//=========================
@lite_set_pte_writable
// Input
//  [GPH]: PTE (or PDE)
// Returns: updated PTE in GPH
//=========================
GPH ALUA
IADF ALUB
SKIP PC
BF FF FF FF //unset W bit mask
AAND ALUM
ALUR GPH
LINK PC
//=========================
// End lite_set_pte_writable
//=========================

//=========================
@lite_set_pte_number
// Input
//  [GPG]: new page / block for number field
//  [GPH]: PTE (or PDE)
// Returns: updated PTE in GPH
//=========================
GPH ALUA
IADF ALUB
SKIP PC
FF F0 00 00 //unset number mask
AAND ALUM
ALUR ALUA
GPG ALUB //new number to ALU
AOR ALUM
ALUR GPH //combine new number with PTE and return in GPH
LINK PC
//=========================
// End lite_set_pte_number
//=========================

//=========================
@lite_set_ppage_field
// Input
//  [GPF]: new value
//  [GPG]: field offset
//  [GPH]: ppage index
// Returns nothing
//=========================
GPH ALUA //physical page index
ICONST_ppage_map_entry_size ALUB
AMUL ALUM
ALUR ALUB //offset into physical page map array
IADF ALUA
SKIP PC
::physical_page_map
AADD ALUM
ALUR RBASE
GPG ROFST //offset into page entry
GPF RMEM //store new value
WBASE RBASE
LINK PC
//=========================
// End lite_set_ppage_field
//=========================

//=========================
@lite_get_pte_kpa
// Input
//  [GPG]: virtual page number
//  [GPH]: table physical page number
// Returns kernel physical address in GPH
//=========================
GPH ALUA //table physical page to ALU
0C ALUB
ALSFT ALUM //shift table physical page left 12 bits
ALUR GPH
GPG ALUA //virtual page number to ALU
02 ALUB //left shift vpage number 2 bits
//combine shift vpage and shifted table ppage to get PTE addr
ALUR ALUB
GPH ALUA
AOR ALUM
//ALUR holds PTE physical address, but needs
//to be transformed into kernel VA space to be readable
ALUR ALUA
IADF ALUB
SKIP PC
00 3F 00 00 //add 0x00 3F 00 00 to get kernel virtual address
    //of any physical address (TODO replace with special MMU mode)
AADD ALUM
ALUR GPH //return resulting KPA in GPH
LINK PC
//=========================
// End lite_get_pte_kpa
//=========================

//=========================
@lite_find_process_map_entry_index_by_id
// Input
//  [GPH]: process ID
// Returns process map entry index in GPH
//  Returns 1 in GPG if successful, 0 if entry not found
//=========================

AMUL ALUM
ICONST_process_map_entry_size ALUA
ICONST_process_map_length ALUB
ALUR GPG //GPG holds stop offset

AADD ALUM
00 ALUA
ICONST_process_map_entry_size ALUB

IADF RBASE
SKIP PC
::process_map

00 ROFST
@lite_find_process_0_loop
ROFST COMPA
GPG COMPB
COMPR PC
:lite_find_process_0_past_range
:lite_find_process_0_in_range

@lite_find_process_0_in_range
GPH COMPA
RMEM COMPB
COMPR PC
:lite_find_process_0_is_match
:lite_find_process_0_not_match
@lite_find_process_0_not_match


ALUR ROFST
ALUR ALUA
IADN PC
:lite_find_process_0_loop

@lite_find_process_0_past_range
00 GPG
LINK PC

@lite_find_process_0_is_match
01 GPG //set the success output to true
ROFST ALUA //entry offset to ALUA
ICONST_process_map_entry_size ALUB
ADIV ALUM //divide entry offset by entry length to get entry index
ALUR GPH //return entry index in GPH
WBASE RBASE
LINK PC

//=========================
// End lite_find_process_map_entry_index_by_id
//=========================

//=========================
@lite_load_pte_to_ppage
// Input
//  [GPC]: process map entry index
//  [GPD]: virtual page / table number
//  [GPE]: process page directory ppage
//  [GPF]: process ID
//  [GPG]: PTE (or PDE)
//  [GPH]: physical page index
// Returns nothing
//=========================
// load from disk block in PTE to physical page
// update physical page map
//update physical page map with new entry
GPH ALUA //physical page index
ICONST_ppage_map_entry_size ALUB
AMUL ALUM
ALUR ALUA //offset into physical page map array

IADF ALUB //physical page map base address
SKIP PC
::physical_page_map
AADD ALUM //ALUR holds base addr + offset to specific entry
    //this way we can use ROFST as offset to specific field within entry

ALUR RBASE
00 ROFST
//RMEM points to first byte of target physical page entry
GPF RMEM //store process ID in first field
04 ROFST
GPE RMEM //store process directory ppage index
08 ROFST
GPD RMEM //store virtual page/table number
0C ROFST
01 RMEM //set number of references to 1
    //this page was just loaded; it has only one reference
    //TODO is this always true?
10 ROFST

//get disk block number from PTE
GPG ALUA
IADF ALUB
SKIP PC
00 0F FF FF //disk block number mask
AAND ALUM
//ALUR holds block number
ALUR RMEM //store block number in physical page map entry

14 ROFST
01 RMEM //set recently referenced to true, since it was just loaded
    //this should prevent this page getting evicted immediately

//load page from disk block (still in ALUR)
IADF RBASE
SKIP PC
CONST_mmio_disk_base_address
//tell disk the target physical page
00 ROFST
GPH RMEM
//tell disk target disk block
04 ROFST
ALUR RMEM //ALUR holds disk block number
//use read mode
08 ROFST
00 RMEM
//initiate transfer from disk to memory
0C ROFST
01 RMEM

//update process map with new # of resident pages
GPC ALUA
ICONST_process_map_entry_size ALUB
AMUL ALUM
ALUR ALUA
IADF ALUB
SKIP PC
::process_map
AADD ALUM
//ALUR holds process map base addr + offset to specific entry
ALUR RBASE
08 ROFST //point to number of resident pages field
RMEM ALUA
01 ALUB
AADD ALUM //increment field value by 1
ALUR RMEM //store back into process map entry

//update the PTE with the new physical page and updated protections
//(but don't store it back to memory)

//unset the number in the PTE
GPG ALUA //PTE to ALU
IADF ALUB
SKIP PC
FF F0 00 00 //mask to unset PTE number
AAND ALUM
ALUR ALUA
//set the new ppage number
GPH ALUB //ppage number to ALU
AOR ALUM
ALUR ALUA
//unset the protection bits
IADF ALUB
SKIP PC
3F FF FF FF //mask to unset RW
AAND ALUM
ALUR ALUA
//set RW to 11
IADF ALUB
SKIP PC
C0 00 00 00 //mask to set RW to 11 (mapped, resident, referenced, clean)
AOR ALUM
ALUR GPG //store updated PTE to return in GPG

WBASE RBASE
LINK PC
//=========================
// End lite_load_pte_to_ppage
//=========================

//=========================
@lite_check_ppage_matches_vpage
//TODO does not work for shared pages
// Checks the given entry in the physical page map
// to determine if it belongs to the active process
// and matches the given virtual page
// Returns 1 if the page does belong to the active process
// Input
//  [GPF] active process ID
//  [GPG] virtual page number
//  [GPH] physical page number
// Returns: 0 or 1 in GPH
//=========================

//The given physical page number might be larger than the number
//of physical pages we have (in which case, it actually represents
//the disk block of the evicted page). Check if the number is in range

//num physical pages > page number?
ICONST_physical_memory_pages ALUA
GPH ALUB
ASUB ALUM //num pages - given page number
ALUR ALUA
IADF ALUB
SKIP PC
80 00 00 00 //two's complement negative number mask
AAND ALUM
ALUR COMPA
00 COMPB //if the masked number is 0, it's positive, and page was in range
COMPR PC
:lite_check_ppage_0_ppage_in_range
:lite_check_ppage_0__ppage_in_range

@lite_check_ppage_0__ppage_in_range
//number given is beyond range of physical pages, so
//it cannot match the given vpage (it represents a disk block)
00 GPH
LINK PC //return false

@lite_check_ppage_0_ppage_in_range
//check the ppage entry to see if the process owner
//and vpage match

GPF COMPA //active process ID to COMPA

IADF RBASE
SKIP PC
::physical_page_map

AMUL ALUM
GPH ALUA //physical page number == index
ICONST_ppage_map_entry_size ALUB
ALUR ROFST //RMEM points to target physical page map entry

RMEM COMPB //ppage process id (offset 0) to COMPA

COMPR PC
:lite_check_ppage_0_process_matches
:lite_check_ppage_0__process_matches

@lite_check_ppage_0__process_matches
//if the ppage owner != active process, the pages can't match
//return false
00 GPH
WBASE RBASE
LINK PC

@lite_check_ppage_0_process_matches
//the active process owns the target physical page, so check if
//the vpage numbers match
ROFST RBASE
08 ROFST //offset into ppage map entry == 8
RMEM COMPA
GPG COMPB
COMPR PC
:lite_check_ppage_0_vpage_matches
:lite_check_ppage_0__vpage_matches

@lite_check_ppage_0_vpage_matches
//the vpage number and active process both match, so
//we know the given vpage is resident in the given ppage
01 GPH
WBASE RBASE
LINK PC

@lite_check_ppage_0__vpage_matches
00 GPH
WBASE RBASE
LINK PC
//=========================
// End lite_check_ppage_matches_vpage
//=========================


//=========================
@lite_make_ppage_writable
// Determines if the target ppage is allowed to be
// written to, and updates the physical page map
// accordingly. Returns the physical page index of
// the writable version of the page (which may be
// different from the input page index, since the
// page may have had to be split to be made writable)
// Input
//  [GPH]: physical page index
// Returns physical page index in GPH
//=========================

//calculate address of target physical page entry in map
AMUL ALUM
ICONST_ppage_map_entry_size ALUA
GPH ALUB
ALUR ALUB
AADD ALUM
IADF ALUA
SKIP PC
::physical_page_map
ALUR RBASE
18 ROFST
RMEM GPA //dirty field to GPA
0C ROFST
RMEM GPB //number of references to GPB

GPA COMPA //dirty field to COMOPA
01 COMPB //1 == physical page is dirty
COMPR PC
:lite_make_ppage_0_is_dirty
:lite_make_ppage_0_not_dirty
@lite_make_ppage_0_is_dirty
//the page is already dirty, it must be writable already; return
WBASE RBASE
LINK PC

@lite_make_ppage_0_not_dirty
//page is clean, check if we need to split it
GPB COMPA //number of processes referencing this physical page
01 COMPB //1 == physical page is not shared
COMPR PC
:lite_make_ppage_0_not_shared
:lite_make_ppage_0_is_shared
@lite_make_ppage_0_is_shared
//the page is shared, so we need to split it
    //this process will get a new copy of the page while
    //existing processes will keep the same physical page
    7F FLAG //TODO not implemented
        //we might need a new datastructure to implement shared pages
        //1. when checking if a page is resident, we check that the process ID
        //  matches; since there's only 1 process ID, this fails for sharing processes
        //2. when splitting a page, the owning process may abandon the page, but
        //  sharing processes can still use it; how will this be represented?

@lite_make_ppage_0_not_shared
//the page is not shared, mark it as dirty
18 ROFST //dirty field offset
01 RMEM //set as dirty

WBASE RBASE
LINK PC
//=========================
// End lite_make_ppage_writable
//=========================



//=========================
// Returns open physical page number in GPH
@lite_get_open_ppage
//=========================

//look for open pages, which we can use without evicting anything
//open pages have a process ID of 0

//iterate over physical page map
    //point RMEM to physical page map array
        IADF RBASE
        SKIP PC
        ::physical_page_map
        00 ROFST

    00 GPA //GPA = index
    ICONST_physical_memory_pages GPB //GPB = max_index

    @open_page_loop
    //check if we're at the end of the loop (index == 16?)
        GPA COMPA
        GPB COMPB
        COMPR PC
        :open_page_loop_end
        :open_page_loop_go

    @open_page_loop_go
    //calculate offset from index
        02 ALUM //multiply mode
        GPA ALUA // ALUA = index
        ICONST_ppage_map_entry_size ALUB
        ALUR ROFST

    //read process ID (offset 0)
    RMEM GPC //GPC = process id

    //process id == 0?
        GPC COMPA
        00 COMPB
        COMPR PC
        :open_page_proc_id_0
        :open_page_loop_next

    @open_page_proc_id_0
    // found an open page
    // return the index as the open physical page
        GPA GPH //move result into designated return register
        WBASE RBASE
        LINK PC //return



    @open_page_loop_next
    //increment index
        01 ALUM //add mode
        GPA ALUA
        01 ALUB
        ALUR GPA //index += 1
    //go to start of loop
    IADN PC
    :open_page_loop

    //TODO implement this


@open_page_loop_end
//no open pages
BREAK

//      TODO important insight:
//      An evictable page is one where no other non-empty page mapping entry has a "directory physical page" that
//      matches the evictable page's physical page (this is true of all leaf pages, all page tables with
//      no child pages in memory, and all page directories with no child tables in memory -- except page
//      directories point to themselves, so never count self reference)
//      TODO the above is only true if leaf pages point to their page tables; right now, the "directory physical page"
//      variable points to the owning process's page directory
//          Why is it set up that way? To make it easy to see if a process has any non-directory tables or pages in memory?
//          It should be possible to find the owning process of any page mapping entry by following the "directory physical page"
//          pointer until it references itself (only true of directories), which is at most 2 steps (2 for leafs, 1 for tables)
//          The process descriptor in the process map also has the number of resident pages

//TODO implement this
7F FLAG
//return
WBASE RBASE
LINK PC
//=========================
// End lite_get_open_ppage
//=========================



FRAME_START
//=========================
// syscall handler stack layout / local variables
//==========
    STACK flag_val 04           // FLAG contents
    STACK syscall_number 04     // syscall function (mpa page, unmap page, etc.)
    STACK syscall_arg1 04       // syscall argument 1
//=========================

//=========================
// Users can make system calls by sending a kernel interrupt with all signal bits set to 0
// The system call number should be stored in GPA
//=========================

@system_call
    ISIZE_FRAME WOFST //when the interrupt handler begins, the stack is set up for the page fault handler
        //but for syscalls, we jump here, and use a different stack frame layout, so replace the size
        //that was set up for the page fault handler with the size needed for the syscall handler

    // retrieve syscall number and arguments from the dumped processor state
    // (user stores syscall num in GPA, args in other GP registers)
    00 GPA
    IADF RBASE
    SKIP PC
    ::KERNEL_STACK
    iconst_gpa_dump ROFST
    RMEM GPA
    iconst_gpa_dump ROFST
    RMEM GPB

    // store syscall number in local variable
    WBASE RBASE
    ISTACK_syscall_number ROFST
    GPA RMEM

    // determine which syscall to execute
    // (TODO: change this to a table with syscall addresses instead of if statements)
    GPA COMPA
    01 COMPB // allocate memory
    COMPR PC
    :syscall_map_page
    :syscall_not_map_page
    @syscall_not_map_page
        // TODO not implemented
        break
        7f FLAG

    @syscall_map_page
        break
        // check if desired page is already mapped
        // update the page table and directory
        // update process map
        // TODO:
        //      How to find an open disk block? Should OS keep track, or let the disk do it?
        //      Change fetching nonresident pages to consider a disk block of 0 to be an empty page
        //          which will avoid reading from disk for empty (just mapped) pages
        //      MMU clear page: give the MMU a function to clear a page, which can be used when
        //          paging a non-resident page in when it had no disk block
        7f FLAG

//=========================
// system call handler stack frame end
//==========
FRAME_END
//=========================

//nothing left (this should never run)
7F FLAG









////////////////////////////////////
////////////////// Old function call code for reference
////////////////////////////////////


// //push function address onto stack
// IADF WMEM
// SKIP PC
// break //TODO commented out because it will probably be removed
// // ::get_open_physical_page

// 01 ALUM //add mode
// ISIZE_FRAME ALUA //stack frame size
// 04 ALUB //add 4 for room for the target address, which we just pushed to the stack
// //TODO theoretically this is known at compile time; add macro for ISIZE_FRAME+X ?
// ALUR WOFST //SP = size of stack frame + 4

// //jump to function
// RTRN LINK
// IADN PC
// ::FUNC

// //result of function call is target physical page number
// //pop result from stack
//     ISIZE_FRAME WOFST //size of stack frame, top of stack
//     WMEM GPH
// //store in local variable
//     WBASE RBASE
//     ISTACK_target_ppage ROFST
//     GPH RMEM
