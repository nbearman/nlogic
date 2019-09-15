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

        //the location of the readout within the console window
        private static Dictionary<READOUT, Tuple<int, int>> readout_coordinates = new Dictionary<READOUT, Tuple<int, int>>
        {
            { READOUT.Header, new Tuple<int, int>(0, 1) },
            ////////////////////////////////////////////////////////////////////////////
            { READOUT.FLAG, new Tuple<int, int>(4, 2) },
            { READOUT.FLAG_contents, new Tuple<int, int>(12, 2) },
            { READOUT.CurrentInstruction, new Tuple<int, int>(33, 2) },
            { READOUT.CurrentInstruction_contents, new Tuple<int, int>(55, 2) },
            { READOUT.CurrentInstruction_expansion, new Tuple<int, int>(65, 2) },
            ////////////////////////////////////////////////////////////////////////////
            { READOUT.EXE, new Tuple<int, int>(4, 5) },
            { READOUT.EXE_contents, new Tuple<int, int>(12, 5) },
            { READOUT.GPA, new Tuple<int, int>(35, 5) },
            { READOUT.GPA_contents, new Tuple<int, int>(43, 5) },
            { READOUT.ALUM, new Tuple<int, int>(66, 5) },
            { READOUT.ALUM_contents, new Tuple<int, int>(74, 5) },
            { READOUT.ALUM_expansion, new Tuple<int, int>(94, 5) },
            ////////////////////////////////////////////////////////////////////////////
            { READOUT.PC, new Tuple<int, int>(4, 6) },
            { READOUT.PC_contents, new Tuple<int, int>(12, 6) },
            { READOUT.GPB, new Tuple<int, int>(35, 6) },
            { READOUT.GPB_contents, new Tuple<int, int>(43, 6) },
            { READOUT.ALUA, new Tuple<int, int>(66, 6) },
            { READOUT.ALUA_contents, new Tuple<int, int>(74, 6) },
            { READOUT.ALUA_expansion, new Tuple<int, int>(94, 6) },
            ////////////////////////////////////////////////////////////////////////////
            { READOUT.GPC, new Tuple<int, int>(35, 7) },
            { READOUT.GPC_contents, new Tuple<int, int>(43, 7) },
            { READOUT.ALUB, new Tuple<int, int>(66, 7) },
            { READOUT.ALUB_contents, new Tuple<int, int>(74, 7) },
            { READOUT.ALUB_expansion, new Tuple<int, int>(94, 7) },
            ////////////////////////////////////////////////////////////////////////////
            { READOUT.SKIP, new Tuple<int, int>(4, 8) },
            { READOUT.SKIP_contents, new Tuple<int, int>(12, 8) },
            { READOUT.GPD, new Tuple<int, int>(35, 8) },
            { READOUT.GPD_contents, new Tuple<int, int>(43, 8) },
            { READOUT.ALUR, new Tuple<int, int>(66, 8) },
            { READOUT.ALUR_contents, new Tuple<int, int>(74, 8) },
            { READOUT.ALUR_expansion, new Tuple<int, int>(94, 8) },
            ////////////////////////////////////////////////////////////////////////////
            { READOUT.RTRN, new Tuple<int, int>(4, 9) },
            { READOUT.RTRN_contents, new Tuple<int, int>(12, 9) },
            ////////////////////////////////////////////////////////////////////////////
            { READOUT.LINK, new Tuple<int, int>(4, 10) },
            { READOUT.LINK_contents, new Tuple<int, int>(12, 10) },
            { READOUT.GPE, new Tuple<int, int>(35, 10) },
            { READOUT.GPE_contents, new Tuple<int, int>(43, 10) },
            { READOUT.FPUM, new Tuple<int, int>(66, 10) },
            { READOUT.FPUM_contents, new Tuple<int, int>(74, 10) },
            { READOUT.FPUM_expansion, new Tuple<int, int>(94, 10) },
            ////////////////////////////////////////////////////////////////////////////
            { READOUT.GPF, new Tuple<int, int>(35, 11) },
            { READOUT.GPF_contents, new Tuple<int, int>(43, 11) },
            { READOUT.FPUA, new Tuple<int, int>(66, 11) },
            { READOUT.FPUA_contents, new Tuple<int, int>(74, 11) },
            { READOUT.FPUA_expansion, new Tuple<int, int>(94, 11) },
            ////////////////////////////////////////////////////////////////////////////
            { READOUT.GPG, new Tuple<int, int>(35, 12) },
            { READOUT.GPG_contents, new Tuple<int, int>(43, 12) },
            { READOUT.FPUB, new Tuple<int, int>(66, 11) },
            { READOUT.FPUB_contents, new Tuple<int, int>(74, 12) },
            { READOUT.FPUB_expansion, new Tuple<int, int>(94, 12) },
            ////////////////////////////////////////////////////////////////////////////
            { READOUT.COMPA, new Tuple<int, int>(4, 13) },
            { READOUT.COMPA_contents, new Tuple<int, int>(12, 13) },
            { READOUT.GPH, new Tuple<int, int>(35, 13) },
            { READOUT.GPH_contents, new Tuple<int, int>(43, 13) },
            { READOUT.FPUR, new Tuple<int, int>(66, 13) },
            { READOUT.FPUR_contents, new Tuple<int, int>(74, 13) },
            { READOUT.FPUR_expansion, new Tuple<int, int>(94, 13) },
            ////////////////////////////////////////////////////////////////////////////
            { READOUT.COMPB, new Tuple<int, int>(4, 14) },
            { READOUT.COMPB_contents, new Tuple<int, int>(12, 14) },
            ////////////////////////////////////////////////////////////////////////////
            { READOUT.COMPR, new Tuple<int, int>(4, 15) },
            { READOUT.COMPR_contents, new Tuple<int, int>(12, 15) },
            ////////////////////////////////////////////////////////////////////////////
            { READOUT.IADN, new Tuple<int, int>(4, 17) },
            { READOUT.IADN_contents, new Tuple<int, int>(12, 17) },
            ////////////////////////////////////////////////////////////////////////////
            { READOUT.IADF, new Tuple<int, int>(4, 18) },
            { READOUT.IADF_contents, new Tuple<int, int>(12, 18) },
            ////////////////////////////////////////////////////////////////////////////
            { READOUT.RBASE, new Tuple<int, int>(4, 22) },
            { READOUT.RBASE_contents, new Tuple<int, int>(12, 22) },
            { READOUT.MemoryContext1, new Tuple<int, int>(29, 22) },
            { READOUT.WBASE, new Tuple<int, int>(58, 22) },
            { READOUT.WBASE_contents, new Tuple<int, int>(66, 22) },
            { READOUT.MemoryContext2, new Tuple<int, int>(82, 22) },
            ////////////////////////////////////////////////////////////////////////////
            { READOUT.ROFST, new Tuple<int, int>(4, 23) },
            { READOUT.ROFST_contents, new Tuple<int, int>(12, 23) },
            { READOUT.WOFST, new Tuple<int, int>(58, 23) },
            { READOUT.WOFST_contents, new Tuple<int, int>(66, 23) },
            ////////////////////////////////////////////////////////////////////////////
            { READOUT.RMEM, new Tuple<int, int>(4, 24) },
            { READOUT.RMEM_contents, new Tuple<int, int>(12, 24) },
            { READOUT.WMEM, new Tuple<int, int>(58, 24) },
            { READOUT.WMEM_contents, new Tuple<int, int>(66, 24) },


        };

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
                    //standard register contents
                    //split string into doublets separated by spaces
                    //take a maximum of 4 doublets
                    //pad string with 2 spaces on both sides
                    //white when not "00 00 00 00"
                    //dark gray otherwise
                    break;
                case (READOUT.CurrentInstruction_contents):
                    //same as standard register contents
                    //take only 2 doublets
                    //TODO color instructions conditionally
                    break;
                case (READOUT.CurrentInstruction_expansion):
                    //pad string with 1 space to the left
                    //change known register names to their corresponding colors
                    break;
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

        private enum READOUT
        {
            Header = 0,
            FLAG = 1, FLAG_contents = 2,
            CurrentInstruction, CurrentInstruction_contents, CurrentInstruction_expansion,
            EXE, EXE_contents,
            PC, PC_contents,
            SKIP, SKIP_contents,
            RTRN, RTRN_contents,
            LINK, LINK_contents,
            COMPA, COMPA_contents,
            COMPB, COMPB_contents,
            COMPR, COMPR_contents,
            IADN, IADN_contents,
            IADF, IADF_contents,
            GPA, GPA_contents,
            GPB, GPB_contents,
            GPC, GPC_contents,
            GPD, GPD_contents,
            GPE, GPE_contents,
            GPF, GPF_contents,
            GPG, GPG_contents,
            GPH, GPH_contents,
            ALUM, ALUM_contents, ALUM_expansion,
            ALUA, ALUA_contents, ALUA_expansion,
            ALUB, ALUB_contents, ALUB_expansion,
            ALUR, ALUR_contents, ALUR_expansion,
            FPUM, FPUM_contents, FPUM_expansion,
            FPUA, FPUA_contents, FPUA_expansion,
            FPUB, FPUB_contents, FPUB_expansion,
            FPUR, FPUR_contents, FPUR_expansion,
            RBASE, RBASE_contents,
            ROFST, ROFST_contents,
            RMEM, RMEM_contents,
            WBASE, WBASE_contents,
            WOFST, WOFST_contents,
            WMEM, WMEM_contents,
            MemoryContext1,
            MemoryContext2,
        };

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


            throw new NotImplementedException();
        }
    }
}