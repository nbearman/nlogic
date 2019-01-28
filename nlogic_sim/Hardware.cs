using System.Collections.Generic;

namespace nlogic_sim
{
    public class Processor
    {
        private struct state
        {
            bool halted;
            ushort current_instruction;
        }

        public Dictionary<byte, I_Register> registers;

        public Processor()
        {
            registers = new Dictionary<byte, I_Register>();
        }
    }

    public interface I_Register
    {
        byte[] data_array
        {
            get;
            set;
        }

        uint size_in_bytes
        {
            get;
        }

    }

    public class Register_32 : I_Register
    {
        public readonly string name_short;
        public readonly string name_full;

        private readonly uint size;
        private uint _internal_data;
        private byte[] _internal_data_array;

        public uint size_in_bytes
        {
            get
            {
                return size;
            }
        }

        public uint data
        {
            get
            {
                return _internal_data;
            }

            set
            {
                _internal_data = value;
                update_array_from_data();
            }
        }


        public byte[] data_array
        {
            get
            {
                return _internal_data_array;
            }

            set
            {
                _internal_data_array = value;
                update_data_from_array();
            }
        }

        public Register_32(string name_full, string name_short)
        {
            size = size_in_bytes;
            this.name_full = name_full;
            this.name_short = name_short;

            data_array = new byte[size];
            for (int i = 0; i < data_array.Length; i++)
            {
                data_array[i] = 0;
            }

        }

        private void update_array_from_data()
        {
            _internal_data_array = Utility.byte_array_from_uint32(4, _internal_data);
        }

        private void update_data_from_array()
        {
            _internal_data = Utility.uint32_from_byte_array(_internal_data_array);
        }
    }
}