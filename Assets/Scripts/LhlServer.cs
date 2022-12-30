using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class LhlServer : MonoBehaviour
{
    public string connectIP = "192.168.1.115";
    public string connectPort = "5000";
    private void Start()
    {
        Class1 class1 = new Class1(connectIP, connectPort);
    }
    /// <summary>
    /// Summary description for Class1
    /// </summary>
    public class Class1
    {

        string editString; //编辑框文字
                           //定义服务器的IP和端口，端口与服务器对应
        public string IPAddress_Server;
        public string Port_Server;

        public Class1(string IPAddress_Server, string Port_Server)
        {
            this.IPAddress_Server = IPAddress_Server;
            this.Port_Server = Port_Server;
        }

        private void ServerStart()
        {
            //1 创建Socket对象
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Socket socketSER = socket;
            //2 绑定端口ip
            socket.Bind(new IPEndPoint(IPAddress.Parse(IPAddress_Server), int.Parse(Port_Server)));
            Debug.Log("[Server]绑定成功");
            //3 开启侦听
            socket.Listen(10);//链接等待队列：同时来了100个链接请求，队列里放10个等待链接客户端，其他返回错误信息
                              //4 开始接受客户端的链接

            var serverSocket = socket as Socket;//强制类型转换  

            serverSocket.Accept();

        }
        static void Main()
        {
            Class1 ac = new Class1("192.168.1.115", "5000");
            ac.ServerStart();
        }
    }
}