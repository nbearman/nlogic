//=========================================================================
// kernel entry point
//=========================================================================
FILLB0
33 GPE

//set the MMU VA break point
IADF WBASE
SKIP PC
00 00 10 00 //MMIO base address (in VA)
08 WOFST //breakpoint register
00 WMEM //breakpoint at 0

//enable the breakpoint
10 WOFST //breakpoint enabled register
01 WMEM //non-zero -> enabled

00 RBASE
00 ROFST

//jump to 0 in user space
00 PC

//=========================================================================
// interrupt handler
//=========================================================================
FILL200
WBASE DMEM00
WOFST DMEM04

//TODO change this to save registers to kernel's stack
//dump registers to VA 0100
IADF WBASE
SKIP PC
00 00 01 00
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
00 00 10 00
0C ROFST
RMEM GPA

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
00 COMPB
COMPR PC
:r0w0
:_r0w0
@_r0w0
01 COMPB
COMPR PC
:r0w1
:_r0w1
@_r0w1
02 COMPB
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

//set up the stack
00 WOFST
IADF WBASE
SKIP PC
::KERNEL_STACK

//push function address onto stack
IADF WMEM
SKIP PC
::get_open_physical_page
04 WOFST
RTRN LINK
IADN PC
::FUNC

//result of function call is target physical page number
//pop result from stack
00 WOFST
WMEM GPH



//evict page
//load page from disk
//return from interrupt

//load page from disk
//get disk block from PTE
GPA ALUA //PTE to ALU
IADF ALUB //mask to ALU
SKIP PC
00 0F FF FF
08 ALUM //AND mode
ALUR GPD





//nothing left
02 FPUM
11 FPUA
22 FPUB
FPUR GPC
7F FLAG

FILL600
//physical page map

//=========================
// physcial page mapping entry
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
00 00 00 00 //virtual page 0
00 00 00 01 //kernel process references this physical page
00 00 00 00 //no disk block number, can never be evicted

//kernel virtual page 0
00 00 00 01 //kernel process ID == 1
00 00 00 01 //owning page directory is kernel directory
00 00 00 00 //virtual page 0
00 00 00 01 //kernel process references this physical page
00 00 00 00 //no disk block number, can never be evicted

//end physical page map (16 page mappings)
FILL840
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

//end process map (16 process descriptors)
FILL940

@@get_open_physical_page
//returns physical page number that is available for incoming page
//may or may not result in page eviction
BREAK
//TODO implement this
00 RBASE
00 ROFST
7F RMEM
LINK PC
