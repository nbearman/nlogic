Use the bash script "builder.txt" to run programs:
	create a folder with assembly files
		"pro files", can contain comments, dmem instructions, labels, etc.
	name each file with ".pro" extension
	run "./builder.txt [folder] [-v] [-d]" to assemble the pro files in that folder and run them in the simulator
		add the "-v" flag to run the simulator with the visualizer enabled
		add the "-d" flag to attach the visual studio debugger to the simulator
			allows adding breakpoints, inspecting variables, pause on exception, etc.
