using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GameClient
{
    public partial class FormRoom : Form
    {
        #region 说明
        #region 消息说明
        /*
         * . Sorry:服务器游戏室人员已满。客户端接收到此命令后,由于无法进入游戏室,继续运行程序已经没有意义,因此直接结束接收线程,并退出整个客户端程序。 
         * . Tables:各小房间的游戏桌情况。客户端接收到此信息后,需要计算对应的桌数,并以,CheckBox的形式显示出各桌是否有人的情况,供玩家选择座位。
         * . SitDown:玩家坐到某个小房间的游戏桌上,客户端接收到此信息后,需要判断是自己还,是对家,并在窗体中显示相应信息。
         * . GetUp:玩家离开小房间的游戏桌,回到游戏室。客户端收到此信息后,需要判断是对家,离座了还是自己离座了,如果对家离座,显示一个对话框;如果是自己离座,除了设置对应的标,志外,不做其他任何处理。
         * . Lost:对家与服务器失去联系。由于游戏无法继续进行,直接从游戏桌返回到游戏室即可。
         * . StopService:服务器停止服务。由于服务器已经退出接收线程,继续运行客户端程序已经!没有意义,所以此时不论是否正在进行游戏,都要立即结束。
         * · Talk:谈话内容。由于谈话中可能包括逗号,因此需要单独处理。. Message:服务器向游戏桌玩家发送的信息,比如入座信息等。
         * . Level:难度级别。客户端收到此信息后,需要显示出相应的级别。
         * . SetDot:在棋盘上自动产生棋子的信息,信息中包含了棋子位置以及颜色。客户端收到此,信息后,需要在棋盘相应位置将对应颜色的棋子显示出来。
         * . UnsetDot:消去棋子的信息。客户端收到此信息后,需要将棋盘上对应位置的棋子去掉,并更新对应的成绩。
         * . Win:有一家已经出现相邻棋子,未出现相邻棋子的为胜方。除了负责接收的线程外,客户端还需要根据服务器发送的命令,及时更新客户端程序的运行界面。
         */
        #endregion

        #endregion

        private int maxPlayingTables;
        private CheckBox[,] checkBoxesGameTables;
        private TcpClient client = null;
        private StreamReader sr;
        private StreamWriter sw;
        private Service service;
        private FormPlaying formPlaying;
        //是否正常退出接收线程
        private bool normalExit = false;
        //命令是否来自服务器
        private bool isReceiveCommand = false;
        //所做的游戏座位号，-1标识未入座,0表示坐到黑方，1表示坐到白方
        private int side = -1;

        public FormRoom()
        {
            InitializeComponent();
        }

        private void FormRoom_Load(object sender, EventArgs e)
        {
            Random r = new Random((int)DateTime.Now.Ticks);
            textBoxName.Text = "Player" + r.Next(1, 100);
            maxPlayingTables = 0;
            textBoxLocal.ReadOnly = true;
            textBoxServer.ReadOnly = true;
        }

        /// <summary>
        /// 【登录】按钮的Click事件
        /// </summary>
        private void buttonConnect_Click(object sender, EventArgs e)
        {
            try
            {
                //仅作本机测试，实际使用时要将Dns.GetHostName()改为服务器域名
                client = new TcpClient(Dns.GetHostName(), 51888);
            }
            catch 
            {
                MessageBox.Show("与服务器连接失败","",MessageBoxButtons.OK,MessageBoxIcon.Information);
                return;
            }
            groupBox1.Visible = true;
            textBoxLocal.Text = client.Client.LocalEndPoint.ToString();
            textBoxServer.Text = client.Client.RemoteEndPoint.ToString();
            buttonConnect.Enabled = false;
            //获取网络流
            NetworkStream network = client.GetStream();
            sr = new StreamReader(network, Encoding.UTF8);
            sw = new StreamWriter(network, Encoding.UTF8);
            service = new Service(listBox1, sw);
            //登录服务器，获取服务器各桌信息
            //格式：Login,昵称
            service.SendToServer("Login," + textBoxName.Text.Trim());
            Thread threadReceive = new Thread(new ThreadStart(ReceiveData));
            threadReceive.Start();
        }

        /// <summary>
        /// 处理接收数据
        /// </summary>
        private void ReceiveData()
        {
            bool exitWhile = false;
            while (exitWhile==false)
            {
                string receiveString = null;
                try
                {
                    receiveString = sr.ReadLine();
                }
                catch 
                {
                    service.AddItemToList("接收数据失败");
                }
                if (string.IsNullOrEmpty(receiveString))
                {
                    if (normalExit==false)
                    {
                        MessageBox.Show("与服务器失去联系，游戏无法继续");
                    }
                    if (side!=-1)
                    {
                        ExitFormPlaying();
                    }
                    side = -1;
                    normalExit = true;
                    //结束线程
                    break;
                }
                service.AddItemToList("收到：" + receiveString);
                string[] splitString = receiveString.Split(',');
                string command = splitString[0].ToLower();
                switch (command)
                {
                    case "sorry":
                        MessageBox.Show("连接成功，但游戏室人数已满，无法进入。");
                        exitWhile = true;
                        break;
                    case "tables":
                        //字符串格式：Tables,各桌是否有人的字符串
                        //其中每位表示一个座位,1表示有人，0表示无人
                        string s = splitString[1];
                        //如果maxPlayingTables为0，说明尚未创建checkBoxGameTables
                        if (maxPlayingTables==0)
                        {
                            //计算所开桌数
                            maxPlayingTables = s.Length / 2;
                            checkBoxesGameTables = new CheckBox[maxPlayingTables, 2];
                            isReceiveCommand = true;
                            //将CheckBox对象添加到数组中，以便管理
                            for (int i = 0; i < maxPlayingTables; i++)
                            {
                                AddCheckeBoxToPanel(s, i);
                            }
                            isReceiveCommand = false;
                        }
                        else
                        {
                            isReceiveCommand = true;
                            for (int i = 0; i < maxPlayingTables; i++)
                            {
                                for (int j = 0; j < 2; j++)
                                {
                                    if (s[2*i+j]=='0')
                                    {
                                        UpdateCheckBox(checkBoxesGameTables[i, j], false);
                                    }
                                    else
                                    {
                                        UpdateCheckBox(checkBoxesGameTables[i, j], true);
                                    }
                                }
                            }
                            isReceiveCommand = false;
                        }
                        break;
                    case "sitdown":
                        //格式：SitDown,座位号,用户名
                        formPlaying.SetTableSideText(splitString[1], splitString[2], string.Format("{0}进入", splitString[2]));
                        break;
                    case "getup":
                        //格式：GetUp,座位号,用户名
                        //自己或者对方离座
                        if (side==int.Parse(splitString[1]))
                        {
                            //自己离座
                            side = -1;
                        }
                        else
                        {
                            //对方离座
                            formPlaying.SetTableSideText(splitString[1], "", string.Format("{0}退出", splitString[2]));
                            formPlaying.Restart("敌人逃跑了，我放胜利");
                        }
                        break;
                    case "lost":
                        //格式：Lost,座位号,用户名
                        //对家与服务器失去联系
                        formPlaying.SetTableSideText(splitString[1], "", string.Format("[{0}]与服务器失去联系", splitString[2]));
                        formPlaying.Restart("对家与服务器失去联系，游戏无法继续");
                        break;
                    case "talk":
                        //格式：Talk,说话者，对话内容
                        if (formPlaying!=null)
                        {
                            //由于说话内容可能包含都好，所以需要特殊处理
                            formPlaying.ShowTalk(splitString[1], receiveString.Substring(splitString[0].Length + splitString[1].Length + splitString[2].Length + 3));
                        }
                        break;
                    case "message":
                        //格式：Message,内容
                        //服务器自动发送的一般信息（比如进入游戏桌入座等）
                        formPlaying.ShowMessage(splitString[1]);
                        break;
                    case "level":
                        //设置难度级别
                        //格式：Time,桌号，难度级别
                        formPlaying.SetLevel(splitString[2]);
                        break;
                    case "setdot":
                        //产生的棋子位置信息
                        //格式：Setdot,行,列,颜色
                        formPlaying.SetDot(int.Parse(splitString[1]), int.Parse(splitString[2]), (DotColor)int.Parse(splitString[3]));
                        break;
                    case "unsetdot":
                        //消去棋子的信息
                        //格式：UnsetDot,行,列,黑方成绩,白方成绩
                        int x = 20 * (int.Parse(splitString[1]) + 1);
                        int y = 20 * (int.Parse(splitString[2]) + 1);
                        formPlaying.UnsetDot(x, y);
                        formPlaying.SetGradeText(splitString[3], splitString[4]);
                        break;
                    case "win":
                        //格式：Win,相邻棋子的颜色，黑色成绩，白方成绩
                        string winner = "";
                        if ((DotColor)int.Parse(splitString[1])==DotColor.Black)
                        {
                            winner = "黑方出现邻带你，白方胜利";
                        }
                        else
                        {
                            winner = "白方出现邻带你，黑方胜利";
                        }
                        formPlaying.ShowMessage(winner);
                        formPlaying.Restart(winner);
                        break;
                }
            }
            //接收线程结束后，游戏进行已经没有意义了，所以直接退出程序
            Application.Exit();
        }

        delegate void ExitFormPlayingDelegate();
        /// <summary>
        /// 退出游戏
        /// </summary>
        private void ExitFormPlaying()
        {
            if (formPlaying.InvokeRequired)
            {
                ExitFormPlayingDelegate d = ExitFormPlaying;
                this.Invoke(d);
            }
            else
            {
                formPlaying.Close();
            }
        }

        delegate void PanelDelegate(string s, int i);
        /// <summary>
        /// 添加一个游戏桌
        /// </summary>
        /// <param name="s">表示游戏桌的字符串</param>
        /// <param name="i">用于确定第几桌</param>
        private void AddCheckeBoxToPanel(string s, int i)
        {
            if (panel1.InvokeRequired)
            {
                PanelDelegate d = AddCheckeBoxToPanel;
                this.Invoke(d, s, i);
            }
            else
            {
                Label label = new Label();
                label.Location = new Point(10, 15 + i * 30);
                label.Text = string.Format("第{0}桌：", i + 1);
                label.Width = 70;
                this.panel1.Controls.Add(label);
                CreateCheckBox(i, 0, s, "黑方");
                CreateCheckBox(i, 1, s, "白方");
            }
        }

        delegate void CheckBoxDelegate(CheckBox checkBox, bool isChecked);
        /// <summary>
        /// 修改选择状态
        /// </summary>
        /// <param name="checkBox">注定选择的复选框</param>
        /// <param name="isChecked">是否被选择</param>
        private void UpdateCheckBox(CheckBox checkBox, bool isChecked)
        {
            if (checkBox.InvokeRequired)
            {
                CheckBoxDelegate d = UpdateCheckBox;
                this.Invoke(d, checkBox, isChecked);
            }
            else
            {
                if (side==-1)
                {
                    checkBox.Enabled = !isChecked;
                }
                else
                {
                    //已经坐到某个游戏桌上，不允许再选其他桌
                    checkBox.Enabled = false;
                }
                //注意：改变Checked属性会触发checked_Changed事件
                checkBox.Checked = isChecked;
            }
        }

        /// <summary>
        /// 添加游戏桌座位选项
        /// </summary>
        /// <param name="i">指定游戏桌序号</param>
        /// <param name="j">指定座位序号</param>
        /// <param name="s">表示游戏桌的字符串</param>
        /// <param name="text">说明信息</param>
        private void CreateCheckBox(int i, int j, string s, string text)
        {
            int x = j == 0 ? 100 : 200;
            checkBoxesGameTables[i, j] = new CheckBox();
            checkBoxesGameTables[i, j].Name = string.Format("check{0:0000}{1:0000}", i, j);
            checkBoxesGameTables[i, j].Width = 60;
            checkBoxesGameTables[i, j].Location = new Point(x, 10 + i * 30);
            checkBoxesGameTables[i, j].Text = text;
            checkBoxesGameTables[i, j].TextAlign = ContentAlignment.MiddleLeft;
            if (s[2*i+j]=='1')
            {
                //1表示有人
                checkBoxesGameTables[i, j].Enabled = false;
                checkBoxesGameTables[i, j].Checked = true;
            }
            else
            {
                //0表示无人
                checkBoxesGameTables[i, j].Enabled = true;
                checkBoxesGameTables[i, j].Checked = false;
            }
            this.panel1.Controls.Add(checkBoxesGameTables[i, j]);
            checkBoxesGameTables[i, j].CheckedChanged += new EventHandler(checkBox_CheckedChanged);
        }

        /// <summary>
        /// 每个CheckBox的Checked属性发生变化都会触发此事件
        /// </summary>
        private void checkBox_CheckedChanged(object sender, EventArgs e)
        {
            //是否为服务器更新各桌
            if (isReceiveCommand==true)
            {
                return;
            }
            CheckBox checkBox = (CheckBox)sender;
            //Checked为true表示玩家坐到第i桌第j位上
            if (checkBox.Checked==true)
            {
                int i = int.Parse(checkBox.Name.Substring(5, 4));
                int j = int.Parse(checkBox.Name.Substring(9, 4));
                side = j;
                //字符串格式：SitDown,昵称,桌号,座位号
                //只有坐下后，服务器才保存该玩家的昵称
                service.SendToServer(string.Format("SitDown,{0},{1}", i, j));
                formPlaying = new FormPlaying(i, j, sw);
                formPlaying.Show();
            }
        }

        /// <summary>
        /// 关闭窗口时触发的事件
        /// </summary>
        private void FormRoom_FormClosing(object sender, FormClosingEventArgs e)
        {
            //未与服务器连接前client为null
            if (client!=null)
            {
                //不允许玩家从游戏桌直接退出整个程序
                //只允许从游戏桌返回游戏室，再从游戏室退出
                if (side!=-1)
                {
                    MessageBox.Show("请先从游戏桌站起，返回游戏室，然后再退出");
                    e.Cancel = true;
                }
                else
                {
                    //服务器停止服务时,normalExited为true,其他情况为false
                    if (normalExit==false)
                    {
                        normalExit = true;
                        //通知服务器从游戏室退出
                        service.SendToServer("Logout");
                    }

                    //通过关闭TcpClient对象，使服务器接收字符串为null;
                    //服务器结束接收线程后，本程序的接收线程接收字符串也为null
                    //从而实现退出程序功能
                    client.Close();
                }
            }
        }
    }
}
