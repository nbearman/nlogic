using System;
using System.Diagnostics;

namespace nlogic_sim
{
    public class TestProgram
    {
        public static void main(string[] args)
        {

            string[] test_names = new string[]
            {
                "alu_shift_test",
            };

            //for each test name, get the test assembly and the correct output
            //load the test assembly into the processor
            //test the output against the correct output


            string[] assembly_files = new string[]
            {
                "testing/test_assemblies/no_op.txt",
            };

            ////////////////////////////////////////////////////////////////
            string[] code_files = new string[]
            {
                "programs/log_testing/add_test.txt",

            };

            //return;

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

            environment.run(false, 0x0000007F);

            Console.WriteLine("processor halted");

            Console.Read();
        }
    }
}