using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net.Sockets;
using System.Net;

namespace FTP
{
    public partial class Form2 : Form
    {
        private Socket cmdSocket;//命令socket
        private Socket dataSocket;//数据socket
        private byte[] MsgBuffer = new byte[4096];
        private int MsgLength;
        private bool connected;
        private string[] msg;
        IPAddress IP;

        public Form2()
        {
            InitializeComponent();
        }

        


        private void upload_Click(object sender, EventArgs e)
        {
            if (!connected)
            {
                MessageBox.Show("请先与FTP服务器建立连接！");
                return;
            }
            string localPath = "";
            string path = getSelectedPath(false);
            if (path == "?")
                return;
            OpenFileDialog op = new OpenFileDialog();
            op.Filter = "All Files(*.*)|*.*";
            if (op.ShowDialog() == DialogResult.OK)
            {
                localPath = op.FileName;
                dataConnect();
                Upload(localPath, path);
            }
            else
            {
                localPath = "";
            }
            
        }

        public void Upload(string filename, string path)
        {
            
            setWorkDir(path);

            Send(cmdSocket, "STOR " + filename.Substring(filename.LastIndexOf('\\')+1));
            msg = Receive(cmdSocket);
            if (msg[0] == "226" || msg[0] == "250")
                MessageBox.Show("请求成功，正在上传");
            FileStream fstrm = new FileStream(filename, FileMode.Open);
            byte[] fbytes = new byte[1024];
            int cnt = 0;
            string x;
            while ((cnt = fstrm.Read(fbytes, 0, 1024)) > 0)
            {
                x = Encoding.Default.GetString(fbytes, 0, cnt);
                dataSocket.Send(fbytes, cnt, 0);
                //               Thread.Sleep(1000);
            }
            dataSocket.Close();
            fstrm.Close();
            Receive(cmdSocket);
            PaintTreeView();
            //Receive(dataSocket);
            //Send(dataSocket, "CWD " + path);
           // Receive(dataSocket);

        }

        private void Download(string filename,string serverPath, string local_path)
        {
            Send(cmdSocket, "RETR " +serverPath + "\\" + filename);
            msg = Receive(cmdSocket);
            if (msg[0] == "226" || msg[0] == "250")
                MessageBox.Show("请求成功，正在下载");
            try
            {
                FileStream fs = new FileStream(local_path + "\\" + filename, FileMode.Create);
                
                //StreamWriter sw = new StreamWriter(local_path + "\\" + filename);
                while (true)
                {
                    int cnt = dataSocket.Receive(MsgBuffer, 1024, 0);
                    fs.Write(MsgBuffer, 0, cnt);
                    if (cnt <= 0)
                    {
                        fs.Close();
                        break;
                    }
                }
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message);
            }
            
            dataSocket.Close();
        }


        private void Form2_Load(object sender, EventArgs e)
        {
            //treeView1.LabelEdit = true;//可编辑状态。
        }

        private void setWorkDir(string path)
        {
            Send(cmdSocket, "CWD " + path);
            Receive(cmdSocket);
        }

