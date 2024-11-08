using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace nlogic_sim
{
    [DebuggerDisplay("INST = {Utility.instruction_string_from_uint32(current_instruction)}")]
    public partial class Processor
    {
        private I_Environment environment;

        public const ushort interrupt_handler_address = 0x600; //TODO arbitrary address for now; must match location in 1_handler.pro
        public const ushort interrupt_register_dump_address = 0x0028; //TODO reevaluate if this is the best place for these; selected so that FLAG, GPA, and PC get stored at the end of the DMEM accessible region

        private ushort current_instruction;

        private uint cycle_count = 0; // count total number of cycles for visualizer only; not accessible by the processor

        private LastStateCache last_instruction_cache;

        public Dictionary<byte, I_Register> registers;

        public Processor(I_Environment environment)
        {
            this.environment = environment;
            environment.register_signal_callback(raise_signal);
            current_instruction = 0;
            initialize_registers();
            update_read_only_registers(0);
        }

        public uint cycle()
        {
            this.cycle_count += 1;

            //check the status register to determine if we should trap
            check_status();

            uint pc = ((Register_32)registers[PC]).data;
            uint exe = ((Register_32)registers[EXE]).data;

            //fetch from memory and decode the current instruction
            //[memory read]
            byte[] instruction_array = read_memory(pc + exe, 2);
            byte source = instruction_array[0];
            byte destination = instruction_array[1];

            Debug.Assert(source <= RTRN || source >= DMEM, "source is unused processor location: " + source.ToString());
            Debug.Assert(destination <= RTRN || destination >= DMEM, "destination is unused processor location: " + destination.ToString());

            //[debug]
            //set the current instruction (currently only used for the debugger readout)
            this.current_instruction = (ushort)Utility.uint32_from_byte_array(instruction_array);

            //update the last instruction cache to hold the state of processor before executing this instruction
            //store the intended register for this instruction (and its current contents), just in case this
            //instruction clobbers that register with data from a read the MMU rejected
            this.last_instruction_cache.PC = pc;
            this.last_instruction_cache.EXE = exe;
            this.last_instruction_cache.stored_register = destination;
            if (destination >= FLAG
                && destination < DMEM
                && !memory_write_registers.Contains(destination)
                && !read_only_registers.Contains(destination))
                this.last_instruction_cache.stored_register_contents = ((Register_32)registers[destination]).data;
            else
                this.last_instruction_cache.stored_register_contents = 0;

            //increment the program counter
            ((Register_32)registers[PC]).data = pc += 2;

            //update the read-only registers
            update_read_only_registers(pc);



            //execute source -> destination

            //retrieve the source data
            uint source_data;
            if (source < FLAG)
            {
                //source is an immediate value
                source_data = (uint)source;
            }
            else if (memory_read_registers.Contains(source) || source >= DMEM)
            {
                //source data comes from memory
                //determine the address of the source data in memory
                uint source_address;
                if (source == COMPR)
                    if (((Register_32)registers[COMPA]).data == ((Register_32)registers[COMPB]).data)
                        source_address = exe + pc;
                    else
                        source_address = exe + pc + 4;
                else if (source == IADF)
                    source_address = exe + pc + 2;
                else if (source == IADN)
                    source_address = exe + pc;
                else if (source == RMEM)
                    source_address = ((Register_32)registers[RBASE]).data + ((Register_32)registers[ROFST]).data;
                else if (source == WMEM)
                    source_address = ((Register_32)registers[WBASE]).data + ((Register_32)registers[WOFST]).data;
                else if (source >= DMEM)
                    source_address = source - (uint)DMEM;
                else
                    throw new NotImplementedException("source register in memory_read_registers has no access logic");

                //retrieve the data at the source address
                //[memory read]
                source_data = Utility.uint32_from_byte_array(read_memory(source_address, 4));
            }
            else
            {
                //source data comes from on processor
                source_data = ((Register_32)registers[source]).data;
            }


            //write to the destination

            if (destination == Processor.BREAKPOINT)
            {
                //special breakpoint logic
                //not part of the spec; if the instruction writes to 7B, do not do anything
                //and return the breakpoint code as this cycle's status so the simulation environment
                //can enable the visualizer
                return Processor.BREAKPOINT;
            }

            uint destination_address;
            if (destination < FLAG)
            {
                //destination is an immediate value
                //do nothing
            }
            else if (read_only_registers.Contains(destination))
            {
                //destination is read-only
                //do nothing
            }
            else if (memory_write_registers.Contains(destination) || destination >= DMEM)
            {
                //destination is in memory
                if (destination == RMEM)
                    destination_address = ((Register_32)registers[RBASE]).data + ((Register_32)registers[ROFST]).data;
                else if (destination == WMEM)
                    destination_address = ((Register_32)registers[WBASE]).data + ((Register_32)registers[WOFST]).data;
                else if (destination >= DMEM)
                    destination_address = destination - (uint)DMEM;
                else
                    throw new NotImplementedException("destination register in memory_write_registers has no access logic");

                //write the source data to the destination
                //[memory write]
                write_memory(destination_address, Utility.byte_array_from_uint32(4, source_data));
            }
            else
            {
                //destination is on the processor
                write_register(destination, source_data);
            }



            //return the contents of FLAG
            return ((Register_32)registers[FLAG]).data;
        }

        public void raise_signal(Interrupt interrupt_signal)
        {

            uint channel = interrupt_signal.channel;

            //there is one channel per bit in the FLAG register, however:
            //1 bit (MSB) is reserved for the UNLOCKED flag, to prevent overwriting flags
            //1 bit (next MSB) is reserved as the DISABLED bit, to ignore interrupts
            //1 bit is reserved for the DELAY flag
            //1 bit is reserved for the RETRY flag
            //1 bit is reserved for the KERNEL flag
            //1 bit is reserved for the USER DISABLED flag
            //1 bit is reserved for the USER DELAY flag
            uint num_reserved_channels = 7;
            uint num_channels = registers[FLAG].size_in_bytes * 8;
            if (channel > (num_channels - num_reserved_channels - 1))
            {
                throw new ArgumentOutOfRangeException("channel", "Processor cannot receive signals on that channel");
            }

            //create the masks to apply to the FLAG register
            //changing the bit of the signal channel and the corresponding flags to 1
            uint retry_mask = (uint)0b1 << ((int)num_channels - (int)Flags.RETRY - 1);
            uint kernel_mask = (uint)0b1 << ((int)num_channels - (int)Flags.KERNEL - 1);
            uint signal_mask = (uint)0b1 << (int)channel;

            //combine the masks into 1
            uint combined_mask = retry_mask | kernel_mask | signal_mask;

            //get the current contents of FLAG
            uint flag_contents = Utility.uint32_from_byte_array(registers[FLAG].data_array);

            //set the bit of the given signal channel and the flags by applying the mask
            //bits that have already been set will not be unset
            uint new_flag_contents = flag_contents | combined_mask;

            //store the result back into FLAG
            registers[FLAG].data_array = Utility.byte_array_from_uint32(registers[FLAG].size_in_bytes, new_flag_contents);

        }

        private void update_read_only_registers(uint pc)
        {
            ((Register_32)registers[SKIP]).data = pc + 4;
            ((Register_32)registers[RTRN]).data = pc + 6;
            update_alu();
            update_fpu();
        }

        private void write_register(byte destination, uint data)
        {
            if (destination == FLAG)
            {
                write_FLAG(data);
            }
            else
            {
                ((Register_32)registers[destination]).data = data;
            }
        }

        private void write_FLAG(uint data)
        {
            uint flag_data = ((Register_32)registers[FLAG]).data;

            //check the unlocked bit
            if (!Utility.get_flag_bit(flag_data, Flags.UNLOCKED))
            {
                //FLAG is not unlocked; prevent changing the upper 4 bits
                //  UNLOCKED should not be writable because a user could use it to circumvent the lock
                //  DISABLED should not be writable because a user could disable all interrupts, e.g. MMU
                //  DELAY should not be writable because a user could set it to postpone all interrupts
                //  RETRY (TODO does this need to locked? Probably, but describe situation)
                //  KERNEL should be writable so a user can initiate a kernel interrupt (syscall)
                //      There is no risk of user overwriting K, because signals are processed after a cycle,
                //      so user will never see FLAG when it is non-0 if K is set

                //flag_data & M(ask) = (L)ocked portion = [L | 0000...]
                //data & N(mask 2) = U(nlocked portion) = [...0000 | U]
                //U | L = new flag contents             = [L | U]

                //create M and N
                uint M = ((uint)0b1111) << 28; //TODO a certain number of signal bits should be locked and only writable by hardware interrupters
                uint N = ~M;

                //create L and U
                uint L = flag_data & M;
                uint U = data & N;

                //create the result
                uint R = L | U;

                //store the result into FLAG
                ((Register_32)registers[FLAG]).data = R;

            }
            else
            {
                //FLAG is unlocked; directly write the new data
                ((Register_32)registers[FLAG]).data = data;
            }
        }

        private byte[] read_memory(uint address, uint length)
        {
            return environment.read_address(address, length);
        }

        private void write_memory(uint address, byte[] data)
        {
            environment.write_address(address, data);
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

        private void check_status()
        {

            //retrieve the contents of FLAG, the control register
            uint flag_contents = ((Register_32)registers[FLAG]).data;

            if (flag_contents == 0)
            {
                //do nothing if FLAG is clear
                return;
            }


            //ignore flag is 1 if iterrupts should be ignored
            if (Utility.get_flag_bit(flag_contents, Flags.DISABLED))
            {
                //do not enter the trap; do nothing
                return;
            }

            //check if the DELAY flag is set
            if (Utility.get_flag_bit(flag_contents, Flags.DELAY))
            {
                //the delay flag is set
                //change it to 0
                flag_contents = Utility.clear_flag_bit(flag_contents, Flags.DELAY);

                //store the updated flag contents back to the FLAG register
                ((Register_32)registers[FLAG]).data = flag_contents;

                //do nothing this cycle, return
                return;
            }

            //determine if the kernel needs to handle this interrupt
            if (Utility.get_flag_bit(flag_contents, Flags.KERNEL))
            {
                //disable interrupts
                flag_contents = Utility.set_flag_bit(flag_contents, Flags.DISABLED);

                //unlock the FLAG register
                flag_contents = Utility.set_flag_bit(flag_contents, Flags.UNLOCKED);

                //send the kernel signal to the environment
                //TODO on page faults, this might not be necessary anymore (because there is no kernel mode)
                //however, signalling the MMU to switch to the kernel page table might be necessary for interrupts
                //etc. that do not originate from the MMU
                environment.signal_kernel_mode();
            }

            //otherwise, this interrupt can be handled in user mode
            else
            {
                //if user interrupts are disabled, return
                if (Utility.get_flag_bit(flag_contents, Flags.USER_DISABLED))
                {
                    return;
                }

                //check if the USER DELAY flag is set
                if (Utility.get_flag_bit(flag_contents, Flags.USER_DELAY))
                {
                    //the delay flag is set
                    //change it to 0
                    flag_contents = Utility.clear_flag_bit(flag_contents, Flags.USER_DELAY);

                    //store the updated flag contents back to the FLAG register
                    ((Register_32)registers[FLAG]).data = flag_contents;

                    //do nothing this cycle, return
                    return;
                }

                //disabled user interrupts
                flag_contents = Utility.set_flag_bit(flag_contents, Flags.USER_DISABLED);
            }

            //dump FLAG, GPA, and processor state
            //TODO determine if any other registers need to be dumped
            //  GPA might not need to be dumped if the interrupt handler can succesfully dump without using GPA

            //create a list (to convert to array) of all the data that will be dumped to memory
            //in order to consolidate the dump to a single call to write_memory()
            List<byte> dumped_data = new List<byte>();

            //add the contents of FLAG and GPA
            dumped_data.AddRange(((Register_32)registers[FLAG]).data_array);
            dumped_data.AddRange(((Register_32)registers[GPA]).data_array);

            //if the RETRY flag is set, dump the last instruction cache
            if (Utility.get_flag_bit(flag_contents, Flags.RETRY))
            {
                //add the contents of the last instruction cache
                dumped_data.AddRange(Utility.byte_array_from_uint32(4, last_instruction_cache.PC));
                dumped_data.AddRange(Utility.byte_array_from_uint32(4, last_instruction_cache.EXE));
                dumped_data.AddRange(Utility.byte_array_from_uint32(4, last_instruction_cache.stored_register_contents));
                //instead of storing the location itself, store the stack offset where that location's contents are dumped by the handler
                //this is for the convenience of the interrupt handler
                byte register_stack_offset_byte = 0;
                bool valid_offset_mapping = interrupt_dump_register_locations.TryGetValue(last_instruction_cache.stored_register, out register_stack_offset_byte);
                //if a fault occurred during instruction fetch, the stored destination will be invalid (0)
                //the interrupt handler doesn't need to restore the destination's old contents, so set a sentinel value (0xFFFFFFFF) that the handler will check for
                uint register_stack_offset = valid_offset_mapping ? register_stack_offset_byte : 0xFFFFFFFF;
                dumped_data.AddRange(Utility.byte_array_from_uint32(4, register_stack_offset));
            }

            //otherwise, dump the current instruction
            else
            {
                dumped_data.AddRange(((Register_32)registers[PC]).data_array);
                dumped_data.AddRange(((Register_32)registers[EXE]).data_array);
            }

            //write the dumped data to memory
            //(since this goes through MMU, this page must be mapped and writes must be allowed)
            write_memory(interrupt_register_dump_address, dumped_data.ToArray());

            //set PC to the interrupt handler location
            ((Register_32)registers[PC]).data = interrupt_handler_address;

            //store the updated flag contents back to the FLAG register
            ((Register_32)registers[FLAG]).data = flag_contents;

            //return
            return;

        }

        public uint get_cycle_count()
        {
            return this.cycle_count;
        }

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
                    //{RMEM, (new Register_32(
                    //    "Memory Accessor A Result",
                    //    "RMEM",
                    //    true))},
                    {WBASE, (new Register_32(
                        "Memory Accessor B Base Register",
                        "WBASE",
                        true))},
                    {WOFST, (new Register_32(
                        "Memory Accessor B Offset Register",
                        "WOFST",
                        true))},
                    //{WMEM, (new Register_32(
                    //    "Memory Accessor B Result",
                    //    "WMEM",
                    //    true))},
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
                    //{COMPR, (new Register_32(
                    //    "Branching Comparator Read Result",
                    //    "COMPR",
                    //    false))},
                    //{IADN, (new Register_32(
                    //    "Near Instructions-as-data Accessor",
                    //    "IADN",
                    //    false))},
                    //{IADF, (new Register_32(
                    //    "Far Instructions-as-data Accessor",
                    //    "IADF",
                    //    false))},
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

        /// <summary>
        /// The list of registers which must be restored when if an instruction that
        /// writes to them is retried.
        /// </summary>
        public static List<byte> cacheable_registers = new List<byte>(new byte[]
        {
            COMPA,
            COMPB,
            RBASE,
            ROFST,
            WBASE,
            WOFST,
        });

        public static HashSet<byte> memory_write_registers = new HashSet<byte>
        {
            RMEM,
            WMEM,
        };

        public static HashSet<byte> memory_read_registers = new HashSet<byte>
        {
            COMPR,
            IADF,
            IADN,
            RMEM,
            WMEM,
        };

        public static HashSet<byte> read_only_registers = new HashSet<byte>
        {
            ALUR,
            FPUR,
            RTRN,
            SKIP,
        };

        /// <summary>
        /// Mapping of processor locations to offsets into the kernel stack.
        /// The contents of each location is written to the kernel stack at the given
        /// offset when the processor is dumped during the start of the interrupt
        /// handler. These offsets are determined by the interrupt handler code (1_handler.pro right now)
        /// If that code changes the order the registers are stored, this needs to be updated.
        /// 
        /// TODO this is a convenient way to tell the interrupt handler where to write the store_register_contents
        /// from the last intstruction cache if we need to restore that register's contents during a retry interrupt.
        /// This offset will be stored with the last instruction cache, so to replace the destination register's dumped
        /// contents with its previous contents, the interrupt handler can use the offset provided from this map
        /// to know where in the stacked register dump to write.
        /// 
        /// We could just save the processor location, but then the interrupt handler would need to use a huge conditional
        /// to calculate the offset where it should overwrite. We make this easier for the interrupt handler by just providing
        /// the offset directly from this mapping.
        /// 
        /// Another possible alternative would be have the trap (in the simulator) directly replace the contents of the register
        /// that needs to be restored instead of forcing the interrupt handler to do it.
        /// 
        /// This should probably be changed because it doesn't seem true to how the hardware might work (would this mapping be stored in hardware somehow?)
        /// </summary>
        public static Dictionary<byte, byte> interrupt_dump_register_locations = new Dictionary<byte, byte>()
        {
            { GPA, 0x00 },
            { GPB, 0x04 },
            { GPC, 0x08 },
            { GPD, 0x0C },
            { GPE, 0x10 },
            { GPF, 0x14 },
            { GPG, 0x18 },
            { GPH, 0x1C },

            { COMPA, 0x20 },
            { COMPB, 0x24 },
            { RBASE, 0x28 },
            { ROFST, 0x2C },
            { ALUM, 0x30 },
            { ALUA, 0x34 },
            { ALUB, 0x38 },
            { FPUM, 0x3C },
            { FPUA, 0x40 },
            { FPUB, 0x44 },

            { WBASE, 0x48 },
            { WOFST, 0x4C },
        };
    }
}