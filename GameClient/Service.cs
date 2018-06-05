using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GameClient
{
    class Service
    {
        ListBox listBox;
        StreamWriter sw;

        public Service(ListBox listBox,StreamWriter sw)
        {
            this.listBox = listBox;
            this.sw = sw;
        }

        /// <summary>
        /// 向服务器发送数据
        /// </summary>
        /// <param name="str">发送内容</param>
        public void SendToServer(string str)
        {
            try
            {
                sw.WriteLine(str);
                sw.Flush();
            }
            catch 
            {
                AddItemToList("发送信息失败");
            }
        }


        delegate void AddItemToListDelegate(string str);
        /// <summary>
        /// 在listbox中追加信息
        /// </summary>
        /// <param name="str">要追加的信息</param>
        public void AddItemToList(string str)
        {
            if (listBox.InvokeRequired)
            {
                AddItemToListDelegate d = AddItemToList;
                listBox.Invoke(d, str);
            }
            else
            {
                listBox.Items.Add(str);
                listBox.SelectedIndex = listBox.Items.Count - 1;
                listBox.ClearSelected();
            }
        }
    }
}
