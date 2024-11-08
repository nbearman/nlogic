﻿=====================================
Go
=====================================

Use the bash script "go.sh" to build programs then run them on the simulator
    Requires exporting 2 environment variables:
        SIM_EXE: file location of nlogic_sim.exe (simulator)
        PY_ASSEMBLER: file location of AssembleDebug.py (debug assembler)
    e.g.: ```
        cd tools/
        export PY_ASSEMBLER=$(realpath AssembleDebug.py)
        export SIM_EXE=$(realpath ../bin/Debug/nlogic_sim.exe)
        ./go.sh ../programs/OS/ -v -d
    ```

    run "./go.sh [program folder] [-v] [-d]" to fully build the disk and boot program and run the simulator
        [program folder] should be the path to directory with "disk" and "program" subdirectories
            disk subdirectory should contain an unbuilt virtual disk layout
            program subdirectory should contain an unbuilt boot program (pro files)
    use the guide in "Debug Build" section for hohw to configure the disk and program folders


=====================================
Test
=====================================

Use the bash script "run_test_case.sh" to create or run an end-to-end test
    Requires exporting 2 environment variables:
        SIM_EXE: file location of nlogic_sim.exe (simulator)
        PY_ASSEMBLER: file location of AssembleDebug.py (debug assembler)
    Hint: use `realpath` command to get full paths to files to avoid errors

    create a test case, which is a folder containing an "input" folder
        the "input" folder should have a "program" folder and a "disk" folder
            the "program" folder contains any number of .pro files to be compiled into the boot program
            the "disk" folder contains files to be built into a virtual disk
                these will be built with disk_builder_python, so follow the instructions for that script's input
            for tests that don't need any data on disk, the "disk" folder can be omitted
    run "./run_test_case.sh [test case folder] --generate" to generate the expected output
        the program and disk will be built and run in the simulator
        the debug builds of all files, the assembled programs, and the CPU state logs will be saved
        these files and logs will be the "expected" output of future runs of this test case
    run "./run_test_case.sh [test case folder]" to run the test case
        the program and disk will be built and run in the simulator
        the debug builds, assembled programs, and state logs will be compared with those saved in the "expected" folder
        if there are any differences, the test fails

Use the bash script "test_all.sh" to run all end-to-end test cases in a directory
    Requires exporting 3 environment variables:
        SIM_EXE: file location of nlogic_sim.exe (simulator)
        PY_ASSEMBLER: file location of AssembleDebug.py (debug assembler)
        TEST_SH: file location of run_test_case.sh (test case runner)
    Hint: use `realpath` command to get full paths to files to avoid errors

    run "./test_all.sh [test directory]" to run all tests in the directory
        runs all test cases in the test directory
        each test case should be a directory that has been run with "./run_test_case.sh [folder] --generate"

=====================================
Original build scripts
=====================================


Use the bash script "builder.txt" to run programs:
	create a folder with assembly files
		"pro files", can contain comments, dmem instructions, labels, etc.
	name each file with ".pro" extension
	run "./builder.txt [folder] [-v] [-d]" to assemble the pro files in that folder and run them in the simulator
		add the "-v" flag to run the simulator with the visualizer enabled
		add the "-d" flag to attach the visual studio debugger to the simulator
			allows adding breakpoints, inspecting variables, pause on exception, etc.

Use the bash script "disk_builder.txt" to create a virtual disk:
    assembles individual programs from pro files, breaks them into disk block files, moves them into virtual_disk folder
    create a folder called "virtual_disk"
    create a folder containing folders named as numbers (e.g. "20", "100", "210")
        each folder can contain ".pro" assembly files
        the files within a single folder will be combined into a single assembled program
            therefore, one program will be assembled for each numbered folder
        NOTE: the numbered names of the folders correspond to the disk block that the program will START AT
            programs may be larger than 1 disk block, and will therefore overwrite programs that are not spaced far enough apart
            1 disk block is 4096 bytes, so if finished assembled programs are each 8KB, each numbered folder should be at least 2 away from any other
                e.g. folders should be "20" and "22", because "20" and "21" are too close to each other
    run "./disk_builder [folder]" to fully assemble all programs in "[folder]" and populate the virtual disk




=====================================
Debug Build
=====================================

Use the bash script "builder_python.txt" to run programs and create a debug annotated version of the source
    create a folder with assembly files
        "pro files", can contain comments, dmem instructions, labels, fills, etc.
    name each file with a ".pro" extension
    run "./builder_python.txt [folder] [-v] [-d]" to assemble the pro files in that folder and run them as one program in the simulator
        add the "-v" flag to run the simulator with the visualizer enabled
        add the "-d" flag to attach the visual studio debugger to the simulator
            allows adding C# breakpoints, inspecting variables, pause on exception, etc.
        outputs a folder called "BUILD_DEBUG"
            contains the fully assembled binary called "program.asm"
            contains a folder matching the input folder's name and debug versions of each source file, with a ".debug" extension
                debug version of source files have all lines prefixed with the final virtual address of each instruction

Use the bash script "disk_builder_python.txt" to create a virtual disk and debug versions of all source files:
    assembles individual program from pro files, breaks them into disk block files, moves them into virtual_disk folder
    create a folder containing folders named as numbers (e.g. "20", "100", "210")
        each folder can contain ".pro" assembly files
        the files within a single folder will be combined into a single assembled program
            therefore, one program will be assembled for each numbered folder
        the numbered names of the folders correspond to the disk block that the program will START at
            programs may be larger than 1 disk block, and will therefore overwrite programs that are not spaced far enough aprt
            1 disk block is 4096 bytes, so if finished assembled programs are each 8KB, each numbered folder should be at least 2 away from any other
                e.g. folders should be "20" and "22", because "20" and "21" are too close to each other
    run "./disk_builder_python.txt [folder]" to fully assemble all programs in "[folder]" and populate the virtual disk
    the virtual disk will be output to a folder called "virtual_disk"
        contains a ".txt" file for each disk block
        each file is named by its disk block number, e.g. "20.txt" for block 20
        each file contains assembled binary
            each file may only be a partial program (up to 4096 bytes of a program)
    outputs a folder called "DISK_DEBUG"
        contains a folder for each original numbered folder
            each folder contains a debug annotated version of the source contained in the corresponding original numbered folder


AssembleDebug.py
    build .pro files into an assembled program
    run "python3 AssembleDebug.py [File names] [-p] [-o]" to assemble the give files into a program and produce debug annotated versions of source files
        add the "-p" flag to print the fully assembled program to stdout
        add the "-o" to specify an output directory for the debug annotated source files
            uses or creates a folder with the name specified after "-o"
            folder contains annotated source files with ".debug" extensions corresponding to original source files
    run "python3 AssembleDebug.py --help" to see the usage guide
