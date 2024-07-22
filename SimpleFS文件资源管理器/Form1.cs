using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

[StructLayout(LayoutKind.Sequential)]
public struct fs_block_description_t
{
    public IntPtr fp;
    public UInt32 blocksize;
    public UInt32 current_block;
    public UIntPtr current_block_data;
    public UIntPtr __write_cache;
    public Int64 __write_cache_block;
};

[StructLayout(LayoutKind.Sequential)]
public struct fs_superblock_t
{
    public UInt32 crc32;        // 当前块的CRC（固定）
    public UInt32 magic;       // 超级块识别头
    public UInt32 version;      // 文件系统版本
    public UInt32 block_total;   // 总块数
    public UInt32 block_size;    // 块大小（字节）
    public UInt32 first_block;  // 根目录块号
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
    public byte[] volume_name; // 卷标，UTF-8编码
                               // 之后全0
};
[StructLayout(LayoutKind.Sequential)]
public struct fs_handle_t
{
    public IntPtr block;
    public IntPtr superblock;
};


[StructLayout(LayoutKind.Sequential)]
public struct fs_general_file_header_t // 文件头
{
    public UInt16 magic;       // 表示该块是目录还是文件
    public UInt16 name_length; // 文件名长度
    public UInt32 file_size;   // 文件大小
    public UInt32 create_time; // 文件创建日期
    public UInt32 modify_time; // 文件修改日期
    // 之后存储文件名（注意检查最长256字节）
    // 之后存储文件内容
};

