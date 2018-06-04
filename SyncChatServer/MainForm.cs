using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SyncChatServer
{
    #region 需求说明
    /*
     * 利用同步TCP和BinaryReader对象及BinaryWriter对象编写一个简单的网络聊天, "程序。功能要求如下。
     * 
     * (1)任何一个客户,均可以与服务器进行通信。
     * (2)服务器要能显示客户端连接的状态,当客户端连接成功后,要及时告知客户端已经连接,成功的信息,并将当前在线的所有客户告知该客户端。
     * (3)客户和服务器建立连接后,即可以通过服务器和任一个在线的其他客户聊天。
     * (4)不论客户何时退出程序,服务器都要做出正确判断,同时将该客户是否在线的情况告诉,其他所有在线的客户。
     * 
     * 实际上,服务器和客户端只是相对的概念,当我们把服务器程序安装在A机上,把客户端程, ,序安装在B机上,则A机就是服务器, B机就是客户端,反之亦然。在这个例子中,之所以使用BinaryReader对象和BinaryWriter对象而不是StreamReader对象,和StreamWriter对象,是因为发送的信息可能不只一行,而StreamReader对象和StreamWriter对!象对于发送单行信息是比较合适的,而对于可能包含多行的信息,使用BinaryReader对象和!BinaryWriter对象更方便。
     * 
     * 从TCP的特点中,我们知道客户端只能和服务器通信,无法和另一个客户端直接通信。那么如何实现一个客户和另一个客户聊天呢?解决方法其实也很简单,所有客户一律先把聊天信息发送给服务器,并告诉服务器该信息是发送给哪个客户的,服务器收到信息后,再将该信息“转发”给另一个客户即可。
     */
    #endregion

    public partial class MainForm : Form
    {
        /// <summary>
        /// 保存连接上的所有用户
        /// </summary>
        private List<User> userList = new List<User>();

        /// <summary>
        /// 使用的本机IP地址
        /// </summary>
        IPAddress localAddress;

        /// <summary>
        /// 监听端口
        /// </summary>
        private const int port = 51888;

        private TcpListener myListener;

        /// <summary>
        /// 是否正常退出所有接收线程
        /// </summary>
        bool isNormalExist = false;

        public MainForm()
        {
            InitializeComponent();
            listBoxStatus.HorizontalScrollbar = true;
            IPAddress[] addrIP = Dns.GetHostAddresses(Dns.GetHostName());
            localAddress = addrIP[0];
            buttonStop.Enabled = false;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            myListener = new TcpListener(localAddress, port);
            myListener.Start();

            AddItemToListBox(String.Format("开始在{0}：{1}监听客户连接", localAddress, port));

            //创建一个线程监听客户端连接请求
            Thread myThread = new Thread(ListenClientConnect);
            myThread.Start();
            buttonStart.Enabled = false;
            buttonStop.Enabled = true;
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            AddItemToListBox("开始停止服务，并以此使用户退出");
            isNormalExist=true;
            for (int i =userList.Count-11; i >=0; i--)
            {
                RemoveUser(userList[i]);
            }
            //通过停止监听让myListener.AcceptTcpClieny()产生异常退出监听线程
            myListener.Stop();
            buttonStart.Enabled = true;
            buttonStop.Enabled = false;
        }

        /// <summary>
        /// 接收客户端连接
        /// </summary>
        private void ListenClientConnect()
        {
            TcpClient newClient = null;
            while (true)
            {
                try
                {
                    newClient = myListener.AcceptTcpClient();
                }
                catch
                {
                    //当点击 “停止监听” 或者退出此窗体时AcceptTcpClient()会产生异常
                    //因此可以利用此异常退出循环
                    break;
                }

                //每接收一个客户端连接，就创建一个对应的循环线程来接收该客户端发来的信息
                User user = new User(newClient);
                Thread threadReceive = new Thread(ReceiveData);
                threadReceive.Start(user);
                userList.Add(user);
                AddItemToListBox(String.Format("[{0}]进入", newClient.Client.RemoteEndPoint));
                AddItemToListBox(String.Format("当前连接用户数：{0}", userList.Count));

            }
        }

        /// <summary>
        /// 处理接收的客户端数据
        /// </summary>
        /// <param name="userState">客户端信息</param>
        private void ReceiveData(object userState)
        {
            User user = (User)userState;
            TcpClient client = user.client;
            while (isNormalExist==false)
            {
                string receiveString = String.Empty;
                try
                {
                    //从网络流中读出字符串,此方法会自动判断字符串长度前缀
                    receiveString = user.br.ReadString();
                }
                catch (Exception)
                {
                    if (isNormalExist==false)
                    {
                        AddItemToListBox(String.Format("与[{0}]失去联系，已终止接收该用户信息",client.Client.RemoteEndPoint));
                        RemoveUser(user);
                    }
                    break;
                }
                AddItemToListBox(String.Format("来自[{0}]:{1}", user.client.Client.RemoteEndPoint, receiveString));
                string[] splitString = receiveString.Split(',');
                switch (splitString[0])
                {
                    case "Login":
                        user.userName = splitString[1];
                        SendToAllClient(user, receiveString);
                        break;
                    case "Logout":
                        SendToAllClient(user, receiveString);
                        break;
                    case "Talk":
                        string talkString = receiveString.Substring(splitString[0].Length + splitString[1].Length + 2);
                        AddItemToListBox(String.Format("{0}对{1}说：{2}", user.userName, splitString[1], talkString));
                        SendToClient(user, "talk," + user.userName + "," + talkString);
                        foreach (User target in userList)
                        {
                            if (target.userName==splitString[1]&&user.userName!=splitString[1])
                            {
                                SendToClient(target, "talk," + user.userName + "," + talkString);
                                break;
                            }
                        }
                        break;
                    default:
                        AddItemToListBox("什么意思啊：" + receiveString);
                        break;
                }

            }
        }

        /// <summary>
        /// 发消息给所有客户(通知所有用户的信息)
        /// </summary>
        /// <param name="user">指定发给哪个用户</param>
        /// <param name="message">信息内容</param>
        private void SendToAllClient(User user, string message)
        {
            string command = message.Split(',')[0].ToLower();
            if (command == "login")
            {
                for (int i = 0; i < userList.Count; i++)
                {
                    SendToClient(userList[i], message);
                    if (userList[i].userName != user.userName)
                    {
                        SendToClient(user, "login," + userList[i].userName);
                    }
                }
            }
            else if (command == "logout")
            {
                for (int i = 0; i < userList.Count; i++)
                {
                    if (userList[i].userName != user.userName)
                    {
                        SendToClient(userList[i], message);
                    }
                }
            }
        }

        /// <summary>
        /// 发送message给user
        /// </summary>
        /// <param name="user">指定发给哪个用户</param>
        /// <param name="message">信息内容</param>
        private void SendToClient(User user, string message)
        {
            try
            {
                //将字符串写入网络流，此方法会自动附加字符串长度
                user.bw.Write(message);
                user.bw.Flush();
                AddItemToListBox(String.Format("向[{0}]发送：{1}", user.userName, message));
            }
            catch (Exception)
            {
                AddItemToListBox(String.Format("向[{0}]发送信息失败", user.userName));
            }
        }

        /// <summary>
        /// 移除用户
        /// </summary>
        /// <param name="user">指定要删除的用户</param>
        private void RemoveUser(User user)
        {
            userList.Remove(user);
            user.Close();
            AddItemToListBox(String.Format("当前连接数：{0}", userList.Count()));
        }

        private delegate void AddItemToListBoxDelegate(string str);
        /// <summary>
        /// ListBox中追加状态信息
        /// </summary>
        /// <param name="str">要追加的信息</param>
        private void AddItemToListBox(string str)
        {
            if (listBoxStatus.InvokeRequired)
            {
                AddItemToListBoxDelegate d = new AddItemToListBoxDelegate(AddItemToListBox);
                listBoxStatus.Invoke(d, str);
            }
            else
            {
                listBoxStatus.Items.Add(str);
                listBoxStatus.SelectedIndex = listBoxStatus.Items.Count - 1;
                listBoxStatus.ClearSelected();
            }
        }
    }
}
