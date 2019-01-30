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
            p.print_current_state();
            
            Console.Read();
        }
    }
}
