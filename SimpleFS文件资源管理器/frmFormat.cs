using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimpleFS文件资源管理器
{
    public partial class frmFormat : Form
    {
        public frmFormat()
        {
            InitializeComponent();
        }
        public static bool format = false;
        public static int blocksize = 1024;
        public static string volume_name = "SimpleFS";
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (comboBox1.SelectedIndex)
            {
                case 0:
                    blocksize = 512;
                    break;
                case 1:
                    blocksize = 1024;
                    break;
                case 2:
                    blocksize = 4096;
                    break;
                case 3:
                    blocksize = 16384;
                    break;
                case 4:
                    blocksize = 32768;
                    break;
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            format = true;
            volume_name = textBox1.Text;
            this.Close();
        }
        private void frmFormat_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = 1;
        }
    }
}
