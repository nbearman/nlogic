//using System;
//using System.Collections.Generic;
//using System.Diagnostics;

//namespace nlogic_sim
//{
//    [DebuggerDisplay("INST = {Utility.instruction_string_from_uint32(current_instruction)}")]
//    public partial class Processor
//    {
//        //reference to the containing simuation environment
//        private I_Environment environment;

//        //whether or not the trap is enabled
//        private bool trap_enabled;

//        //processor constants
//        public const ushort interrupt_handler_address = 0x9999; //TODO arbitrary address for now
//        public const ushort interrupt_register_dump_address = 0x0033; //TODO reevaluate if this is the best place for these; selected so that FLAG, GPA, and PC get stored at the end of the DMEM accessible region

//        //processor state
//        public ushort current_instruction;
//        private string current_instruction_expansion
//        { 
//            get { return Utility.instruction_string_from_uint32(current_instruction); }
//        }
//        //store the part of the processor state that needs to be restored if an instruction is retried
//        private LastStateCache last_instruction_cache = new LastStateCache();

//        public Dictionary<byte, I_Register> registers;

//        /// <summary>
//        /// Completes one cycle of work on the processor and returns the processor status (contents of FLAG).
//        /// </summary>
//        public uint cycle()
//        {
//            //check the status of the processor
//            //(and check for interrupts)
//            if (this.trap_enabled)
//            {
//                check_status();
//                //can possibly result in MMU fault while dumping registers
//                //can be avoided if dump addresses are guaranteed to be mapped
//                //<-- interrupt here results in registers not being saved
//                //      return to faulted process impossible
//            }

//            //load current instruction
//            load_current_instruction();
//                //can result in MMU fault while fetching instruction from memory
//                //<-- interrupt here during interrupt handler makes
//                //      handling MMU interrupts impossible
//                //  during normal process:
//                //      MMU interrupt triggered, no op returned as current instruction
//                //      

//            //increment program counter
//            ((Register_32)registers[PC]).data += 2;

//            //update COMPR, IADN, IADF, SKIP, RTRN
//            update_accessors();
//                //can result in MMU fault while fetching COMPR, IADN, IADF

//            //execute current instruction
//            execute();
//                //can result in MMU fault while writing to RMEM or WMEM
//                //no faults resulting from reads; reads updating registers
//                //are resolved before execute()

//            //return contents of FLAG
//            return Utility.uint32_from_byte_array(registers[FLAG].data_array);
//        }


//        /// <summary>
//        /// Send a signal to the processor on the given signal channel.
//        /// Throws an exception if the channel is out of range.
//        /// </summary>
//        /// <param name="channel"></param>
//        public void raise_signal(Interrupt interrupt_signal)
//        {

//            uint channel = interrupt_signal.channel;

//            //there is one channel per bit in the FLAG register, however:
//            //1 bit (MSB) is reserved for the UNLOCKED flag, to prevent overwriting flags
//            //1 bit (next MSB) is reserved as the DISABLED bit, to ignore interrupts
//            //1 bit is reserved for the DELAY flag
//            //1 bit is reserved for the RETRY flag
//            //1 bit is reserved for the KERNEL flag
//            //1 bit is reserved for the USER DISABLED flag
//            //1 bit is reserved for the USER DELAY flag
//            uint num_reserved_channels = 7;
//            uint num_channels = registers[FLAG].size_in_bytes * 8;
//            if (channel > (num_channels - num_reserved_channels - 1))
//            {
//                throw new ArgumentOutOfRangeException("channel", "Processor cannot receive signals on that channel");
//            }

//            //create the masks to apply to the FLAG register
//            //changing the bit of the signal channel and the corresponding flags to 1
//            uint retry_mask = (uint)0b1 << ((int)num_channels - (int)Flags.RETRY - 1);
//            uint kernel_mask = (uint)0b1 << ((int)num_channels - (int)Flags.KERNEL - 1);
//            uint signal_mask = (uint)0b1 << (int)channel;

//            //combine the masks into 1
//            uint combined_mask = retry_mask | kernel_mask | signal_mask;

//            //get the current contents of FLAG
//            uint flag_contents = Utility.uint32_from_byte_array(registers[FLAG].data_array);

