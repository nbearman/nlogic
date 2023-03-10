# Operating System

Work in progress. This describes what the OS and disk are currently set up to execute.

## Program
Boot program

Loads the kernel and user programs from disk, creates page tables for the kernel and user processes, enables the MMU, and jumps to the kernel entry point

## Disk

### 64 - Kernel
Kernel program. The entry point basically just jumps to the beginning of the user program. The rest of the code is the page fault handler.

### 100 - User program
User program. The program is just over 1 page long. After boot, the program jumps to the start of the second page, which is mapped but not resident in physical memory. This triggers a page fault so we can debug the page fault handler.

