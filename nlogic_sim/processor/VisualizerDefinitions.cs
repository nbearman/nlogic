using System;
using System.Collections.Generic;

namespace nlogic_sim
{
    public partial class Processor
    {
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

        private static Dictionary<READOUT, Tuple<int, int>> readout_coordinates = new Dictionary<READOUT, Tuple<int, int>>
        {
            { READOUT.Header, new Tuple<int, int>(1, 0) },
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
            { READOUT.RBASE, new Tuple<int, int>(4, 21) },
            { READOUT.RBASE_contents, new Tuple<int, int>(12, 21) },
            { READOUT.MemoryContext1, new Tuple<int, int>(29, 21) },
            { READOUT.WBASE, new Tuple<int, int>(58, 21) },
            { READOUT.WBASE_contents, new Tuple<int, int>(66, 21) },
            { READOUT.MemoryContext2, new Tuple<int, int>(83, 21) },
            ////////////////////////////////////////////////////////////////////////////
            { READOUT.ROFST, new Tuple<int, int>(4, 22) },
            { READOUT.ROFST_contents, new Tuple<int, int>(12, 22) },
            { READOUT.WOFST, new Tuple<int, int>(58, 22) },
            { READOUT.WOFST_contents, new Tuple<int, int>(66, 22) },
            ////////////////////////////////////////////////////////////////////////////
            { READOUT.RMEM, new Tuple<int, int>(4, 23) },
            { READOUT.RMEM_contents, new Tuple<int, int>(12, 23) },
            { READOUT.WMEM, new Tuple<int, int>(58, 23) },
            { READOUT.WMEM_contents, new Tuple<int, int>(66, 23) },


        };

        /// <summary>
        /// Mapping of register short names to visualizer colors
        /// </summary>
        private static Dictionary<string, ConsoleColor> register_name_to_color = new Dictionary<string, ConsoleColor>
        {
            {"FLAG", ConsoleColor.Green },
            {"EXE", ConsoleColor.DarkYellow },
            {"PC", ConsoleColor.White },
            {"SKIP", ConsoleColor.Gray },
            {"RTRN", ConsoleColor.White },
            {"LINK", ConsoleColor.Cyan },
            {"COMPA", ConsoleColor.DarkBlue},
            {"COMPB", ConsoleColor.DarkMagenta },
            {"COMPR", ConsoleColor.DarkGreen },
            {"IADN", ConsoleColor.Red },
            {"IADF", ConsoleColor.Green },
            {"GPA", ConsoleColor.DarkGray },
            {"GPB", ConsoleColor.Gray },
            {"GPC", ConsoleColor.White },
            {"GPD", ConsoleColor.Blue },
            {"GPE", ConsoleColor.Cyan },
            {"GPF", ConsoleColor.Green },
            {"GPG", ConsoleColor.Yellow },
            {"GPH", ConsoleColor.Red },
            {"ALUM", ConsoleColor.DarkMagenta },
            {"ALUA", ConsoleColor.DarkCyan },
            {"ALUB", ConsoleColor.DarkCyan },
            {"ALUR", ConsoleColor.Blue },
            {"FPUM", ConsoleColor.DarkMagenta },
            {"FPUA", ConsoleColor.DarkCyan },
            {"FPUB", ConsoleColor.DarkCyan },
            {"FPUR", ConsoleColor.Magenta },
            {"RBASE", ConsoleColor.Yellow },
            {"ROFST", ConsoleColor.Yellow },
            {"RMEM", ConsoleColor.Green },
            {"WBASE", ConsoleColor.Yellow },
            {"WOFST", ConsoleColor.Yellow },
            {"WMEM", ConsoleColor.Green },
            {"CurrentInstruction", ConsoleColor.Cyan },
            {"Header", ConsoleColor.DarkYellow },

        };

    }
}