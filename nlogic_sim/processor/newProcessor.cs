﻿using System;
using System.Collections.Generic;

namespace nlogic_sim
{
    public partial class newProcessor
    {
        //reference to the containing simuation environment
        private SimulationEnvironment environment;

        //processor constants
        public const ushort interrupt_handler_address = 0x002A;
        public const ushort interrupt_register_dump_address = 0x001A;

        //processor state
        public ushort current_instruction;
        public Dictionary<byte, I_Register> registers;

        /// <summary>
        /// Completes one cycle of work on the processor and returns the processor status (contents of FLAG).
        /// </summary>
        public uint cycle()
        {
            //check the status of the processor
            //(and check for interrupts)
            check_status();

            //load current instruction
            load_current_instruction();

            //increment program counter
            ((Register_32)registers[PC]).data += 2;

            //update COMPR, IADN, IADF, SKIP, RTRN
            update_accessors();

            //execute current instruction
            execute();

            //return contents of FLAG
            return Utility.uint32_from_byte_array(registers[FLAG].data_array);
        }


        /// <summary>
        /// Send a signal to the processor on the given signal channel.
        /// Throws an exception if the channel is out of range.
        /// </summary>
        /// <param name="channel"></param>
        public void raise_signal(uint channel)
        {
            //there is one channel per bit in the FLAG register
            //1 bit is reserved as the 'ignore interrupts' bit, however
            uint num_channels = registers[FLAG].size_in_bytes * 8;
            if (channel > (num_channels - 2))
            {
                throw new ArgumentOutOfRangeException("channel", "Processor cannot receive signals on that channel");
            }

            uint signal_mask = 0b1;
            for (int i = 0; i < channel; i++)
            {
                signal_mask = signal_mask << 1;
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Processor using the given simulation environment
        /// Processor is ready to call cycle() after construction
        /// </summary>
        /// <param name="environment"></param>
        public newProcessor(SimulationEnvironment environment)//SimulationEnvironment environment)
        {
            //save a reference to the simulation environment
            this.environment = environment;

            //give the environment a way to signal the processor
            //necessary for other devices in the environment to send interrupts
            //to the processor
            environment.register_signal_callback(raise_signal);

            //set the current instruction to a no-op just in case
            current_instruction = 0;

            initialize_registers();

            //ensures the special registers (SKIP, COMPR, etc.) have the correct values before the first cycle
            //must happen after registers are intialized
            update_accessors();
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
            //if not writeable, do nothing

            if (registers.ContainsKey(destination) && registers[destination].writeable)
            {
                ((Register_32)registers[destination]).data = source_data;
            }

            //update result registers as needed

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

        private void update_accessors()
        {
            //calculate the address of EXE + PC in memory
            uint base_addr = ((Register_32)registers[EXE]).data;
            uint offset = ((Register_32)registers[PC]).data;

            //update SKIP and RETURN
            ((Register_32)registers[SKIP]).data = ((Register_32)registers[PC]).data + 4;
            ((Register_32)registers[RTRN]).data = ((Register_32)registers[PC]).data + 6;

            //update IADN and IADF
            byte[] iadn_value = read_memory(base_addr + offset, 4); //IADN value might be reused by COMPR
            ((Register_32)registers[IADN]).data_array = iadn_value;
            ((Register_32)registers[IADF]).data_array = read_memory(base_addr + offset + 2, 4);


            //compare COMPA and COMPB
            uint compa_value = ((Register_32)registers[COMPA]).data;
            uint compb_value = ((Register_32)registers[COMPB]).data;
            //if COMPA == COMPB, COMPR = memory[EXE + PC]
            if (compa_value == compb_value)
            {
                ((Register_32)registers[COMPR]).data_array = iadn_value;
            }

            //else COMPR = memory[EXE + PC + 4]
            else
            {
                ((Register_32)registers[COMPR]).data_array = read_memory(base_addr + offset + 4, 4);
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
            float arga = ((Register_32)registers[FPUA]).float_data();
            float argb = ((Register_32)registers[FPUB]).float_data();
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
            ((Register_32)registers[FPUR]).data = data;
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
        /// Return an array of length bytes from memory, starting at address.
        /// Results may come from a memory-mapped input-output device
        /// </summary>
        private byte[] read_memory(uint address, uint length)
        {
            return environment.read_address(address, length);
        }

        private void write_memory(uint address, byte[] data)
        {
            environment.write_address(address, data);
        }


        /// <summary>
        /// Check the status of processor, handling interrupts as appropriate
        /// </summary>
        private void check_status()
        {
            //check the most significant bit to determine if interrupts are being ignored
            uint flag_contents = Utility.uint32_from_byte_array(registers[FLAG].data_array);
            //mask for only the most significant of 32 bits
            uint ignore_flag_mask = 0x80000000;
            uint ignore_flag = flag_contents & ignore_flag_mask;

            //ignore flag is 1 if iterrupts should be ignored
            if (!(ignore_flag == 0))
            {
                //ignore the flag register; do nothing
                return;
            }


            //if flag register is non 0, we are interrupted

            //dump PC, EXE, FLAG, GPA to memory
            uint address_increment = registers[PC].size_in_bytes;
            write_memory(interrupt_register_dump_address, registers[PC].data_array);
            write_memory(interrupt_register_dump_address + address_increment, registers[EXE].data_array);
            write_memory(interrupt_register_dump_address + (2 * address_increment), registers[FLAG].data_array);
            write_memory(interrupt_register_dump_address + (3 * address_increment), registers[GPA].data_array);

            //enable ignore interrupts
            uint new_flag_value = flag_contents | ignore_flag_mask;
            registers[FLAG].data_array = Utility.byte_array_from_uint32(registers[FLAG].size_in_bytes, new_flag_value);

            //change PC/EXE to trap address
            registers[PC].data_array = Utility.byte_array_from_uint32(registers[PC].size_in_bytes, interrupt_handler_address);
            registers[EXE].data_array = Utility.byte_array_from_uint32(registers[EXE].size_in_bytes, 0);

        }


        /// <summary>
        /// Fill the registers dictionary
        /// </summary>
        private void initialize_registers()
        {
            this.registers = new Dictionary<byte, I_Register>
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
                    {GPA, (new Register_32(
                        "General Purpose Register A",
                        "GPA",
                        true))},
                    {GPB, (new Register_32(
                        "General Purpose Register B",
                        "GPB",
                        true))},
                    {GPC, (new Register_32(
                        "General Purpose Register C",
                        "GPC",
                        true))},
                    {GPD, (new Register_32(
                        "General Purpose Register D",
                        "GPD",
                        true))},
                    {GPE, (new Register_32(
                        "General Purpose Register E",
                        "GPE",
                        true))},
                    {GPF, (new Register_32(
                        "General Purpose Register F",
                        "GPF",
                        true))},
                    {GPG, (new Register_32(
                        "General Purpose Register G",
                        "GPG",
                        true))},
                    {GPH, (new Register_32(
                        "General Purpose Register H",
                        "GPH",
                        true))},
                    {COMPA, (new Register_32(
                        "Branching Comparator Input A",
                        "COMPA",
                        true))},
                    {COMPB, (new Register_32(
                        "Branching Comparator Input B",
                        "COMPB",
                        true))},
                    {COMPR, (new Register_32(
                        "Branching Comparator Read Result",
                        "COMPR",
                        false))},
                    {IADN, (new Register_32(
                        "Near Instructions-as-data Accessor",
                        "IADN",
                        false))},
                    {IADF, (new Register_32(
                        "Far Instructions-as-data Accessor",
                        "IADF",
                        false))},
                    {LINK, (new Register_32(
                        "Link Register",
                        "LINK",
                        true))},
                    {SKIP, (new Register_32(
                        "Skip Immediate Address Register",
                        "SKIP",
                        false))},
                    {RTRN, (new Register_32(
                        "Return Address Register",
                        "RTRN",
                        false))},
                };
        }
    }

}

namespace nlogic_sim
{
    public partial class newProcessor
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
        public const byte GPA = 0x91;
        public const byte GPB = 0x92;
        public const byte GPC = 0x93;
        public const byte GPD = 0x94;
        public const byte GPE = 0x95;
        public const byte GPF = 0x96;
        public const byte GPG = 0x97;
        public const byte GPH = 0x98;
        public const byte COMPA = 0x99;
        public const byte COMPB = 0x9A;
        public const byte COMPR = 0x9B;
        public const byte IADN = 0x9C;
        public const byte IADF = 0x9D;
        public const byte LINK = 0x9E;
        public const byte SKIP = 0x9F;
        public const byte RTRN = 0xA0;
    }
}