using System;
using System.Collections.Generic;

namespace nlogic_sim
{
    public class Processor
    {
        public enum ALU_MODE
        {
            NoOp = 0,
            Add = 1,
            Multiply = 2,
            Subtract = 3,
            Divide = 4,
            ShiftLeft = 5,
            ShiftRight = 6,
            OR = 7,
            AND = 8,
            XOR = 9,
            NAND = 10,
            NOR = 11,
        }

        public enum FPU_MODE
        {
            NoOp = 0,
            Add = 1,
            Multiply = 2,
            Subtract = 3,
            Divide = 4,
        }

        public const byte IMM = 0x00;
        public const byte FLAG = 0x80;
        public const byte EXE = 0x81;
        public const byte PC = 0x82;
        public const byte ALUM = 0x83;
        public const byte ALUA = 0x84;
        public const byte ALUB = 0x85;
        public const byte ALUR = 0x86;
        public const byte FPUM = 0x87;
        public const byte FPUA = 0x88;
        public const byte FPUB = 0x89;
        public const byte FPUR = 0x8A;
        public const byte RBASE = 0x8B;
        public const byte ROFST = 0x8C;
        public const byte RMEM = 0x8D;
        public const byte WBASE = 0x8E;
        public const byte WOFST = 0x8F;
        public const byte WMEM = 0x90;

        public bool halted;
        public ushort current_instruction;

        public byte[] memory;

        public Dictionary<byte, I_Register> registers;

        /// <summary>
        /// Completes one cycle of work on the processor and returns the processor status (contents of FLAG).
        /// </summary>
        public uint cycle()
        {
            //load current instruction
            load_current_instruction();

            //increment program counter
            ((Register_32)registers[PC]).data += 2;

            //execute current instruction
            execute();

            //return contents of FLAG
            return Utility.uint32_from_byte_array(registers[FLAG].data_array);
        }

        /// <summary>
        /// Execute the current instruction.
        /// </summary>
        private void execute()
        {
            byte[] instruction_array = Utility.byte_array_from_uint32(2, (uint)current_instruction);
            byte source = instruction_array[0];
            byte destination = instruction_array[1];

            uint source_data;

            //retrieve the source data

            if (registers.ContainsKey(source))
            {
                source_data = ((Register_32)registers[source]).data;
            }
            else if (source < FLAG)
            {
                source_data = (uint)source;
            }
            else
            {
                source_data = 0;
            }

            //store to the destination

            if (registers.ContainsKey(destination) && registers[destination].writeable)
            {
                ((Register_32)registers[destination]).data = source_data;
            }

            //update result registers

            if (
                destination == ALUM ||
                destination == ALUA ||
                destination == ALUB)
            {
                update_alu();
            }

            else if (
                destination == FPUM ||
                destination == FPUA ||
                destination == FPUB)
            {
                update_fpu();
            }

            else if (
                destination == RBASE ||
                destination == ROFST)
            {
                update_accessor_a();
            }

            else if (
                destination == WBASE ||
                destination == WOFST)
            {
                update_accessor_b();
            }

            else if (destination == RMEM)
            {
                uint base_addr = ((Register_32)registers[RBASE]).data;
                uint offset = ((Register_32)registers[ROFST]).data;
                uint address = base_addr + offset;
                byte[] data_array = Utility.byte_array_from_uint32(4, source_data);
                write_memory(address, data_array);
            }

            else if (destination == WMEM)
            {
                uint base_addr = ((Register_32)registers[WBASE]).data;
                uint offset = ((Register_32)registers[WOFST]).data;
                uint address = base_addr + offset;
                byte[] data_array = Utility.byte_array_from_uint32(4, source_data);
                write_memory(address, data_array);
            }


        }

        private void update_accessor_a()
        {
            uint base_addr = ((Register_32)registers[RBASE]).data;
            uint offset = ((Register_32)registers[ROFST]).data;
            uint address = base_addr + offset;
            byte[] data_array = read_memory(address, 4);
            uint data = Utility.uint32_from_byte_array(data_array);
            ((Register_32)registers[RMEM]).data = data;
        }

        private void update_accessor_b()
        {
            uint base_addr = ((Register_32)registers[WBASE]).data;
            uint offset = ((Register_32)registers[WOFST]).data;
            uint address = base_addr + offset;
            byte[] data_array = read_memory(address, 4);
            uint data = Utility.uint32_from_byte_array(data_array);
            ((Register_32)registers[WMEM]).data = data;
        }

        private void update_fpu()
        {
            float arga = Utility.float_from_byte_array(registers[FPUA].data_array);
            float argb = Utility.float_from_byte_array(registers[FPUB].data_array);
            uint mode = ((Register_32)registers[FPUM]).data;

            float result = 0;

            if (mode == (uint)FPU_MODE.NoOp)
            {
                return;
            }

            else if (mode == (uint)FPU_MODE.Add)
            {
                result = arga + argb;
            }

            else if (mode == (uint)FPU_MODE.Subtract)
            {
                result = arga - argb;
            }

            else if (mode == (uint)FPU_MODE.Multiply)
            {
                result = arga * argb;
            }

            else if (mode == (uint)FPU_MODE.Divide)
            {
                result = arga / argb;
            }

            else
            {
                return;
            }

            byte[] data_array = Utility.byte_array_from_float(result);
            uint data = Utility.uint32_from_byte_array(data_array);
            ((Register_32)registers[ALUR]).data = data;
        }

