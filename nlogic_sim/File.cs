using System;
using System.IO;

namespace nlogic_sim
{
    class File_Input
    {

        public static string get_file_contents(string file_path)
        {
            StreamReader file_input = new StreamReader(file_path);
            string contents = file_input.ReadToEnd();
            file_input.Close();
            return (contents);

        }
    }
}