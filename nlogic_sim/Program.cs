using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.RegularExpressions;

namespace nlogic_sim
{
    class Program
    {
        static readonly string[] flag_values = { "debug", "-d", };

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("no command line arguments given; run with 'help' to get options");
                return;
            }

            List<string> arg_list = args.ToList();
            List<string> flags = new List<string>();
            List<string> additional_args = new List<string>();

            //detect and remove all legal global flags from the arguments
            foreach (string f in flag_values)
            {
                int num_removed = arg_list.RemoveAll((arg) => arg == f);
                if (num_removed > 0)
                {
                    flags.Add(f);
                }
            }

            //get the command, the first argument after flags have been stripped
            string command = arg_list[0];
            arg_list.RemoveAt(0);

            //add remaining non-flag arguments to the additional args list
            foreach (string a in arg_list)
            {
                additional_args.Add(a.Replace("\r", ""));
            }

            //debug mode
            //print the process ID and wait for a keypress to give the opportunity to attach the debugger
            if (flags.Contains("-d") || flags.Contains("debug"))
            {
                Console.WriteLine(Process.GetCurrentProcess().Id);
                Debugger.Launch();
            }


            if (command == "help")
            {
                Console.WriteLine("[nlogic sim help]");
                Console.WriteLine("help\n\tthis help prompt");
                Console.WriteLine("run [code files] {visualizer?} {-log [log output location]?}\n\tassemble the given code files;" +
                    "run the program with the visualizer; store state logs to the given location");
                Console.WriteLine("assemble [code files]\n\tassemble the given code files; output the assembly");
                Console.WriteLine("test\n\trun the test program");
                return;
            }

            if (command == "run")
            {
                bool visualizer = false;
                string log_output_filepath = null;

                List<string> codefiles = new List<string>();
                for (int i = 0; i < additional_args.Count(); i++)
                {
                    string aa = additional_args[i];
                    if (aa.ToLower() == "visualizer" || aa.ToLower() == "-v")
                    {
                        visualizer = true;
                    }
                    else if (aa.ToLower() == "log" || aa.ToLower() == "-l")
                    {
                        if (i == additional_args.Count() - 1)
                        {
                            Console.WriteLine("No valid log output path provided");
                            return;
                        }
                        else
                        {
                            //interpret the next argument as the output filepath
                            log_output_filepath = additional_args[i + 1];
                            //skip the next argument
                            i++;
                        }
                    }
                    else
                    {
                        //it's not a flag, so it's a codefile
                        codefiles.Add(aa);
                    }
                }

                if (codefiles.Count() == 0)
                {
                    Console.WriteLine("No code files provided");
                    return;
                }

                assemble_and_run(codefiles.ToArray(), visualizer, log_output_filepath);
                return;
            }

            if (command == "assemble")
            {
                string assemble_time = (DateTime.Now.ToString().Replace(' ', '_').Replace('/', '-').Replace(':', '-'));
                //Assembler.assemble(additional_args.ToArray(), String.Format("{0}_assembler_output.txt", assemble_time));
                Assembler.assemble(additional_args.ToArray(), "assembler_output.txt");
                Console.WriteLine(Assembler.dump_assembly());
                return;
            }

            if (command == "test")
            {
                if (additional_args.Count == 0)
                {
                    Console.WriteLine("No test file provided");
                    return;
                }

                TestProgram.run_tests(additional_args[0]);
                return;
            }

            if (command == "testcase")
            {
                if (additional_args.Count == 0)
                {
                    Console.WriteLine("No test case name provided");
                    return;
                }

                if (additional_args.Count == 1)
                {
                    Console.WriteLine("No code files provided");
                    return;
                }

                string[] code_files = additional_args.GetRange(1, additional_args.Count - 1).ToArray();
                TestProgram.create_test_file_pair(additional_args[0], code_files);
                return;
            }

            if (command == "pro")
            {
                if (additional_args.Count == 0)
                {
                    Console.WriteLine("No pro files provided");
                    return;
                }

                string[] code_files = additional_args.ToArray();
                AssemblerPro.run(code_files);
                return;
            }

