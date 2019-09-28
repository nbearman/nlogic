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
            //test_processor_visualizer.run();
            //return;

            //generate_memory_test();
            //Console.ReadKey();
            //return;

            //var original_out = Console.Out;
            //var file_output = new StreamWriter("output.txt");
            //Console.SetOut(file_output);
            //Console.WriteLine("out putting to out put");
            //Console.SetOut(original_out);
            //file_output.Close();
            //Console.WriteLine("Done.");
            //Console.ReadKey();
            //return;


            //input code files
            string[] code_files = new string[]
            {
                //"programs/memory_test.txt",
                //"programs/alu_shift_test.txt",
                "programs/fptest.txt",
            };


            //assemble code files
            Assembler.assemble(code_files, "assembler_output.txt");
            string output = Assembler.dump_assembly();
            Console.WriteLine(output);

            if (!Assembler.assembled)
            {
                Console.WriteLine("assembler failed");
                Console.ReadKey();
                return;
            }


            /////////////////////////////////////////////////////////////////////////
            //automatic benchmark execution
            //int runs = 1000;
            //long sum = 0;

            //for (int r = 0; r < runs; r++)
            //{

            //    Processor p = new Processor(new MMIO[] {new VirtualDisplay(90, 30)});
            //    for (int i = 0; i < Assembler.program_data.Length; i++)
            //    {
            //        p.memory[i] = Assembler.program_data[i];
            //    }


            //    sum += time_execution(p);
            //}

            //double avg = (double)sum / (double)runs;
            //Console.WriteLine("average time: " + avg + " ms");

            //Console.ReadKey();
            //return;

            /////////////////////////////////////////////////////////////////////////
            //manual execution

            Processor p = new Processor(new MMIO[] { new VirtualDisplay(90, 30) });
            //load program into memory
            for (int i = 0; i < Assembler.program_data.Length; i++)
            {
                p.memory[i] = Assembler.program_data[i];
            }

            Console.Clear();
            p.initialize_visualizer();
            p.print_current_state();

            while (((Register_32)p.registers[Processor.FLAG]).data == 0)
            {
                Console.ReadKey();
                p.cycle();
                Stopwatch s = new Stopwatch();
                s.Reset();
                s.Start();
                p.print_current_state();
                s.Stop();
                Console.Write(s.ElapsedMilliseconds);
            }


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
