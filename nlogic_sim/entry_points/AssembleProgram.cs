using System;

namespace nlogic_sim
{
    public class AssembleProgram
    {
        public static void main(string[] args)
        {
            const string OUTPUT_FILEPATH = "alu_shift_test_assmebly.txt";

            string[] input_files = new string[]
            {
                "programs/alu_shift_test.txt",
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