//IADF WBASE
//SKIP PC
//FF 00 00 14
//
////physical page
//00 WOFST
//01 WMEM
//
////disk block
//04 WOFST
////22 WMEM
//IADF WMEM
//SKIP PC
//00 00 30 39
//
////read/write mode
//08 WOFST
//00 WMEM
//
////initiate
//0C WOFST
//01 WMEM

////////////////////////////
// End test section
////////////////////////////

//kernel boot

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
// page 4: kernel page 1, MMIO devices, 0x00 00 10 00 in kernel VM
//==========
// page 5: user page directory
//==========
// page 6: user page table 0
//==========
// page 7: user page 0, 0x00 00 00 00 in user VM
//==========
//=========================


//=========================================================================
// intialize kernel memory
//=========================================================================

//create a PDE in the kernel's page directory
IADF WBASE
SKIP PC
00 00 10 00

IADF WMEM
SKIP PC
// R !W physical page 2
80 00 00 02

//create a PTE in the kernel's page table for virtual page 0
//the page table is at physical address 00 00 20 00, so write the PTE there
IADF WBASE
SKIP PC
00 00 20 00

IADF WMEM
SKIP PC
// R W physical page 3
C0 00 00 03

//add another PTE for virtual page 1
//map it to the MMIO devices (physical address FF 00 00 00)
04 WOFST //(each PTE is 4 bytes, so the next PTE is 00 00 20 04)
IADF WMEM
SKIP PC
C0 0F F0 00

//=========================================================================
// initialize user memory
//=========================================================================

//create a PDE in the users's page directory (page 5)
IADF WBASE
SKIP PC
00 00 50 00

IADF WMEM
SKIP PC
// R !W physical page 6
80 00 00 02

//create a PTE in the user page table for virtual page 0
//the page table is at physical address 00 00 50 00, so write the PTE there
IADF WBASE
SKIP PC
00 00 60 00

IADF WMEM
SKIP PC
// R !W physical page 7
80 00 00 07

//=========================================================================
// load user program into memory from disk
//=========================================================================

//virtual disk is MMIO device; MMIO devices start at 0xFF 00 00 00
IADF WBASE
SKIP PC
FF 00 00 14

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

//set ALU to add mode (pretty sure this instruction is only here as a sentinel for looking at the visualizer)
01 ALUA

//load address of MMU registers
IADF RBASE
SKIP PC
:MMU_registers

RMEM WBASE

//enable the MMU and see what happens
10 WOFST
01 WMEM

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
//10 enabled

//=========================================================================
//=========================================================================

//end of boot
//halt the processor
7F FLAG

//=========================================================================
// Physical page 3
// Kernel virtual address 0x00 00 00 00
//=========================================================================
FILL3000
//we will land somewhere in here after enabling the MMU
FILL3090

///////////////////////////


33 GPE

//set the MMU VA break point
IADF WBASE
SKIP PC
00 00 10 00
08 WOFST
00 WMEM

00 RBASE
00 ROFST

//jump to 0 in user space
00 PC


///////////////////////////

FILL3200

FILL3300
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
FILL3440
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
FILL3540
