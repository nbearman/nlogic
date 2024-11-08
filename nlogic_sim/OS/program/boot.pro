//kernel boot

//=========================================================================
// Entry point / MMU departure point
//=========================================================================
//This instruction, physical address 0, will be run twice before we enable virtual addressing
//The first time is as the very first instruction; in this case, it will basically be a NOP
//The second time, we want to write a value to the MMU in order to enable it
//We want the PC to be as close to 0 as possible when the MMU is enabled so that the kernel
//entry point in VA can be close to 0 (to avoid wasting space and confusing FILL macros)
//Therefore, we put the final instruction we want to run before the MMU is enabled at 0x00
//	We will jump back here after completing the rest of boot and preparing WMEM to point to the MMU
//	That means these instructions will be run twice: once "on boot," and once "on jumpback"

//The last instruction we need to run before starting the MMU is a write to WMEM
//On jumpback, WMEM will be pointed to the MMU, but on boot, WMEM is pointed at 0x00
//	Therefore, on boot, the WMEM write will overwrite the first 2 instructions
//	They must be NOPs to avoid erasing any useful code
	00 00 00 00
//This is the final instruction we need to execute to enable the MMU
//On boot, this will overwrite the previous 4 bytes (noted above)
//On jumpback, this will enable the MMU, and we will continue executing in VA
	01 WMEM

//On jumpback, execution will not reach here; the MMU is enabled and we are no longer executing from PPage 0
//On boot, continue with the real initialization process
//	All instructions from here forward are therefore only executed on boot (no double execution from jumpback)

//=========================
// physcial memory layout
//==========
// page 0: [here] boot sequence
//==========
// page 1: kernel page directory
//==========
// page 2: kernel page table 0
//==========
// page 3: kernel page 0, 0x00 00 00 00 in kernel VM
//==========
// page 4: empty
//==========
// page 5: user page directory
//==========
// page 6: user page table 0
//==========
// page 7: user page 0, 0x00 00 00 00 in user VM
//==========
// page 8-15: empty
//=========================

//To initialize memory, load a bunch of page directories, page tables, and programs from disk
//Point the WMEM accessor to the virtual disk MMIO device
//Virtual disk is MMIO device starting at 0xFF 00 00 20
// (MMU is first MMIO device, starts at 0xFF 00 00 00)
IADF WBASE
SKIP PC
FF 00 00 24

//=========================================================================
// load kernel page directory into memory from disk
//=========================================================================

// 0x00: physical page destination (physical page 1)
00 WOFST 01 WMEM
// 0x04: disk block source (disk block 64)
04 WOFST 40 WMEM
// 0x08: read/write mode (set read mode)
08 WOFST 00 WMEM
// 0x0C: start transfer
0C WOFST 01 WMEM

//=========================================================================
// load kernel page table 0 into memory from disk
//=========================================================================

// 0x00: physical page destination (physical page 2)
00 WOFST 02 WMEM
// 0x04: disk block source (disk block 65)
04 WOFST 41 WMEM
// 0x08: read/write mode (set read mode)
08 WOFST 00 WMEM
// 0x0C: start transfer
0C WOFST 01 WMEM


//=========================================================================
// load user page directory into memory from disk
//=========================================================================

// 0x00: physical page destination (physical page 5)
00 WOFST 05 WMEM
// 0x04: disk block source (disk block 100)
04 WOFST 64 WMEM
// 0x08: read/write mode (set read mode)
08 WOFST 00 WMEM
// 0x0C: start transfer
0C WOFST 01 WMEM

//=========================================================================
// load user page table 0 into memory from disk
//=========================================================================

// 0x00: physical page destination (physical page 6)
00 WOFST 06 WMEM
// 0x04: disk block source (disk block 101)
04 WOFST 65 WMEM
// 0x08: read/write mode (set read mode)
08 WOFST 00 WMEM
// 0x0C: start transfer
0C WOFST 01 WMEM

//=========================================================================
// load kernel program into memory from disk
//=========================================================================

// 0x00: physical page destination (physical page 3)
00 WOFST 03 WMEM
// 0x04: disk block source (disk block 66)
04 WOFST 42 WMEM
// 0x08: read/write mode (set read mode)
08 WOFST 00 WMEM
// 0x0C: start transfer
0C WOFST 01 WMEM

//=========================================================================
// load user program into memory from disk
//=========================================================================

// 0x00: physical page destination (physical page 7)
00 WOFST 07 WMEM
// 0x04: disk block source (disk block 102)
04 WOFST 66 WMEM
// 0x08: read/write mode (set read mode)
08 WOFST 00 WMEM
// 0x0C: start transfer
0C WOFST 01 WMEM


//=========================================================================
// set MMU page directory base registers
//=========================================================================

//set the active page directory base register to physical page 1 (kernel page directory)

//load address of MMU registers into WBASE
IADF RBASE
SKIP PC
:MMU_registers
RMEM WBASE
//set offset to active page directory register
00 WOFST
01 WMEM

//set offset to queued page directory register
04 WOFST
//set the queued page directory base register to physical page 5 (user page directory)
05 WMEM

//=========================================================================
// enable MMU
//=========================================================================

@@ENABLE_MMU

//load address of MMU registers
IADF RBASE
SKIP PC
:MMU_registers

RMEM WBASE

//enable the MMU and see what happens
//point WMEM at the correct MMU register
	18 WOFST
//this is the instruction that we want to run, but we want PC to be closer to 0
	//This instruction is at the beginning of physical memory, so jump there to execute it
	//(more explanation around that instruction at 0x00)
	//To start the MMU, we would execute: 01 WMEM

//jump back to the start of memory, where we will actually enable the MMU
00 PC

//=========================================================================
//=========================================================================



//=========================================================================
// constants
//=========================================================================


//for now, hard code the physical address of the MMU
//also hard code this into the simulation environment
//TODO figure out how to choose an address for the MMU and communicate it to the kernel during boot
@MMIO_devices
@MMU_registers
FF 00 00 00
//00 active page dir base addr
//04 queued page dir base addr
//08 virtual addr mmu breakpoint
//0C faulted pte
//10 fautled addr
//14 breakpoint enabled
//18 enabled
//1C breakpoint cycle delay counter
//20 faulted operation (1 for write / 0 for read)

//=========================================================================
//=========================================================================

//end of boot
//halt the processor
7F FLAG

//=========================================================================
// Physical page 3
// Kernel virtual address 0x00 00 00 00
//=========================================================================
//=========================================================================
// kernel entry point
//=========================================================================


//=========================================================================
// interrupt handler
//=========================================================================

//=========================================================================
// user program
//=========================================================================
