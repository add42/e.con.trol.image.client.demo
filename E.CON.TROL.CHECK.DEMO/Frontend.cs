using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace E.CON.TROL.CHECK.DEMO
{
    public partial class Frontend : Form
    {
        Backend Backend { get; }

        string CurrentImageFile { get; set; }

        internal Frontend(Backend backend)
        {
            Backend = backend;

            InitializeComponent();

            Backend.LogEventOccured += Backend_LogEventOccured;
        }

        private void Backend_LogEventOccured(object sender, string e)
        {
            if (InvokeRequired)
            {
                this.Invoke(new EventHandler<string>(this.Backend_LogEventOccured), sender, e);
            }
            else
            {
                while (listBox1.Items.Count > 100)
                {
                    listBox1.Items.RemoveAt(0);
                }

                listBox1.Items.Add(e);
                int visibleItems = listBox1.ClientSize.Height / listBox1.ItemHeight;
                listBox1.TopIndex = Math.Max(listBox1.Items.Count - visibleItems + 1, 0);
            }
        }

        private void TimerUpdate_Tick(object sender, EventArgs e)
        {
            var currenFile = Backend.ImageFiles.LastOrDefault();
            if (currenFile != CurrentImageFile)
            {
                CurrentImageFile = currenFile;
                if (File.Exists(CurrentImageFile))
                {
                    var bmp = new Bitmap(CurrentImageFile);
                    this.pictureBox1.Image = bmp;
                }
            }
        }
    }
}