//            //set the bit of the given signal channel and the flags by applying the mask
//            //bits that have already been set will not be unset
//            uint new_flag_contents = flag_contents | combined_mask;

//            //store the result back into FLAG
//            registers[FLAG].data_array = Utility.byte_array_from_uint32(registers[FLAG].size_in_bytes, new_flag_contents);

//        }

//        /// <summary>
//        /// Processor using the given simulation environment
//        /// Processor is ready to call cycle() after construction
//        /// </summary>
//        /// <param name="environment"></param>
//        public Processor(I_Environment environment, bool trap_enabled)
//        {
//            //save a reference to the simulation environment
//            this.environment = environment;

//            //allow the trap to be disabled, which prevents special behavior when FLAG is changed
//            this.trap_enabled = trap_enabled;

//            //give the environment a way to signal the processor
//            //necessary for other devices in the environment to send interrupts
//            //to the processor
//            environment.register_signal_callback(raise_signal);

//            //set the current instruction to a no-op just in case
//            current_instruction = 0;

//            initialize_registers();

//            //ensures the special registers (SKIP, COMPR, etc.) have the correct values before the first cycle
//            //must happen after registers are intialized
//            update_accessors();
//        }

//        /// <summary>
//        /// Execute the current instruction.
//        /// </summary>
//        private void execute()
//        {
//            byte[] instruction_array = Utility.byte_array_from_uint32(2, (uint)current_instruction);
//            byte source = instruction_array[0];
//            byte destination = instruction_array[1];

//            uint source_data;

//            //retrieve the source data

//            if (registers.ContainsKey(source))
//            {
//                source_data = ((Register_32)registers[source]).data;
//            }
//            else if (source < FLAG)
//            {
//                //if the source is less than FLAG, it's an immediate value
//                source_data = (uint)source;
//            }
//            else if (source >= DMEM)
//            {
//                //if the source is greater than the lowest DMEM instruction, its a DMEM instruction
//                //DMEM instructions read directly from memory

//                //calculate the target address
//                //lowest DMEM accesses address 0, DMEM + 1 -> address 1, etc.
//                uint address = (uint)source - (uint)DMEM;

//                source_data = Utility.uint32_from_byte_array(read_memory(address, 4));
//            }
//            else
//            {
//                //otherwise, its an unused instruction
//                source_data = 0;
//            }

//            //update the last instruction cache before changing the state of any registers
//            //cache the PC of the current instruction (PC is incremented before executing instructions)
//            this.last_instruction_cache.PC = ((Register_32)registers[PC]).data - 2;
//            this.last_instruction_cache.EXE = ((Register_32)registers[EXE]).data;
//            //destination needs to be cached in case we need to retry this instruction
//            //TODO determine if we still need to be caching this (for undoing writing to a register from a memory read?)
//            this.last_instruction_cache.stored_register = destination;
//            //TODO this conditional is temporary until it is determined what storing the destination is needed for
//            if (destination >= Processor.FLAG && destination < Processor.DMEM)
//                this.last_instruction_cache.stored_register_contents = ((Register_32)registers[destination]).data;

//            //store to the destination
//            //if not writeable, do nothing

//            if (registers.ContainsKey(destination) && registers[destination].writeable)
//            {
//                //only some bits of FLAG can be written if the UNLOCKED bit is not set
//                //use special writing behavior when writing to this register
//                if (destination == FLAG)
//                {
//                    write_FLAG(source_data);
//                }

//                //the destination is not FLAG; write the register normally
//                else
//                {
//                    //destination is a writeable register
//                    ((Register_32)registers[destination]).data = source_data;
//                }
//            }
//            else if (destination >= DMEM)
//            {
//                //destination is a DMEM location in memory
//                uint address = (uint)destination - (uint)DMEM;
//                byte[] data_bytes = Utility.byte_array_from_uint32(4, source_data);
//                write_memory(address, data_bytes);
//            }

//            //update result registers as needed

//            if (
//                destination == ALUM ||
//                destination == ALUA ||
//                destination == ALUB)
//            {
//                update_alu();
//            }

