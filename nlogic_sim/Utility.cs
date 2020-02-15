using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace nlogic_sim
{
    class Utility
    {

        /// <summary>
        /// Maps register short names to register location codes.
        /// </summary>
        public static Dictionary<string, byte> register_name_to_location = new Dictionary<string, byte>
        {
            {"IMM" , Processor.IMM },
            {"//" , 0x00 },

            {"FLAG", Processor.FLAG},
            {"EXE", Processor.EXE},
            {"PC", Processor.PC},

            {"ALUM", Processor.ALUM},
            {"ALUA", Processor.ALUA},
            {"ALUB", Processor.ALUB},
            {"ALUR", Processor.ALUR},

            {"FPUM", Processor.FPUM},
            {"FPUA", Processor.FPUA},
            {"FPUB", Processor.FPUB},
            {"FPUR", Processor.FPUR},

            {"RBASE", Processor.RBASE},
            {"ROFST", Processor.ROFST},
            {"RMEM", Processor.RMEM},

            {"WBASE", Processor.WBASE},
            {"WOFST", Processor.WOFST},
            {"WMEM", Processor.WMEM},

            {"GPA", Processor.GPA },
            {"GPB", Processor.GPB },
            {"GPC", Processor.GPC },
            {"GPD", Processor.GPD },
            {"GPE", Processor.GPE },
            {"GPF", Processor.GPF },
            {"GPG", Processor.GPG },
            {"GPH", Processor.GPH },

            {"COMPA", Processor.COMPA },
            {"COMPB", Processor.COMPB },
            {"COMPR", Processor.COMPR },

            {"IADN", Processor.IADN },
            {"IADF", Processor.IADF },
            {"LINK", Processor.LINK },
            {"SKIP", Processor.SKIP },
            {"RTRN", Processor.RTRN },
        };

        /// <summary>
        /// Maps register location codes to register short names.
        /// </summary>
        public static Dictionary<byte, string> register_location_to_name = new Dictionary<byte, string>
        {
            {Processor.IMM, "IMM" },

            {Processor.FLAG, "FLAG"},
            {Processor.EXE, "EXE"},
            {Processor.PC, "PC"},

            {Processor.ALUM, "ALUM"},
            {Processor.ALUA, "ALUA"},
            {Processor.ALUB, "ALUB"},
            {Processor.ALUR, "ALUR"},

            {Processor.FPUM, "FPUM"},
            {Processor.FPUA, "FPUA"},
            {Processor.FPUB, "FPUB"},
            {Processor.FPUR, "FPUR"},

            { Processor.RBASE, "RBASE"},
            { Processor.ROFST, "ROFST"},
            {Processor.RMEM, "RMEM"},

            { Processor.WBASE, "WBASE"},
            { Processor.WOFST, "WOFST"},
            {Processor.WMEM, "WMEM"},

            {Processor.GPA, "GPA" },
            {Processor.GPB, "GPB" },
            {Processor.GPC, "GPC" },
            {Processor.GPD, "GPD" },
            {Processor.GPE, "GPE" },
            {Processor.GPF, "GPF" },
            {Processor.GPG, "GPG" },
            {Processor.GPH, "GPH" },

            {Processor.COMPA, "COMPA" },
            {Processor.COMPB, "COMPB" },
            {Processor.COMPR, "COMPR" },

            {Processor.IADN, "IADN" },
            {Processor.IADF, "IADF" },
            {Processor.LINK, "LINK" },
            {Processor.SKIP, "SKIP" },
            {Processor.RTRN, "RTRN" },
        };

        /// <summary>
        /// Maps ALU modes to short names.
        /// </summary>
        public static Dictionary<Processor.ALU_MODE, string> alu_mode_to_name = new Dictionary<Processor.ALU_MODE, string>
        {
            {Processor.ALU_MODE.NoOp, "NoOP" },
            {Processor.ALU_MODE.Add, "ADD" },
            {Processor.ALU_MODE.AND, "AND" },
            {Processor.ALU_MODE.Divide, "DIV" },
            {Processor.ALU_MODE.Multiply, "MULT" },
            {Processor.ALU_MODE.NAND, "NAND" },
            {Processor.ALU_MODE.NOR, "NOR" },
            {Processor.ALU_MODE.OR, "OR" },
            {Processor.ALU_MODE.ShiftLeft, "LSFT" },
            {Processor.ALU_MODE.ShiftRight, "RSFT" },
            {Processor.ALU_MODE.Subtract, "SUB" },
            {Processor.ALU_MODE.XOR, "XOR" },
        };

        /// <summary>
        /// Maps FPU modes to short names.
        /// </summary>
        public static Dictionary<Processor.FPU_MODE, string> fpu_mode_to_name = new Dictionary<Processor.FPU_MODE, string>
        {
            {Processor.FPU_MODE.NoOp, "NoOP" },
            {Processor.FPU_MODE.Add, "ADD" },
            {Processor.FPU_MODE.Divide, "DIV" },
            {Processor.FPU_MODE.Multiply, "MULT" },
            {Processor.FPU_MODE.Subtract, "SUB" },
        };

        /// <summary>
        /// Returns string of the form "SRC -> DST" from the instruction represented by a uint32
        /// </summary>
        /// <param name="instruction">uint32 of an instruction</param>
        /// <returns></returns>
        public static string instruction_string_from_uint32(uint instruction)
        {
            byte[] b = byte_array_from_uint32(2, (uint)instruction);
            return instruction_string_from_byte_array(b);
        }

        /// <summary>
        /// Returns string of the form "SRC -> DST" from a big-endian array of 2 bytes
        /// [MSB] [LSB]
        /// [SRC] [DST]
        /// </summary>
        /// <param name="instruction_bytes">Byte array representing an instruction, a pair of bytes</param>
        /// <returns></returns>
        public static string instruction_string_from_byte_array(byte[] instruction_bytes)
        {
            Debug.Assert(instruction_bytes.Length == 2);

            byte s_byte = instruction_bytes[0];
            byte d_byte = instruction_bytes[1];

            string s = s_byte.ToString("X2");
            if (Utility.register_location_to_name.ContainsKey(s_byte))
                s = Utility.register_location_to_name[s_byte];

            string d = d_byte.ToString("X2");
            if (Utility.register_location_to_name.ContainsKey(d_byte))
                d = Utility.register_location_to_name[d_byte];

            string instruction_expansion = s + " -> " + d;
            return instruction_expansion;
        }

        /// <summary>
        /// Converts a big-endian array of bytes into a floating point number.
        /// </summary>
        public static float float_from_byte_array(byte[] data_array)
        {
            Debug.Assert(data_array.Length == 4);
            bool flipped = false;
            if (BitConverter.IsLittleEndian)
            {
                flipped = true;
                Array.Reverse(data_array);
            }

            float result = BitConverter.ToSingle(data_array, 0);

            if (flipped)
            {
                Array.Reverse(data_array);
            }

            return result;
        }

        /// <summary>
        /// Converts a floating point number to an array of bytes.
        /// </summary>
        public static byte[] byte_array_from_float(float data)
        {
            byte[] result = BitConverter.GetBytes(data);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(result);
            }
            return result;
        }

        /// <summary>
        /// Converts a big-endian array of bytes into a uint32.
        ///     [MSB][...][...][LSB]
        ///             from
        ///  ...[n-3][n-1][n-1][ n ]
        /// </summary>
        public static uint uint32_from_byte_array(byte[] data_array)
        {
            uint[] numbers = new uint[data_array.Length];

            int lower = 0;
            int upper = data_array.Length;

            if (data_array.Length > 4)
            {
                lower = data_array.Length - 4;
            }

            for (int i = lower; i < data_array.Length; i++)
            {
                int shift = (8 * (((int)data_array.Length - 1) - i));
                numbers[i] = ((uint)(data_array[i])) << shift;
            }

            uint sum = 0;

            for (int i = 0; i < numbers.Length; i++)
            {
                sum += numbers[i];
            }

            return sum;
        }

        /// <summary>
        /// Converts a big-endian array of bytes into a uint16.
        ///     [MSB][LSB]
        ///        from
        ///  ...[n-1][ n ]
        /// </summary>
        public static ushort ushort_from_byte_array(byte[] data_array)
        {
            ushort[] numbers = new ushort[data_array.Length];

            int lower = 0;
            int upper = data_array.Length;

            if (data_array.Length > 2)
            {
                lower = data_array.Length - 2;
            }

            for (int i = lower; i < data_array.Length; i++)
            {
                int shift = (8 * (((int)data_array.Length - 1) - i));
                numbers[i] = (ushort)(((ushort)(data_array[i])) << shift);
            }

            ushort sum = 0;

            for (int i = 0; i < numbers.Length; i++)
            {
                sum += numbers[i];
            }

            return sum;
        }


        /// <summary>
        /// Returns a big-endian byte array of the specified size from the given data.
        /// </summary>
        public static byte[] byte_array_from_uint32(uint size, uint data)
        {
            byte[] result = new byte[size];

            for (int i = 0; i < result.Length; i++)
            {
                int shift = (8 * (((int)result.Length - 1) - i));
                result[i] = (byte)(((uint)(data)) >> shift);
            }

            return result;

        }

        /// <summary>
        /// Returns a string of the given byte array.
        /// </summary>
        public static string byte_array_string(byte[] data, string separator = " ", bool prepend_separator = false)
        {
            string result = "";
            for (int i = 0; i < data.Length; i++)
            {
                if (prepend_separator || i > 0)
                    result += separator;
                result += data[i].ToString("X2");
            }

            return result;
        }

        public static byte[] byte_array_from_string(string input, string separator = " ")
        {
            string[] bytes = input.Split(new string[] { separator }, StringSplitOptions.RemoveEmptyEntries);
            byte[] result = new byte[bytes.Length];

            for (int i = 0; i < result.Length; i++)
            {
                //convert the string to a uint32
                //convert the uint32 to a byte array of length 1
                //store the first (and only) byte into the output array
                result[i] = byte_array_from_uint32(1, uint.Parse(bytes[i], System.Globalization.NumberStyles.HexNumber))[0];
            }

            return result;
        }

        public static void print_byte()
        {
            byte b = 7;
            Console.WriteLine("b = " + b);
            b = (byte)(b << 1);
            Console.WriteLine("{0:X2}", b);
            Console.WriteLine("b = " + b.ToString("X2"));
            Console.ReadKey();
        }
    }
}