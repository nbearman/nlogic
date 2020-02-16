using System;
using System.Diagnostics;
using System.IO;

namespace nlogic_sim
{
    public class TestProgram
    {
        public const uint TEST_HALT_CODE = 0x0000007F;

        public static void run_tests(string test_file_path)
        {
            Console.WriteLine("\nRunning tests...\n____________________________\n");
            string directory_path = Path.GetDirectoryName(test_file_path);
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
                string input_assembly_path = Path.Combine(directory_path, files[0]);
                string expected_output_path = Path.Combine(directory_path, files[1]);

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

        public static void create_test_file_pair(string test_name, string[] code_file_paths)
        {
            string assembly_output_file_name = test_name + ".assembly";
            string test_output_file_name = test_name + ".testlog";

            //assemble the program
            Assembler.assemble(code_file_paths);

            if (!Assembler.assembled)
            {
                Console.WriteLine("Could not generate test; assembler failed");
                return;
            }

            //run the program with logging
            Console.WriteLine("Setting up environment...");
            SimulationEnvironment environment = new SimulationEnvironment(65536, Assembler.program_data, null);
            environment.enable_logging();
            Console.WriteLine("Running program...");
            environment.run(false, TEST_HALT_CODE);

            //store the assembly
            string assembly_output = Assembler.dump_assembly();
            File_Input.write_file(assembly_output_file_name, assembly_output);

            //store the log output
            string log_output = environment.get_log();
            File_Input.write_file(test_output_file_name, log_output);

            Console.WriteLine(String.Format("Test case generated: {0} and {1}\n", assembly_output_file_name, test_output_file_name));
            return;
        }
    }
}