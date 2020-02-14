using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nlogic_sim
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("no command line arguments given; run with 'help' to get options");
                return;
            }

            if (args[0] == "help")
            {
                Console.WriteLine("[nlogic sim help]");
                Console.WriteLine("help\n\tthis help prompt");
                Console.WriteLine("run [code files] {visualizer?} {-log [log output location]?}\n\tassemble the given code files;" +
                    "run the program with the visualizer; store state logs to the given location");
                Console.WriteLine("assemble [code files]\n\tassemble the given code files; output the assembly");
                Console.WriteLine("test\n\trun the test program");
                return;
            }

            if (args[0] == "run")
            {
                //remove the first argument (which was run)
                var additional_args = args.Skip(1).Take(args.Length - 1).ToArray();

                foreach (var a in additional_args)
                {
                    Console.WriteLine(a);
                }


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

            if (args[0] == "assemble")
            {
                //remove the first argument (which was assemble)
                var additional_args = args.Skip(1).Take(args.Length - 1).ToArray();
                string assemble_time = (DateTime.Now.ToString().Replace(' ', '_').Replace('/', '-').Replace(':', '-'));
                Assembler.assemble(additional_args, String.Format("{0}_assembler_output.txt", assemble_time));
                Console.WriteLine(Assembler.dump_assembly());
                return;
            }

            if (args[0] == "test")
            {
                TestProgram.run_tests(args[1]);
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

            //set up the simulation environment
            SimulationEnvironment environment =
                new SimulationEnvironment(
                    65536,
                    Assembler.program_data,
                    new MMIO[] { new VirtualDisplay(90, 30) });

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Simluation environment setup successful");

            Console.ForegroundColor = original_color;
            Console.WriteLine("Simulation ready; press any key to run...");
            Console.ReadKey();

            //clear the output messages so far
            if (visualizer_enabled)
                Console.Clear();

            //begin the simulation
            environment.run(visualizer_enabled, 0x0000007F);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Processor halted");
            Console.ForegroundColor = original_color;
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
                    new MMIO[] { new VirtualDisplay(90, 30) });


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
        
    }

}
