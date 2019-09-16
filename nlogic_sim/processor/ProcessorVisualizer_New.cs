using System;
using System.Linq;
using System.Collections.Generic;

namespace nlogic_sim
{

    public partial class Processor
    {
        /// <summary>
        /// Show the current internal state of the processor
        /// </summary>
        public void _print_current_state()
        {
            print_skeleton();
            throw new NotImplementedException();
        }

        public void initialize()
        {
            //print skeleton
            //populate name fields
            throw new NotImplementedException();
        }

        //the current state of each readout
        private static Dictionary<READOUT, ColorString[]> readout_cache;

        /// <summary>
        /// Write the given ColorStrings to the given READOUT
        /// </summary>
        /// <param name="location">The READOUT to update</param>
        /// <param name="value">Fromatted value to display</param>
        private static void update_readout(READOUT location, ColorString[] value)
        {
            throw new NotImplementedException();
            return;
        }

        /// <summary>
        /// Change the specified readout to display the given value using the location's default styling
        /// </summary>
        /// <param name="location">READOUT to be changed</param>
        /// <param name="value">Unformatted value to display</param>
        private static void update_readout(READOUT location, string value)
        {

            //the memory context readouts cannot be updated with this method
            if (location == READOUT.MemoryContext1 || location == READOUT.MemoryContext2)
            {
                throw new Exception("cannot use update_readout() for memory contexts; " +
                    "use update_memory_context() instead");
            }

            //format the given string
            ColorString[] formatted_value = format_to_readout_style(location, value);

            //update the given readout

            //check the cache to avoid rewriting unchanged data
            if (readout_cache[location].SequenceEqual(formatted_value))
            {
                //no change in value already displayed
                return;
            }

            else
            {
                //the value shown needs to be changed

                //save the previous color and cursor position
                ConsoleColor previous_color = Console.ForegroundColor;
                Tuple<int, int> previous_coordinates =
                    new Tuple<int, int>(Console.CursorLeft, Console.CursorTop);

                //print the formatted string at the correct location
                Tuple<int, int> coordinates = readout_coordinates[location];
                Console.SetCursorPosition(coordinates.Item1, coordinates.Item2);
                foreach (var colorstring in formatted_value)
                    colorstring.print();

                //update the cache
                readout_cache[location] = formatted_value;

                //reset the console color and cursor position
                Console.ForegroundColor = previous_color;
                Console.SetCursorPosition(previous_coordinates.Item1, previous_coordinates.Item2);
            }


        }

        /// <summary>
        /// Print the string at the specified memory_context, using memory context formatting.
        /// Supplying a READOUT other than a memory context will cause an error.
        /// </summary>
        /// <param name="memory_context">READOUT specifying which context to print to</param>
        private static void update_memory_context(READOUT memory_context, uint address, byte[] memory)
        {

            //TODO add caching

            if (!(memory_context == READOUT.MemoryContext1 || memory_context == READOUT.MemoryContext2))
            {
                throw new Exception("the supplied READOUT must be a memory context readout");
            }

            //get the neighboring lines of memory as ColorStrings
            ColorString[][] neighboring_lines = get_neighboring_memory_lines(address, memory);

            //save the previous color and cursor position
            ConsoleColor previous_color = Console.ForegroundColor;
            Tuple<int, int> previous_coordinates = 
                new Tuple<int, int>(Console.CursorLeft, Console.CursorTop);

            Tuple<int, int> start_position = readout_coordinates[memory_context];
            //print all the lines of the context
            for (int i = 0; i < neighboring_lines.Length; i++)
            {
                Console.SetCursorPosition(start_position.Item1, start_position.Item2 + i);
                foreach (var colorstring in neighboring_lines[i])
                    colorstring.print();
            }

            //reset the console color and cursor position
            Console.ForegroundColor = previous_color;
            Console.SetCursorPosition(previous_coordinates.Item1, previous_coordinates.Item2);

        }

