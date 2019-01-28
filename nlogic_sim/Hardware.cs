namespace nlogic_sim
{
    public class Register
    {
        public readonly string name_short;
        public readonly string name_full;

        public readonly uint size;

        private uint _internal_data_uint32;
        private byte[] _internal_data_array;

        public uint data_uint32
        {
            get
            {
                return _internal_data_uint32;
            }

            set
            {
                _internal_data_uint32 = value;
                //update data array
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

        public Register(uint size_in_bytes, string name_full, string name_short)
        {
            size = size_in_bytes;
            this.name_full = name_full;
            this.name_short = name_short;

            data_array = new byte[size];
            for (int i = 0; i < data_array.Length; i++)
            {
                data_array[i] = 0;
            }

            update_data_from_array();
        }

        /// <summary>
        /// Update all data fields based on the contents of the data array
        /// </summary>
        private void update_data_from_array()
        {
            _internal_data_uint32 = Utility.uint32_from_byte_array(data_array);
        }

        /// <summary>
        /// Update the array from the internal uint32 data field
        /// </summary>
        private void update_array_from_data()
        {
            _internal_data_array = Utility.byte_array_from_uint32(size, _internal_data_uint32);
            update_data_from_array();
        }
    }
}