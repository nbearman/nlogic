using System;
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
            //draw_test();
            //Console.ReadKey();
            //return;

            string[] code_files = new string[]
            {
                "programs/sample_memory_read.txt",
            };
            Assembler.assemble(code_files);
            string output = Assembler.dump_assembly();
            Console.WriteLine(output);

            if (!Assembler.assembled)
            {
                Console.ReadKey();
                return;
            }

            Processor p = new Processor();
            for (int i = 0; i < Assembler.program_data.Length; i++)
            {
                p.memory[i] = Assembler.program_data[i];
            }

            p.print_current_state();

            while (true)
            {
                Console.ReadKey();
                p.cycle();
                p.print_current_state();
            }


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
        
    }

}
