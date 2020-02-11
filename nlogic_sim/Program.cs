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
            //OldMain(args);
            //TestProgram.main(args);
            AssembleProgram.main(args);

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
                "programs/external_labels1.txt",
                "programs/external_labels2.txt",
                //"programs/comments.txt",
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

            environment.run(true, true, 0xFFFFFFFF);

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