//            else if (
//                destination == FPUM ||
//                destination == FPUA ||
//                destination == FPUB)
//            {
//                update_fpu();
//            }

//            else if (destination == RMEM)
//            {
//                uint base_addr = ((Register_32)registers[RBASE]).data;
//                uint offset = ((Register_32)registers[ROFST]).data;
//                uint address = base_addr + offset;
//                byte[] data_array = Utility.byte_array_from_uint32(4, source_data);
//                write_memory(address, data_array);
//            }

//            else if (destination == WMEM)
//            {
//                uint base_addr = ((Register_32)registers[WBASE]).data;
//                uint offset = ((Register_32)registers[WOFST]).data;
//                uint address = base_addr + offset;
//                byte[] data_array = Utility.byte_array_from_uint32(4, source_data);
//                write_memory(address, data_array);
//            }


//        }

//        private void update_accessors()
//        {
//            //calculate the address of EXE + PC in memory
//            uint base_addr = ((Register_32)registers[EXE]).data;
//            uint offset = ((Register_32)registers[PC]).data;

//            //update SKIP and RETURN
//            ((Register_32)registers[SKIP]).data = ((Register_32)registers[PC]).data + 4;
//            ((Register_32)registers[RTRN]).data = ((Register_32)registers[PC]).data + 6;

//            //update IADN and IADF
//            byte[] iadn_value = read_memory(base_addr + offset, 4); //IADN value might be reused by COMPR
//            ((Register_32)registers[IADN]).data_array = iadn_value;
//            ((Register_32)registers[IADF]).data_array = read_memory(base_addr + offset + 2, 4);


//            //compare COMPA and COMPB
//            uint compa_value = ((Register_32)registers[COMPA]).data;
//            uint compb_value = ((Register_32)registers[COMPB]).data;
//            //if COMPA == COMPB, COMPR = memory[EXE + PC]
//            if (compa_value == compb_value)
//            {
//                ((Register_32)registers[COMPR]).data_array = iadn_value;
//            }

//            //else COMPR = memory[EXE + PC + 4]
//            else
//            {
//                ((Register_32)registers[COMPR]).data_array = read_memory(base_addr + offset + 4, 4);
//            }


//            update_accessor_a();
//            update_accessor_b();

//        }

//        /// <summary>
//        /// Store a new value to the FLAG register, following the lock adhering to the UNLOCKED bit
//        /// </summary>
//        /// <param name="data">Data to write to FLAG</param>
//        private void write_FLAG(uint data)
//        {
//            uint flag_data = ((Register_32)registers[FLAG]).data;

//            //check the unlocked bit
//            if (!Utility.get_flag_bit(flag_data, Flags.UNLOCKED))
//            {
//                //FLAG is not unlocked; prevent changing the upper 5 bits

//                //flag_data & M(ask) = (L)ocked portion = [L | 0000...]
//                //data & N(mask 2) = U(nlocked portion) = [...0000 | U]
//                //U | L = new flag contents             = [L | U]

//                //create M and N
//                uint M = ((uint)0b11111) << 27;
//                uint N = ~M;

//                //create L and U
//                uint L = flag_data & M;
//                uint U = data & N;

//                //create the result
//                uint R = L | U;

//                //store the result into FLAG
//                ((Register_32)registers[FLAG]).data = R;

//            }
//            else
//            {
//                //FLAG is unlocked; directly write the new data
//                ((Register_32)registers[FLAG]).data = data;
//            }
//        }

//        private void update_accessor_a()
//        {
//            uint base_addr = ((Register_32)registers[RBASE]).data;
//            uint offset = ((Register_32)registers[ROFST]).data;
//            uint address = base_addr + offset;
//            byte[] data_array = read_memory(address, 4);
//            uint data = Utility.uint32_from_byte_array(data_array);
//            ((Register_32)registers[RMEM]).data = data;
//        }

//        private void update_accessor_b()
//        {
//            uint base_addr = ((Register_32)registers[WBASE]).data;
//            uint offset = ((Register_32)registers[WOFST]).data;
//            uint address = base_addr + offset;
//            byte[] data_array = read_memory(address, 4);
//            uint data = Utility.uint32_from_byte_array(data_array);
//            ((Register_32)registers[WMEM]).data = data;
//        }

