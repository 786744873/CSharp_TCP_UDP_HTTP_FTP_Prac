using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UDPSocket
{
    /*
     * UDP使用无连接的套接字,无连接的套接字不需要在网络设备之间发送连接信息。因此,很,难确定谁是服务器谁是客户端。如果一个设备最初是在等待远程设备的信息,则套接字就必须用, " Bind方法绑定到一个本地"IP地址/端口”上。完成绑定之后,该设备就可以利用套接字接收数,据了。由于发送设备没有建立到接收设备地址的连接,所以收发数据均不需要Connect方法。
     */

    /*
     * 由于不存在固定的连接,所以可以直接使用SendTo方法和ReceiveFrom方法发送和接收数据,在两个主机之间的通信结束之后,可以像TCP中使用的方法一样,对套接字使用Shutdown和Close方法。1再次提醒读者注意,必须使用Bind方法将套接字绑定到一个本地地址和端口之后才能使用 ReceiveFrom方法接收数据,如果只发送而不接收,则不需要使用Bind方法。
     */
    class Program
    {
        private static int myport = 8889;
        static void Main(string[] args)
        {
            //服务器IP地址1
            IPAddress iP = IPAddress.Parse("127.0.0.1");
            //接收端：介绍路准备
            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            EndPoint senderRemote = (EndPoint)sender;
            Socket receiveSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            receiveSocket.Bind(new IPEndPoint(iP, myport));
            byte[] result = new byte[1024];
            //发送方：发送数据
            Socket senderSocket= new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            senderSocket.SendTo(Encoding.UTF8.GetBytes("测试数据"), new IPEndPoint(iP, myport));
            senderSocket.Shutdown(SocketShutdown.Both);
            senderSocket.Close();
            //接收方：接收数据
            //引用类型参数为EndPoint类型，用于存放发送方的IP地址和端点
            int length = receiveSocket.ReceiveFrom(result, ref senderRemote);
            Console.WriteLine("接收到{0}消息{1}",senderRemote.ToString(),Encoding.UTF8.GetString(result,0,length));
            receiveSocket.Shutdown(SocketShutdown.Both);
            receiveSocket.Close();
            Console.ReadLine();
        }
    }
}
