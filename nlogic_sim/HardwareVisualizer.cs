using System;

namespace nlogic_sim
{
    public partial class Processor
    {
        public void print_current_state()
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("|PROCESSOR");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("====================================================================================================");

            Console.Write("##");
            print_register(registers[FLAG], ConsoleColor.DarkGreen, ConsoleColor.White);
            Console.Write("##");
            print_current_instruction();
            Console.Write("##");
            Console.WriteLine();


            Console.Write("######################################################################################");
            Console.WriteLine();


            Console.WriteLine();


            Console.Write("  ");
            print_register(registers[EXE], ConsoleColor.DarkYellow, ConsoleColor.White);
            Console.Write("    ");
            print_register(registers[ALUM], ConsoleColor.DarkMagenta, ConsoleColor.White);

            uint alu_mode = ((Register_32)registers[ALUM]).data;
            string alu_mode_string = alu_mode.ToString("X2");
            if (Utility.alu_mode_to_name.ContainsKey((ALU_MODE)alu_mode))
            {
                alu_mode_string = Utility.alu_mode_to_name[(ALU_MODE)alu_mode];
            }

            alu_mode_string = String.Format("{0, -5}", alu_mode_string);

            Console.Write(" ]> " + alu_mode_string + "<[");

            Console.Write("            ");
            print_register(registers[RBASE], ConsoleColor.Yellow, ConsoleColor.White);

            Console.WriteLine();


            Console.Write("  ");
            print_register(registers[PC], ConsoleColor.White, ConsoleColor.White);
            Console.Write("    ");
            print_register(registers[ALUA], ConsoleColor.DarkCyan, ConsoleColor.White);
            string int_value = ((Register_32)registers[ALUA]).data.ToString();
            int_value = String.Format("{0, 11}", int_value);
            Console.Write(" [ " + int_value + " ]");

            Console.Write("       ");
            print_register(registers[ROFST], ConsoleColor.Yellow, ConsoleColor.White);
            Console.WriteLine();

            Console.Write("                                 ");
            print_register(registers[ALUB], ConsoleColor.DarkCyan, ConsoleColor.White);
            int_value = ((Register_32)registers[ALUB]).data.ToString();
            int_value = String.Format("{0, 11}", int_value);
            Console.Write(" [ " + int_value + " ]");

            Console.Write("       ");
            print_register(registers[RMEM], ConsoleColor.Green, ConsoleColor.White);
            Console.WriteLine();

            Console.Write("                                 ");
            print_register(registers[ALUR], ConsoleColor.Blue, ConsoleColor.White);
            int_value = ((Register_32)registers[ALUR]).data.ToString();
            int_value = String.Format("{0, 11}", int_value);
            Console.Write(" [ " + int_value + " ]");
            Console.WriteLine();

            Console.Write("                                                                                   ");
            print_register(registers[WBASE], ConsoleColor.Yellow, ConsoleColor.White);
            Console.WriteLine();

            //////////////////////

            Console.Write("                                 ");
            print_register(registers[FPUM], ConsoleColor.DarkMagenta, ConsoleColor.White);

            uint fpu_mode = ((Register_32)registers[FPUM]).data;
            string fpu_mode_string = fpu_mode.ToString("X2");
            if (Utility.fpu_mode_to_name.ContainsKey((FPU_MODE)fpu_mode))
            {
                fpu_mode_string = Utility.fpu_mode_to_name[(FPU_MODE)fpu_mode];
            }

            fpu_mode_string = String.Format("{0, -5}", fpu_mode_string);

            Console.Write(" ]> " + fpu_mode_string + "<[");

            Console.Write("            ");
            print_register(registers[WOFST], ConsoleColor.Yellow, ConsoleColor.White);
            Console.WriteLine();


            Console.Write("                                 ");
            print_register(registers[FPUA], ConsoleColor.DarkCyan, ConsoleColor.White);
            string float_value = ((Register_32)registers[FPUA]).float_data().ToString();
            float_value = String.Format("{0, 11}", float_value);
            Console.Write(" [ " + float_value + " ]");

            Console.Write("       ");
            print_register(registers[WMEM], ConsoleColor.Green, ConsoleColor.White);
            Console.WriteLine();

            Console.Write("                                 ");
            print_register(registers[FPUB], ConsoleColor.DarkCyan, ConsoleColor.White);
            float_value = ((Register_32)registers[FPUB]).float_data().ToString();
            float_value = String.Format("{0, 11}", float_value);
            Console.Write(" [ " + float_value + " ]");
            Console.WriteLine();

            Console.Write("                                 ");
            print_register(registers[FPUR], ConsoleColor.Magenta, ConsoleColor.White);
            float_value = ((Register_32)registers[FPUR]).float_data().ToString();
            float_value = String.Format("{0, 11}", float_value);
            Console.Write(" [ " + float_value + " ]");
            Console.WriteLine();
        }

        private void print_current_instruction()
        {

            Console.Write("|| ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            string formatted_name = String.Format("{0, -5}", "Current Instruction");
            Console.Write(formatted_name);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(" |  ");

            Console.ForegroundColor = ConsoleColor.White;

            byte[] b = Utility.byte_array_from_uint32(2, (uint)current_instruction);
            Console.Write(Utility.byte_array_string(b));
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("  | ");
            string s = b[0].ToString("X2");
            if (Utility.register_location_to_name.ContainsKey(b[0]))
                s = Utility.register_location_to_name[b[0]];
            string d = b[1].ToString("X2");
            if (Utility.register_location_to_name.ContainsKey(b[1]))
                d = Utility.register_location_to_name[b[1]];

            s = String.Format("{0, -5}", s);
            d = String.Format("{0, -5}", d);

            Console.Write(s + " -> " + d);
            Console.Write("  ||");
        }

        private void print_register(I_Register register, ConsoleColor name_color, ConsoleColor value_color)
        {
            Register_32 r = register as Register_32;

            Console.Write("|| ");
            Console.ForegroundColor = name_color;
            string formatted_name = String.Format("{0, -5}", r.name_short);
            Console.Write(formatted_name);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(" |  ");
            uint v = r.data;

            if (v != 0)
                Console.ForegroundColor = value_color;
            else
                Console.ForegroundColor = ConsoleColor.DarkGray;

            Console.Write(Utility.byte_array_string(r.data_array));
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("  ||");
        }
    }
}