using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nlogic_sim
{
    class Program
    {
        static void Main(string[] args)
        {
            byte[] data = new byte[] { 0x0B, 0x70, 0xAA, 0xAA, 0xAA, 0xAA };
            uint number = Utility.uint32_from_byte_array(data);
            Console.WriteLine(number);

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            uint n = 0xAABBCCDD;
            byte[] d2 = Utility.byte_array_from_uint32(2, n);
            Console.WriteLine(Utility.byte_array_string(d2));

            Register_32 r = new Register_32("nothing", "TEST", true);
            //r.data_uint32 = 32;
            //Console.WriteLine(r.data_uint32);

            uint test = 913402;
            byte[] ta = Utility.byte_array_from_uint32(4, test);
            Console.WriteLine(Utility.byte_array_string(ta));

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();

            uint b = 0x0000FFFF;
            byte[] arr = Utility.byte_array_from_uint32(4, b);
            float c = Utility.float_from_byte_array(arr);
            Console.WriteLine(BitConverter.IsLittleEndian);
            Console.WriteLine(c);

            Console.ReadKey();
        }
    }
}
