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
// 0x08 |   uint    |   virtual page number
// 0x0C |   uint    |   number of references
// 0x10 |   uint    |   disk block number
//=========================

//boot sequence (no owner)
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

//kernel page table 0
00 00 00 01 //kernel process ID == 1
00 00 00 01 //owning page directory is kernel directory
00 00 00 00 //virtual table 0
00 00 00 01 //kernel process references this physical page
00 00 00 00 //no disk block number, can never be evicted

//kernel virtual page 0
00 00 00 01 //kernel process ID == 1
00 00 00 01 //owning page directory is kernel directory
00 00 00 00 //virtual page 0
00 00 00 01 //kernel process references this physical page
00 00 00 00 //no disk block number, can never be evicted

//empty (no owner)
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

//user page table 0
00 00 00 02 //user process ID == 2
00 00 00 05 //owning page directory is user page directory
00 00 00 00 //virtual table 0
00 00 00 01 //user process references this physical page
00 00 00 00 //no disk block number yet

//user virtual page 0
00 00 00 02 //user process ID == 2
00 00 00 05 //owning page directory is user page directory
00 00 00 00 //virtual page 0
00 00 00 01 //user process references this physical page
00 00 00 64 //loaded from disk block 100



//empty (no owner)
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
//empty (no owner)
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
//empty (no owner)
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
//empty (no owner)
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

//end physical page map (32 page mappings)(TODO only 16 shown; space reserved for the rest with FILL)
FILL380

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

//kernel process descriptor
00 00 00 01 //kernel process ID == 1
00 00 00 03 //3 pages are mapped: 2 pages of memory and 1 mapped to the MMU
00 00 00 03 //page directory, page table, 1 page of memory
00 00 00 00 //no disk block number, kernel page directory can never be evicted

//user process descriptor
00 00 00 01 //user process ID == 2
00 00 00 03 //2 pages are mapped: 2 pages of memory
00 00 00 03 //page directory, page table, 1 page of memory
00 00 00 00 //TODO figure out if we're supposed to load process page directory from disk...
            //(it should probably be built dynamically from some kind of description file
            // that the kernel can read to determine how many pages of the program are
            // mapped out of the box [length of program data])

//end process map (16 process descriptors)
FILL480


//TODO unused space between 0x480 and 0x500
//  interrupt handler could be moved to 0x480


//=========================================================================
// interrupt handler
//=========================================================================
FILL500
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
// 0x00 |   uint    |   FLAG
// 0x04 |   uint    |   faulted PTE
// 0x08 |   uint    |   faulted virtual address
// 0x0C |   uint    |   faulted PTE disk block
// 0x10 |   uint    |   virtual page number from faulted virtual address
// 0x14 |   uint    |   active process ID
// 0x18 |   uint    |   active process page directory physical page
// 0x1C |   uint    |   physical page new page is moved into
//=========================

    STACK flag_val 04
    STACK faulted_pte 04
    STACK faulted_va 04
    STACK faulted_pte_block 04
    STACK faulted_va_vpage_num 04
    STACK active_process_id 04
    STACK active_process_page_directory_ppage 04
    STACK target_ppage 04
    STACK fetched_pde 04 // possibly different than faulted PTE if the leaf page faulted
    STACK fetched_pte 04 // retrieved by following the PDE if leaf page faulted
                            //should be the same as faulted PTE in that case
    STACK fetched_pde_kpa 04
    STACK fetched_pte_kpa 04

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
    08 ALUM //AND mode
    ALUR COMPA
    00 COMPB //if flag channel is 1, jump to mmu interrupt handler
    COMPR PC
    :non_mmu_interrupt
    :mmu_interrupt

@non_mmu_interrupt
//if interrupt is not from MMU
    //do nothing if the interrupt is from anywhere besides the MMU
    7F FLAG

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

//store faulted PTE and faulted virtual address in local variables
    WBASE RBASE
    ISTACK_faulted_pte ROFST
    GPA RMEM
    ISTACK_faulted_va ROFST
    GPB RMEM

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