        private void Send(Socket socket, string sedStr)
        {
            try
            {
                socket.Send(System.Text.Encoding.Default.GetBytes(sedStr + "\r\n"));
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private string[] Receive(Socket socket)
        {
            if(socket==null)
            {
                MessageBox.Show("连接已断开！");
                return null;
            }
            string recStr = "";
            MsgLength = socket.Receive(MsgBuffer, MsgBuffer.Length, SocketFlags.None);
            recStr += Encoding.Default.GetString(MsgBuffer, 0, MsgLength);
            //Console.WriteLine(recStr);  //调试用

            msg = recStr.Split(' ');
            return msg;

        }

        private string[] Receive(Socket socket, int buildtree)//在dataSocket传输服务器文件列表时，不能用空格分隔而应用\r\n分隔，故重写了Receive方法
        {
            if (socket == null)
            {
                MessageBox.Show("连接已断开！");
                return null;
            }
            string recStr = "";
            System.Threading.Thread.Sleep(500);
            MsgLength = socket.Receive(MsgBuffer, MsgBuffer.Length, 0);
            recStr += Encoding.Default.GetString(MsgBuffer, 0, MsgLength);
            //Console.WriteLine(recStr);  //调试用

            msg = recStr.Split(new char[2] { '\r', '\n' },StringSplitOptions.RemoveEmptyEntries);
            if (msg.Length == 0)
                msg = new string[1] { "" };
            return msg;

        }

        private int Connect_Server(string username, string password, IPAddress IP)
        {
            try
            {
                IPEndPoint ipe = new IPEndPoint(IP, 21);//21命令端口
                cmdSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                cmdSocket.Connect(ipe);

                msg = Receive(cmdSocket);
                //以上socket连接

                //以下登陆用户名和密码
                Send(cmdSocket, "USER " + username);

                msg = Receive(cmdSocket);

                Send(cmdSocket, "PASS " + password);

                msg = Receive(cmdSocket);
                //MessageBox.Show(msg[0]);  //调试用
                if (msg[0] == "220" || msg[0] == "230")
                {
                    connected = true;
                    login.Text = "断开";
                    //dataConnect();
                    PaintTreeView();
                    treeView1.LabelEdit = false;
                }
                else
                {
                    MessageBox.Show("登陆失败，请检查用户名密码及网络设置！");
                    connected = false;
                }
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message);
            }
            return 0;
        }

        private void dataConnect()
        {
            //Receive(cmdSocket);
            Send(cmdSocket, "PASV");
            string[] tmp = Receive(cmdSocket);
            string msg = tmp[tmp.Length-1];
            string[] data = msg.Split(',');
            int p1 = Int32.Parse(data[4]);
            int i = data[5].LastIndexOf(')');
            int j = 0;
            string s = "";
            while (j != i)
            {
                s += data[5][j];
                j++;
            }
            int p2 = Int32.Parse(s);
            //取出p1和p2的值

            //Console.Write(msg[4]);  //调试用

            IPEndPoint ipe = new IPEndPoint(IP, p1 * 256 + p2);
            dataSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //dataSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            dataSocket.Connect(ipe);
        }

        private void disconnect()
        {
            if(connected)
            {
                Send(cmdSocket, "QUIT");
                Receive(cmdSocket);
                connected = false;
                login.Text = "连接";
                treeView1.LabelEdit = true;
                treeView1.Nodes.Clear();
                cmdSocket = null;
                dataSocket = null;
               
            }
            string[] msg = Receive(cmdSocket);
        }

        private string getSelectedPath(bool isDownload)
        {
            string path = "";
            TreeNode node = treeView1.SelectedNode;
            if(node == null)
            {
                MessageBox.Show("请先选择一个文件！");
                return "?";
            }
            if(isDownload)
            {
                if (node.Nodes.Count != 0) //只有选中节点是文件节点而非文件夹节点时才能下载
                {
                    MessageBox.Show("当前选中的不是文件节点！");
                    path = "?";//文件夹和文件名都不可能出现?
                    return path;
                }
            }
            else  //选择上传文件的路径
            {
                if (node.Nodes.Count == 0) //只有选中节点是文件夹节点时才能上传
                {
                    MessageBox.Show("当前选中的不是文件夹节点！");
                    path = "?";//文件夹和文件名都不可能出现?
                    return path;
                }
            }
            node = node.Parent;
            while(node!=null)
            {

                if (node.Parent == null)//根节点名不用加入到路径当中
                {
                    path = "\\" + path;
                    break;
                }
                path = node.Text + "\\" + path;
                node = node.Parent;
            }

            return path;
        }


        private void PaintTreeView()
        { 
            try
            {
                treeView1.Nodes.Clear(); //清空TreeView
                treeView1.Nodes.Add("root","Root Directory");
                addNode("",treeView1.Nodes["root"]);
               
            }
            catch { }
        }

        private void addNode(string path,TreeNode node)
        {
            dataConnect();
            Send(cmdSocket, "LIST " + path);
            string[] infos = Receive(dataSocket, 1);
            dataSocket.Close();
            msg = Receive(cmdSocket);
            foreach (string info in infos)


            {
                if(info.LastIndexOf("<DIR>")>0)//含有<DIR>字段，表明是文件夹
                {
                    string name = info.Substring(info.LastIndexOf(" ") + 1);
                    TreeNode newNode = node.Nodes.Add(name);
                    addNode(path + "\\" + name, newNode);
                }
                else
                {
                    string name = info.Substring(info.LastIndexOf(" ") + 1);
                    TreeNode newNode = node.Nodes.Add(name);                                               
                }
                
            }
        }

        private void login_Click(object sender, EventArgs e)
        {
            if (!connected)
            {
                try
                {
                    IP = IPAddress.Parse(serverAddr.Text);
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }
                Connect_Server(username.Text, password.Text,IP);
            }
            else
                disconnect();
            
        }

        //private void Download(string filename, string ftp_path, string local_path)
        private void download_Click(object sender, EventArgs e)
        {
            if(!connected)
            {
                MessageBox.Show("请先与FTP服务器建立连接！");
                return;
            }
            string serverPath = getSelectedPath(true);//根据选择的文件设置工作目录
            if (serverPath == "?")
                return;
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            string filename = treeView1.SelectedNode.Text;
            string foldPath;//本地目录
            //setWorkDir(serverPath);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                foldPath = dialog.SelectedPath;
                dataConnect();
                Download(filename,serverPath, foldPath);
            }

        }
    }


}
