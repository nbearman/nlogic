using System;
using System.Diagnostics;

namespace nlogic_sim
{
    public class TestProgram
    {
        public static void run_tests(string test_file_path)
        {
            Console.WriteLine(test_file_path);
            string test_file_contents = File_Input.get_file_contents(test_file_path);
            string[] test_pairs = test_file_contents.Split(new string[] { "\n", "\r\n"}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var tp in test_pairs)
            {
                string[] files = tp.Split(' ');
                Assembler.load_assembly_from_file(files[0]);
                SimulationEnvironment environment = new SimulationEnvironment(65536, Assembler.program_data, null);
                environment.enable_logging();
                environment.run(false, 0x0000007F);
                string actual_output = environment.get_log();

                string expected_output = File_Input.get_file_contents(files[1]);
                if (expected_output != actual_output)
                {
                    Console.WriteLine(String.Format("Failed test {0}", files[0]));
                }
            }

            Console.WriteLine("All tests run");
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