//check PTE protections
//get them from PTE
    GPA ALUA //PTE to ALU
    IADF ALUB //mask to ALU
    SKIP PC
    C0 00 00 00 //mask for the RW bits of the PTE
    08 ALUM //AND mode

    ALUR ALUA //RW bits to ALU
    1E ALUB //shift 30 bits right
    06 ALUM //right shift mode

//see which of 00, 01, 10, 11 the RW bits are
ALUR COMPA
00 COMPB //0b00
COMPR PC
:r0w0
:_r0w0
@_r0w0
01 COMPB //0b01
COMPR PC
:r0w1
:_r0w1
@_r0w1
02 COMPB //0b10
COMPR PC
:r1w0
:_r1w0
@_r1w0
7F FLAG //halt; RW was 11, no page fault should have occurred

@r0w0
//not mapped (possibly syscall)
7F FLAG //TODO for now, just halt

@r1w0
//page is readable but not writable
//either a shared page or a clean page
//page needs to be split or marked as dirty
7F FLAG //TODO for now, just halt

@r0w1
//not readable and "writable" indicates the page is mapped but paged out
//page not resident

//push function address onto stack
IADF WMEM
SKIP PC
::get_open_physical_page

01 ALUM //add mode
ISIZE_FRAME ALUA //stack frame size
04 ALUB //add 4 for room for the target address, which we just pushed to the stack
//TODO theoretically this is known at compile time; add macro for ISIZE_FRAME+X ?
ALUR WOFST //SP = size of stack frame + 4

//jump to function
RTRN LINK
IADN PC
::FUNC

//result of function call is target physical page number
//pop result from stack
    ISIZE_FRAME WOFST //size of stack frame, top of stack
    WMEM GPH
//store in local variable
    WBASE RBASE
    ISTACK_target_ppage ROFST
    GPH RMEM

//load page from disk
//get disk block from PTE
    GPA ALUA //PTE to ALU
    IADF ALUB //mask to ALU
    SKIP PC
    00 0F FF FF //disk block mask
    08 ALUM //AND mode
    ALUR GPE
//get virtual page / virtual directory number from virtual address
    GPB ALUA //virtual address to ALU
    IADF ALUB //mask to ALU
    SKIP PC
    FF FF F0 00 //remove physical offset part of virtual address
    ALUR GPF

//store disk block and virtual page number in local variables
    WBASE RBASE
    ISTACK_faulted_pte_block ROFST
    GPE RMEM
    ISTACK_faulted_va_vpage_num ROFST
    GPF RMEM

//load from disk
    //point RMEM to virtual disk
        IADF RBASE
        SKIP PC
        00 00 10 20 //MMIO starts at VA 1000, disk starts at 20
    //tell disk the target physical page (stored in GPH)
        00 ROFST
        GPH RMEM
    //tell disk target disk block (stored in GPE)
        04 ROFST
        GPE RMEM
    //use read mode
        08 ROFST
        00 RMEM
    //initiate transfer from disk to memory
        0C ROFST
        01 RMEM



//update physical page map
    //point RMEM to physical page map array
        IADF RBASE
        SKIP PC
        ::physical_page_map

    //calculate offset of target physical page entry
        02 ALUM //multiply
        GPH ALUA //target physical page
        14 ALUB //20 bytes per entry
        ALUR ROFST //physical page map offset

    GPC RMEM //set process ID to user process

    //move to next field in entry
        01 ALUM //add
        ROFST ALUA
        04 ALUB
        ALUR ROFST

    GPD RMEM //set directory physical page to user directory

    //move to next field
        ALUR ALUA
        ALUR ROFST
        
    GPF RMEM //set virtual page/directory number to that of the faulted address

    //move to next field
        ALUR ALUA
        ALUR ROFST

    01 RMEM //only one process references this page

    //move to next field
        ALUR ALUA
        ALUR ROFST

    GPE RMEM //store origin disk block as the disk block


