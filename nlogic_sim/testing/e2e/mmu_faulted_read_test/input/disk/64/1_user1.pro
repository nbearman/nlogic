//=========================================================================
// program 1 entry point
//=========================================================================


// PC is at 0x08 when MMU is enabled, so these won't be executed
//   (unless the breakpoint doesn't work and we jump here instead of
//      program 2)
00 00 // this shouldn't run, since the breakpoint was triggered
    // the other program's code should be running instead
00 00
7F FLAG

// entry point; MMU enabled, we're in VA now
// read an unmapped page to trigger a page fault; the faulted operation register should be 0 (for read)
IADF RBASE
SKIP PC
00 A0 00 00 // unmapped page
00 ROFST
RMEM GPA

// should not reach here
01 FPUA
02 FPUB
03 FPUM
7F FLAG // test case failed; no page fault on read of unmapped page

