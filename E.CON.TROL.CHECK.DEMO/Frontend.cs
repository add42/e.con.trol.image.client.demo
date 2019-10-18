using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