//        private void update_fpu()
//        {
//            float arga = ((Register_32)registers[FPUA]).float_data();
//            float argb = ((Register_32)registers[FPUB]).float_data();
//            uint mode = ((Register_32)registers[FPUM]).data;

//            float result = 0;

//            if (mode == (uint)FPU_MODE.NoOp)
//            {
//                return;
//            }

//            else if (mode == (uint)FPU_MODE.Add)
//            {
//                result = arga + argb;
//            }

//            else if (mode == (uint)FPU_MODE.Subtract)
//            {
//                result = arga - argb;
//            }

//            else if (mode == (uint)FPU_MODE.Multiply)
//            {
//                result = arga * argb;
//            }

//            else if (mode == (uint)FPU_MODE.Divide)
//            {
//                result = arga / argb;
//            }

//            else
//            {
//                return;
//            }

//            byte[] data_array = Utility.byte_array_from_float(result);
//            uint data = Utility.uint32_from_byte_array(data_array);
//            ((Register_32)registers[FPUR]).data = data;
//        }

//        private void update_alu()
//        {
//            uint arga = ((Register_32)registers[ALUA]).data;
//            uint argb = ((Register_32)registers[ALUB]).data;
//            uint mode = ((Register_32)registers[ALUM]).data;

//            if (mode == (uint)ALU_MODE.NoOp)
//            {
//                return;
//            }

//            else if (mode == (uint)ALU_MODE.Add)
//            {
//                ((Register_32)registers[ALUR]).data = arga + argb;
//            }

//            else if (mode == (uint)ALU_MODE.Subtract)
//            {
//                ((Register_32)registers[ALUR]).data = arga - argb;
//            }

//            else if (mode == (uint)ALU_MODE.Multiply)
//            {
//                ((Register_32)registers[ALUR]).data = arga * argb;
//            }

//            else if (mode == (uint)ALU_MODE.Divide)
//            {
//                ((Register_32)registers[ALUR]).data = arga / argb;
//            }

//            else if (mode == (uint)ALU_MODE.ShiftLeft)
//            {
//                ((Register_32)registers[ALUR]).data = arga << (int)argb;
//            }

//            else if (mode == (uint)ALU_MODE.ShiftRight)
//            {
//                ((Register_32)registers[ALUR]).data = arga >> (int)argb;
//            }

//            else if (mode == (uint)ALU_MODE.OR)
//            {
//                ((Register_32)registers[ALUR]).data = arga | argb;
//            }

//            else if (mode == (uint)ALU_MODE.AND)
//            {
//                ((Register_32)registers[ALUR]).data = arga & argb;
//            }

//            else if (mode == (uint)ALU_MODE.NOR)
//            {
//                ((Register_32)registers[ALUR]).data = ~(arga | argb);
//            }

//            else if (mode == (uint)ALU_MODE.NAND)
//            {
//                ((Register_32)registers[ALUR]).data = ~(arga & argb);
//            }

//            else if (mode == (uint)ALU_MODE.XOR)
//            {
//                ((Register_32)registers[ALUR]).data = arga ^ argb;
//            }

//            else
//            {
//                return;
//            }
//        }

//        /// <summary>
//        /// Fills current_instruction with the value in memory pointed to by [EXE + PC]
//        /// </summary>
//        private void load_current_instruction()
//        {
//            uint base_addr = ((Register_32)registers[EXE]).data;
//            uint offset = ((Register_32)registers[PC]).data;
//            uint address = base_addr + offset;
//            byte[] bytes = read_memory(address, 2);
//            ushort instruction = Utility.ushort_from_byte_array(bytes);
//            current_instruction = instruction;
//        }

//        /// <summary>
//        /// Return an array of length bytes from memory, starting at address.
//        /// Results may come from a memory-mapped input-output device
//        /// </summary>
//        private byte[] read_memory(uint address, uint length)
//        {
//            return environment.read_address(address, length);
//        }

//        private void write_memory(uint address, byte[] data)
//        {
//            environment.write_address(address, data);
//        }


//        /// <summary>
//        /// Check the status of processor, handling interrupts as appropriate
//        /// </summary>
//        private void check_status()
//        {

