using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    /// <summary>
    /// 用户与玩家进行通信所需要的信息
    /// </summary>
    class User
    {
        public TcpClient client { get; private set; }
        public StreamReader sr { get; private set; }
        public StreamWriter sw { get; private set; }
        public string UserName { get; set; }

        public User(TcpClient client)
        {
            this.client = client;
            this.UserName = "";
            NetworkStream networkStream = client.GetStream();
            sr = new StreamReader(networkStream, Encoding.UTF8);
            sw = new StreamWriter(networkStream, Encoding.UTF8);
        }
    }
}
