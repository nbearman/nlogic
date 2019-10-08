using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace nlogic_sim
{
    /// <summary>
    /// The simulation environment is the object that manages the processor and all other devices
    /// the processor is connected to. The simulation environment is responsible for processing
    /// reads and writes from the processor as well as transmitting signals from devices to the
    /// processor. The simulation environment handles address translation from physical address
    /// of reads and writes from the processor to the local address of the MMIO device.
    /// </summary>
    public class SimulationEnvironment
    {

        /// <summary>
        /// An interval tree which maps uint addresses to (uint base address, MMIO device) tuples
        /// Indexed by an address, returns a tuple of the base address of that range and the
        /// device whose range that address belongs to.
        /// Storing the base address with the device this way makes translating addresses from CPU space
        /// to local MMIO device space simple for the environment, and removes the need for an additional
        /// data structure to hold the base address of each attached device.
        /// </summary>
        private IntervalTree<uint, Tuple<uint, MMIO>> MMIO_devices_by_address;

        //physical memory
        public byte[] memory;

        //the processor managed by this simulated environment
        private Processor processor;

        //true if the processor has registered its signal callback with the environments
        private bool signal_callback_registered = false;
        //the callback to raise signals for the processor
        private Action<uint> processor_signal_callback;

        private object signal_queue_mutex = new object();
        private Queue<uint> signal_queue = new Queue<uint>();

        public SimulationEnvironment(uint memory_size, byte[] initial_memory, MMIO[] MMIO_devices)
        {
            //create memory
            memory = new byte[memory_size];

            //create the MMIO devices
            this.MMIO_devices_by_address = new IntervalTree<uint, Tuple<uint, MMIO>>();
            initialize_MMIO(MMIO_devices);

            //initialize the hardware interrupters
            initialize_hardware_interrupters(new HardwareInterrupter[] { });

            //create the processor
            this.processor = new Processor(this);

            //initialize memory
            this.write_address(0, initial_memory);

            //cannot begin execution unless signal callback has been registered
            //if this assertion fails, the processor is not configured to correctly
            //communicate with the simulation environment
            Debug.Assert(this.signal_callback_registered);
        }

        public void run(bool visualizer_enabled, uint halt_status)
        {
            if (visualizer_enabled)
            {
                this.processor.initialize_visualizer();
            }

            while (((Register_32)this.processor.registers[Processor.FLAG]).data != halt_status)
            {
                //display the visualizer if it is enabled
                if (visualizer_enabled)
                {
                    this.processor.print_current_state();
                    Console.ReadKey();
                }

                //send outstanding interrupts to the processor
                this.resolve_signal_queue();

                //cycle the processor
                this.processor.cycle();
            }

        }

        /// <summary>
        /// Returns the data from the given physical address
        /// The data returned is not necessarily from memory;
        /// it could come from MMIO devices
        /// </summary>
        /// <param name="address">Address to begin reading at</param>
        /// <param name="length">Number of bytes to read</param>
        /// <returns>Returns an array of size length containing the data from the given address</returns>
        public byte[] read_address(uint address, uint length)
        {
            byte[] result = new byte[length];

            //address is beyond physical memory, check MMIO devices instead
            if (address >= memory.Length)
            {
                //get the target device and base address
                Tuple<uint, MMIO> target_device = this.MMIO_devices_by_address.get_data(address);
                uint base_address = target_device.Item1;
                MMIO device = target_device.Item2;

                //translate the processor's requested address to the address space of the device
                uint translated_address = address - base_address;

                //read the data from the device at the translated address
                result = device.read_memory(translated_address, length);
                return result;
            }

            //else use physical memory
            //no translation required, because physical memory always has a base address of 
            //read the bytes from memory at the given address
            else
            {
                for (int i = 0; i < length; i++)
                {
                    result[i] = memory[address + i];
                }

                return result;
            }

        }

        /// <summary>
        /// Writes the given data to the given address,
        /// which may not necessarily be in memory
        /// </summary>
        /// <param name="address">Starting address to write the data to</param>
        /// <param name="data_array">Array of bytes to write at the address</param>
        public void write_address(uint address, byte[] data_array)
        {

            //address is beyond physical memory, check MMIO devices instead
            if (address >= memory.Length)
            {
                //get the target device and base address
                Tuple<uint, MMIO> target_device = this.MMIO_devices_by_address.get_data(address);
                uint base_address = target_device.Item1;
                MMIO device = target_device.Item2;

                //translate the processor's requested address to the address space of the device
                uint translated_address = address - base_address;

                //read the data from the device at the translated address
                device.write_memory(translated_address, data_array);
            }

            //else use physical memory
            //no translation required, because physical memory always has a base address of 
            //write the given bytes to memory at the given address
            for (int i = 0; i < data_array.Length; i++)
            {
                memory[address + i] = data_array[i];
            }

            return;
        }

        /// <summary>
        /// Called by the processor to supply the environment 
        /// with a way to send a signal back to the processor.
        /// The signal callback can be used by the environment
        /// (or devices in the environment) to send signals,
        /// like interrupts, to the processor.
        /// </summary>
        /// <param name="signal_callback">
        /// A method which takes a channel number as a parameter.
        /// Invoking signal_callback(channel_number) will send a signal on
        /// the corresponding signal wire to the processor.
        /// </param>
        public void register_signal_callback(Action<uint> signal_callback)
        {
            this.processor_signal_callback = signal_callback;
            this.signal_callback_registered = true;
        }

        /// <summary>
        /// Set up all the MMIO devices in the environment
        /// </summary>
        private void initialize_MMIO(MMIO[] MMIO_devices)
        {
            //assign base addresses to all MMIO devices

            uint base_address = (uint)memory.Length;
            //for each device
            for (int i = 0; i < MMIO_devices.Length; i++)
            {
                //get size to calculate base address for next device
                uint next_base_address = base_address + MMIO_devices[i].get_size();

                //insert the device into the interval tree
                Tuple<uint, MMIO> base_address_device_pair = new Tuple<uint, MMIO>(base_address, MMIO_devices[i]);
                this.MMIO_devices_by_address.insert(base_address, next_base_address, base_address_device_pair);

                //update the base address to be used next
                base_address = next_base_address;
            }

        }

        private void initialize_hardware_interrupters(HardwareInterrupter[] interrupter_devices)
        {
            //the environment cannot support more interrupters than there are channels
            //TODO change to get the number of supported channels from the processor
            Debug.Assert(interrupter_devices.Length < 32);

            //give each interrupter a callback to use on their own thread
            //which raises a signal on their assigned channel
            for (uint channel = 0; channel < interrupter_devices.Length; channel++)
            {
                //create a method that will raise a signal on this device's assigned channel

                //this method is defined on a hidden functor so that it can be handed off to
                //the interrupter device as a void function; this prevents the interrupter
                //class from having to keep track of what channel to call signals on and
                //prevents interrupters from raising signals on the wrong channel

                signal_functor custom_signal_callback_functor = new signal_functor(this, channel);
                Action custom_signal_callback = new Action(custom_signal_callback_functor.signal);
                interrupter_devices[channel].register_signal_callback(custom_signal_callback);
            }
        }

        /// <summary>
        /// Send all signals built up in the queue to the processor,
        /// removing them from the queue as they are processed
        /// </summary>
        private void resolve_signal_queue()
        {
            //the signal queue is accessible by interrupters' threads
            //so it is protected with a lock
            //hold the lock while sending interrupts to the processor
            //this means that interrupters could be blocked at some point
            //between processor cycles
            lock (signal_queue_mutex)
            {
                while (signal_queue.Count > 0)
                {
                    uint channel = signal_queue.Dequeue();
                    this.processor_signal_callback(channel);
                }
            }

            return;
        }

        /// <summary>
        /// The target method of the customized functors' signal() methods.
        /// This method is responsible for processing a request from a call
        /// to signal() (possibly on a non-main thread) by adding it to the
        /// queue of signals the environment will raise before the next cycle
        /// </summary>
        /// <param name="channel">The channel to raise the signal on</param>
        private void queue_signal(uint channel)
        {
            //add a signal to the signal queue
            //grab the lock, first, because this method is called directly
            //by interrupters, possible on their own threads
            lock (this.signal_queue_mutex)
            {
                this.signal_queue.Enqueue(channel);
            }

            return;
        }

        private struct signal_functor
        {
            private uint channel;
            private SimulationEnvironment environment;

            public void signal()
            {
                this.environment.queue_signal(this.channel);
            }

            public signal_functor(SimulationEnvironment environment, uint channel)
            {
                this.environment = environment;
                this.channel = channel;
            }
        }
    }
}