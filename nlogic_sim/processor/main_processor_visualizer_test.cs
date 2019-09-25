using System;

namespace nlogic_sim
{
    public class test_processor_visualizer
    {

        public static void run()
        {
            Processor p = new Processor();
            p.initialize_visualizer();
            p._print_current_state();
            return;
        }
    }
}