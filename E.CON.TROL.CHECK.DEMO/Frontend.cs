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

            LogHelper.LogEventOccured += Backend_LogEventOccured;

            this.Text = Backend?.Config?.Name;

            checkBox1.Checked = Backend.Config.ReturnBoxResultIo;
            checkBox1.CheckedChanged += OnCheckBoxBoxResult_CheckedChanged;
        }

        private void OnCheckBoxBoxResult_CheckedChanged(object sender, EventArgs e)
        {
            if(Backend?.Config != null)
            {
                Backend.Config.ReturnBoxResultIo = checkBox1.Checked;
                Backend.Config.SaveConfig();
            }
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
                label1.Text = lastImage.BoxTrackingId.ToString();
            }
        }

        private async void buttonOpenConfigEditor_Click(object sender, EventArgs e)
        {
            buttonOpenConfigEditor.Enabled = false;
            await Backend?.Config?.OpenEditor();
            buttonOpenConfigEditor.Enabled = true;
        }
    }
}