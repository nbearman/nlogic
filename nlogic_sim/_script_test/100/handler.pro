//=========================================================================
// kernel entry point
//=========================================================================
@first_line
FILL3000
FILL3090
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
FILL3200
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
08 ALUM //right shift mode
ALUR COMPA
00 COMPB //if flag channel is 1, jump
COMPR PC
:non_mmu_interrupt
:mmu_interrupt
//00 00 03 00
//00 00 03 02

//FILL3300
@non_mmu_interrupt
//if interrupt is not from MMU
//do nothing if the interrupt is from anywhere besides the MMU
7F FLAG

@mmu_interrupt
//else interrupt is from MMU
//retrieve the faulted PTE from the MMU
7F GPB
7F FLAG

IADF RBASE
SKIP PC
00 00 10 00
0C ROFST
RMEM GPA




02 FPUM
11 FPUA
22 FPUB
FPUR GPC
7F FLAG

FILL3600
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
FILL3840
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
FILL3940
