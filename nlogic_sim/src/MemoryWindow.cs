using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;


namespace nlogic_sim
{
    class MemoryWindow
    {
        private Form form;
        private TextBox memory_contents_textbox;
        private NumericUpDown width_input;
        private CheckBox show_line_numbers;
        private Button refresh_button;
        private byte[] environment_memory;

        /// <summary>
        /// Create a new Windows Forms window for viewing the contents of the
        /// given array. Once instantiated, call get_form to get the Form,
        /// which can be opened with Application.Run
        /// </summary>
        /// <param name="environment_memory">Byte array to read from. This window
        /// will always acquire a lock on the array before reading it. Writers
        /// should also acquire a lock before updating the array.
        /// </param>
        public MemoryWindow(byte[] environment_memory)
        {
            this.environment_memory = environment_memory;
            this.form = initialize_form();

            // load the current state of memory so the user doesn't have to click refresh the first time
            this.refresh_display();
        }

        private Form initialize_form()
        {
            FontFamily font_family = FontFamily.GenericMonospace;
            try
            {
                font_family = new FontFamily("Consolas");
            }
            catch (ArgumentException)
            {/* Preferred font not found; continuing with generic font */}

            Form form = new Form();
            form.Dock = DockStyle.Fill;
            form.Width = 800;
            form.Height = 800;
            form.Text = "Physical Memory";

            this.refresh_button = new Button();
            refresh_button.Click += OnClickRefresh;
            refresh_button.Dock = DockStyle.Top;
            refresh_button.Text = "Refresh";

            this.memory_contents_textbox = new TextBox();
            memory_contents_textbox.Dock = DockStyle.Fill;
            memory_contents_textbox.ScrollBars = ScrollBars.Vertical;
            memory_contents_textbox.BackColor = Color.Black;
            memory_contents_textbox.ForeColor = Color.Lime;
            memory_contents_textbox.Multiline = true;
            memory_contents_textbox.ReadOnly = true;
            memory_contents_textbox.WordWrap = false;
            memory_contents_textbox.Text = "(refresh to view memory contents)";
            memory_contents_textbox.Font = new Font(font_family, form.Font.Size + 2);


            this.width_input = new NumericUpDown();
            width_input.Dock = DockStyle.Top;
            width_input.ReadOnly = true;
            width_input.Maximum = 32;
            width_input.Minimum = 1;
            width_input.Increment = 1;
            width_input.Value = 8;

            Label width_label = new Label();
            width_label.Dock = DockStyle.Top;
            width_label.Text = "Bytes per row";

            this.show_line_numbers = new CheckBox();
            show_line_numbers.Dock = DockStyle.Top;
            show_line_numbers.CheckState = CheckState.Checked;
            show_line_numbers.Text = "Show addresses (line numbers)";

            // add controls in the order they should appear in the form
            form.Controls.Add(memory_contents_textbox);
            form.Controls.Add(width_label);
            form.Controls.Add(width_input);
            form.Controls.Add(show_line_numbers);
            form.Controls.Add(refresh_button);

            return form;
        }

        private void OnClickRefresh(object sender, EventArgs e)
        {
            this.refresh_display();
        }

        private void refresh_display()
        {
            lock (environment_memory)
            {
                this.refresh_button.Enabled = false;
                bool line_numbers = this.show_line_numbers.Checked;
                int row_width = (int)this.width_input.Value;
                int num_rows = environment_memory.Length / row_width;

                this.memory_contents_textbox.Text = "(Loading...)";
                this.form.Refresh();

                StringBuilder new_contents = new StringBuilder();

                for (int i = 0; i < num_rows; i++)
                {
                    if (line_numbers)
                    {
                        new_contents.Append(String.Format("0x{0}\t", (i * row_width).ToString("X8")));
                    }

                    new_contents.Append(Utility.byte_array_string(
                        new ArraySegment<byte>(environment_memory, i * row_width, row_width), "  "
                    ));

                    new_contents.Append(Environment.NewLine);
                }

                this.memory_contents_textbox.Text = new_contents.ToString();
                this.form.Refresh();

                this.refresh_button.Enabled = true;
            }
        }

        /// <summary>
        /// Get the Form created by this MemoryWindow. Can be used with Application.Run
        /// to open the window. Opening a new window should be done in a new thread to
        /// avoid blocking the caller.
        /// </summary>
        /// <returns>The Form for this MemoryWindow; can be used with Application.</returns>
        public Form get_form()
        {
            return this.form;
        }
    }
}
