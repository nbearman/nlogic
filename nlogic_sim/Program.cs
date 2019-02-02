using System;
using System.Threading;
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
            string[] code_files = new string[]
            {
                "programs/external_labels1.txt",
                "programs/external_labels2.txt",
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
    }
}
