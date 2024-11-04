//system boot

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
// page 1: program 1 page directory
//==========
// page 2: program 1 page table 0
//==========
// page 3: program 1 page 0, 0x00 00 00 00 in prog1 VM
//==========
// page 4: empty
//==========
// page 5: program 2 page directory
//==========
// page 6: program 2 page table 0
//==========
// page 7: program 2 page 0, 0x00 00 00 00 in prog2 VM
//==========
//=========================


//=========================================================================
// intialize program 1 memory
//=========================================================================

//create a PDE in program 1's page directory
IADF WBASE
SKIP PC
00 00 10 00

IADF WMEM
SKIP PC
// R !W physical page 2
C0 00 00 02

//create a PTE in program 1's page table for virtual page 0
//the page table is at physical address 00 00 20 00, so write the PTE there
IADF WBASE
SKIP PC
00 00 20 00

IADF WMEM
SKIP PC
// R W physical page 3
C0 00 00 03
// 80 00 00 03

//add another PTE for virtual page 1
//map it to the MMIO devices (physical address FF 00 00 00)
04 WOFST //(each PTE is 4 bytes, so the next PTE is 00 00 20 04)
IADF WMEM
SKIP PC
80 0F F0 00


//=========================================================================
// initialize program 2 memory
//=========================================================================

//create a PDE in program 2's page directory (page 5)
00 WOFST
IADF WBASE
SKIP PC
00 00 50 00

IADF WMEM
SKIP PC
// R !W physical page 6
C0 00 00 06

//create a PTE in program 2's page table for virtual page 0
//the page table is at physical address 00 00 60 00, so write the PTE there
IADF WBASE
SKIP PC
00 00 60 00

IADF WMEM
SKIP PC
// R W (resident, clean) physical page 7
80 00 00 07

//add another PTE for virtual page 1
//map it to the MMIO devices (physical address FF 00 00 00)
04 WOFST //(each PTE is 4 bytes, so the next PTE is 00 00 20 04)
IADF WMEM
SKIP PC
80 0F F0 00

//=========================================================================
// load program 1 into memory from disk
//=========================================================================

//virtual disk is MMIO device; MMIO devices start at 0xFF 00 00 00
IADF WBASE
SKIP PC
FF 00 00 24

//load into physical page 3
00 WOFST
03 WMEM

//read from disk block 64
04 WOFST
40 WMEM

//set read mode by writing 0
08 WOFST
00 WMEM

//initiate the transfer
0C WOFST
01 WMEM

//=========================================================================
// load program 2 into memory from disk
//=========================================================================

//virtual disk is MMIO device; MMIO devices start at 0xFF 00 00 00
IADF WBASE
SKIP PC
FF 00 00 24

//load into physical page 7
00 WOFST
07 WMEM

//read from disk block 100
04 WOFST
64 WMEM

//set read mode by writing 0
08 WOFST
00 WMEM

//initiate the transfer
0C WOFST
01 WMEM


//=========================================================================
// set MMU page directory base registers
//=========================================================================

//set the active page directory base register to physical page 1 (program 1 page directory)

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
//set the queued page directory base register to physical page 5 (program 2 page directory)
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


//for now, hard code the physical address of the MMU
//also hard code this into the simulation environment
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
//20 faulted operation register

//=========================================================================
//=========================================================================

//shouldn't reach here is MMU is enabled successfully
//halt the processor
7F FLAG

