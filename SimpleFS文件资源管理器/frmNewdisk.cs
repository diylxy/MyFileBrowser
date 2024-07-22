using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimpleFS文件资源管理器
{
    public partial class frmNewdisk : Form
    {
        public frmNewdisk()
        {
            InitializeComponent();
        }

        [DllImport("SimpleFS.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool sfs_disk_create_empty(IntPtr disk, UInt32 disk_size);
        private void button1_Click(object sender, EventArgs e)
        {
            UInt32 DEFAULT_DISK_SIZE;
            try
            {
                DEFAULT_DISK_SIZE = Convert.ToUInt32(textBox1.Text) * 1024 * 1024;
            }
            catch (Exception)
            {
                MessageBox.Show("输入不合法，请检查", "错误", MessageBoxButtons.OK, MessageBoxIcon.Stop, 0);
                return;
            }
            saveFileDialog1.ShowDialog();
            if (saveFileDialog1.FileName == "")
            {
                return;
            }
            string filename_user = saveFileDialog1.FileName;

            IntPtr filename = Marshal.StringToHGlobalAnsi(filename_user);
            bool succ = sfs_disk_create_empty(filename, DEFAULT_DISK_SIZE);
            if (succ == false)
            {
                MessageBox.Show("磁盘创建失败！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Stop, 0);
                Marshal.FreeHGlobal(filename);
                return;
            }
            MessageBox.Show("磁盘创建成功！", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Marshal.FreeHGlobal(filename);
            this.Close();
        }
    }
}