        /// <summary>
        /// Returns an array of ColorString arrays representing the region of memory
        /// surrounding the given address.
        /// Each ColorString[] in the returned array is a formatted row of the memory context readout
        /// </summary>
        /// <param name="address">The selected address around which to read memory</param>
        /// <param name="memory">The memory to read from</param>
        /// <returns>An array of ColorString[], each the formatted representation of one row of the
        /// memory context readout.</returns>
        private static ColorString[][] get_neighboring_memory_lines(uint address, byte[] memory)
        {
            ColorString[][] result = new ColorString[8][];

            int line_number = (int)(address / 8);
            int base_line_number = line_number - 3;

            if (base_line_number < 0)
            {
                base_line_number = 0;
            }

            int last_line = ((memory.Length) / 8) - 1;

            if (base_line_number > last_line - 8)
            {
                base_line_number = last_line - 7;
            }

            for (int row = 0; row < 8; row++)
            {
                result[row] = new ColorString[8];
                int addr = ((int)base_line_number + row) * 8;

                for (int col = 0; col < 8; col++)
                {
                    ColorString m = new ColorString();
                    m.value = memory[addr + col].ToString("X2");
                    if ((addr + col) >= address && (addr + col) < (address + 4))
                        m.color = ConsoleColor.White;
                    else
                        m.color = ConsoleColor.DarkGray;
                    result[row][col] = m;
                }
            }

            return result;
        }

        /// <summary>
        /// Returns a string in the format expected by the given readout
        /// </summary>
        /// <param name="location">READOUT which style will be used</param>
        /// <param name="value">String to format</param>
        /// <returns>Formatted ColorString using location's style</returns>
        private static ColorString[] format_to_readout_style(READOUT location, string value)
        {

            List<ColorString> result_list = new List<ColorString>();


            switch (location)
            {
                case (READOUT.ALUA_contents):
                case (READOUT.ALUB_contents):
                case (READOUT.ALUM_contents):
                case (READOUT.ALUR_contents):
                case (READOUT.COMPA_contents):
                case (READOUT.COMPB_contents):
                case (READOUT.COMPR_contents):
                case (READOUT.EXE_contents):
                case (READOUT.FLAG_contents):
                case (READOUT.FPUA_contents):
                case (READOUT.FPUB_contents):
                case (READOUT.FPUM_contents):
                case (READOUT.FPUR_contents):
                case (READOUT.GPA_contents):
                case (READOUT.GPB_contents):
                case (READOUT.GPC_contents):
                case (READOUT.GPD_contents):
                case (READOUT.GPE_contents):
                case (READOUT.GPF_contents):
                case (READOUT.GPG_contents):
                case (READOUT.GPH_contents):
                case (READOUT.IADF_contents):
                case (READOUT.IADN_contents):
                case (READOUT.LINK_contents):
                case (READOUT.PC_contents):
                case (READOUT.RBASE_contents):
                case (READOUT.RMEM_contents):
                case (READOUT.ROFST_contents):
                case (READOUT.RTRN_contents):
                case (READOUT.SKIP_contents):
                case (READOUT.WBASE_contents):
                case (READOUT.WMEM_contents):
                case (READOUT.WOFST_contents):
                    {
                        //standard register contents
                        //take a maximum of 4 doublets
                        //pad string with 2 spaces on both sides
                        ColorString cs = new ColorString();
                        cs.value = format_register(value, 4, 2, "  ", "  ");

                        //white when not "  00 00 00 00  "
                        if (cs.value == "  00 00 00 00  ")
                            cs.color = ConsoleColor.White;
                        //dark gray otherwise
                        else
                            cs.color = ConsoleColor.DarkGray;
                        result_list.Add(cs);
                        break;
                    }
                case (READOUT.CurrentInstruction_contents):
                    {
                        //TODO color instructions conditionally

                        //same as standard register contents
                        //take only 2 doublets
                        ColorString cs = new ColorString();
                        cs.value = format_register(value, 2, 2, "  ", "  ");

                        //white when not "  00 00  "
                        if (cs.value == "  00 00  ")
                            cs.color = ConsoleColor.White;
                        //dark gray otherwise
                        else
                            cs.color = ConsoleColor.DarkGray;
                        result_list.Add(cs);
                        break;
                    }
                case (READOUT.CurrentInstruction_expansion):
                    {
                        //TODO figure out how to format to include "->" and correct spacing
                        //pad string with 1 space to the left
                        string truncated = " " + value.Substring(0, 16);
                        string[] separated = truncated.Split(' ');

                        //create a ColorString for each section of the string
                        foreach (string s in separated)
                        {
                            ColorString cs = new ColorString();
                            cs.value = s;
                            //color known register names with the corresponding color
                            if (Processor.register_name_to_color.Keys.Contains(s))
                                cs.color = Processor.register_name_to_color[s];
                            else
                                cs.color = ConsoleColor.Gray;
                        }
                        break;
                    }
                case (READOUT.ALUM_expansion):
                case (READOUT.FPUM_expansion):
                    //pad string with 1 space on both sides
                    break;
                case (READOUT.ALUA_expansion):
                case (READOUT.ALUB_expansion):
                case (READOUT.ALUR_expansion):
                    //pad string with 1 space on both sides
                    //format int
                    break;
                case (READOUT.FPUA_expansion):
                case (READOUT.FPUB_expansion):
                case (READOUT.FPUR_expansion):
                    //pad string with 1 space on both sides
                    //format float
                    break;
                default:
                    //do nothing
                    result_list.Add(new ColorString(value, ConsoleColor.Gray));
                    break;
            }


            throw new NotImplementedException();

            return result_list.ToArray();
        }

