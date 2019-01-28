using System;

namespace nlogic_sim
{
    class Utility
    {
        /// <summary>
        /// Converts an array of 4 bytes or fewer into a uint32.
        /// [MSB][...][...][LSB]
        /// [ 0 ][ 1 ][ 2 ][ 3 ]
        /// </summary>
        public static uint uint32_from_byte_array(byte[] data_array)
        {
            uint[] numbers = new uint[data_array.Length];

            for (int i = 0; i < data_array.Length; i++)
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