//=========================================================================
// user program
//=========================================================================

// This program tests that a non-resident, mapped page of a program causes
// a page fault and resumes after that page is brought into memory

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
7F FLAG
