using System;
using System.Runtime;
using System.Collections.Generic;

namespace nlogic_sim
{
    class Assembler
    {
        public static void assemble(string filepath)
        {
            string input_string = File_Input.get_file_contents(filepath);
            string[] split = input_string.Split(new char[]{ ' ', '\t', '\n', '\n'});

            byte[] converted_data = new byte[split.Length];

            string next = "";
            for (int i = 0; i < split.Length; i++)
            {
                next = split[i];
                if (Utility.register_name_to_location.ContainsKey(next))
                {
                    converted_data[i] = Utility.register_name_to_location[next];
                }

                else
                {
                    byte number;
                    bool result = byte.TryParse(next, out number);
                    if (result)
                    {
                        if (number < 0x80)
                        {
                            converted_data[i] = number;
                        }

                        else
                        {
                            Console.WriteLine("Error, literal number not < 0x80: i = " + i);
                        }
                    }

                    else
                    {
                        Console.WriteLine("Error, unrecognized input: i = " + i);
                    }
                }
            }

        }
    }
}