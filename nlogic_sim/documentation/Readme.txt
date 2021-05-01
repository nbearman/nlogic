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
    run "./disk_builder_python.txt [folder] to fully assemble all programs in "[folder]" and populate the virtual disk
    the virtual disk will be output to a folder called "virtual_disk"
        contains a ".txt" file for each disk block
        each file is named by its disk block number, e.g. "20.txt" for block 20
        each file contains assembled binary
            each file may only be a partial program (up to 4096 bytes of a program)
    outputs a folder called "DISK_DEBUG"
        contains a folder for each original numbered folder
            each folder contains a debug annotated version of the source contained in the corresponding original numbered folder
