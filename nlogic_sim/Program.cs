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
            byte[] data = new byte[] { 0x0B, 0x70 };
            uint number = Utility.uint32_from_byte_array(data);
            Console.WriteLine(number);

            Register_32 r = new Register_32("nothing", "TEST");
            //r.data_uint32 = 32;
            //Console.WriteLine(r.data_uint32);

            uint test = 913402;
            byte[] ta = Utility.byte_array_from_uint32(4, test);
            Console.WriteLine(Utility.byte_array_string(ta));

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();

            Console.ReadKey();
        }
    }
}