[StructLayout(LayoutKind.Sequential)]
public struct fs_general_file_handle_t // 文件句柄
{
    public fs_general_file_header_t header; // 文件头
    public UInt32 block_first;            // 文件第一个块号
    public UInt32 block_current;          // 当前读写的物理块号
    public UInt32 block_offset;           // 当前读写的块内偏移
    public UInt32 pos_current;            // 当前读写的位置(相对于文件第一个字节)
    public bool changed;
};
[StructLayout(LayoutKind.Sequential)]
public struct fs_tree_read_result_t
{
    public fs_general_file_handle_t sub_file_handle;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
    public byte[] name;
    public bool is_dir;
};
public enum simplefs_open_mode_t
{
    MODE_READ,
    MODE_WRITE,
    MODE_APPEND,
};
namespace SimpleFS文件资源管理器
{
    public partial class Form1 : Form
    {
        /// Windows API函数引用/////////////////////////////////////////////////////////////////////
        [DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        public static extern void SetWindowTheme(IntPtr hWnd, string appId, string classId);
        /// SimpleFS函数引用////////////////////////////////////////////////////////////////////////
        [DllImport("SimpleFS.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr sfs_diskopen(IntPtr device, ref bool need_format);
        [DllImport("SimpleFS.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void sfs_diskclose(IntPtr device);
        [DllImport("SimpleFS.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool sfs_diskformat(IntPtr device, UInt32 disk_size, UInt32 block_size, IntPtr volume_name);
        [DllImport("SimpleFS.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 sfs_diskfree(IntPtr device);
        /// SimpleFS文件操作////////////////////////////////////////////////////////////////////////
        [DllImport("SimpleFS.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool sfs_fcreate(IntPtr drive, IntPtr path);
        [DllImport("SimpleFS.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr sfs_fopen(IntPtr drive, IntPtr path, simplefs_open_mode_t mode);
        [DllImport("SimpleFS.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void sfs_fclose(IntPtr f);
        [DllImport("SimpleFS.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool sfs_exists(IntPtr drive, IntPtr path);
        [DllImport("SimpleFS.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 sfs_fread(IntPtr buffer, UInt32 size, IntPtr fp);
        [DllImport("SimpleFS.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 sfs_fwrite(IntPtr buffer, UInt32 size, IntPtr fp);
        [DllImport("SimpleFS.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool sfs_fseek(IntPtr fp, Int32 offset, int origin);
        [DllImport("SimpleFS.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool sfs_remove(IntPtr drive, IntPtr path);
        [DllImport("SimpleFS.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool sfs_rewind(IntPtr fp);
        [DllImport("SimpleFS.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool sfs_mkdir(IntPtr drive, IntPtr path);
        [DllImport("SimpleFS.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool sfs_tree_rmdir(IntPtr drive, IntPtr path);
        [DllImport("SimpleFS.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr sfs_dir_open(IntPtr drive, IntPtr path);
        [DllImport("SimpleFS.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void sfs_dir_close(IntPtr fp);
        [DllImport("SimpleFS.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool sfs_tree_readdir(IntPtr fp, ref fs_tree_read_result_t result);
        [DllImport("SimpleFS.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool sfs_tree_rewind(IntPtr fp);


        private ListViewColumnSorter lvwColumnSorter;
        public Form1()
        {
            InitializeComponent();
            lvwColumnSorter = new ListViewColumnSorter();
            this.listView1.ListViewItemSorter = lvwColumnSorter;
        }
        IntPtr disk = IntPtr.Zero;
        string volume_name = "";
        UInt32 current_blocksize = 0;
        UInt32 current_disksize = 0;
        UInt32 current_free_blocks = 0;
        private void createDisk(object sender, EventArgs e)
        {
            frmNewdisk frm = new frmNewdisk();
            frm.ShowDialog();
        }
        private void openDisk(object sender, EventArgs e)
        {
            bool need_format = false;
            string disk_filename;
            openFileDialog1.FileName = "";
            if (openFileDialog1.ShowDialog() != DialogResult.OK || openFileDialog1.FileName == "")
            {
                return;
            }
            disk_filename = openFileDialog1.FileName;
            IntPtr filename = Marshal.StringToHGlobalAnsi(disk_filename);
            // 获取文件大小
            FileInfo finfo = new FileInfo(disk_filename);
            long file_size = finfo.Length;
            if (file_size < 512 || file_size > 4294967295)
            {
                MessageBox.Show("文件大小不合法！");
                return;
            }
            toolStripStatusLabel1.Text = "正在打开磁盘";
            if (disk != IntPtr.Zero)
            {
                sfs_diskclose(disk);
                current_blocksize = 0;
                current_disksize = 0;
                disk = IntPtr.Zero;
            }
            current_disksize = (UInt32)file_size;
            disk = sfs_diskopen(filename, ref need_format);
            Marshal.FreeHGlobal(filename);
            if (disk == IntPtr.Zero)
            {
                MessageBox.Show("磁盘打开失败！");
                toolStripStatusLabel1.Text = "无法打开磁盘";
                return;
            }
            if(need_format)
            {
                formatDisk(sender, e);
            }
            else
            {
                toolStripStatusLabel1.Text = "设备打开成功";
            }
            updateAllDiskInfo();
            treeView1.Nodes.Clear();
            listDir("/");
        }

        private void formatDisk(object sender, EventArgs e)
        {
            if (disk == IntPtr.Zero)
            {
                MessageBox.Show("请先打开磁盘！");
                return;
            }
            toolStripStatusLabel1.Text = "正在格式化";
            Thread.Yield();
            frmFormat frm = new frmFormat();
            frmFormat.format = false;
            frm.ShowDialog();
            if (frmFormat.format)
            {
                IntPtr volume_name = Marshal.StringToHGlobalAnsi(frmFormat.volume_name);
                bool format_success = sfs_diskformat(disk, current_disksize, (uint)frmFormat.blocksize, volume_name);
                Marshal.FreeHGlobal(volume_name);
                if (!format_success)
                {
                    MessageBox.Show("格式化失败！");
                    sfs_diskclose(disk);
                    current_blocksize = 0;
                    current_disksize = 0;
                    disk = IntPtr.Zero;
                    toolStripStatusLabel1.Text = "格式化失败";
                    return;
                }
                else
                {
                    toolStripStatusLabel1.Text = "格式化成功";
                    MessageBox.Show("格式化成功！");
                    current_blocksize = (uint)frmFormat.blocksize;
                    current_free_blocks = sfs_diskfree(disk);
                }
            }
            else
            {
                sfs_diskclose(disk);
                listView1.Items.Clear();
                treeView1.Nodes.Clear();
                current_blocksize = 0;
                current_disksize = 0;
                disk = IntPtr.Zero;
                toolStripStatusLabel1.Text = "取消操作";
                return;
            }
        }
        private IntPtr stringToUTFToDLL(string str)
        {
            byte[] utf_encoded = Encoding.UTF8.GetBytes(str);
            IntPtr path = Marshal.AllocHGlobal(utf_encoded.Length + 1);
            Marshal.Copy(utf_encoded, 0, path, utf_encoded.Length);
            Marshal.WriteByte(path, utf_encoded.Length, 0);
            return path;
        }
        private string UTFToStringFromDLL(byte[] data)
        {
            string result = Encoding.UTF8.GetString(data);
            return result.Substring(0, result.IndexOf('\0'));
        }
        private void updateAllDiskInfo()
        {
            if (disk == IntPtr.Zero)
            {
                return;
            }
            fs_handle_t fs = (fs_handle_t)Marshal.PtrToStructure(disk, typeof(fs_handle_t));
            fs_superblock_t superblock = (fs_superblock_t)Marshal.PtrToStructure(fs.superblock, typeof(fs_superblock_t));
            volume_name = UTFToStringFromDLL(superblock.volume_name);
            current_blocksize = superblock.block_size;
            updateUsage();
        }
        private void updateUsage()
        {
            current_free_blocks = sfs_diskfree(disk);
            float free_space = (float)current_free_blocks * current_blocksize / 1024 / 1024;
            float total_space = (float)current_disksize / 1024 / 1024;
            toolStripLabel1.Text = "磁盘：" + volume_name;
            toolStripLabel2.Text = free_space.ToString("0.00") + "MB 可用，共 " + total_space.ToString("0.00") + " MB";
            toolStripProgressBar1.Maximum = (int)(current_disksize / current_blocksize);
            toolStripProgressBar1.Value = (int)(current_disksize / current_blocksize) - (int)current_free_blocks;
        }
        private bool mkdir(string dir)
        {
            if (disk == IntPtr.Zero)
                return false;
            dir = "/" + dir.TrimStart('/');
            IntPtr path = stringToUTFToDLL(dir);
            bool success = sfs_mkdir(disk, path);
            Marshal.FreeHGlobal(path);
            if (!success)
                return false;
            return true;
        }

        private bool rmdir(string dir)
        {
            if (disk == IntPtr.Zero)
                return false;
            dir = "/" + dir.TrimStart('/');
            IntPtr path = stringToUTFToDLL(dir);
            bool success = sfs_tree_rmdir(disk, path);
            Marshal.FreeHGlobal(path);
            if (!success)
                return false;
            return true;
        }

        private bool remove(string file)
        {
            if (disk == IntPtr.Zero)
                return false;
            file = "/" + file.TrimStart('/');
            IntPtr path = stringToUTFToDLL(file);
            bool success = sfs_remove(disk, path);
            Marshal.FreeHGlobal(path);
            if (!success)
                return false;
            return true;
        }

        private bool createFile(string file)
        {
            if (disk == IntPtr.Zero)
                return false;
            file = "/" + file.TrimStart('/');
            IntPtr path = stringToUTFToDLL(file);
            bool success = sfs_fcreate(disk, path);
            Marshal.FreeHGlobal(path);
            if (!success)
                return false;
            return true;
        }
        private IntPtr openFile(string path, simplefs_open_mode_t mode)
        {
            path = "/" + path.TrimStart('/');
            IntPtr path_ptr = stringToUTFToDLL(path);
            IntPtr file = sfs_fopen(disk, path_ptr, mode);
            Marshal.FreeHGlobal(path_ptr);
            return file;
        }
        private UInt32 writeFile(IntPtr file, byte[] data)
        {
            IntPtr buffer = Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, buffer, data.Length);
            UInt32 written = sfs_fwrite(buffer, (uint)data.Length, file);
            Marshal.FreeHGlobal(buffer);
            return written;
        }
        private byte[] readFile(IntPtr file, UInt32 size)
        {
            IntPtr buffer_ptr = Marshal.AllocHGlobal((int)size);
            UInt32 read = sfs_fread(buffer_ptr, size, file);
            if (read > 0)
            {
                byte[] buffer = new byte[read];
                Marshal.Copy(buffer_ptr, buffer, 0, (int)read);
                return buffer;
            }
            else
            {
                Marshal.FreeHGlobal(buffer_ptr);
                return new byte[0];
            }
        }

        private void closeFile(IntPtr file)
        {
            sfs_fclose(file);
        }

        private TreeNode findNodeByFullPath_Internal(string[] path, TreeNode node)
        {
            if (path.Length == 0)
                return node;
            foreach (TreeNode child in node.Nodes)
            {
                if (child.Text == path[0])
                {
                    string[] new_path = new string[path.Length - 1];
                    Array.Copy(path, 1, new_path, 0, path.Length - 1);
                    return findNodeByFullPath_Internal(new_path, child);
                }
            }
            return null;
        }
        private TreeNode findNodeByFullPath(string path)
        {
            if(treeView1.Nodes.Count == 0)
            {
                return null;
            }
            if(path == "/")
            {
                return treeView1.Nodes[0];
            }
            path = path.Trim('/');
            string[] path_split = path.Split('/');
            return findNodeByFullPath_Internal(path_split, treeView1.Nodes[0]);
        }
        private void listDir(string dir)
        {
            if (disk == IntPtr.Zero)
            {
                MessageBox.Show("请先打开磁盘！");
                return;
            }
            dir = "/" + dir.TrimStart('/');
            IntPtr path = stringToUTFToDLL(dir);
            IntPtr fp = sfs_dir_open(disk, path);
            if (fp == IntPtr.Zero)
            {
                MessageBox.Show("打开目录失败！");
                Marshal.FreeHGlobal(path);
                return;
            }
            string parent = dir;
            parent.TrimStart('/');
            
            TreeNode node_parent = findNodeByFullPath(parent);
            if (node_parent == null)
            {
                TreeNode node = new TreeNode(parent);
                treeView1.Nodes.Add(node);
                node_parent = node;
            }
            treeView1.SelectedNode = node_parent;
            listView1.Items.Clear();
            List<string> files = new List<string>();
            fs_tree_read_result_t result = new fs_tree_read_result_t();
            while (sfs_tree_readdir(fp, ref result))
            {
                string filename = UTFToStringFromDLL(result.name);
                if (filename == ".." || filename == "/") // ".."跳过
                    continue;
                if (result.is_dir)
                {
                    bool skip = false;
                    files.Add(filename);
                    foreach (TreeNode child in node_parent.Nodes)
                    {
                        if (child.Text == filename)
                        {
                            skip = true;
                            break;
                        }
                    }
                    if (skip == false)
                    {
                        TreeNode node;
                        node = new TreeNode(filename);
                        node_parent.Nodes.Add(node);
                    }
                }
                string[] row = { filename, result.sub_file_handle.header.file_size.ToString(), getDate(result.sub_file_handle.header.create_time), getDate(result.sub_file_handle.header.modify_time) };
                listView1.Items.Add(new ListViewItem(row, getFileIcon(filename, result.is_dir)));
            }
            foreach (TreeNode child in node_parent.Nodes)
            {
                bool skip = false;
                if (child == null)
                {
                    continue;
                }
                foreach (string file in files)
                {
                    if (child.Text == file)
                    {
                        skip = true;
                        break;
                    }
                }
                if (skip == false)
                {
                    node_parent.Nodes.Remove(child);
                }
            }
            sfs_dir_close(fp);
            Marshal.FreeHGlobal(path);
            updateUsage();
        }

        private int getFileIcon(string filename, bool dir)
        {
            if(dir)
                return 3;
            return 1;
        }

        private string getDate(uint create_time)
        {
            DateTime dt = new DateTime(1970, 1, 1);
            dt = dt.AddSeconds(create_time).ToLocalTime();
            return dt.ToString();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            listDir(e.Node.FullPath);
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            SetWindowTheme(this.listView1.Handle, "Explorer", null);
            SetWindowTheme(this.treeView1.Handle, "Explorer", null);
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (disk != IntPtr.Zero)
            {
                sfs_diskclose(disk);
                current_blocksize = 0;
                current_disksize = 0;
                disk = IntPtr.Zero;
            }
        }

        private void 大图标ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView1.View = View.LargeIcon;
        }
        private void 小图标ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView1.View = View.SmallIcon;
        }
        private void 列表ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView1.View = View.List;
        }
        private void 详细信息ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView1.View = View.Details;
        }

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            if(listView1.SelectedItems.Count == 0)
            {
                return;
            }
            if (listView1.SelectedItems[0].ImageIndex == 3)         // 是文件夹
            {
                string parent = "/";
                if(treeView1.SelectedNode != null)
                {
                    parent = treeView1.SelectedNode.FullPath + "/";
                    if(parent == "//")
                    {
                        parent = "/";
                    }
                }
                else
                {
                    treeView1.SelectedNode = treeView1.Nodes[0];
                }
                listDir(parent + listView1.SelectedItems[0].Text);
            }
        }

        private void toolStripButton11_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null)
                return;
            if (treeView1.SelectedNode.Parent == null)
                return;
            treeView1.SelectedNode = treeView1.SelectedNode.Parent;
        }

        private void toolStripButton12_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null)
                return;
            listDir(treeView1.SelectedNode.FullPath);
        }

        private void toolStripButton13_Click(object sender, EventArgs e)
        {
            if (disk != IntPtr.Zero)
            {
                sfs_diskclose(disk);
                current_blocksize = 0;
                current_disksize = 0;
                disk = IntPtr.Zero;
                treeView1.Nodes.Clear();
                listView1.Items.Clear();
            }
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
            {
                return;
            }
            string selected_path = treeView1.SelectedNode.FullPath + "/" + listView1.SelectedItems[0].Text;
            selected_path = "/" + selected_path.Trim('/');
            string parent = "/";
            if (treeView1.SelectedNode != null)
            {
                parent = treeView1.SelectedNode.FullPath + "/";
                if (parent == "//")
                {
                    parent = "/";
                }
            }
            else
            {
                treeView1.SelectedNode = treeView1.Nodes[0];
            }
            if (listView1.SelectedItems[0].ImageIndex == 3)         // 是文件夹
            {
                if (MessageBox.Show("确定要永久删除文件夹\n\n" + selected_path + "\n\n吗？", "递归地删除文件夹", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    rmdir(selected_path);
                    listDir(parent);
                }
            }
            else                          // 是文件
            {
                if (MessageBox.Show("确定要永久删除文件\n\n" + selected_path + "\n\n吗？", "删除文件", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    remove(parent + listView1.SelectedItems[0].Text);
                    listDir(parent);
                }
            }
        }
        string clipboard_fullpath = "";
        string clipboard_filename = "";
        private void toolStripButton9_Click(object sender, EventArgs e)
        {
            if (disk == IntPtr.Zero)
                return;
            if (listView1.SelectedItems.Count == 0)
                return;
            if (listView1.SelectedItems[0].ImageIndex == 3)     // 是文件夹
            {
                MessageBox.Show("暂不支持复制文件夹");
                return;
            }
            string target = "/";
            if (treeView1.SelectedNode != null)
            {
                target = treeView1.SelectedNode.FullPath + "/";
                if (target == "//")
                {
                    target = "/";
                }
            }
            clipboard_fullpath = target + listView1.SelectedItems[0].Text;
            clipboard_fullpath = "/" + clipboard_fullpath.Trim('/');
            clipboard_filename = listView1.SelectedItems[0].Text;
        }
        private void copyFile(string src, string dst)
        {
            toolStripStatusLabel1.Text = "正在复制";
            Thread.Yield();
            IntPtr src_path = stringToUTFToDLL(src);
            IntPtr dst_path = stringToUTFToDLL(dst);
            IntPtr src_fp = sfs_fopen(disk, src_path, simplefs_open_mode_t.MODE_READ);
            Marshal.FreeHGlobal(src_path);
            if (src_fp == IntPtr.Zero )
            {
                MessageBox.Show("无法打开源文件！");
                return;
            }
            IntPtr dst_fp = sfs_fopen(disk, dst_path, simplefs_open_mode_t.MODE_WRITE);
            Marshal.FreeHGlobal(dst_path);
            if (dst_fp == IntPtr.Zero)
            {
                MessageBox.Show("无法打开目标文件！");
                sfs_fclose(src_fp);
                return;
            }
            byte[] buffer;
            do
            {
                buffer = readFile(src_fp, 1024);
                if (buffer.Length > 0)
                    writeFile(dst_fp, buffer);
            } while (buffer.Length > 0);
            sfs_fclose(src_fp);
            sfs_fclose(dst_fp);
            toolStripStatusLabel1.Text = "复制完成";
        }
        private void toolStripButton10_Click(object sender, EventArgs e)
        {
            if (disk == IntPtr.Zero)
                return;
            if (treeView1.SelectedNode == null)
                return;
            if(clipboard_fullpath == "" || clipboard_filename == "")
            {
                return;
            }
            frmRename frm = new frmRename();
            frm.newName = clipboard_filename;
            frm.ShowDialog();
            if (frm.isOK)
            {
                string dst = treeView1.SelectedNode.FullPath + "/" + frm.newName;
                dst = "/" + dst.Trim('/');
                copyFile(clipboard_fullpath, dst);
                listDir(treeView1.SelectedNode.FullPath);
            }
        }

        // 导入文件
        private void addFileFromWindows(string filename, string target, string target_name)
        {
            if (disk == IntPtr.Zero)
            {
                return;
            }
            FileInfo finfo = new FileInfo(filename);
            if (finfo.Length > current_free_blocks * current_blocksize)
            {
                MessageBox.Show("磁盘空间不足！");
                return;
            }
            string path_str= target + "/" + target_name;
            path_str = "/" + path_str.Trim('/');
            IntPtr path = stringToUTFToDLL(path_str);
            IntPtr file = sfs_fopen(disk, path, simplefs_open_mode_t.MODE_WRITE);
            if (file == IntPtr.Zero)
            {
                MessageBox.Show("无法打开文件！");
                Marshal.FreeHGlobal(path);
                return;
            }
            FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
            byte[] buffer = new byte[1024];
            int read;
            while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
            {
                IntPtr buffer_ptr = Marshal.AllocHGlobal(read);
                Marshal.Copy(buffer, 0, buffer_ptr, read);
                sfs_fwrite(buffer_ptr, (uint)read, file);
                Marshal.FreeHGlobal(buffer_ptr);
            }
            fs.Close();
            sfs_fclose(file);
            Marshal.FreeHGlobal(path);
        }
        private void extractFileFromFS(string dst_path, string src_path)
        {
            if (disk == IntPtr.Zero)
            {
                return;
            }
            src_path = "/" + src_path.Trim('/');
            IntPtr path = stringToUTFToDLL(src_path);
            IntPtr file = sfs_fopen(disk, path, simplefs_open_mode_t.MODE_READ);
            if (file == IntPtr.Zero)
            {
                MessageBox.Show("无法打开文件！");
                Marshal.FreeHGlobal(path);
                return;
            }
            byte[] buffer;
            FileStream fs = new FileStream(dst_path, FileMode.Create, FileAccess.Write);
            do
            {
                buffer = readFile(file, 1024);
                if(buffer.Length > 0)
                    fs.Write(buffer, 0, buffer.Length);
            } while (buffer.Length > 0);
            fs.Close();
            sfs_fclose(file);
            Marshal.FreeHGlobal(path);
        }
        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            if (disk == IntPtr.Zero)
                return;
            openFileDialog2.FileName = "";
            if (openFileDialog2.ShowDialog() != DialogResult.OK || openFileDialog2.FileName == "")
            {
                return;
            }
            frmRename frm = new frmRename();
            frm.newName= Path.GetFileName(openFileDialog2.FileName);
            frm.ShowDialog();
            if (frm.isOK)
            {
                string target = "/";
                if(treeView1.SelectedNode != null)
                {
                    target = treeView1.SelectedNode.FullPath + "/";
                    if (target == "//")
                    {
                        target = "/";
                    }
                }
                addFileFromWindows(openFileDialog2.FileName, target, frm.newName);
                listDir(target);
            }
        }

        private void toolStripButton8_Click(object sender, EventArgs e)
        {
            if (disk == IntPtr.Zero)
                return;
            frmRename frm = new frmRename();
            frm.newName = "新建文件夹";
            frm.ShowDialog();
            if (frm.isOK)
            {
                string target = "/";
                if (treeView1.SelectedNode != null)
                {
                    target = treeView1.SelectedNode.FullPath + "/";
                    if (target == "//")
                    {
                        target = "/";
                    }
                }
                mkdir(target + frm.newName);
                listDir(target);
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            if (disk == IntPtr.Zero)
                return;
            if (listView1.SelectedItems.Count == 0)
                return;
            if (listView1.SelectedItems[0].ImageIndex == 3)     // 是文件夹
            {
                MessageBox.Show("暂不支持导出文件夹");
                return;
            }
            saveFileDialog1.FileName = listView1.SelectedItems[0].Text;
            if (saveFileDialog1.ShowDialog() != DialogResult.OK || saveFileDialog1.FileName == "")
                return;
            string target = "/";
            if (treeView1.SelectedNode != null)
            {
                target = treeView1.SelectedNode.FullPath + "/";
                if (target == "//")
                {
                    target = "/";
                }
            }
            extractFileFromFS(saveFileDialog1.FileName, target + listView1.SelectedItems[0].Text); 
        }

        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Determine if clicked column is already the column that is being sorted.
            if (e.Column == lvwColumnSorter.SortColumn)
            {
                // Reverse the current sort direction for this column.
                if (lvwColumnSorter.Order == SortOrder.Ascending)
                {
                    lvwColumnSorter.Order = SortOrder.Descending;
                }
                else
                {
                    lvwColumnSorter.Order = SortOrder.Ascending;
                }
            }
            else
            {
                // Set the column number that is to be sorted; default to ascending.
                lvwColumnSorter.SortColumn = e.Column;
                lvwColumnSorter.Order = SortOrder.Ascending;
            }

            // Perform the sort with these new sort options.
            this.listView1.Sort();
        }

        private void 空白文件ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (disk == IntPtr.Zero)
                return;
            if(treeView1.SelectedNode == null)
            {
                return;
            }
            frmRename frm = new frmRename();
            frm.newName = "新文件";
            frm.ShowDialog();
            if (frm.isOK)
            {
                string target = "/";
                if (treeView1.SelectedNode != null)
                {
                    target = treeView1.SelectedNode.FullPath + "/";
                    if (target == "//")
                    {
                        target = "/";
                    }
                }
                createFile(target + frm.newName);
                listDir(target);
            }
        }

        private void 刷新ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string target = "/";
            if (treeView1.SelectedNode != null)
            {
                target = treeView1.SelectedNode.FullPath + "/";
                if (target == "//")
                {
                    target = "/";
                }
            }
            listDir(target);
        }
    }
}
