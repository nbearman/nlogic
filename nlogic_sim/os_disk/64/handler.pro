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

//enabled the breakpoint
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
7F GPB
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

@r0w1
//write protected; clean page, possibly shared
//must have been write to raise interrupt
7F FLAG //TODO for now, just halt

@r1w0
//page not resident

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





@@FUNC
/////////////////////////////////////////////////
//Call target function with new stack frame
//Processor state is preserved and restored
//before returning to caller

//push target function arguments onto stack
//push target function address onto stack
//load return address into LINK
//jump to FUNC
//WMEM accessor is reserved for stack operations
//DMEM will be overwritten
//target function address will be overwritten with
//function call result

//target function should store result at (FP - 54)
//first function argument is accessible at (FP - 58)

//invoke this helper with:
//RTRN LINK
//IADN PC
//::FUNC
/////////////////////////////////////////////////
//caller save layout
//GPA
//GPB
//GPC
//GPD
//GPE
//GPF
//GPG
//GPH
//ALUM
//ALUA
//ALUB
//FPUM
//FPUA
//FPUB
//RBASE
//ROFST
//COMPA
//COMPB
//LINK
//frame pointer
/////////////////////////////////////////////////

//clear ALU
ALUM DMEM00
ALUA DMEM04
ALUB DMEM08

//clear GPA to store target address
GPA DMEM0C

//set up ALU for popping target function address
03 ALUM //subtract mode
WOFST ALUA
04 ALUB //4 bytes per register

//read top of stack: target function address
ALUR WOFST //SP = SP - 4
WMEM GPA

//restore SP to top of stack
ALUA WOFST //SP = SP

//set up ALU for stacking
01 ALUM //add mode

//push caller save registers onto stack
DMEM0C WMEM //GPA was stored in DMEM earlier
ALUR WOFST
ALUR ALUA
GPB WMEM
ALUR WOFST
ALUR ALUA
GPC WMEM
ALUR WOFST
ALUR ALUA
GPD WMEM
ALUR WOFST
ALUR ALUA
GPE WMEM
ALUR WOFST
ALUR ALUA
GPF WMEM
ALUR WOFST
ALUR ALUA
GPG WMEM
ALUR WOFST
ALUR ALUA
GPH WMEM
ALUR WOFST
ALUR ALUA

//ALU was stored in DMEM temporarily
DMEM00 WMEM
ALUR WOFST
ALUR ALUA
DMEM04 WMEM
ALUR WOFST
ALUR ALUA
DMEM08 WMEM
ALUR WOFST
ALUR ALUA

FPUM WMEM
ALUR WOFST
ALUR ALUA
FPUA WMEM
ALUR WOFST
ALUR ALUA
FPUB WMEM
ALUR WOFST
ALUR ALUA

RBASE WMEM
ALUR WOFST
ALUR ALUA
ROFST WMEM
ALUR WOFST
ALUR ALUA

COMPA WMEM
ALUR WOFST
ALUR ALUA
COMPB WMEM
ALUR WOFST
ALUR ALUA

LINK WMEM
ALUR WOFST
ALUR ALUA

//push frame pointer on to stack
WBASE WMEM
ALUR WOFST

//add a stack frame
WBASE ALUA
WOFST ALUB
ALUR WBASE
00 WOFST

SKIP LINK
GPA PC
00 00 //NOP so SKIP points to the correct address

//return from target function

//retrieve frame pointer from stack
//subtract 4 from the current frame pointer to
//get the last item on the stack (the last FP)
WBASE ALUA
04 ALUB
03 ALUM //subtract mode
ALUR WBASE //set WBASE to last stack slot
00 WOFST //(clear WOFST)
WMEM WBASE //FP is the last thing in the stack

ALUR ALUA //ALUR still holds the top stack slot, equivalent to (old FP + old SP)
WBASE ALUB
//subtract last FP (WBASE) from last FP + SP (ALUR) to get last SP
ALUR WOFST //store last SP in WOFST
//WBASE now holds old FP and WOFST now holds old SP
//FP and SP are now current

//set up ALU for unstacking
WOFST ALUA //FP to ALU
04 ALUB //ALU is in -4 mode

//pop last FP from the stack, don't store because it's already in WBASE
ALUR WOFST
ALUR ALUA

//pop caller save registers from stack
WMEM LINK
ALUR WOFST
ALUR ALUA

WMEM COMPB
ALUR WOFST
ALUR ALUA
WMEM COMPA
ALUR WOFST
ALUR ALUA

WMEM ROFST
ALUR WOFST
ALUR ALUA
WMEM RBASE
ALUR WOFST
ALUR ALUA

WMEM FPUB
ALUR WOFST
ALUR ALUA
WMEM FPUA
ALUR WOFST
ALUR ALUA
WMEM FPUM
ALUR WOFST
ALUR ALUA

//store ALU in DMEM while we're still using it
WMEM DMEM08 //ALUB
ALUR WOFST
ALUR ALUA
WMEM DMEM04 //ALUA
ALUR WOFST
ALUR ALUA
WMEM DMEM00 //ALUM
ALUR WOFST
ALUR ALUA

WMEM GPH
ALUR WOFST
ALUR ALUA
WMEM GPG
ALUR WOFST
ALUR ALUA
WMEM GPF
ALUR WOFST
ALUR ALUA
WMEM GPE
ALUR WOFST
ALUR ALUA
WMEM GPD
ALUR WOFST
ALUR ALUA
WMEM GPC
ALUR WOFST
ALUR ALUA
WMEM GPB
ALUR WOFST
ALUR ALUA
WMEM GPA

//finished with ALU, restore it from DMEM
//TODO this can be made slightly more efficient by just doing ALU last
DMEM00 ALUM
DMEM04 ALUA
DMEM08 ALUB

//return to the caller
LINK PC
/////////////////////////////////////////////////
