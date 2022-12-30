using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class server : MonoBehaviour
{
    List<Socket> ClientProxSocketList = new List<Socket>();

    string editString = "编辑发送内容"; //编辑框文字
    //定义服务器的IP和端口，端口与服务器对应
    public string IPAddress_Server = "192.168.31.240";//可以是局域网或互联网ip
    public string Port_Server = "5000";
    public Transform[] Position;
    public bool SendMessage=false;
    private void Start()
    {
        
    }
    private void Update()
    {
        if (SendMessage)
        {
            SendMessage = false;
            SendHandle();
        }
    }
    void SendHandle()
    {
        //tracker
        for(int i = 0; i < Position.Length; i++)
        {
            editString = "";
            string x = Position[i].position.x.ToString().Substring(0, 7);
            string y = Position[i].position.y.ToString().Substring(0, 7);
            string z = Position[i].position.z.ToString().Substring(0, 7);
            editString += "HO" + "TK" + "0x3" +i + "X" + x + "Y" + y + "Z" + z + "LS";
            SendMsg();
        }
    }
    void OnGUI()
    {
        IPAddress_Server = GUI.TextField(new Rect(10, 10, 100, 20), IPAddress_Server);
        Port_Server = GUI.TextField(new Rect(120, 10, 60, 20), Port_Server);

        if (GUI.Button(new Rect(200, 10, 100, 20), "启动服务器"))
            ServerStart();
        editString = GUI.TextField(new Rect(10, 40, 150, 20), editString);
        if (GUI.Button(new Rect(180, 40, 100, 20), "发送字符串"))
            SendMsg();
    }

    private void ServerStart()
    {
        //1 创建Socket对象
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //2 绑定端口ip
        socket.Bind(new IPEndPoint(IPAddress.Parse(IPAddress_Server), int.Parse(Port_Server)));
        //3 开启侦听
        socket.Listen(10);//链接等待队列：同时来了100个链接请求，队列里放10个等待链接客户端，其他返回错误信息
        //4 开始接受客户端的链接
        ThreadPool.QueueUserWorkItem(new WaitCallback(this.AcceptClientConnect), socket);
    }

    public void AcceptClientConnect(object socket)
    {
        var serverSocket = socket as Socket;//强制类型转换  

        this.AppendTextToConsole("服务器端开始接受客户端的链接");

        while (true)//不断的接收
        {
            var proxSocket = serverSocket.Accept();//会阻塞当前线程，因此必须放入异步线程池中
            this.AppendTextToConsole(string.Format("客户端:{0}链接上了", proxSocket.RemoteEndPoint.ToString()));
            ClientProxSocketList.Add(proxSocket);//使方法体外部也可以访问到方法体内部的数据

            //不停接收当前链接的客户端发送来的消息
            //不能因为接收一个客户端消息阻塞整个线程，启用线程池
            ThreadPool.QueueUserWorkItem(new WaitCallback(this.ReceiveData), proxSocket);
        }
    }

    //接收客户端消息
    public void ReceiveData(object socket)
    {
        var proxSocket = socket as Socket;
        byte[] data = new byte[1024 * 1024];
        while (true)
        {
            int len = 0;
            try
            {
                len = proxSocket.Receive(data, 0, data.Length, SocketFlags.None);
            }
            catch (Exception )
            {
                //异常退出,在阻塞线程时与服务器连接中断或断电等等
                AppendTextToConsole(string.Format("接收到客户端:{0}非正常退出", proxSocket.RemoteEndPoint.ToString()));
                ClientProxSocketList.Remove(proxSocket);
                StopConnect(proxSocket);
                return;
            }


            if (len <= 0)
            {
                //客户端正常退出
                AppendTextToConsole(string.Format("接收到客户端:{0}正常退出", proxSocket.RemoteEndPoint.ToString()));
                ClientProxSocketList.Remove(proxSocket);

                StopConnect(proxSocket);
                return;//让方法结束。终结当前接收客户端数据的异步线程
            }

            //把接收到的数据放到文本框上
            string str = Encoding.Default.GetString(data, 0, len);
            AppendTextToConsole(string.Format("接收到客户端:{0}的消息是:{1}", proxSocket.RemoteEndPoint.ToString(), str));
        }
    }
    private void StopConnect(Socket proxSocket)
    {
        try
        {
            if (proxSocket.Connected)
            {
                proxSocket.Shutdown(SocketShutdown.Both);
                proxSocket.Close(100);//100秒后没有正常关闭则强行关闭
            }
        }
        catch (Exception )
        {

        }
    }
    //发送字符串
    private void SendMsg()
    {
        foreach (var proxSocket in ClientProxSocketList)
        {
            if (proxSocket.Connected)
            {
                //原始的字符串转换成的字节数组
                byte[] data = Encoding.Default.GetBytes(editString);
                proxSocket.Send(data, 0, data.Length, SocketFlags.None);
            }
        }
    }

    //在控制台打印要显示的内容
    public void AppendTextToConsole(string txt)
    {
        Debug.Log(string.Format("{0}", txt));
    }

}