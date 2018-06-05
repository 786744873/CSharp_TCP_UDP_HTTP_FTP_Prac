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
            if (int.TryParse(textBoxMaxTables.Text, out maxTables) == false||int.TryParse(textBoxMaxUsers.Text,out maxUsers)==false)
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
            //停止向游戏桌发送棋子
            for (int i = 0; i <maxTables; i++)
            {
                gameTable[i].StopTimer();
            }
            service.AddItem(string.Format("目前连接用户数：{0}", userList.Count()));
            service.AddItem("开始停止服务,并依次使用户退出！");
            for (int i = 0; i < userList.Count; i++)
            {
                //关闭后，客户端接收字符串为null，
                //使接收客户端的线程ReceiveData接收字符串也为null,
                //从而达到结束线程的目的
                userList[i].client.Close();
            }
            //通过停止监听让myClient.AcceptTcpClient()产生异常退出监听线程
            myListener.Stop();
            buttonStart.Enabled = true ;
            buttonStart.Enabled = false;
            textBoxMaxTables.Enabled = true;
            textBoxMaxUsers.Enabled = true;
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
            SetEnable(buttonStart, true);
            SetEnable(buttonStop, false);
        }

        /// <summary>
        /// 接收、处理客户端信息，每客户端1个线程，参数用于区分是哪个客户
        /// </summary>
        /// <param name="obj">客户端</param>
        private void ReceiveData(object obj)
        {
            User user = (User)obj;
            TcpClient client = user.client;
            //是否正常退出接收线程
            bool normalExit = false;
            //用于控制是否退出循环
            bool exitWhile = false;
            while (exitWhile==false)
            {
                string receiveString = null;
                try
                {
                    receiveString = user.sr.ReadLine();
                }
                catch
                {
                    //该客户底层套接字不存在时会出现异常
                    service.AddItem("接收数据失败");
                }
                //TcpClient对象将套接字进行了封装，如果TcpClient对象关闭了,
                //但是底层套接字未关闭，并不产生异常，但是读取的结果为null
                if (receiveString==null)
                {
                    if (normalExit==false)
                    {
                        //如果停止了监听，Connected为false
                        if (client.Connected==true)
                        {
                            service.AddItem(string.Format("与{0}失去联系，已终止接收该用户信息", client.Client.RemoteEndPoint));
                        }
                        //如果该用户正在游戏桌上，则退出游戏桌
                        RemoveClientfromPlayer(user);
                    }
                    //退出循环
                    break;
                }
                service.AddItem(string.Format("来自{0}：{1}", user.UserName, receiveString));
                string[] splitSring = receiveString.Split(',');
                int tableIndex = -1;        //桌号
                int side = -1;              //座位号
                int anotherSide = -1;       //对方座位号
                string sendString = "";
                string command = splitSring[0].ToLower();
                switch (command)
                {
                    case "login":
                        //格式：Login,昵称
                        //该用户刚刚登陆
                        if (userList.Count > maxUsers)
                        {
                            sendString = "Sorry";
                            service.SendToOne(user, sendString);
                            service.AddItem("人数已满，拒绝" + splitSring[1] + "进入游戏室");
                            exitWhile = true;
                        }
                        else
                        {
                            //将用户昵称保存到用户列表中
                            /*
                             * 由于是引用类型，因此直接给user赋值，也是给userList中对应的user赋值
                             * 用户名中包含其IP和端口的目的是为了帮助理解，实际游戏中一般不会显示IP
                             */
                            user.UserName = string.Format("[{0}--{1}]", splitSring[1], client.Client.RemoteEndPoint);
                            //允许该用户进入游戏室，即将各桌是否有人情况发送给该用户
                            sendString = "Tables," + this.GetOnlineString();
                            service.SendToOne(user, sendString);
                        }
                        break;
                    case "logout":
                        //格式：Logot
                        //用户退出游戏室
                        service.AddItem(string.Format("{0}退出游戏室", user.UserName));
                        normalExit = true;
                        exitWhile = true;
                        break;
                    case "sitdown":
                        //格式：SitDown,桌号,座位号
                        //该用户坐到某座位上
                        tableIndex = int.Parse(splitSring[1]);
                        side = int.Parse(splitSring[2]);
                        gameTable[tableIndex].gamePlayer[side].user = user;
                        gameTable[tableIndex].gamePlayer[side].someone = true;
                        service.AddItem(string.Format("{0}在第{1}桌第{2}座入座", user.UserName, tableIndex + 1, side + 1));
                        //得到对家座位号
                        anotherSide = (side + 1) % 2;
                        //判断对方是否有人
                        if (gameTable[tableIndex].gamePlayer[anotherSide].someone == true)
                        {
                            //先告诉该用户对家已经入座
                            //发送格式：SitDown,座位号,用户名
                            sendString = string.Format("SitDown,{0},{1}", anotherSide, gameTable[tableIndex].gamePlayer[anotherSide].user.UserName);
                            service.SendToOne(user, sendString);
                        }
                        //同时告诉两个用户该用户入座（也可能对方无人）
                        //发送格式：SitDown,座位号,用户名
                        sendString = string.Format("SitDown,{0},{1}", side, user.UserName);
                        service.SendToBoth(gameTable[tableIndex], sendString);
                        //重新将游戏客户端各桌情况发送给所有用户
                        service.SendToAll(userList, "Tables," + this.GetOnlineString());
                        break;
                    case "getup":
                        //格式：GetUp,桌号,座位号
                        //用户离开座位，回到游戏室
                        tableIndex = int.Parse(splitSring[1]);
                        side = int.Parse(splitSring[2]);
                        service.AddItem(string.Format("{0}离座，返回游戏室", user.UserName));
                        gameTable[tableIndex].StopTimer();
                        //将离座信息同时发给两个用户，以便客户端做离座处理
                        //发送格式：GetUp,座位号,用户名
                        service.SendToBoth(gameTable[tableIndex], string.Format("GetUp,{0},{1}", side, user.UserName));
                        //服务器进行离座处理
                        gameTable[tableIndex].gamePlayer[side].someone = false;
                        gameTable[tableIndex].gamePlayer[side].started = false;
                        gameTable[tableIndex].gamePlayer[side].grade = 0;
                        anotherSide = (side + 1) % 2;
                        if (gameTable[tableIndex].gamePlayer[anotherSide].someone == true)
                        {
                            gameTable[tableIndex].gamePlayer[anotherSide].started = false;
                            gameTable[tableIndex].gamePlayer[anotherSide].grade = 0;
                        }
                        //重新将游戏室各桌情况发送给所有的用户
                        service.SendToAll(userList, "Tables," + this.GetOnlineString());
                        break;
                    case "level":
                        //格式：Time,桌号,难度级别
                        //设置级别难度
                        tableIndex = int.Parse(splitSring[1]);
                        gameTable[tableIndex].SetTimerLevel((6 - int.Parse(splitSring[2])) * 100);
                        service.SendToBoth(gameTable[tableIndex], receiveString);
                        break;
                    case "talk":
                        //格式：Talk,桌号,对话内容
                        tableIndex = int.Parse(splitSring[1]);
                        //由于说话内容可能包含逗号,所以需要特殊处理
                        sendString = string.Format("Talk,{0},{1}", user.UserName, receiveString.Substring(splitSring[0].Length + splitSring[1].Length));
                        service.SendToBoth(gameTable[tableIndex], sendString);
                        break;
                    case "start":
                        //格式：Start,桌号,座位号
                        //该用户单击了开始按钮
                        tableIndex = int.Parse(splitSring[1]);
                        side = int.Parse(splitSring[2]);
                        gameTable[tableIndex].gamePlayer[side].started = true;
                        if (side==0)
                        {
                            anotherSide = 1;
                            sendString = "Message,黑方已开始";
                        }
                        else
                        {
                            anotherSide = 0;
                            sendString = "Message,白方已开始";
                        }
                        service.SendToBoth(gameTable[tableIndex], sendString);
                        if (gameTable[tableIndex].gamePlayer[anotherSide].started==true)
                        {
                            gameTable[tableIndex].ResetGrid();
                            gameTable[tableIndex].StartTimer();
                        }
                        break;
                    case "unsetdot":
                        //格式：UnsetDot,桌号,座位号,行,列,颜色
                        //消去客户单击的棋子
                        tableIndex = int.Parse(splitSring[1]);
                        side = int.Parse(splitSring[2]);
                        int xj = int.Parse(splitSring[3]);
                        int yj = int.Parse(splitSring[4]);
                        int color = int.Parse(splitSring[5]);
                        gameTable[tableIndex].UnsetDot(xj, yj, color);
                        break;
                    default:
                        break;
                }
            }
            userList.Remove(user);
            client.Close();
            service.AddItem(string.Format("有一个退出，剩余连接用户数：{0}", userList.Count()));
        }

        /// <summary>
        /// 循环监测该用户是都坐在了某个游戏桌上，如果是，将其从游戏桌上移除，并终止该桌游戏
        /// </summary>
        /// <param name="user">客户端</param>
        private void RemoveClientfromPlayer(User user)
        {
            for (int i = 0; i < gameTable.Length; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if (gameTable[i].gamePlayer[j].user!=null)
                    {
                        //判断是否是同一个对象
                        if (gameTable[i].gamePlayer[j].user==user)
                        {
                            StopPlayer(i, j);
                            return;
                        }
                    }
                }
            }
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
            //未单击开始服务就直接退出时,myListener为null
            if (myListener!=null)
            {
                buttonStop_Click(null, null);
            }
        }

        delegate void SetEnableDelegate(Control control, bool enable);
        private void SetEnable(Control control, bool enable)
        {
            try
            {
                if (control.InvokeRequired)
                {
                    SetEnableDelegate setEnableDelegate = SetEnable;
                    control.Invoke(setEnableDelegate, control, enable);
                }
                else
                {
                    control.Enabled = enable;
                }
            }
            catch
            {
                //在监听未停止的时候关闭窗口会引发此异常，但是既然已经准备关闭了，那这个异常就没有必要处理了
            }
        }
    }
}
