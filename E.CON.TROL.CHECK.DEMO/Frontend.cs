using System;
using System.Linq;
using System.Windows.Forms;

namespace E.CON.TROL.CHECK.DEMO
{
    public partial class Frontend : Form
    {
        Backend Backend { get; }

        object CurrentImage { get; set; }

        internal Frontend(Backend backend)
        {
            Backend = backend;

            InitializeComponent();

            Backend.LogEventOccured += Backend_LogEventOccured;

            this.Text = Backend?.Config?.Name;
        }

        private void Backend_LogEventOccured(object sender, string e)
        {
            if (InvokeRequired)
            {
                this.Invoke(new EventHandler<string>(this.Backend_LogEventOccured), sender, e);
            }
            else
            {
                listBox1.Items.Add(e);

                if (Control.MouseButtons != MouseButtons.Left)
                {
                    while (listBox1.Items.Count > 100)
                    {
                        listBox1.Items.RemoveAt(0);
                    }

                    int visibleItems = listBox1.ClientSize.Height / listBox1.ItemHeight;
                    listBox1.TopIndex = Math.Max(listBox1.Items.Count - visibleItems + 1, 0);
                }
            }
        }

        private void TimerUpdate_Tick(object sender, EventArgs e)
        {
            var lastImage = Backend.QueueImages.LastOrDefault();
            if(lastImage != CurrentImage)
            {
                CurrentImage = lastImage;
                var bmp = lastImage?.GetBitmap();
                this.pictureBox1.Image = bmp;
            }
        }

        private void buttonOpenConfigEditor_Click(object sender, EventArgs e)
        {
            Backend?.Config?.OpenEditor();
        }
    }
}