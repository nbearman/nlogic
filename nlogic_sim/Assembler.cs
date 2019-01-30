using System;
using System.Runtime;
using System.Collections.Generic;

namespace nlogic_sim
{
    class Assembler
    {
        public static byte[] program_data;

        public static string dump_assembly()
        {
            return Utility.byte_array_string(program_data);
        }

        public static void assemble(string filepath)
        {
            ConsoleColor original_color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("|ASSEMBLER");
            Console.ForegroundColor = ConsoleColor.Gray;

            Assembler.print_message("opening file for assembly...", MESSAGE_TYPE.Status);

            bool successful = true;

            string input_string = "";
            try
            {
                input_string = File_Input.get_file_contents(filepath);
            }
            catch (Exception e)
            {
                Assembler.print_message("unable to open file: \t\t" + filepath, MESSAGE_TYPE.Error);
                successful = false;
            }

            string[] split = input_string.Split(new string[]{ " ", "\t", "\n", "\r", Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries);

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
                    //bool result = byte.TryParse(next, out number);
                    bool result = byte.TryParse(next, System.Globalization.NumberStyles.HexNumber, null, out number);

                    if (result)
                    {
                        converted_data[i] = number;

                        if (number >= 0x80)
                        {
                            Assembler.print_message("literal number not < 0x80: \t\ti = " + i, MESSAGE_TYPE.Warning);
                        }
                    }

                    else
                    {
                        Assembler.print_message("unrecognized input \"" + next + "\" : \t\ti = " + i, MESSAGE_TYPE.Error);
                        successful = false;
                    }
                }
            }

            program_data = converted_data;

            if (successful)
            {
                Assembler.print_message("assembly successful", MESSAGE_TYPE.Success);
            }
            else
            {
                Assembler.print_message("assembly failure; output unreliable", MESSAGE_TYPE.Failure);
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("|end ASSEMBLER");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.ForegroundColor = original_color;

        }

        private enum MESSAGE_TYPE
        {
            None = (int)ConsoleColor.Gray,
            Warning = (int)ConsoleColor.Yellow,
            Error = (int)ConsoleColor.Red,
            Failure = (int)ConsoleColor.DarkRed,
            Status = (int)ConsoleColor.Blue,
            Success = (int)ConsoleColor.Green
        }

        private static void print_message(string message, MESSAGE_TYPE message_type = MESSAGE_TYPE.None)
        {
            ConsoleColor original_color = Console.ForegroundColor;
            Console.ForegroundColor = (ConsoleColor)message_type;

            Console.Write("|\t:> ");
            switch (message_type)
            {
                case (MESSAGE_TYPE.Warning):
                    Console.ForegroundColor = (ConsoleColor)MESSAGE_TYPE.Warning;
                    Console.Write("Warning: ");
                    break;
                case (MESSAGE_TYPE.Error):
                    Console.ForegroundColor = (ConsoleColor)MESSAGE_TYPE.Error;
                    Console.Write("Error: ");
                    break;
                case (MESSAGE_TYPE.Failure):
                    Console.ForegroundColor = (ConsoleColor)MESSAGE_TYPE.Failure;
                    Console.Write("Failure: ");
                    break;
                case (MESSAGE_TYPE.Status):
                    Console.ForegroundColor = (ConsoleColor)MESSAGE_TYPE.Status;
                    Console.Write("Status: ");
                    break;
                case (MESSAGE_TYPE.Success):
                    Console.ForegroundColor = (ConsoleColor)MESSAGE_TYPE.Success;
                    Console.Write("Success: ");
                    break;
                default:
                    break;
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);

            Console.ForegroundColor = original_color;
        }
    }
}