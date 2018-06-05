using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GameServer
{
    class Service
    {
        private ListBox listBox;
        private delegate void AddItemDelegate(string str);
        private AddItemDelegate addItemDelegate;

        public Service(ListBox listBox)
        {
            this.listBox = listBox;
            addItemDelegate = new AddItemDelegate(AddItem);
        }

        /// <summary>
        /// 在ListBox中追加信息
        /// </summary>
        /// <param name="str">要追加的信息</param>
        public void AddItem(string str)
        {
            if (this.listBox.InvokeRequired)
            {
                listBox.Invoke(addItemDelegate, str);
            }
            else
            {
                listBox.Items.Add(str);
                listBox.SelectedIndex = listBox.Items.Count - 1;
                listBox.ClearSelected();
            }
        }

        /// <summary>
        /// 向某个客户发送信息
        /// </summary>
        /// <param name="user">指定客户</param>
        /// <param name="str">信息</param>
        public void SendToOne(User user, string str)
        {
            try
            {
                user.sw.WriteLine(str);
                user.sw.Flush();
                AddItem(string.Format("向[{0}]发送{1}", user.UserName, str));
            }
            catch
            {
                AddItem(string.Format("向[{0}]发送信息失败",user.UserName));
            }
        }

        /// <summary>
        /// 向同一桌的两个客户端发送信息
        /// </summary>
        /// <param name="gameTable">指定游戏桌</param>
        /// <param name="str">信息</param>
        public void SendToBoth(GameTable gameTable,string str)
        {
            for (int i = 0; i < 2; i++)
            {
                if (gameTable.gamePlayer[i].someone==true)
                {
                    SendToOne(gameTable.gamePlayer[i].user, str);
                }
            }
        }

        /// <summary>
        /// 向所有的客户端发送信息
        /// </summary>
        /// <param name="userList">客户列表</param>
        /// <param name="str">信息</param>
        public void SendToAll(List<User> userList, string str)
        {
            for (int i = 0; i < userList.Count; i++)
            {
                SendToOne(userList[i], str);
            }
        }
    }
}
