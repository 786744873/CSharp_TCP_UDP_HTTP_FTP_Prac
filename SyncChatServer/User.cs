using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SyncChatServer
{
    class User
    {
        public TcpClient client { get; private set; }
        public BinaryReader br { get; private set; }
        public BinaryWriter bw { get; private set; }
        public String userName { get; set; }

        public User(TcpClient client)
        {
            this.client = client;
            NetworkStream networkStream = client.GetStream();
            br = new BinaryReader(networkStream);
            bw = new BinaryWriter(networkStream);
        }

        public void Close()
        {
            //关闭br.bw会关闭基础流,但关闭client却不关闭基础连接。
            br.Close();
            bw.Close();
            client.Close();
        }

    }
}