//            //retrieve the contents of FLAG, the control register
//            uint flag_contents = ((Register_32)registers[FLAG]).data;

//            if (flag_contents == 0)
//            {
//                //do nothing if FLAG is clear
//                return;
//            }            


//            //ignore flag is 1 if iterrupts should be ignored
//            if (Utility.get_flag_bit(flag_contents, Flags.DISABLED))
//            {
//                //do not enter the trap; do nothing
//                return;
//            }

//            //check if the DELAY flag is set
//            if (Utility.get_flag_bit(flag_contents, Flags.DELAY))
//            {
//                //the delay flag is set
//                //change it to 0
//                flag_contents = Utility.clear_flag_bit(flag_contents, Flags.DELAY);

//                //store the updated flag contents back to the FLAG register
//                ((Register_32)registers[FLAG]).data = flag_contents;

//                //do nothing this cycle, return
//                return;
//            }

//            //determine if the kernel needs to handle this interrupt
//            if (Utility.get_flag_bit(flag_contents, Flags.KERNEL))
//            {
//                //disable interrupts
//                flag_contents = Utility.set_flag_bit(flag_contents, Flags.DISABLED);

//                //unlock the FLAG register
//                flag_contents = Utility.set_flag_bit(flag_contents, Flags.UNLOCKED);

//                //send the kernel signal to the environment
//                environment.signal_kernel_mode();
//            }

//            //otherwise, this interrupt can be handled in user mode
//            else
//            {
//                //if user interrupts are disabled, return
//                if (Utility.get_flag_bit(flag_contents, Flags.USER_DISABLED))
//                {
//                    return;
//                }

//                //check if the USER DELAY flag is set
//                if (Utility.get_flag_bit(flag_contents, Flags.USER_DELAY))
//                {
//                    //the delay flag is set
//                    //change it to 0
//                    flag_contents = Utility.clear_flag_bit(flag_contents, Flags.USER_DELAY);

//                    //store the updated flag contents back to the FLAG register
//                    ((Register_32)registers[FLAG]).data = flag_contents;

//                    //do nothing this cycle, return
//                    return;
//                }

//                //disabled user interrupts
//                flag_contents = Utility.set_flag_bit(flag_contents, Flags.USER_DISABLED);
//            }

//            //dump FLAG, GPA, and processor state
//            //TODO determine if any other registers need to be dumped

//            //create a list (to convert to array) of all the data that will be dumped to memory
//            //in order to consolidate the dump to a single call to write_memory()
//            List<byte> dumped_data = new List<byte>();

//            //add the contents of FLAG and GPA
//            dumped_data.AddRange(((Register_32)registers[FLAG]).data_array);
//            dumped_data.AddRange(((Register_32)registers[GPA]).data_array);

//            //if the RETRY flag is set, dump the last instruction cache
//            if (Utility.get_flag_bit(flag_contents, Flags.RETRY))
//            {
//                //add the contents of the last instruction cache
//                dumped_data.AddRange(Utility.byte_array_from_uint32(4, last_instruction_cache.PC));
//                dumped_data.AddRange(Utility.byte_array_from_uint32(4, last_instruction_cache.EXE));
//                dumped_data.AddRange(Utility.byte_array_from_uint32(4, last_instruction_cache.stored_register_contents));
//                dumped_data.Add(last_instruction_cache.stored_register);
//            }

//            //otherwise, dump the current instruction
//            else
//            {
//                dumped_data.AddRange(((Register_32)registers[PC]).data_array);
//                dumped_data.AddRange(((Register_32)registers[EXE]).data_array);
//            }

//            //write the dumped data to memory
//            write_memory(interrupt_register_dump_address, dumped_data.ToArray());

//            //set PC to the interrupt handler location
//            ((Register_32)registers[PC]).data = interrupt_handler_address;

//            //store the updated flag contents back to the FLAG register
//            ((Register_32)registers[FLAG]).data = flag_contents;

//            //return
//            return;

//        }


