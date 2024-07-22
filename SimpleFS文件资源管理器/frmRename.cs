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
    public partial class frmRename : Form
    {
        public frmRename()
        {
            InitializeComponent();
        }
        public bool isOK = false;
        public string newName = "";
        private void button1_Click(object sender, EventArgs e)
        {
            newName = textBox1.Text;
            isOK = true;
            this.Close();
        }

        private void frmRename_Load(object sender, EventArgs e)
        {
            textBox1.Text = newName;
            isOK = false;
            textBox1.Focus();
            textBox1.SelectAll();
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                newName = textBox1.Text;
                isOK = true;
                this.Close();
            }
        }
    }
}
