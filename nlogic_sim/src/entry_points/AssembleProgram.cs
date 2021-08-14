using System;

namespace nlogic_sim
{
    public class AssembleProgram
    {
        public static void _Main(string[] args)
        {
            const string OUTPUT_FILEPATH = "assembler_output/add_test_assembly.txt";

            string[] input_files = new string[]
            {
                "programs/log_testing/add_test.txt",
            };

            Assembler.assemble(input_files);

            if (!Assembler.assembled)
            {
                Console.WriteLine("assembler failed");
                Console.ReadKey();
            }

            else
            {
                File_Input.write_file(OUTPUT_FILEPATH, Assembler.dump_assembly());
                Console.WriteLine("assembler succeeded; wrote output to " + OUTPUT_FILEPATH);
                Console.ReadKey();
            }
        }
    }
}