            return;
        }

        private static void assemble_and_run(string[] codefiles, bool visualizer_enabled, string logging_file_path)
        {
            ConsoleColor original_color = Console.ForegroundColor;

            //assemble code files
            Assembler.assemble(codefiles);
            if (!Assembler.assembled)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Assembler failed; press any key to end...");
                Console.ForegroundColor = original_color;
                Console.ReadKey();
                return;
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Program assembly successful");

            SimpleVirtualDisplay VIRTUAL_DISPLAY = new SimpleVirtualDisplay();
            VirtualDisk VIRTUAL_DISK = new VirtualDisk("./virtual_disk");

            List<MMIO> mmio_devices = new List<MMIO> { VIRTUAL_DISK, VIRTUAL_DISPLAY };

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write("Connected MMIO devices: ");
            foreach (var mmio in mmio_devices)
                Console.Write(mmio + ", ");
            Console.WriteLine();

            //set up the simulation environment
            SimulationEnvironment environment =
                new SimulationEnvironment(
                    65536,
                    Assembler.program_data,
                    mmio_devices);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Simluation environment setup successful");

            Console.ForegroundColor = original_color;
            Console.WriteLine("Simulation ready; press any key to run...");
            Console.ReadKey();

            //clear the output messages so far
            if (visualizer_enabled)
                Console.Clear();

            //enable logging
            if (logging_file_path != null)
            {
                environment.enable_logging();
            }

            //begin the simulation
            environment.run(visualizer_enabled, 0x0000007F);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Processor halted");
            Console.ForegroundColor = original_color;

            if (logging_file_path != null)
            {
                Console.WriteLine("Writing log file");
                File_Input.write_file(logging_file_path, environment.get_log());
            }

            if (visualizer_enabled)
            {
                Console.WriteLine("Press enter.");
                Console.ReadLine();
            }
            Console.WriteLine("Press any key to end...");
            Console.ReadKey();
            Console.Clear();
            return;
        }

        static void OldMain(string[] args)
        {
            //input code files
            string[] code_files = new string[]
            {
                //"programs/memory_test.txt",
                //"programs/alu_shift_test.txt",
                //"programs/sample_memory_read.txt",
                //"programs/skip_test.txt",
                //"programs/external_labels1.txt",
                //"programs/external_labels2.txt",
                //"programs/comments.txt",
                "programs/log_testing/add_test.txt",
            };


            //assemble code files
            Assembler.assemble(code_files);//, "assembler_output.txt");
            string output = Assembler.dump_assembly();
            Console.WriteLine(output);

            if (!Assembler.assembled)
            {
                Console.WriteLine("assembler failed");
                Console.ReadKey();
                //return;
            }

            SimulationEnvironment environment =
                new SimulationEnvironment(
                    65536,
                    Assembler.program_data,
                    new List<MMIO> { new VirtualDisplay(90, 30) });


            Console.WriteLine("simulation environment setup complete");
            Console.ReadLine();
            Console.Clear();

            environment.run(true, 0xFFFFFFFF);

            Console.WriteLine("processor halted");

            Console.Read();


        }

        static long time_execution(Processor p)
        {
            Stopwatch s = new Stopwatch();
            s.Reset();
            s.Start();

            while (p.cycle() == 0);

            s.Stop();

            return s.ElapsedMilliseconds;
        }

        static void draw_test()
        {
            Console.CursorVisible = false;
            Random random = new Random();

            Stopwatch s = new Stopwatch();
            s.Reset();
            s.Start();
            Thread.Sleep(10);
            s.Stop();
            Console.WriteLine(s.ElapsedMilliseconds);
            Console.ReadKey();


            Stopwatch sw = new Stopwatch();

            int locx = 50;
            int locy = 15;

            while (true)
            {
                locx += random.Next(-1, 2);
                locy += random.Next(-1, 2);
                locx = locx % 100;
                locy = locy % 28;

                sw.Reset();
                sw.Start();

                for (int y = 0; y < 28; y++)
                {
                    Console.SetCursorPosition(0, y);

                    for (int x = 0; x < 100; x++)
                    {
                        //int p = random.Next(0, 10);
                        if (locx == x && locy == y)
                            Console.Write('#');
                        Console.Write(' ');
                    }

                }

                sw.Stop();
                Console.WriteLine("\n" + sw.ElapsedMilliseconds + "    ");
                Console.WriteLine("\n" + locx + "    ");
                Console.WriteLine("\n" + locy + "    ");
                //Thread.Sleep(60);
            }

        }

        static void generate_memory_test()
        {
            string contents = "";
            Random random = new Random();

            for (int i = 0; i < 1000; i++)
            {
                uint address = (uint)random.Next(0x2EE1, 0xFFFC);
                byte[] b = Utility.byte_array_from_uint32(4, address);
                string s = Utility.byte_array_string(b, "\n");
                contents += "IADF\nROFST\nSKIP\nPC\n" + s + "\nRMEM\nGPA\nPC\nRMEM\n";
            }

            File_Input.write_file("memory_test.txt", contents);
        }

        static void array_read_test()
        {
            Stopwatch s = new Stopwatch();
            s.Reset();
            s.Start();
            Thread.Sleep(10);
            s.Stop();
            Console.WriteLine(s.ElapsedMilliseconds);
            Console.ReadKey();

            int memory_size = 65536;
            int reads = 10000;
            Random random = new Random();

            List<long> for_loop_times = new List<long>();
            for (int run = 0; run < 10; run++)
            {
                byte[] memory = make_mock_memory(memory_size);
                byte[] result = new byte[4];

                for (int read = 0; read < reads; read++)
                {
                    int address = random.Next(memory_size - 4);

                    s.Reset();
                    s.Start();
                    for (int i = 0; i < 4; i++)
                    {
                        result[i] = memory[address + i];
                    }
                    s.Stop();
                    for_loop_times.Add(s.ElapsedMilliseconds);
                    Console.WriteLine(s.Elapsed);
                }
            }

            Console.WriteLine("Moving to array take");

            List<long> array_take_times = new List<long>();
            for (int run = 0; run < 10; run++)
            {
                byte[] memory = make_mock_memory(memory_size);
                byte[] result = new byte[4];

                for (int read = 0; read < reads; read++)
                {
                    int address = random.Next(memory_size - 4);

                    s.Reset();
                    s.Start();
                    result = memory.Skip(address).Take(4).ToArray();
                    s.Stop();
                    array_take_times.Add(s.ElapsedMilliseconds);
                    Console.WriteLine(s.Elapsed);
                }
            }

            Console.WriteLine("for loop avg: {0}", for_loop_times.Average());
            Console.WriteLine("arr take avg: {0}", array_take_times.Average());
            Console.ReadKey();
        }

        static byte[] make_mock_memory(int size)
        {
            byte[] result = new byte[size];
            Random random = new Random();
            for (int i = 0; i < size; i++)
            {
                result[i] = (byte)random.Next(255);
            }
            return result;
        }
        
    }

}
