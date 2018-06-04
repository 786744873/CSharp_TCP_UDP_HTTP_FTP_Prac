using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketServer
{
    class Program
    {
        private static byte[] result = new byte[1024];
        private static int myport = 8889;
        static Socket serverSocket;

        static void Main(string[] args)
        {
            //服务器IP地址
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(ip,myport));
            serverSocket.Listen(10);
            Console.WriteLine($"启动监听{serverSocket.LocalEndPoint.ToString()}成功");
            //通过clientSocket发送数据
            Thread myThread = new Thread(ListenClientConnect);
            myThread.Start();
            Console.ReadLine();
        }

        /// <summary>
        /// 接收连接
        /// </summary>
        private static void ListenClientConnect()
        {
            while (true)
            {
                Socket clientsocket = serverSocket.Accept();
                clientsocket.Send(Encoding.ASCII.GetBytes("Server Say Hello"));
                Thread receivedThread = new Thread(ReceiveMessage);
                receivedThread.Start(clientsocket);
            }
        }

        /// <summary>
        /// 接收消息
        /// </summary>
        /// <param name="clientSocket"></param>
        private static void ReceiveMessage(Object clientSocket)
        {
            Socket myClientSocket = (Socket)clientSocket;
            while (true)
            {
                try
                {
                    //通过clientSocket接收数据
                    int receiveNumber = myClientSocket.Receive(result);
                    Console.WriteLine("接收客户端{0}消息{1}",myClientSocket.RemoteEndPoint.ToString(),Encoding.ASCII.GetString(result,0,receiveNumber));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    myClientSocket.Shutdown(SocketShutdown.Both);
                    myClientSocket.Close();
                    break;
                }
            }
        }


    }
}
