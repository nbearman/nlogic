using System;
using System.Diagnostics;

namespace nlogic_sim
{
    public class TestProgram
    {
        public const uint TEST_HALT_CODE = 0x0000007F;

        public static void run_tests(string test_file_path)
        {
            Console.WriteLine("\nRunning tests...\n____________________________\n");
            string test_file_contents = File_Input.get_file_contents(test_file_path);

            //each line represents a pair of files
            //  "
            //  assembly_file.txt expected_output.txt
            //  assembly_file2.txt expected_output2.txt
            //  "
            string[] test_pairs = test_file_contents.Split(new string[] { "\n", "\r\n"}, StringSplitOptions.RemoveEmptyEntries);

            //for each pair of files, simulate the given program and compare its output (processor state logs) to the given expected output
            foreach (var tp in test_pairs)
            {
                //split the line into the two file paths
                string[] files = tp.Split(' ');
                string input_assembly_path = files[0];
                string expected_output_path = files[1];

                //create the program data from the input assembly file
                string assembly_bytes_string = File_Input.get_file_contents(input_assembly_path);
                byte[] program_data = Utility.byte_array_from_string(assembly_bytes_string);

                //load the disassembly for debugging
                //TODO make this process better
                Assembler.program_data = program_data;
                Assembler.disassembly = Assembler.generate_disassembly();

                //prepare and run the simulation with the program data
                SimulationEnvironment environment = new SimulationEnvironment(65536, program_data, null);
                environment.enable_logging();
                environment.run(false, TEST_HALT_CODE);
                string actual_output = environment.get_log();

                //get the expected output from the file
                string expected_output = File_Input.get_file_contents(expected_output_path);

                //compare the output
                if (expected_output != actual_output)
                {
                    Console.WriteLine(String.Format("Failed test {0}", input_assembly_path));
                }
            }

            Console.WriteLine("\n____________________________");
            Console.WriteLine("All tests run\n");
            return;
        }
    }
}