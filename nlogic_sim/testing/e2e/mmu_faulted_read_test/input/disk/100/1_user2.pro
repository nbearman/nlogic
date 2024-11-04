//=========================================================================
// program 2 entry point
//=========================================================================
FILL5FE
7F FLAG // failing test case; this address should not be reached

FILL600 // entry point for the kernel fault handler (hardcoded in the simulation environment)
// read from the MMU faulted operation register
IADF RBASE
SKIP PC
00 00 10 00
20 ROFST
RMEM GPA // should be 0 because a read operation caused the fault
7F FLAG // Correct exit for passing test case
