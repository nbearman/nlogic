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
//=========================

//create a PTE in the kernel's page directory
IADF WBASE
SKIP PC
00 00 10 00

IADF WMEM
SKIP PC
// R !W physical page 2
80 00 00 02

//create a PTE in the kernel's page directory
IADF WBASE
SKIP PC
00 00 20 00

IADF WMEM
SKIP PC
// R !W physical page 3
80 00 00 03

//set the page table base registers to both be physical page 1

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
01 WMEM


IADN PC
::ENABLE_MMU

//sentinel region
//catch bad behavior
7F 7F 7F 7F
7F 7F 7F 7F
7F 7F 7F 7F
7F 7F 7F 7F
7F 7F 7F FLAG


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


FILL3000
FILL3100
01 FPUM
02 FPUA
03 FPUB
FPUR GPA
