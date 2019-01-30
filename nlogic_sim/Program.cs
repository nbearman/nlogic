using System;
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
            Assembler.assemble("program.txt");
            string output = Assembler.dump_assembly();
            Console.WriteLine(output);

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
    }
}
