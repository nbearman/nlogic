using System;
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
            throw new NotImplementedException();
        }

        //the location of the readout within the console window
        private Dictionary<READOUT, Tuple<int, int>> readout_coordinates;

        //the current state of each readout
        private Dictionary<READOUT, ColorString> readout_cache;

        /// <summary>
        /// Change the specified readout to display the given value
        /// </summary>
        /// <param name="location">READOUT to be changed</param>
        /// <param name="value">Unformatted value to display</param>
        private void update_readout(READOUT location, string value)
        {
            if (location == READOUT.MemoryContext1 || location == READOUT.MemoryContext2)
            {
                //the memory contexts have multiple display rows, so there is special behavior
            }

            else
            {
                //update the given readout
            }

            throw new NotImplementedException();

        }

        /// <summary>
        /// Returns a string in the format expected by the given readout
        /// </summary>
        /// <param name="location">READOUT which style will be used</param>
        /// <param name="value">String to format</param>
        /// <returns>Formatted ColorString using location's style</returns>
        private ColorString format_to_readout_style(READOUT location, string value)
        {
            throw new NotImplementedException();
            return new ColorString();
        }

        /// <summary>
        /// String with color information
        /// </summary>
        private struct ColorString
        {
            public ColorString(string value, ConsoleColor color)
            {
                this.value = value;
                this.color = color;
            }

            string value;
            ConsoleColor color;
        }

        private enum READOUT
        {
            Header = 0,
            FLAG = 1,
            CurrentInstruction,
            CurrentInstruction_expansion,
            EXE,
            PC,
            SKIP,
            RTRN,
            LINK,
            COMPA,
            COMPB,
            COMPR,
            IADN,
            IADF,
            GPA,
            GPB,
            GPC,
            GPD,
            GPE,
            GPF,
            GPG,
            GPH,
            ALUM,
            ALUA,
            ALUB,
            ALUR,
            ALUM_expansion,
            ALUA_expansion,
            ALUB_expansion,
            ALUR_expansion,
            FPUM,
            FPUA,
            FPUB,
            FPUR,
            FPUM_expansion,
            FPUA_expansion,
            FPUB_expansion,
            FPUR_expansion,
            RBASE,
            ROFST,
            RMEM,
            WBASE,
            WOFST,
            WMEM,
            MemoryContext1,
            MemoryContext2,

        };

        /// <summary>
        /// Print the skeleton of the display to the console
        /// </summary>
        private void print_skeleton()
        {
            throw new NotImplementedException();
        }
    }
}