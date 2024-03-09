//=========================================================================
// kernel global variables
//=========================================================================
@@ACTIVE_PROCESS_ID
00 00 00 02
@@ACTIVE_PROCESS_PAGE_DIRECTORY_PHYSICAL_PAGE
00 00 00 05
//=========================================================================
// kernel stack begins after all kernel code
//=========================================================================
//TODO this will need to be moved as kernel code stretches into VA
//  this probably technically doesn't have to be FILLed; it can just be
//  placed automatically, as long as this file comes after all other kernel
//  kernel code files
//  In real systems, the stack would start at the end of VA space and grow down
//      to do this, we would need to change kernel to use an upside-down stack
FILL0F00
@@KERNEL_STACK
