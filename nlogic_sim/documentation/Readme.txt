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