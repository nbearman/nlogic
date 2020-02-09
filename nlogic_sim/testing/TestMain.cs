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
        }
    }
}