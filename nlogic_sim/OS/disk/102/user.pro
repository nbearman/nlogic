//=========================================================================
// user program
//=========================================================================

// This program tests that a non-resident, mapped page of a program causes
// a page fault and resumes after that page is brought into memory

01 ALUM
12 GPB
GPB GPC
GPC ALUA
02 ALUB
ALUR GPB

IADN PC
00 00 0F FC

FILLFFC
00 00 //BREAK can go here to inspect before page fault
11 GPG

//page boundary, trigger page fault on instruction fetch
12 GPF
13 GPE

BREAK
01 GPA // syscall == map page
IADF GPB
SKIP PC
00 00 00 02 // vpage number to map
IADF FLAG
SKIP PC
08 00 00 00 // test kernel interrupt from user program

// attempt to read an unmapped page
IADF RBASE
SKIP PC
00 00 20 00
00 ROFST
// fatal page fault; unmapped page
// RMEM GPA // uncomment to test

// attempt to read an unmapped page within an unmapped page table
IADF RBASE
SKIP PC
10 00 20 00
// fatal page fault; unmapped page table
// RMEM GPA // uncomment to test

// attempt to write to a clean page
IADF WBASE
SKIP PC
00 00 0A 00
00 WOFST
// page fault; clean page; page fault handler will mark it as dirty
11 WMEM

22 FPUA
33 FPUB
FMUL FPUM



// halt, end of test
7F FLAG
