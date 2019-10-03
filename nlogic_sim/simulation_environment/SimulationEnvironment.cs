using System;

namespace nlogic_sim
{
    public class SimulationEnvironment
    {

        private IntervalTree<uint, MMIO> MMIO_devices_by_address;
        private byte[] memory;

        private Processor processor;

        public SimulationEnvironment(uint memory_size, MMIO[] MMIO_devices)
        {
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
            throw new NotImplementedException();
            return null;
        }

        /// <summary>
        /// Writes the given data to the given address,
        /// which may not necessarily be in memory
        /// </summary>
        /// <param name="address">Starting address to write the data to</param>
        /// <param name="data_array">Array of bytes to write at the address</param>
        public void write_address(uint address, byte[] data_array)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

    }
}