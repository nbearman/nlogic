using System;
using System.Diagnostics;

namespace nlogic_sim
{
    public class TestProgram
    {
        public const uint TEST_HALT_CODE = 0x0000007F;

        public static void run_tests(string test_file_path)
        {
            Console.WriteLine(test_file_path);
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

            Console.WriteLine("All tests run");
            return;
        }

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