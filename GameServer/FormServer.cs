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

namespace GameServer
{
    public partial class FormServer : Form
    {
        #region 说明

        #region 游戏规则
        /*
         * (1)玩家通过Inernet和坐在同一桌的另一个玩家对,,一个玩家选择黑方,另一个玩家选,择自方。
         * (2)游戏开始后,计算机自动在15x 15的棋盘方格内,以固定的时间间隔,不停地在未放,置棋子的位置,随机产生黑色棋子或白色棋子。
         * (3)玩家的目标是快速单击自动出现在棋盘上的自己所选颜色的棋子,让棋子从棋盘上消失,以免自己的棋子出现在相邻的位置。
         * (4)每当棋子从棋盘上消失,具有相应颜色的玩家即得1分。注意,如果玩家单击了对方的棋子,则对方得1分。
         * (5)如果棋盘上出现两个或者两个以上相邻的同色棋子,游戏就结束了,该颜色对应的玩家就是失败者。
         */
        #endregion

        #region 游戏功能
        /*
         * (1)服务器可以同时服务多桌,每桌允许两个玩家通过Internet对弈。
         * (2)允许玩家自由选择坐在哪一桌的哪一方。如果两个玩家坐在同一桌,双方应都能看到对方的状态。两个玩家均单击“开始”按钮后,游戏才开始。
         * (3)某桌游戏开始后,服务器以固定的时间间隔同时在15x 15的棋盘方格内向该桌随机地,发送黑白两种颜色棋子的位置,客户端程序接收到服务器发送的棋子位置和颜色后,在15x15棋盘的相应位置显示棋子。
         * (4)玩家坐到游戏桌座位上后,不论游戏是否开始,该玩家都可以随时调整服务器发送棋子。位置的时间间隔。
         * (5)游戏开始后,客户端程序响应鼠标单击,并根据游戏规则计算玩家的得分。
         * (6)如果两个相同颜色的棋子在水平方向或垂直方向是相邻的,那么就认为这两个棋子是相,邻的,这里不考虑对角线相邻的情况。
         * (7)如果相同颜色的棋子出现在相邻的位置,本局游戏结束。
         * (8)同一桌的两个玩家可以聊天。
         */
        #endregion

        #endregion

        //游戏室允许进入的最多人数
        private int maxUsers;
        //连接的用户
        List<User> userList = new List<User>();
        //游戏开出的桌数
        private int maxTables;
        private GameTable[] gameTable;
        //使用的本机IP地址
        IPAddress localAddress;
        //监听端口
        private int port = 51888;
        private TcpListener myListener;
        private Service service;


        public FormServer()
        {
            InitializeComponent();
            service = new Service(listBox1);
        }


        private void FormServer_Load(object sender, EventArgs e)
        {
            listBox1.HorizontalScrollbar = true;
            IPAddress[] addrIP = Dns.GetHostAddresses(Dns.GetHostName());
            localAddress = addrIP[0];
            buttonStop.Enabled = false;
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            if (int.TryParse(textBoxMaxTables.Text, out maxTables) == false||int.TryParse(textBoxMaxUsers.Text,out maxUsers))
            {
                MessageBox.Show("请输入在规定范围内的正整数");
                return;
            }
            if (maxUsers<1||maxUsers>300)
            {
                MessageBox.Show("允许进入的人数只能在1~300之间");
                return;
            }
            if (maxTables<1||maxTables>100)
            {
                MessageBox.Show("允许的桌数只能在1~100之间");
                return;
            }
            textBoxMaxUsers.Enabled = false;
            textBoxMaxUsers.Enabled = false;
            //初始化数组
            gameTable = new GameTable[maxTables];
            for (int i = 0; i < maxTables; i++)
            {
                gameTable[i] = new GameTable(listBox1);
            }
            myListener = new TcpListener(localAddress, port);
            myListener.Start();
            service.AddItem(string.Format("开始在{0}：{1}监听客户端连接", localAddress, port));
            //创建一个线程监听客户端连接请求
            ThreadStart ts = new ThreadStart(ListenClientConnect);
            Thread myThread = new Thread(ts);
            myThread.Start();
            buttonStart.Enabled = false;
            buttonStop.Enabled = true;

        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// 接收客户端连接
        /// </summary>
        private void ListenClientConnect()
        {
            while (true)
            {
                TcpClient newClient = null;
                try
                {
                    //等待用户进入
                    newClient = myListener.AcceptTcpClient();
                }
                catch
                {
                    //当单机 “监听停止” 或者退出此窗体时AcceptTcpClient()会产生异常
                    //因此可以利用此异常退出循环
                    break;
                }
                //每接受一个客户端连接，就创建一个对应的线程循环来接受该客户端发来的信息
                ParameterizedThreadStart pts = new ParameterizedThreadStart(ReceiveData);
                Thread threadReceive = new Thread(pts);
                User user = new User(newClient);
                threadReceive.Start(user);
                userList.Add(user);
                service.AddItem(string.Format("{0}进入", newClient.Client.RemoteEndPoint));
                service.AddItem(string.Format("当前连接数：{0}", userList.Count()));
            }
        }

        /// <summary>
        /// 接收、处理客户端信息，每客户端1个线程，参数用于区分是哪个客户
        /// </summary>
        /// <param name="obj">客户端</param>
        private void ReceiveData(object obj)
        {
            User user = (User)obj;
            //TODO
        }

        /// <summary>
        /// 停止第i桌游戏
        /// </summary>
        /// <param name="i">第i桌</param>
        /// <param name="j">座位号</param>
        private void StopPlayer(int i, int j)
        {
            gameTable[i].StopTimer();
            gameTable[i].gamePlayer[j].someone = false;
            gameTable[i].gamePlayer[j].started = false;
            gameTable[i].gamePlayer[j].grade = 0;
            int otherSide = (j + 1) % 2;
            if (gameTable[i].gamePlayer[otherSide].someone==true)
            {
                gameTable[i].gamePlayer[otherSide].started = false;
                gameTable[i].gamePlayer[otherSide].grade = 0;
                if (gameTable[i].gamePlayer[otherSide].user.client.Connected==true)
                {
                    //发送格式：Lost,座位号,用户名
                    service.SendToOne(gameTable[i].gamePlayer[otherSide].user, string.Format("Lost,{0},{1}", j,gameTable[i].gamePlayer[j].user.UserName));
                }
            }
        }

        /// <summary>
        /// 获取每桌是否有人的字符串，每座用1位标识，0表示无人，1表示有人
        /// </summary>
        /// <returns></returns>
        private string GetOnlineString()
        {
            string str = "";
            for (int i = 0; i < gameTable.Length; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    str += gameTable[i].gamePlayer[j].someone == true ? "1" : "0";
                }
            }
            return str;
        }

        private void FormServer_FormClosing(object sender, FormClosingEventArgs e)
        {
            //TODO
        }
    }
}