//update process map
    //point RMEM to process map
        IADF RBASE
        SKIP PC
        ::process_map

    //calculate offset of target process entry ((process ID - 1) is index into map)
        // (16 * (PID - 1)) + 8
        03 ALUM //subtract
        GPC ALUA //process ID
        01 ALUB //minus 1
        ALUR ALUA //ALUA = PID - 1

        10 ALUB //16 byte per entry
        02 ALUM //multiply
        ALUR ALUA //process entry map offset
        08 ALUB //offset into entry for number of resident pages
        01 ALUM //add
        ALUR ROFST //total offset into process map

    //increment number of resident pages for this process
        RMEM ALUA
        01 ALUB
        ALUR RMEM

//update page table
//TODO how do we know if we loaded a page table or a leaf page?
//  if we loaded a leaf page, we need to update the page table
//  but if we loaded a page table, we need to update the page directory
//      Use the page table to see which of the PDE/PTE was protected -> caused this fault
//  traverse the page table: the first PDE/PTE with matching protection bits
//  was the one that failed
//      this is true because !R,W always means the page is mapped but not resident.
//      if we are trying to load a leaf page, it is not possible that the parent table
//      is !R,W (evicted), because we would have faulted on accessing that table before
//      we ever knew we needed to bring this leaf page into memory.
//      therefore, if the PDE is !R,W, we are definitely loading a page table
//      ...if PDE caused fault: we are loading a page table into memory
//      ...if PTE caused fault: we are loading a leaf page into memory

//update page table
// get the PDE (don't just use the faulted PTE; we still don't know if that is a PDE or PTE)
//  if the PDE was the one that faulted, it should have been !R,W
//      now the page table it points to is in memory, mark the PDE as R,!W
//          the page table is write protected because it is clean compared to disk
//      mark the PDE as !F,!D since the page table is clean and newly loaded
//          handling this fault does not count as accessing/referencing the newly loaded page table
//          a subsequent read, or handling a PTE fault, would mark the PDE as referenced, though
//      update the PDE with the new physical page where the page table was put (local var at 0x1C)
//      done
//  if the PDE was not the one that faulted, it should have been R,W
//      mark the PDE as F,D since we will be updating the page table with the newly loaded leaf page's information
//          F bit should have been set before MMU faulted on the page anyway, since we read the page table
//      the PTE should have been the one that faulted; the leaf page was just loaded into memory
//          the PTE should be !R,W
//          get the PTE by following the PDE to the page table
//          set the PTE to R,!W
//              R because the page is now resident in memory and safe to read
//              !W because the page is clean compared to disk (it was just loaded)
//          mark the PTE as !F,!D since the leaf page is clean and newly loaded
//              F bit will be set when the instruction is retried, but loading the page right now doesn't count as reference
//          update the PTE with the new physical page where the page table was put (local var at 0x1C)
//      done

//get PDE
    05 ALUM //left shift
    GPD ALUA //active process page directory physical page
    0C ALUB //12 bits
    ALUR GPA //GPA = page directory base address

    //page table number = (virtual address & 0xFFC00000) >> 22
    08 ALUM //AND
    GPB ALUA //virtual address
    IADF ALUB //mask for first 12 bits
    SKIP PC
    FF C0 00 00

    ALUR ALUA
    06 ALUM //right shift
    14 ALUB //20 bits (page number is given by shifting 22 bits, but offset into page table is page number * 4, so shift left 2 bits)

    ALUR ALUB
    GPA ALUA
    07 ALUM //OR
        //TODO setting OR then AND? probably do the same thing in this case
        // probably need to swap the next two instructions: use OR, retrieve result, then set AND
    01 ALUM //add
    ALUR ALUA //page directory entry physical address
    IADF ALUB //add 0x00 3F 00 00 to get kernel virtual address of physical address
    SKIP PC
    00 3F 00 00

    //save kernel's VA of the PDE in local variable; we'll need to write the updated PDE back later
    WBASE RBASE
    ISTACK_fetched_pde_kpa ROFST
    ALUR RMEM

    //set RBASE to point to the PDE for the faulted address (may or may not be the PDE/PTE that faulted)
    ALUR RBASE
    00 ROFST