        /// <summary>
        /// Changes the given string to be of the form "[padR]AB CD EF GH[padL]"
        /// </summary>
        /// <param name="value">String to be formatted</param>
        /// <param name="num_groups">Number of groups of characters allowed</param>
        /// <param name="group_size">Size of character groups in characters</param>
        /// <param padL="padL">Padding for the left side</param>
        /// <param name="padR">Padding for the right side</param>
        /// <returns></returns>
        private static string format_register(string value, int num_groups, int group_size, string padL, string padR)
        {
            //standard register contents
            //take a maximum of 4 doublets
            //pad string with 2 spaces on both sides
            string truncated_string = value.Substring(0, Math.Min(value.Length, group_size * num_groups));
            string result = padL;
            for (int i = 0; i < truncated_string.Length; i++)
            {
                //split string into doublets separated by spaces
                result += truncated_string[i];
                if (i % group_size == 0 && i > 0)
                    result += " ";
            }
            result += padR;

            return result;
        }

        /// <summary>
        /// String with color information
        /// </summary>
        private struct ColorString
        {
            public string value;
            public ConsoleColor color;

            public ColorString(string value, ConsoleColor color)
            {
                this.value = value;
                this.color = color;
            }

            public void print()
            {
                Console.ForegroundColor = this.color;
                Console.Write(value);
            }


        }

        /// <summary>
        /// Print the skeleton of the display to the console
        /// </summary>
        private static void print_skeleton()
        {
            Console.WriteLine("|");
            Console.WriteLine("============================================================================================================");
            Console.WriteLine("##||       |               ||##||                     |         |                 ||##");
            Console.WriteLine("######################################################################################");
            Console.WriteLine();
            Console.WriteLine("  ||       |               ||    ||       |               ||    ||       |               || ]>      <[");
            Console.WriteLine("  ||       |               ||    ||       |               ||    ||       |               || [            ]");
            Console.WriteLine("                           ||    ||       |               ||    ||       |               || [            ]");
            Console.WriteLine("  ||       |               ||    ||       |               ||    ||       |               || [            ]");
            Console.WriteLine("  ||       |               ||");
            Console.WriteLine("  ||       |               ||    ||       |               ||    ||       |               || ]>      <[");
            Console.WriteLine("                           ||    ||       |               ||    ||       |               || [            ]");
            Console.WriteLine("                           ||    ||       |               ||    ||       |               || [            ]");
            Console.WriteLine("  ||       |               ||    ||       |               ||    ||       |               || [            ]");
            Console.WriteLine("  ||       |               ||");
            Console.WriteLine("  ||       |               ||");
            Console.WriteLine();
            Console.WriteLine("  ||       |               ||");
            Console.WriteLine("  ||       |               ||");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("_____________________________________________________________________________________________________________");
            Console.WriteLine("  ||       |               ||                           ||       |               ||");
            Console.WriteLine("  ||       |               ||                           ||       |               ||");
            Console.WriteLine("  ||       |               ||                           ||       |               ||");
        }
    }
}