//        /// <summary>
//        /// Fill the registers dictionary
//        /// </summary>
//        private void initialize_registers()
//        {
//            this.registers = new Dictionary<byte, I_Register>
//                {
//                    {FLAG, (new Register_32(
//                        "Status Register",
//                        "FLAG",
//                        true))},
//                    {EXE, (new Register_32(
//                        "Program Counter Base Register",
//                        "EXE",
//                        true))},
//                    {PC, (new Register_32(
//                        "Program Counter",
//                        "PC",
//                        true))},
//                    {ALUM, (new Register_32(
//                        "Arithmetic Logic Unit Mode",
//                        "ALUM",
//                        true))},
//                    {ALUA, (new Register_32(
//                        "Arithmetic Logic Unit Input A",
//                        "ALUA",
//                        true))},
//                    {ALUB, (new Register_32(
//                        "Arithmetic Logic Unit Input B",
//                        "ALUB",
//                        true))},
//                    {ALUR, (new Register_32(
//                        "Arithmetic Logic Unit Result",
//                        "ALUR",
//                        false))},
//                    {FPUM, (new Register_32(
//                        "Floating Point Unit Mode",
//                        "FPUM",
//                        true))},
//                    {FPUA, (new Register_32(
//                        "Floating Point Unit Input A",
//                        "FPUA",
//                        true))},
//                    {FPUB, (new Register_32(
//                        "Floating Point Unit Input B",
//                        "FPUB",
//                        true))},
//                    {FPUR, (new Register_32(
//                        "Floating Point Unit Result",
//                        "FPUR",
//                        false))},
//                    {RBASE, (new Register_32(
//                        "Memory Accessor A Base Register",
//                        "RBASE",
//                        true))},
//                    {ROFST, (new Register_32(
//                        "Memory Accessor A Offset Register",
//                        "ROFST",
//                        true))},
//                    {RMEM, (new Register_32(
//                        "Memory Accessor A Result",
//                        "RMEM",
//                        true))},
//                    {WBASE, (new Register_32(
//                        "Memory Accessor B Base Register",
//                        "WBASE",
//                        true))},
//                    {WOFST, (new Register_32(
//                        "Memory Accessor B Offset Register",
//                        "WOFST",
//                        true))},
//                    {WMEM, (new Register_32(
//                        "Memory Accessor B Result",
//                        "WMEM",
//                        true))},
//                    {GPA, (new Register_32(
//                        "General Purpose Register A",
//                        "GPA",
//                        true))},
//                    {GPB, (new Register_32(
//                        "General Purpose Register B",
//                        "GPB",
//                        true))},
//                    {GPC, (new Register_32(
//                        "General Purpose Register C",
//                        "GPC",
//                        true))},
//                    {GPD, (new Register_32(
//                        "General Purpose Register D",
//                        "GPD",
//                        true))},
//                    {GPE, (new Register_32(
//                        "General Purpose Register E",
//                        "GPE",
//                        true))},
//                    {GPF, (new Register_32(
//                        "General Purpose Register F",
//                        "GPF",
//                        true))},
//                    {GPG, (new Register_32(
//                        "General Purpose Register G",
//                        "GPG",
//                        true))},
//                    {GPH, (new Register_32(
//                        "General Purpose Register H",
//                        "GPH",
//                        true))},
//                    {COMPA, (new Register_32(
//                        "Branching Comparator Input A",
//                        "COMPA",
//                        true))},
//                    {COMPB, (new Register_32(
//                        "Branching Comparator Input B",
//                        "COMPB",
//                        true))},
//                    {COMPR, (new Register_32(
//                        "Branching Comparator Read Result",
//                        "COMPR",
//                        false))},
//                    {IADN, (new Register_32(
//                        "Near Instructions-as-data Accessor",
//                        "IADN",
//                        false))},
//                    {IADF, (new Register_32(
//                        "Far Instructions-as-data Accessor",
//                        "IADF",
//                        false))},
//                    {LINK, (new Register_32(
//                        "Link Register",
//                        "LINK",
//                        true))},
//                    {SKIP, (new Register_32(
//                        "Skip Immediate Address Register",
//                        "SKIP",
//                        false))},
//                    {RTRN, (new Register_32(
//                        "Return Address Register",
//                        "RTRN",
//                        false))},
//                };
//        }
//    }

//}