//check if PDE or PTE faulted
//  PDE faulted if the PDE is !R,W
//  else PTE faulted (if PTE faulted, PDE is necessarily R,W, else it would have faulted first)
    //extract RW bits from PDE
        RMEM ALUA //PDE to ALU

            //store PDE in local variable for later
            WBASE RBASE
            ISTACK_fetched_pde ROFST
            ALUA RMEM

        IADF ALUB //protection mask to ALU
        SKIP PC
        C0 00 00 00 //mask for the RW bits of the PDE
        08 ALUM //AND mode

        ALUR ALUA //RW bits to ALU
        1E ALUB //shift 30 bits right
        06 ALUM //right shift mode

    //compare PDE RW bits to !R,W (0x01)
        ALUR COMPA //RW bits to comparator
        01 COMPB //!R,W (0x01) to comparator
        COMPR PC
        :table_caused_r0w1 //the PDE faulted, so we just loaded a page table into memory
        :leaf_caused_r0w1 //the PTE faulted, so we just loaded a leaf page into memory

@table_caused_r0w1 //IF CLAUSE (PDE faulted)
//if PDE faulted, we just loaded a page table in; update the PDE
//  update the PDE to be R,!W,!F,!D
    7F FLAG // TODO implement this

// skip else clause
IADN PC
:conclude_r0w1

@leaf_caused_r0w1 //ELSE CLAUSE (PTE faulted)
//else if PTE faulted, we just loaded a leaf page in; update the PDE and PTE
    //fetch the PTE through the PDE
        //we know the page table is resident, since the PDE didn't fault
        //therefore PDE holds the physical page of the PTE

        //page table base address == (PDE & 0x000FFFFF) << 12
        WBASE RBASE
        ISTACK_fetched_pde ROFST
        RMEM ALUA //PDE to ALU
        IADF ALUB //mask to ALU
        SKIP PC
        00 0F FF FF //physical page mask
        08 ALUM //AND mode

        ALUR ALUA //physical page number of page table
        0C ALUB //12 bits
        05 ALUM //left shift
        ALUR GPA //GPA = page table base address

        //page number = (virtual address & 0x003FF000) >> 12
            // PTEs are 4 bytes, so address is (table addr + (page number * 4))
            // skip the *4 by shifting only 10 bits instead of 12
        //(virtual address & 0x003FF000) >> 10
        ISTACK_faulted_va ROFST
        RMEM ALUA //VA to ALU
        IADF ALUB //mask to ALU
        SKIP PC
        00 3F F0 00 //page number mask
        08 ALUM //AND
        
        ALUR ALUA // page number of PTE (from PDE, unshifted)
        06 ALUM //right shift
        0A ALUB //10 bits

        ALUR ALUB //page number * 4
        GPA ALUA //page table base address
        07 ALUM //OR

        ALUR ALUA //page table entry physical address to ALU
        IADF ALUB //add 0x00 3F 00 00 to get kernel virtual address of any physical address
        SKIP PC
        00 3F 00 00 //PA->kernel VA offset
        01 ALUM //ADD
        ALUR RBASE //PTE address in kernel VA
        00 ROFST //RMEM points to PTE

        RMEM GPB //save the PTE somewhere
        WBASE RBASE
        ISTACK_fetched_pte ROFST
        GPB RMEM //store the fetched PTE in local variable

        ISTACK_fetched_pte_kpa ROFST
        ALUR RMEM //store kernel's VA of PTE in local variable; we'll need to write the updated PTE back

    //assert that the PTE is !R,W
        //get the protection bits
            GPB ALUA //PTE to ALU
            IADF ALUB //mask to ALU
            SKIP PC
            C0 00 00 00 //mask for the RW bits of the PTE
            08 ALUM //AND

            ALUR ALUA //RW bits to ALU
            1E ALUB //shift 30 bits right
            06 ALUM //right shift mode

        //compare PTE RW bits to !R,W (0x01)
            ALUR COMPA //RW bits to comparator
            01 COMPB //!R,W (0x01)
            COMPR PC
            :assertion_passed_faulted_pte_not_0rw1
            :assertion_failed_faulted_pte_not_0rw1

        @assertion_failed_faulted_pte_not_0rw1
        //if these are not the protection bits, neither the PDE nor PTE faulted?
        //we are in the !R,W branch, so one of PDE and PTE should have !R,W
        //since neither meet that criteria, HALT; PANIC
            7F FLAG

    @assertion_passed_faulted_pte_not_0rw1
    //update the PDE to be F,D
        ISTACK_fetched_pde ROFST
        RMEM GPC //GPC = PDE we need to update
        //GPB already holds PTE we need to update

        GPC ALUA //PDE to ALU
        IADF ALUB //mask to ALU
        SKIP PC
        30 00 00 00 //F,D mask
        07 ALUM //OR mode

    //write updated PDE back to page directory
        ISTACK_fetched_pde_kpa ROFST
        RMEM RBASE
        00 ROFST
        ALUR RMEM

    //update the PTE to be R,!W,!F,!D
        GPB ALUA //PTE to ALU
        IADF ALUB //mask to ALU
        SKIP PC
        80 00 00 00 //R,!W,!F,!D mask

    //update the PTE with the physical page where the page now lives
    //  we're replacing the disk block in the PTE, but we already saved it
    //  in the kernel's physical page map

        // erase the disk block part of the PTE
            ALUR ALUA //updated PTE to ALU
            IADF ALUB //mask to ALU
            SKIP PC
            FF F0 00 00 //disk block mask
            08 ALUM //AND mode

            ALUR ALUA //PTE with disk block removed to ALU
            WBASE RBASE
            ISTACK_target_ppage ROFST
            RMEM ALUB //physical page where page now resides to ALU
            07 ALUM //OR mode

        //write updated PTE back to page table
            ISTACK_fetched_pte_kpa ROFST
            RMEM RBASE
            00 ROFST
            ALUR RMEM
        //done; return from interrupt handler

