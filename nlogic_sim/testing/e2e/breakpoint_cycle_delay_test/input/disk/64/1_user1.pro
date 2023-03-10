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
00 RBASE
00 ROFST

01 GPA
02 GPB

//set the MMU VA break point
IADF WBASE
SKIP PC
00 00 10 00 //MMIO base address (in VA)
08 WOFST //breakpoint register
00 WMEM //breakpoint at 0

//set the breakpoint cycle delay
1C WOFST // breakpoint cycle delay counter register
04 WMEM // set a delay of 4 cycles

//enable the breakpoint
14 WOFST //breakpoint enabled register
01 WMEM //non-zero -> enabled

// [cycle - counter @ 3]
RMEM GPD // read from the breakpoint address (0x00)
// this shouldn't trigger the breakpoint, since the delay counter is > 0

// [cycle - counter @ 2]

03 GPC // this will be executed, since the breakpoint was skipped

// [cycle - counter @ 1]
//jump to 0 in user space
RMEM PC // breakpoint is not hit here, since the counter > 0 when RMEM (0x00) is accessed

// [cycle - counter @ 0; breakpoint enabled]
// instruction fetch after this should trigger the breakpoint, since we access 0x00