        private void update_alu()
        {
            uint arga = ((Register_32)registers[ALUA]).data;
            uint argb = ((Register_32)registers[ALUB]).data;
            uint mode = ((Register_32)registers[ALUM]).data;

            if (mode == (uint)ALU_MODE.NoOp)
            {
                return;
            }

            else if (mode == (uint)ALU_MODE.Add)
            {
                ((Register_32)registers[ALUR]).data = arga + argb;
            }

            else if (mode == (uint)ALU_MODE.Subtract)
            {
                ((Register_32)registers[ALUR]).data = arga - argb;
            }

            else if (mode == (uint)ALU_MODE.Multiply)
            {
                ((Register_32)registers[ALUR]).data = arga * argb;
            }

            else if (mode == (uint)ALU_MODE.Divide)
            {
                ((Register_32)registers[ALUR]).data = arga / argb;
            }

            else if (mode == (uint)ALU_MODE.ShiftLeft)
            {
                ((Register_32)registers[ALUR]).data = arga << (int)argb;
            }

            else if (mode == (uint)ALU_MODE.ShiftRight)
            {
                ((Register_32)registers[ALUR]).data = arga >> (int)argb;
            }

            else if (mode == (uint)ALU_MODE.OR)
            {
                ((Register_32)registers[ALUR]).data = arga | argb;
            }

            else if (mode == (uint)ALU_MODE.AND)
            {
                ((Register_32)registers[ALUR]).data = arga & argb;
            }

            else if (mode == (uint)ALU_MODE.NOR)
            {
                ((Register_32)registers[ALUR]).data = ~(arga | argb);
            }

            else if (mode == (uint)ALU_MODE.NAND)
            {
                ((Register_32)registers[ALUR]).data = ~(arga & argb);
            }

            else if (mode == (uint)ALU_MODE.XOR)
            {
                ((Register_32)registers[ALUR]).data = arga ^ argb;
            }

            else
            {
                return;
            }
        }

        /// <summary>
        /// Fills current_instruction with the value in memory pointed to by [EXE + PC]
        /// </summary>
        private void load_current_instruction()
        {
            uint base_addr = ((Register_32)registers[EXE]).data;
            uint offset = ((Register_32)registers[PC]).data;
            uint address = base_addr + offset;
            byte[] bytes = read_memory(address, 2);
            ushort instruction = Utility.ushort_from_byte_array(bytes);
            current_instruction = instruction;
        }

        /// <summary>
        /// Return an array of length bytes from memory, starting at address
        /// </summary>
        private byte[] read_memory(uint address, uint length)
        {
            byte[] result = new byte[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = memory[address + i];
            }

            return result;
        }

        private void write_memory(uint address, byte[] data)
        {
        }

        public Processor()
        {
            halted = false;
            current_instruction = 0;

            memory = new byte[65536];

            registers = new Dictionary<byte, I_Register>
                {
                    {FLAG, (new Register_32(
                        "Status Register", 
                        "FLAG",
                        true))},
                    {EXE, (new Register_32(
                        "Program Counter Base Register",
                        "EXE",
                        true))},
                    {PC, (new Register_32(
                        "Program Counter",
                        "PC",
                        true))},
                    {ALUM, (new Register_32(
                        "Arithmetic Logic Unit Mode",
                        "ALUM",
                        true))},
                    {ALUA, (new Register_32(
                        "Arithmetic Logic Unit Input A",
                        "ALUA",
                        true))},
                    {ALUB, (new Register_32(
                        "Arithmetic Logic Unit Input B",
                        "ALUB",
                        true))},
                    {ALUR, (new Register_32(
                        "Arithmetic Logic Unit Result",
                        "ALUR",
                        false))},
                    {FPUM, (new Register_32(
                        "Floating Point Unit Mode",
                        "FPUM",
                        true))},
                    {FPUA, (new Register_32(
                        "Floating Point Unit Input A",
                        "FPUA",
                        true))},
                    {FPUB, (new Register_32(
                        "Floating Point Unit Input B",
                        "FPUB",
                        true))},
                    {FPUR, (new Register_32(
                        "Floating Point Unit Result",
                        "FPUR",
                        false))},
                    {RBASE, (new Register_32(
                        "Memory Accessor A Base Register",
                        "RBASE",
                        true))},
                    {ROFST, (new Register_32(
                        "Memory Accessor A Offset Register",
                        "ROFST",
                        true))},
                    {RMEM, (new Register_32(
                        "Memory Accessor A Result",
                        "RMEM",
                        true))},
                    {WBASE, (new Register_32(
                        "Memory Accessor B Base Register",
                        "WBASE",
                        true))},
                    {WOFST, (new Register_32(
                        "Memory Accessor B Offset Register",
                        "WOFST",
                        true))},
                    {WMEM, (new Register_32(
                        "Memory Accessor B Result",
                        "WMEM",
                        true))},
                };
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

        bool writeable
        {
            get;
        }

    }

    public class Register_32 : I_Register
    {
        public readonly string name_short;
        public readonly string name_full;

        private bool _internal_writeable;
        private readonly uint _internal_size;
        private uint _internal_data;
        private byte[] _internal_data_array;

        public bool writeable
        {
            get
            {
                return _internal_writeable;
            }
        }

        public uint size_in_bytes
        {
            get
            {
                return _internal_size;
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

        public Register_32(string name_full, string name_short, bool writeable)
        {
            _internal_writeable = writeable;
            _internal_size = size_in_bytes;
            this.name_full = name_full;
            this.name_short = name_short;

            data_array = new byte[_internal_size];
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