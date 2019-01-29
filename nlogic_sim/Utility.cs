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
        };

        /// <summary>
        /// Converts a big-endian array of bytes into a floating point number.
        /// </summary>
        public static float float_from_byte_array(byte[] data_array)
        {
            Debug.Assert(data_array.Length == 4);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data_array);
            }
            return BitConverter.ToSingle(data_array, 0);
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
        /// Returns a big-endian byte array from of the specified size from the given data.
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