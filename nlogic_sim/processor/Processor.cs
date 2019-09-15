using System;
using System.Collections.Generic;

namespace nlogic_sim
{
    public partial class Processor
    {
        public ushort current_instruction;

        public byte[] memory;

        public Dictionary<byte, I_Register> registers;

        //map address ranges to memory-mapped input-output devices
        public IntervalTree<uint, MMIO> devices;

        /// <summary>
        /// Completes one cycle of work on the processor and returns the processor status (contents of FLAG).
        /// </summary>
        public uint cycle()
        {
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
            byte[] result = new byte[length];

            //address is beyond physical memory, check MMIO devices instead
            if (address >= memory.Length)
            {
                result = devices.get_data(address).read_memory(address, length);
                return result;
            }

            //else use physical memory
            else
            {
                for (int i = 0; i < length; i++)
                {
                    result[i] = memory[address + i];
                }

                return result;
            }
        }

        private void write_memory(uint address, byte[] data)
        {
            //address is beyond physical memory, check MMIO devices instead
            if (address >= memory.Length)
            {
                devices.get_data(address).write_memory(address, data);
            }

            //else use physical memory
            for (int i = 0; i < data.Length; i++)
            {
                memory[address + i] = data[i];
            }

        }

        /// <summary>
        /// Set up all the MMIO devices attached to the processor
        /// </summary>
        private void initialize_MMIO(MMIO[] MMIO_devices)
        {
            //assign base addresses to all MMIO devices

            uint base_address = (uint)memory.Length;
            //for each device
            for (int i = 0; i < MMIO_devices.Length; i++)
            {
                //  give next base address (must be known for address translation)
                MMIO_devices[i].set_base_address(base_address);

                //  get size to calculate base address for next device
                uint next_base_address = base_address + MMIO_devices[i].get_size();

                //insert the device into the interval tree
                this.devices.insert(base_address, next_base_address, MMIO_devices[i]);

                //update the base address to be used next
                base_address = next_base_address;
            }

        }
        
        /// <summary>
        /// Processor with no MMIO devices
        /// </summary>
        public Processor() : this(new MMIO[] { }) { }


        /// <summary>
        /// Processor using the given MMIO devices
        /// </summary>
        /// <param name="MMIO_devices">Array of MMIO objects to map to memory</param>
        public Processor(MMIO[] MMIO_devices)
        {
            current_instruction = 0;

            memory = new byte[65536];

            this.devices = new IntervalTree<uint, MMIO>();
            initialize_MMIO(MMIO_devices);

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