@conclude_r0w1
//return from interrupt
//TODO restore processor state and return from interrupt handler
//TODO to ensure the MMU breakpoint isn't triggered by any reads from DMEM,
//  need to add delay to MMU breakpoint


//set the queued page directory to the active process
    WBASE RBASE
    ISTACK_active_process_page_directory_ppage ROFST
    RMEM GPA

    IADF RBASE
    SKIP PC
    00 00 10 00 //MMU base address in kernel VA
    04 ROFST //MMU queued page directory base address register
    GPA RMEM

//set the MMU breakpoint
    //TODO change this to include setting the number of cycles before the breakpoint
    //activates
    08 ROFST //MMU breakpoint address register
    DMEM30 RMEM //PC from last instruction cache
        //TODO is the breakpoint supposed to be set at the PC of the first user instruction, or the PC of the last kernel instruction?
        //TODO step through to validate that interrupts are re-enabled atomically

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


//nothing left
02 FPUM
11 FPUA
22 FPUB
FPUR GPC
7F FLAG
// end interrupt handler

FRAME_END

//=========================================================================
// [function] get_open_physical_page | void TODO: this is not void
//=========================================================================
@@get_open_physical_page
//returns physical page number that is available for incoming page
//may or may not result in page eviction


//calculate address where return value should be stored
    03 ALUM //subtract
    WBASE ALUA //original FP
    54 ALUB // -84
    ALUR GPH //GPH = result address = FP - 84

//look for open pages, which we can use without evicting anything
//open pages have a process ID of 0

//iterate over physical page map
    //point RMEM to physical page map array
        IADF RBASE
        SKIP PC
        ::physical_page_map
        00 ROFST

    00 GPA //GPA = index
    10 GPB //GPB = max_index

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
        14 ALUB //20 bytes per entry
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
        WBASE ALUA //store FP somewhere
        GPH WBASE //point WMEM to the result address
        00 WOFST
        GPA WMEM //result = index into physical page map
        ALUA WBASE //restore FP
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
00 RBASE
00 ROFST
7F RMEM

//return
LINK PC
